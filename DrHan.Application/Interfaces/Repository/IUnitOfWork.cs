using DrHan.Domain.Entities;

namespace DrHan.Application.Interfaces.Repository
{
    public interface IUnitOfWork
    {
        Task BeginTransactionAsync(CancellationToken cancellationToken = default);
        Task CommitTransactionAsync(CancellationToken cancellationToken = default);
        int Complete();
        Task<int> CompleteAsync(CancellationToken cancellationToken = default);
        void Dispose();
        ValueTask DisposeAsync();
        Task<T> ExecuteInTransactionAsync<T>(Func<Task<T>> operation, CancellationToken cancellationToken = default);
        bool HasChanges();
        void RejectChanges();
        void DetachAllEntities();
        void DetachEntity(object entity);
        IGenericRepository<TEntity> Repository<TEntity>() where TEntity : BaseEntity;
        Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
    }
}