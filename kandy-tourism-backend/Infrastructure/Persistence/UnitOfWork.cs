using KandyTourism.Api.Application.Interfaces;

namespace KandyTourism.Api.Infrastructure.Persistence;

public sealed class UnitOfWork(KandyTourismDbContext db) : IUnitOfWork
{
    public Task SaveChangesAsync(CancellationToken ct) => db.SaveChangesAsync(ct);

    public async Task<T> ExecuteInTransactionAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken ct)
    {
        if (db.Database.CurrentTransaction is not null)
        {
            return await operation(ct);
        }

        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        var result = await operation(ct);
        await transaction.CommitAsync(ct);
        return result;
    }
}
