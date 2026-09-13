using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace TixTapGo.Shared.Persistence.Extensions;

public static class DbContextExtensions
{
    public static async Task SaveWithRetryOnConcurrencyAsync(this DbContext dbContext, Action action,
        int maxAttempts = 3,
        CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(maxAttempts, 1);

        int attempts = 0;
        bool saved = false;
        while (!saved && !cancellationToken.IsCancellationRequested)
        {
            action();
            attempts++;
            try
            {
                await dbContext.SaveChangesAsync(cancellationToken);
                saved = true;
            }
            catch (DbUpdateConcurrencyException e)
            {
                if (attempts >= maxAttempts)
                {
                    throw;
                }

                await Task.Delay(50, cancellationToken);

                var entriesToReload = e.Entries.Where(entry =>
                    entry.State is EntityState.Modified or EntityState.Deleted);

                foreach (EntityEntry entityEntry in entriesToReload)
                {
                    await entityEntry.ReloadAsync(cancellationToken);
                }
            }
        }

        if (!saved)
        {
            cancellationToken.ThrowIfCancellationRequested();
        }
    }
}
