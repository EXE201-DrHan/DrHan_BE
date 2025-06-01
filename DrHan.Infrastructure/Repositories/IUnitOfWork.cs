using DrHan.Domain.Entities;

namespace DrHan.Infrastructure.Repositories.HCP.Repository.GenericRepository
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
        Repositories.IGenericRepository<TEntity> Repository<TEntity>() where TEntity : BaseEntity;
        Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
    }
}