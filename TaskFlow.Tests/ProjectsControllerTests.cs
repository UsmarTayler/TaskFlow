using TaskFlow.Controllers;
using TaskFlow.Models;
using TaskFlow.Services;
using TaskFlow.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

// Alias the System.Security types to avoid name collision with old Claim/Identity model names
using IdentityClaim  = System.Security.Claims.Claim;
using ClaimsIdentity = System.Security.Claims.ClaimsIdentity;
using ClaimsPrincipal = System.Security.Claims.ClaimsPrincipal;
using ClaimTypes      = System.Security.Claims.ClaimTypes;

namespace TaskFlow.Tests
{
    /// <summary>
    /// Unit tests for <see cref="ProjectsController"/> and <see cref="WorkItemsController"/>
    /// using Moq to mock the service layer — no database required.
    /// Each test constructs a minimal controller context (fake user, TempData) to satisfy
    /// what the action methods depend on.
    /// </summary>
    public class ProjectsControllerTests
    {
        // ── Test helper factories ─────────────────────────────────────────────────

        /// <summary>
        /// Creates a mock UserManager. The constructor requires many dependencies
        /// so we use Mock.Of<T>() for the ones we don't need to configure.
        /// </summary>
        private static Mock<UserManager<ApplicationUser>> BuildUserManagerMock()
        {
            var store = new Mock<IUserStore<ApplicationUser>>();
            return new Mock<UserManager<ApplicationUser>>(
                store.Object,
                Mock.Of<IOptions<IdentityOptions>>(),
                Mock.Of<IPasswordHasher<ApplicationUser>>(),
                Array.Empty<IUserValidator<ApplicationUser>>(),
                Array.Empty<IPasswordValidator<ApplicationUser>>(),
                Mock.Of<ILookupNormalizer>(),
                Mock.Of<IdentityErrorDescriber>(),
                Mock.Of<IServiceProvider>(),
                Mock.Of<ILogger<UserManager<ApplicationUser>>>());
        }

        /// <summary>
        /// Creates a ClaimsPrincipal with a single Role claim — simulates a logged-in user
        /// with the specified role without needing a real Identity cookie.
        /// </summary>
        private static ClaimsPrincipal BuildPrincipal(string role)
        {
            var identity = new ClaimsIdentity(
                new[] { new IdentityClaim(ClaimTypes.Role, role) }, "TestAuth");
            return new ClaimsPrincipal(identity);
        }

