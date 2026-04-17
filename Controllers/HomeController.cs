using TaskFlow.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace TaskFlow.Controllers
{
    /// <summary>
    /// Handles centralised error rendering for the application.
    /// [AllowAnonymous] ensures unauthenticated users still see friendly error pages
    /// rather than being redirected to the login screen when something goes wrong.
    /// </summary>
    [AllowAnonymous]
    public class HomeController : Controller
    {
        // ILogger lets us write structured error details to the application's log sink
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Renders a user-friendly error page for any HTTP error.
        /// Wired up in Program.cs via:
        ///   - UseExceptionHandler("/Home/Error/500") for unhandled exceptions
        ///   - UseStatusCodePagesWithReExecute("/Home/Error/{0}") for 404s and other status codes
        /// The optional <paramref name="statusCode"/> route segment carries the HTTP status number.
        /// </summary>
        [Route("Home/Error/{statusCode?}")]
        public IActionResult Error(int? statusCode)
        {
            // IExceptionHandlerFeature is only populated when we arrive via UseExceptionHandler
            var feature   = HttpContext.Features.Get<IExceptionHandlerFeature>();
            var requestId = HttpContext.TraceIdentifier;  // unique ID for correlating logs

            // Log the full exception details for debugging while showing a clean message to the user
            if (feature?.Error != null)
                _logger.LogError(feature.Error, "Unhandled exception for request {RequestId}", requestId);

            var code = statusCode ?? 500;  // default to 500 if no status code was provided

            // Build a friendly, context-appropriate error message based on the status code
            var vm = new ErrorViewModel
            {
                RequestId  = requestId,
                StatusCode = code,
                Title = code switch
                {
                    404 => "Page Not Found",
                    403 => "Access Denied",
                    _   => "Something Went Wrong"
                },
                Message = code switch
                {
                    404 => "The page you're looking for doesn't exist or has been moved.",
                    403 => "You don't have permission to view this page.",
                    _   => "An unexpected error occurred. Please try again or contact support."
                }
            };

            // Set the actual HTTP response status code so browsers and search engines see it correctly
            Response.StatusCode = code;
            return View(vm);
        }
    }
}
