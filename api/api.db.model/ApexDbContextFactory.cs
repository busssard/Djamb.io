using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Djambi.Api.Db.Model
{
    public class DjambiDbContextFactory : IDesignTimeDbContextFactory<DjambiDbContext>
    {
        public DjambiDbContext CreateDbContext(string[] args)
        {
            var builder = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: true)
                .AddJsonFile("appsettings.Development.json", optional: true)
                .AddEnvironmentVariables("DJAMBI_");

            var config = builder.Build();

            var connStr = config.GetValue<string>("Sql:ConnectionString");

            var optionsBuilder = new DbContextOptionsBuilder<DjambiDbContext>();
            var serverVersion = new MySqlServerVersion(new Version(8, 0, 0));
            optionsBuilder.UseMySql(connStr, serverVersion);

            return new DjambiDbContext(optionsBuilder.Options);
        }
    }
}
