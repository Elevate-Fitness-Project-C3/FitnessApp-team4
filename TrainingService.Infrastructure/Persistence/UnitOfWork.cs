using Microsoft.EntityFrameworkCore.Storage;
using TrainingService.Application.Common.Interfaces;

namespace TrainingService.Infrastructure.Persistence;


 public class UnitOfWork(TrainingDbContext context)
     : IUnitOfWork, IAsyncDisposable
 {
     private IDbContextTransaction? _transaction;
     private int _depth;
     private bool _hasFailed;
 
     public async Task<int> SaveChangesAsync(
         CancellationToken cancellationToken = default)
     {
         return await context.SaveChangesAsync(cancellationToken);
     }
 
     public async Task ExecuteAsync(
         Func<CancellationToken, Task> action,
         CancellationToken cancellationToken = default)
     {
         ArgumentNullException.ThrowIfNull(action);
 
         var isOuterTransaction = _depth == 0;
 
         if (isOuterTransaction)
         {
             _transaction = await context.Database
                 .BeginTransactionAsync(cancellationToken);
 
             _hasFailed = false;
         }
 
         _depth++;
 
         try
         {
             cancellationToken.ThrowIfCancellationRequested();
 
             await action(cancellationToken);
 
             if (isOuterTransaction)
             {
                 if (_hasFailed)
                 {
                     throw new InvalidOperationException(
                         "A nested operation has failed. Transaction cannot be committed.");
                 }
 
                 await context.SaveChangesAsync(cancellationToken);
 
                 await _transaction!.CommitAsync(cancellationToken);
             }
         }
         catch
         {
             _hasFailed = true;
 
             if (isOuterTransaction && _transaction is not null)
             {
                 await _transaction.RollbackAsync(
                     CancellationToken.None);
             }
 
             throw;
         }
         finally
         {
             _depth--;
 
             if (isOuterTransaction)
             {
                 if (_transaction is not null)
                 {
                     await _transaction.DisposeAsync();
                     _transaction = null;
                 }
 
                 _hasFailed = false;
             }
         }
     }
 
     public async Task AddSavePointAsync(
         string name,
         CancellationToken cancellationToken = default)
     {
         EnsureTransaction();
 
         await context.SaveChangesAsync(cancellationToken);
 
         await _transaction!.CreateSavepointAsync(
             name, cancellationToken);
     }
 
     public async Task RollbackToSavePointAsync(
         string name,
         CancellationToken cancellationToken = default)
     {
         EnsureTransaction();
 
         await _transaction!.RollbackToSavepointAsync(
             name, cancellationToken);
     }
 
     private void EnsureTransaction()
     {
         if (_transaction is null)
         {
             throw new InvalidOperationException(
                 "No active transaction.");
         }
     }
 
     public async ValueTask DisposeAsync()
     {
         if (_transaction is not null)
         {
             await _transaction.DisposeAsync();
             _transaction = null;
         }
     }
 }