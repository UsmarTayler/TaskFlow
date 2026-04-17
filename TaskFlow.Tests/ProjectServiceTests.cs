using TaskFlow.Data;
using TaskFlow.Models;
using TaskFlow.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace TaskFlow.Tests
{
    /// <summary>
    /// Unit tests for <see cref="ProjectService"/> using EF Core's in-memory provider.
    /// Each test method gets its own named in-memory database so tests are fully isolated —
    /// data created in one test does not bleed into another.
    /// </summary>
    public class ProjectServiceTests
    {
        // ── Helpers ───────────────────────────────────────────────────────────────

        /// <summary>Creates a fresh in-memory AppDbContext for the given test name.</summary>
        private static AppDbContext BuildContext(string dbName)
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(dbName)  // unique name → unique isolated database
                .Options;
            return new AppDbContext(options);
        }

        // Factory methods keep test arrange sections concise and readable
        private static Project  SampleProject(string name = "Test Project") => new()
        {
            Name      = name,
            CreatedAt = DateTime.UtcNow
        };

        private static WorkItem SampleItem(int projectId, string title = "Test Task") => new()
        {
            Title     = title,
            ProjectId = projectId,
            Priority  = Priority.Medium,
            Status    = ItemStatus.Todo,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // ── GetAllProjectsAsync ───────────────────────────────────────────────────

        [Fact]
        public async Task GetAllProjectsAsync_ReturnsAllProjects()
        {
            using var ctx = BuildContext(nameof(GetAllProjectsAsync_ReturnsAllProjects));
            ctx.Projects.AddRange(SampleProject("Alpha"), SampleProject("Beta"));
            await ctx.SaveChangesAsync();

            var result = await new ProjectService(ctx).GetAllProjectsAsync();

            Assert.Equal(2, result.Count);
        }

        [Fact]
        public async Task GetAllProjectsAsync_ReturnsEmpty_WhenNoProjects()
        {
            using var ctx = BuildContext(nameof(GetAllProjectsAsync_ReturnsEmpty_WhenNoProjects));

            var result = await new ProjectService(ctx).GetAllProjectsAsync();

            Assert.Empty(result);
        }

        // ── GetProjectByIdAsync ───────────────────────────────────────────────────

        [Fact]
        public async Task GetProjectByIdAsync_ReturnsProject_WhenIdExists()
        {
            using var ctx = BuildContext(nameof(GetProjectByIdAsync_ReturnsProject_WhenIdExists));
            var project = SampleProject("Portal");
            ctx.Projects.Add(project);
            await ctx.SaveChangesAsync();

            var result = await new ProjectService(ctx).GetProjectByIdAsync(project.ProjectId);

            Assert.NotNull(result);
            Assert.Equal("Portal", result!.Name);
        }

        [Fact]
        public async Task GetProjectByIdAsync_ReturnsNull_WhenIdDoesNotExist()
        {
            using var ctx = BuildContext(nameof(GetProjectByIdAsync_ReturnsNull_WhenIdDoesNotExist));

            // 999 is an ID that was never inserted — should return null, not throw
            var result = await new ProjectService(ctx).GetProjectByIdAsync(999);

            Assert.Null(result);
        }

        // ── CreateProjectAsync ────────────────────────────────────────────────────

        [Fact]
        public async Task CreateProjectAsync_PersistsProject_AndSetsCreatedAt()
        {
            using var ctx = BuildContext(nameof(CreateProjectAsync_PersistsProject_AndSetsCreatedAt));
            var svc    = new ProjectService(ctx);
            var before = DateTime.UtcNow.AddSeconds(-1);

            var created = await svc.CreateProjectAsync(SampleProject("New Project"));
            var after   = DateTime.UtcNow.AddSeconds(1);

            // The service should have assigned a real PK and set CreatedAt to "now"
            Assert.True(created.ProjectId > 0);
            Assert.Equal(1, await ctx.Projects.CountAsync());
            Assert.True(created.CreatedAt >= before && created.CreatedAt <= after);
        }

        // ── GetAllWorkItemsAsync ──────────────────────────────────────────────────

        [Fact]
        public async Task GetAllWorkItemsAsync_ReturnsAllItems()
        {
            using var ctx = BuildContext(nameof(GetAllWorkItemsAsync_ReturnsAllItems));
            var project = SampleProject();
            ctx.Projects.Add(project);
            await ctx.SaveChangesAsync();

            ctx.WorkItems.AddRange(
                SampleItem(project.ProjectId, "Task A"),
                SampleItem(project.ProjectId, "Task B"),
                SampleItem(project.ProjectId, "Task C")
            );
            await ctx.SaveChangesAsync();

            var result = await new ProjectService(ctx).GetAllWorkItemsAsync();

            Assert.Equal(3, result.Count);
        }

        // ── GetWorkItemsByProjectAsync ─────────────────────────────────────────────

        [Fact]
        public async Task GetWorkItemsByProjectAsync_ReturnsOnlyItemsForProject()
        {
            using var ctx = BuildContext(nameof(GetWorkItemsByProjectAsync_ReturnsOnlyItemsForProject));
            var p1 = SampleProject("P1");
            var p2 = SampleProject("P2");
            ctx.Projects.AddRange(p1, p2);
            await ctx.SaveChangesAsync();

            ctx.WorkItems.AddRange(
                SampleItem(p1.ProjectId, "P1 Task 1"),
                SampleItem(p1.ProjectId, "P1 Task 2"),
                SampleItem(p2.ProjectId, "P2 Task 1")  // should be excluded
            );
            await ctx.SaveChangesAsync();

            var result = await new ProjectService(ctx).GetWorkItemsByProjectAsync(p1.ProjectId);

            // Should return only the 2 items that belong to p1
            Assert.Equal(2, result.Count);
            Assert.All(result, w => Assert.Equal(p1.ProjectId, w.ProjectId));
        }

        // ── GetWorkItemByIdAsync ──────────────────────────────────────────────────

        [Fact]
        public async Task GetWorkItemByIdAsync_ReturnsItem_WhenIdExists()
        {
            using var ctx = BuildContext(nameof(GetWorkItemByIdAsync_ReturnsItem_WhenIdExists));
            var project = SampleProject();
            ctx.Projects.Add(project);
            await ctx.SaveChangesAsync();

            var item = SampleItem(project.ProjectId, "Fix Bug");
            ctx.WorkItems.Add(item);
            await ctx.SaveChangesAsync();

            var result = await new ProjectService(ctx).GetWorkItemByIdAsync(item.WorkItemId);

            Assert.NotNull(result);
            Assert.Equal("Fix Bug", result!.Title);
        }

        [Fact]
        public async Task GetWorkItemByIdAsync_ReturnsNull_WhenIdDoesNotExist()
        {
            using var ctx = BuildContext(nameof(GetWorkItemByIdAsync_ReturnsNull_WhenIdDoesNotExist));

            var result = await new ProjectService(ctx).GetWorkItemByIdAsync(9999);

            Assert.Null(result);
        }

        // ── CreateWorkItemAsync ───────────────────────────────────────────────────

        [Fact]
        public async Task CreateWorkItemAsync_PersistsItem_WithTimestamps()
        {
            using var ctx = BuildContext(nameof(CreateWorkItemAsync_PersistsItem_WithTimestamps));
            var project = SampleProject();
            ctx.Projects.Add(project);
            await ctx.SaveChangesAsync();

            var before  = DateTime.UtcNow.AddSeconds(-1);
            var created = await new ProjectService(ctx).CreateWorkItemAsync(SampleItem(project.ProjectId));
            var after   = DateTime.UtcNow.AddSeconds(1);

            // The service should set both timestamps and generate a PK
            Assert.True(created.WorkItemId > 0);
            Assert.True(created.CreatedAt >= before && created.CreatedAt <= after);
            Assert.True(created.UpdatedAt >= before && created.UpdatedAt <= after);
        }

        // ── UpdateWorkItemStatusAsync ─────────────────────────────────────────────

        [Fact]
        public async Task UpdateWorkItemStatusAsync_ChangesStatus_WhenItemExists()
        {
            using var ctx = BuildContext(nameof(UpdateWorkItemStatusAsync_ChangesStatus_WhenItemExists));
            var project = SampleProject();
            ctx.Projects.Add(project);
            await ctx.SaveChangesAsync();

            var item = SampleItem(project.ProjectId);
            ctx.WorkItems.Add(item);
            await ctx.SaveChangesAsync();

            var svc    = new ProjectService(ctx);
            var result = await svc.UpdateWorkItemStatusAsync(item.WorkItemId, ItemStatus.Done);

            Assert.True(result);

            // Re-read from the database to confirm the change was persisted
            var updated = await ctx.WorkItems.FindAsync(item.WorkItemId);
            Assert.Equal(ItemStatus.Done, updated!.Status);
        }

        [Fact]
        public async Task UpdateWorkItemStatusAsync_ReturnsFalse_WhenItemNotFound()
        {
            using var ctx = BuildContext(nameof(UpdateWorkItemStatusAsync_ReturnsFalse_WhenItemNotFound));

            // Should return false rather than throwing when the ID doesn't exist
            var result = await new ProjectService(ctx).UpdateWorkItemStatusAsync(9999, ItemStatus.Done);

            Assert.False(result);
        }

        [Fact]
        public async Task UpdateWorkItemStatusAsync_UpdatesTimestamp()
        {
            using var ctx = BuildContext(nameof(UpdateWorkItemStatusAsync_UpdatesTimestamp));
            var project = SampleProject();
            ctx.Projects.Add(project);
            await ctx.SaveChangesAsync();

            // Seed the item with an old UpdatedAt so we can verify it gets refreshed
            var item = SampleItem(project.ProjectId);
            item.UpdatedAt = DateTime.UtcNow.AddDays(-5);
            ctx.WorkItems.Add(item);
            await ctx.SaveChangesAsync();

            var before = DateTime.UtcNow.AddSeconds(-1);
            await new ProjectService(ctx).UpdateWorkItemStatusAsync(item.WorkItemId, ItemStatus.InProgress);
            var after = DateTime.UtcNow.AddSeconds(1);

            var updated = await ctx.WorkItems.FindAsync(item.WorkItemId);
            Assert.True(updated!.UpdatedAt >= before && updated.UpdatedAt <= after);
        }

        // ── WorkItem computed property: IsOverdue ─────────────────────────────────

        /// <summary>
        /// Parameterised test — covers the three key cases for IsOverdue in one method.
        /// </summary>
        [Theory]
        [InlineData(null, false)]  // no due date → never overdue
        [InlineData(-1,   true)]   // due yesterday, not done → overdue
        [InlineData( 5,   false)]  // due in 5 days → not overdue yet
        public void IsOverdue_ReflectsDueDateAndStatus(int? daysOffset, bool expectedOverdue)
        {
            var item = new WorkItem
            {
                Status  = ItemStatus.Todo,
                DueDate = daysOffset.HasValue
                    ? DateTime.UtcNow.AddDays(daysOffset.Value)
                    : null
            };

            Assert.Equal(expectedOverdue, item.IsOverdue);
        }

        [Fact]
        public void IsOverdue_IsFalse_WhenItemIsDone_EvenIfPastDue()
        {
            // Completed tasks should never show as overdue regardless of the due date
            var item = new WorkItem
            {
                Status  = ItemStatus.Done,
                DueDate = DateTime.UtcNow.AddDays(-10)
            };

            Assert.False(item.IsOverdue);
        }

        // ── Project computed properties: ProgressPct ──────────────────────────────

        [Fact]
        public void ProgressPct_IsZero_WhenNoItems()
        {
            // Guard against divide-by-zero when a project has no work items
            var project = new Project();
            Assert.Equal(0, project.ProgressPct);
        }

        /// <summary>
        /// Parameterised test covering typical progress scenarios.
        /// </summary>
        [Theory]
        [InlineData(4, 2, 50)]   // half done
        [InlineData(3, 3, 100)]  // fully complete
        [InlineData(5, 0, 0)]    // nothing done yet
        public void ProgressPct_CalculatesCorrectly(int total, int done, int expectedPct)
        {
            var project = new Project();
            for (int i = 0; i < total; i++)
                project.WorkItems.Add(new WorkItem { Status = i < done ? ItemStatus.Done : ItemStatus.Todo });

            Assert.Equal(expectedPct, project.ProgressPct);
        }
    }
}
