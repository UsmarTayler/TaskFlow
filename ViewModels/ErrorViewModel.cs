namespace TaskFlow.ViewModels
{
    /// <summary>
    /// Carries the data needed to render the error page.
    /// Populated by HomeController.Error() based on the HTTP status code
    /// and any unhandled exception that was caught by the middleware.
    /// </summary>
    public class ErrorViewModel
    {
        // The ASP.NET Core trace identifier — useful for correlating this error in the server logs
        public string RequestId  { get; set; } = string.Empty;

        // HTTP status code (e.g. 404, 403, 500) — also written back to Response.StatusCode
        public int    StatusCode { get; set; } = 500;

        // Short heading shown in large text on the error page (e.g. "Page Not Found")
        public string Title      { get; set; } = "Something Went Wrong";

        // Longer explanation shown below the heading
        public string Message    { get; set; } = string.Empty;

        // Only show the Request ID section when there is one to display
        // (avoids rendering an empty <p> tag)
        public bool   ShowRequestId => !string.IsNullOrEmpty(RequestId);
    }
}
