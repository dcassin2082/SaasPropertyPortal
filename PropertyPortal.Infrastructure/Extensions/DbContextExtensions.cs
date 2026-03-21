using Microsoft.EntityFrameworkCore;

namespace PropertyPortal.Infrastructure.Extensions
{
    public static class DbContextExtensions
    {
        public static async Task<bool> SaveChangesWithConcurrencyAsync(this DbContext context)
        {
            try
            {
                await context.SaveChangesAsync();
                return true;
            }
            catch (DbUpdateConcurrencyException ex)
            {
                // Logic: Loop through the entries that failed
                foreach (var entry in ex.Entries)
                {
                    // Option A: "Database Wins" (Refresh your local object with DB values)
                    // await entry.ReloadAsync(); 

                    // Option B: Throw it back to the UI to show a "Data changed" message
                    throw new Exception("The record you attempted to edit was modified by another user.");
                }
                return false;
            }
        }
    }
}
