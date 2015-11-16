using Microsoft.Dnx.Runtime;
using System;
using Microsoft.Framework.Logging;
using System.Reflection;
using Microsoft.Framework.Runtime.Common.CommandLine;
using Microsoft.Framework.Configuration;
using Microsoft.Framework.Logging.Console;
using System.Data.Entity.Migrations.Design;
using System.IO;
using System.Resources;
using System.Data.Entity.Migrations;

namespace Atquin.EntityFramework.Migrations
{
    public class Program
    {
        private const string _defaultConnectionStringName = "Data:DefaultConnection:ConnectionString";
        private const string _defaultProviderName = "System.Data.SqlClient";
        private readonly IApplicationEnvironment _appEnv;
        private readonly ILogger _logger;

        public Program(IApplicationEnvironment appEnv, ILoggerProvider logProvider)
        {
            _appEnv = appEnv;
            _logger = logProvider?.CreateLogger(this.GetType().ToString()) ?? new ConsoleLogger(this.GetType().ToString(), (catgory,level) => { return true; });

            Configuration =
                new ConfigurationBuilder()
                .SetBasePath(appEnv.ApplicationBasePath)
                .AddJsonFile("config.json", true)
                .Build();
        }

        public IConfiguration Configuration { get; set; }

        public virtual int Main(string[] args)
        {
            var app = new CommandLineApplication
            {
                Name = "ef",
                FullName = "Entity Framework 6 Commands"
            };
            app.VersionOption(
                "--version",
                typeof(Program).GetTypeInfo().Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()
                    .InformationalVersion);
            app.HelpOption("-?|-h|--help");
            app.OnExecute(
                () =>
                {
                    app.ShowHelp();
                    return 0;
                });
            app.Command("add", add =>
            {
                add.Description = "Add a new migration";
                AddHelp(add);
                var name = add.Argument("[name]", "Migration name label");
                var connectionString = add.Option("-c|--connectionstring", "The connection string", CommandOptionType.SingleValue);
                var providerName = add.Option("-p|--provider", "The provider name", CommandOptionType.SingleValue);
                var ignoreChanges = add.Option(
                    "-i|--ignore",
                    "Ignore model changes",
                    CommandOptionType.NoValue);
                add.OnExecute(() =>
                {
                    if (string.IsNullOrEmpty(name.Value))
                    {
                        _logger.LogError("Missing required argument '{0}'", name.Name);

                        add.ShowHelp();

                        return 1;
                    }
                    AddMigration(name.Value, GetConfiguration(connectionString, providerName), ignoreChanges.HasValue());
                    return 0;
                }
                );
            });
            app.Command("update", update =>
            {
                update.Description = "Update a target database with any schema changes";
                AddHelp(update);
                var connectionString = update.Option("-c|--connectionstring", "The connection string", CommandOptionType.SingleValue);
                var providerName = update.Option("-p|--provider", "The provider name", CommandOptionType.SingleValue);
                update.OnExecute(() =>
                {
                    var config = GetConfiguration(connectionString, providerName);
                    return UpdateDatabase(config);
                });
            });

            return app.Execute(args);
        }

        private static void AddHelp(CommandLineApplication command)
        {
            command.HelpOption("-?|-h|--help");
        }

        private DbMigrationsConfiguration GetConfiguration(CommandOption connectionStringOption, CommandOption providerNameOption)
        {
            Console.WriteLine($"Connection String Specified: {connectionStringOption.Value()}");
            var connectionString = connectionStringOption.HasValue() ? connectionStringOption.Value() : Configuration[_defaultConnectionStringName];
            var providerName = providerNameOption.HasValue() ? providerNameOption.Value() : _defaultProviderName;
            return GetConfiguration(connectionString, providerName: providerName);
        }

        private DbMigrationsConfiguration GetConfiguration(string connectionString = null, string connectionStringName = null, string providerName = null)
        {
            if (string.IsNullOrEmpty(providerName))
                providerName = _defaultProviderName;

            if (string.IsNullOrEmpty(connectionString))
            {
                if (string.IsNullOrEmpty(connectionStringName))
                    connectionStringName = _defaultConnectionStringName;
                connectionString = Configuration[connectionStringName];
            }

            var config = new DbContextOperations(_appEnv.ApplicationName).GetMigrationConfiguration();
            config.TargetDatabase = new System.Data.Entity.Infrastructure.DbConnectionInfo(connectionString, providerName);
            return config;
        }

        private void AddMigration(string name, DbMigrationsConfiguration config, bool ignoreChanges)
        {
            Console.WriteLine($"Creating migration {name}...");

            var scaffolder = new MigrationScaffolder(config);
            var migration = scaffolder.Scaffold(name, ignoreChanges);

            File.WriteAllText(Path.Combine(migration.Directory, migration.MigrationId + ".cs"), migration.UserCode);

            File.WriteAllText(Path.Combine(migration.Directory, migration.MigrationId + ".Designer.cs"), migration.DesignerCode);

            var resourceDirectory = Path.Combine(migration.Directory, "Resources");
            if (!Directory.Exists(resourceDirectory))
                Directory.CreateDirectory(resourceDirectory);

            var resxFile = Path.Combine(resourceDirectory, name + ".resx");
            using (ResXResourceWriter resx = new ResXResourceWriter(resxFile))
            {
                foreach (var kvp in migration.Resources)
                    resx.AddResource(kvp.Key, kvp.Value);
            }
        }

        private static int UpdateDatabase(DbMigrationsConfiguration config)
        {
            Console.WriteLine("Updating...");
            var migrator = new DbMigrator(config);
            migrator.Update();
            return 0;
        }
    }
}
