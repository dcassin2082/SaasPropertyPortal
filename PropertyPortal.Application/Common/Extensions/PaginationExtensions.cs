using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace PropertyPortal.Application.Common.Extensions
{
    public static class PaginationExtensions
    {
        public static void AddPaginationHeader(this HttpResponse response, int currentPage, int itemsPerPage, int totalItems, int totalPages)
        {
            var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
            var metadata = new { currentPage, itemsPerPage, totalItems, totalPages };

            response.Headers.Append("X-Pagination", JsonSerializer.Serialize(metadata, options));
            // Ensure the frontend (CORS) can actually see this custom header
            response.Headers.Append("Access-Control-Expose-Headers", "X-Pagination");
        }
    }
}
