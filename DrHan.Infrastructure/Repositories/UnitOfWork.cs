using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using DrHan.Domain.Entities;
using DrHan.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DrHan.Infrastructure.Repositories.HCP.Repository.GenericRepository
{
    public class UnitOfWork : IUnitOfWork, IAsyncDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<UnitOfWork>? _logger;
        private readonly ConcurrentDictionary<Type, object> _repositories; // Changed from string to Type
        private bool _disposed = false;

        public UnitOfWork(ApplicationDbContext context, ILogger<UnitOfWork>? logger = null)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger;
            _repositories = new ConcurrentDictionary<Type, object>(); // Changed to Type key
        }

        /// <summary>
        /// Saves all changes asynchronously with optional cancellation support
        /// </summary>
        public async Task<int> CompleteAsync(CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();

            try
            {
                var result = await _context.SaveChangesAsync(cancellationToken);
                _logger?.LogDebug("Successfully saved {ChangeCount} changes to database", result);
                return result;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error occurred while saving changes to database");
                throw;
            }
        }

        /// <summary>
        /// Saves all changes synchronously (use CompleteAsync when possible)
        /// </summary>
        public int Complete()
        {
            ThrowIfDisposed();

            try
            {
                var result = _context.SaveChanges();
                _logger?.LogDebug("Successfully saved {ChangeCount} changes to database", result);
                return result;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error occurred while saving changes to database");
                throw;
            }
        }

        /// <summary>
        /// Gets or creates a repository for the specified entity type
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IGenericRepository<TEntity> Repository<TEntity>()
            where TEntity : BaseEntity
        {
            ThrowIfDisposed();

            return (IGenericRepository<TEntity>)_repositories.GetOrAdd(
                typeof(TEntity),
                static (entityType, state) =>
                {
                    var (context, logger) = ((ApplicationDbContext, ILogger<UnitOfWork>?))state;

                    logger?.LogDebug("Creating repository for {EntityType}", entityType.Name); 

                    return entityType.Name switch
                    {
                        // Add your most common entities here for direct instantiation
                        // Example patterns:
                        // nameof(User) when entityType == typeof(User) => new GenericRepository<User>(context),
                        // nameof(Product) when entityType == typeof(Product) => new GenericRepository<Product>(context),
                        // nameof(Order) when entityType == typeof(Order) => new GenericRepository<Order>(context),

                        // Fallback to reflection for other types
                        _ => Activator.CreateInstance(
                            typeof(GenericRepository<>).MakeGenericType(entityType),
                            context) ?? throw new InvalidOperationException($"Failed to create repository for {entityType.Name}")
                    };
                },
                (_context, _logger)
            );
        }

        /// <summary>
        /// Begins a database transaction
        /// </summary>
        public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();

            if (_context.Database.CurrentTransaction == null)
            {
                await _context.Database.BeginTransactionAsync(cancellationToken);
                _logger?.LogDebug("Database transaction started");
            }
        }

        /// <summary>
        /// Commits the current transaction
        /// </summary>
        public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();

            if (_context.Database.CurrentTransaction != null)
            {
                await _context.Database.CommitTransactionAsync(cancellationToken);
                _logger?.LogDebug("Database transaction committed");
            }
        }

        /// <summary>
        /// Rolls back the current transaction
        /// </summary>
        public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();

            if (_context.Database.CurrentTransaction != null)
            {
                await _context.Database.RollbackTransactionAsync(cancellationToken);
                _logger?.LogDebug("Database transaction rolled back");
            }
        }

        /// <summary>
        /// Executes a function within a database transaction
        /// </summary>
        public async Task<T> ExecuteInTransactionAsync<T>(
            Func<Task<T>> operation,
            CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();

            if (operation == null)
                throw new ArgumentNullException(nameof(operation));

            await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                var result = await operation();
                await transaction.CommitAsync(cancellationToken);
                _logger?.LogDebug("Transaction executed successfully");
                return result;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                _logger?.LogError(ex, "Transaction rolled back due to error");
                throw;
            }
        }

        /// <summary>
        /// Checks if there are any unsaved changes in the context
        /// </summary>
        public bool HasChanges()
        {
            ThrowIfDisposed();
            return _context.ChangeTracker.HasChanges();
        }

        /// <summary>
        /// Discards all changes made to tracked entities
        /// </summary>
        public void RejectChanges()
        {
            ThrowIfDisposed();

            foreach (var entry in _context.ChangeTracker.Entries())
            {
                switch (entry.State)
                {
                    case EntityState.Modified:
                        entry.Reload(); // Proper way to reload original values
                        break;
                    case EntityState.Deleted:
                        entry.State = EntityState.Unchanged;
                        break;
                    case EntityState.Added:
                        entry.State = EntityState.Detached;
                        break;
                }
            }

            _logger?.LogDebug("All changes rejected and change tracker reset");
        }

        private void ThrowIfDisposed()
        {
            ObjectDisposedException.ThrowIf(_disposed, this); // Modern .NET approach
        }

        #region IDisposable & IAsyncDisposable Implementation

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public async ValueTask DisposeAsync()
        {
            await DisposeAsyncCore();
            Dispose(false);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed && disposing)
            {
                _repositories.Clear();
                _context?.Dispose();
                _disposed = true;
                _logger?.LogDebug("UnitOfWork disposed");
            }
        }

        protected virtual async ValueTask DisposeAsyncCore()
        {
            if (!_disposed)
            {
                _repositories.Clear();

                if (_context != null)
                {
                    await _context.DisposeAsync();
                }

                _disposed = true;
                _logger?.LogDebug("UnitOfWork disposed asynchronously");
            }
        }

        #endregion
    }
}