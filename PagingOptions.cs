using System.Collections.Generic;

namespace EntityFrameworkTesting
{
    public class PagingOptions<T>
    {
        public int TotalPages { get; set; }

        public int PageSize { get; set; }

        public int? LastId { get; set; }

        public int CurrentPage { get; set; }

        public List<T> PagedResult { get; set; }
    }
}
