namespace krt_api.Core.Utils
{
    public class PaginateModel
    {
        public int Page { get; set; }
        public int PageSize { get; set; } = 10;
        public int PageCount { get; set; } = 0;
        public int TotalCount { get; set; }
    }
}
