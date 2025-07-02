using System.Linq.Expressions;
using DrHan.Application.Commons;
using DrHan.Domain.Entities;
using Microsoft.EntityFrameworkCore.Query;

namespace DrHan.Application.Interfaces.Repository
{
    public interface IGenericRepository<T>
    where T : BaseEntity
    {
        Task<T?> GetEntityByIdAsync(int id);
        Task<IReadOnlyList<T>> ListAllAsync();
        Task<T?> FindAsync(Expression<Func<T, bool>> match);
        Task AddAsync(T entity);
        void Update(T entity);
        T? Delete(T entityToDelete);
        T? Delete(object id);
        Task<int> CountAsync();
        Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate);
        
        // Batch operations
        Task AddRangeAsync(IEnumerable<T> entities);
        void UpdateRange(IEnumerable<T> entities);
        void DeleteRange(IEnumerable<T> entities);
        Task<bool> DeleteRangeAsync(Expression<Func<T, bool>> predicate);
        
        Task<int> SaveChangesAsync();
        //Task<IReadOnlyList<T>> ListAsync(
        //    Expression<Func<T, bool>>? filter = null,
        //    Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null
        //);
        Task<IReadOnlyList<T>> ListAsync(
           Expression<Func<T, bool>>? filter = null,
           Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null,
           Func<IQueryable<T>, IIncludableQueryable<T, object>>? includeProperties = null
       );
        Task<T?> FindAsync(
            Expression<Func<T, bool>> match,
            Func<IQueryable<T>, IIncludableQueryable<T, object>>? includeProperties = null
       );

        Task<IPaginatedList<T>> ListAsyncWithPaginated(
        Expression<Func<T, bool>>? filter = null,
        Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null,
        Func<IQueryable<T>, IIncludableQueryable<T, object>>? includeProperties = null,
        PaginationRequest? pagination = null,
        CancellationToken cancellationToken = default);
    }
}
