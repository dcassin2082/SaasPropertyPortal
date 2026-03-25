using System;
using System.Collections.Generic;
using System.Text;

namespace PropertyPortal.Application.Common.Models
{
    public class PaginatedResult<T>
    {
        public List<T> Items { get; set; }
        public int PageNumber { get; set; }
        public int TotalPages { get; set; }
        public int TotalCount { get; set; }
        public bool HasPreviousPage => PageNumber > 1;
        public bool HasNextPage => PageNumber < TotalPages;

        public PaginatedResult(List<T> items, int count, int pageNumber, int pageSize)
        {
            PageNumber = pageNumber < 1 ? 1 : pageNumber;
            TotalPages = (int)Math.Ceiling(count / (double)(pageSize < 1 ? 10 : pageSize));
            TotalCount = count;
            Items = items;
        }
    }

}
