﻿using DrHan.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace DrHan.Infrastructure.Repositories
{
    public interface IGenericRepository<T>
    where T : BaseEntity
    {
        Task<T?> GetEntityByIdAsync(Guid id);
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
        Task<IReadOnlyList<T>> ListAsync(
            Expression<Func<T, bool>>? filter = null,
            Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null
        );
        Task<IReadOnlyList<T>> ListAsync(
           Expression<Func<T, bool>>? filter = null,
           Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null,
           Func<IQueryable<T>, IIncludableQueryable<T, object>>? includeProperties = null
       );
    }
}
