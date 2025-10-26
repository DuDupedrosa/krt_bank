using System.Net;

namespace krt_api.Core.Utils
{
    public class ResponseModel
    {
        public object? Content { get; set; }
        public string? Message { get; set; }
        public HttpStatusCode StatusCode { get; set; }
        public List<ValidationError>? Errors { get; set; }

    }
    public class ValidationError
    {
        public string Field { get; set; } = string.Empty;
        public string Error { get; set; } = string.Empty;
    }
}