        /// <summary>
        /// Wires up a ProjectsController with a fake HttpContext user and TempData provider
        /// so we can call action methods as if an authenticated user made the request.
        /// </summary>
        private static ProjectsController BuildProjectsController(
            Mock<IProjectService> svcMock,
            Mock<UserManager<ApplicationUser>> umMock,
            string role = "Developer",
            Mock<IOrganisationService>? orgsMock = null)
        {
            orgsMock ??= new Mock<IOrganisationService>();
            var ctrl = new ProjectsController(svcMock.Object, orgsMock.Object, umMock.Object);
            ctrl.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = BuildPrincipal(role) }
            };
            // TempData needs a provider; Mock.Of<ITempDataProvider>() gives a no-op one
            ctrl.TempData = new TempDataDictionary(
                ctrl.ControllerContext.HttpContext, Mock.Of<ITempDataProvider>());
            return ctrl;
        }

        /// <summary>Same wiring for WorkItemsController tests.</summary>
        private static WorkItemsController BuildWorkItemsController(
            Mock<IProjectService> svcMock,
            Mock<UserManager<ApplicationUser>> umMock,
            string role = "Developer")
        {
            var ctrl = new WorkItemsController(svcMock.Object, umMock.Object);
            ctrl.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = BuildPrincipal(role) }
            };
            ctrl.TempData = new TempDataDictionary(
                ctrl.ControllerContext.HttpContext, Mock.Of<ITempDataProvider>());
            return ctrl;
        }

        // ── ProjectsController.Index ──────────────────────────────────────────────

        [Fact]
        public async Task Index_ReturnsView_WithAllProjects()
        {
            var projects = new List<Project>
            {
                new() { ProjectId = 1, Name = "Alpha" },
                new() { ProjectId = 2, Name = "Beta"  }
            };

            const string userId = "user-1";
            var svcMock = new Mock<IProjectService>();
            // Index uses GetProjectsForUserAsync for non-Admin roles
            svcMock.Setup(s => s.GetProjectsForUserAsync(userId)).ReturnsAsync(projects);

            var umMock = BuildUserManagerMock();
            umMock.Setup(u => u.GetUserId(It.IsAny<ClaimsPrincipal>())).Returns(userId);

            var ctrl   = BuildProjectsController(svcMock, umMock);
            var result = await ctrl.Index(null, null) as ViewResult;

            Assert.NotNull(result);
            var model = Assert.IsType<List<Project>>(result!.Model);
            Assert.Equal(2, model.Count);
        }

        // ── ProjectsController.Detail ─────────────────────────────────────────────

        [Fact]
        public async Task Detail_ReturnsNotFound_WhenProjectMissing()
        {
            var svcMock = new Mock<IProjectService>();
            svcMock.Setup(s => s.GetProjectByIdAsync(99)).ReturnsAsync((Project?)null);

            var ctrl   = BuildProjectsController(svcMock, BuildUserManagerMock());
            var result = await ctrl.Detail(99);

            // A null project should produce a 404, not a NullReferenceException
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Detail_ReturnsView_WhenProjectExists()
        {
            var project = new Project { ProjectId = 1, Name = "Portal" };
            var svcMock = new Mock<IProjectService>();
            svcMock.Setup(s => s.GetProjectByIdAsync(1)).ReturnsAsync(project);

            var ctrl   = BuildProjectsController(svcMock, BuildUserManagerMock());
            var result = await ctrl.Detail(1) as ViewResult;

            Assert.NotNull(result);
            var model = Assert.IsType<Project>(result!.Model);
            Assert.Equal("Portal", model.Name);
        }

        // ── ProjectsController.Create ─────────────────────────────────────────────

        [Fact]
        public async Task Create_Post_RedirectsToDetail_OnSuccess()
        {
            // Arrange: service returns the newly created project with a generated ID
            var svcMock = new Mock<IProjectService>();
            svcMock.Setup(s => s.CreateProjectAsync(It.IsAny<Project>()))
                   .ReturnsAsync(new Project { ProjectId = 5, Name = "New" });

            var orgsMock = new Mock<IOrganisationService>();
            orgsMock.Setup(o => o.GetOrganisationsForUserAsync(It.IsAny<string>()))
                    .ReturnsAsync(new List<Organisation>());

            var umMock = BuildUserManagerMock();
            umMock.Setup(u => u.GetUserId(It.IsAny<ClaimsPrincipal>())).Returns("user-1");

            var ctrl  = BuildProjectsController(svcMock, umMock, "ProjectManager", orgsMock);
            var model = new CreateProjectViewModel { Name = "New Project" };

            // Act
            var result = await ctrl.Create(model) as RedirectToActionResult;

            // Assert: successful creation should redirect to the detail page
            Assert.NotNull(result);
            Assert.Equal("Detail", result!.ActionName);
        }

        [Fact]
        public async Task Create_Post_ReturnsView_WhenModelInvalid()
        {
            var svcMock  = new Mock<IProjectService>();
            var orgsMock = new Mock<IOrganisationService>();
            orgsMock.Setup(o => o.GetOrganisationsForUserAsync(It.IsAny<string>()))
                    .ReturnsAsync(new List<Organisation>());
            var ctrl = BuildProjectsController(svcMock, BuildUserManagerMock(), "ProjectManager", orgsMock);

            // Simulate a validation failure by adding a model error manually
            ctrl.ModelState.AddModelError("Name", "Required");

            var result = await ctrl.Create(new CreateProjectViewModel()) as ViewResult;

            Assert.NotNull(result);

            // The service should never be called when the model is invalid
            svcMock.Verify(s => s.CreateProjectAsync(It.IsAny<Project>()), Times.Never);
        }

        // ── WorkItemsController.Detail ────────────────────────────────────────────

        [Fact]
        public async Task WorkItem_Detail_ReturnsNotFound_WhenItemMissing()
        {
            var svcMock = new Mock<IProjectService>();
            svcMock.Setup(s => s.GetWorkItemByIdAsync(999)).ReturnsAsync((WorkItem?)null);

            var ctrl   = BuildWorkItemsController(svcMock, BuildUserManagerMock());
            var result = await ctrl.Detail(999);

            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task WorkItem_Detail_ReturnsView_WhenItemExists()
        {
            var item = new WorkItem { WorkItemId = 1, Title = "Fix bug", ProjectId = 1 };

            var svcMock = new Mock<IProjectService>();
            svcMock.Setup(s => s.GetWorkItemByIdAsync(1)).ReturnsAsync(item);

            var umMock = BuildUserManagerMock();
            // Simulate GetUsersInRoleAsync returning an empty developer list
            umMock.Setup(u => u.GetUsersInRoleAsync("Developer"))
                  .ReturnsAsync(new List<ApplicationUser>());

            var ctrl   = BuildWorkItemsController(svcMock, umMock, "ProjectManager");
            var result = await ctrl.Detail(1) as ViewResult;

            Assert.NotNull(result);
            var model = Assert.IsType<WorkItem>(result!.Model);
            Assert.Equal("Fix bug", model.Title);
        }

        // ── WorkItemsController.UpdateStatus ──────────────────────────────────────

        [Fact]
        public async Task UpdateStatus_RedirectsToDetail_WhenSuccessful()
        {
            var svcMock = new Mock<IProjectService>();
            svcMock.Setup(s => s.UpdateWorkItemStatusAsync(3, ItemStatus.Done)).ReturnsAsync(true);

            var ctrl   = BuildWorkItemsController(svcMock, BuildUserManagerMock());
            var result = await ctrl.UpdateStatus(3, ItemStatus.Done, 1) as RedirectToActionResult;

            Assert.NotNull(result);
            Assert.Equal("Detail", result!.ActionName);
        }

        [Fact]
        public async Task UpdateStatus_SetsTempDataError_WhenItemNotFound()
        {
            var svcMock = new Mock<IProjectService>();
            svcMock.Setup(s => s.UpdateWorkItemStatusAsync(999, ItemStatus.Done)).ReturnsAsync(false);

            var ctrl = BuildWorkItemsController(svcMock, BuildUserManagerMock());
            await ctrl.UpdateStatus(999, ItemStatus.Done, 1);

            // When the service returns false the controller should store an error in TempData
            Assert.NotNull(ctrl.TempData["Error"]);
        }

        // ── WorkItemsController.MyTasks ───────────────────────────────────────────

        [Fact]
        public async Task MyTasks_ReturnsView_WithAssignedItems()
        {
            const string userId = "dev-42";
            var items = new List<WorkItem>
            {
                new() { WorkItemId = 1, Title = "Task A", ProjectId = 1 },
                new() { WorkItemId = 2, Title = "Task B", ProjectId = 1 }
            };

            var svcMock = new Mock<IProjectService>();
            // Ensure the service is called with the right user ID
            svcMock.Setup(s => s.GetWorkItemsByAssigneeAsync(userId)).ReturnsAsync(items);

            var umMock = BuildUserManagerMock();
            // Make GetUserId return our test user ID for the fake principal
            umMock.Setup(u => u.GetUserId(It.IsAny<ClaimsPrincipal>())).Returns(userId);

            var ctrl   = BuildWorkItemsController(svcMock, umMock);
            var result = await ctrl.MyTasks() as ViewResult;

            Assert.NotNull(result);
            var model = Assert.IsType<List<WorkItem>>(result!.Model);
            Assert.Equal(2, model.Count);
        }
    }
}
