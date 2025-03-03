namespace OSI.Core.Models.Responses
{
    public class EsfUploadResponse
    {
        public string Id { get; set; } = string.Empty;

        public bool IsSuccess { get; set; }

        public string ErrorMessage { get; set; } = string.Empty;
    }
}
