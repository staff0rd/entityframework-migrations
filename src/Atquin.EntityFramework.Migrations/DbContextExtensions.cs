using Microsoft.Extensions.Configuration;
using System.Data.Entity;

namespace Atquin.EntityFramework.Migrations
{
    public static class DbContextExtensions
    {
        public static void UpdateDatabase(this DbContext context, IConfiguration configuration, string providerName)
        {
            var migrator = new Migrator(configuration, context.GetType().Assembly.FullName);
            migrator.UpdateDatabase(migrator.GetConfiguration(providerName: providerName));
        }
    }
}
