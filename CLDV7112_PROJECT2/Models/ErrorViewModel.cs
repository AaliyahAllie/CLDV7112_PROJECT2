namespace CLDV7112_PROJECT2.Models
{
    /// <summary>
    /// View model used by the ASP.NET Core error view to display request correlation IDs.
    /// </summary>
    public class ErrorViewModel
    {
        // Unique request tracking identifier
        public string? RequestId { get; set; }

        // Determines whether the RequestId should be rendered on screen
        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
    }
}
