using PropertyPortal.Domain.Common;

namespace Application.Common.Interfaces
{
    public interface IBaseRepository<T> where T : BaseEntity
    {
        Task<T> DeleteAsync(Guid id);
        Task<bool> ExistsAsync(Guid id);
        Task<T> GetByIdAsync(Guid id);
        Task<T> PostAsync(T entity);
        Task<T> PutAsync(Guid id, T entity);
        IQueryable<T> Query();
    }
}