using PropertyPortal.Application.Common.Models;
using PropertyPortal.Domain.Common;

namespace Application.Common.Interfaces
{
    public interface IBaseRepository<T> where T : BaseEntity
    {
        Task<T> DeleteAsync(Guid id);
        Task<bool> ExistsAsync(Guid id);
        Task<T?> GetByIdAsync(Guid id);
        Task<T> PostAsync(T entity);
        Task<T> PutAsync(Guid id, T entity);
        IQueryable<T> Query();
        Task<PaginatedResult<TDestination>> GetPagedAsync<TDestination>(IQueryable<TDestination> query, int pageNumber, int pageSize, string? sortBy, bool isDescending);
        Task AddRangeAsync(IEnumerable<T> entities);
    }
}