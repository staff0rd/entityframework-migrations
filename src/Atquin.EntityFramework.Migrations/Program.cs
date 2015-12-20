using System;
using System.Reflection;
using Microsoft.Framework.Runtime.Common.CommandLine;
using System.Data.Entity.Migrations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.PlatformAbstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;

namespace Atquin.EntityFramework.Migrations
{
    public class Program
    {
        private readonly ILogger _logger;
        private readonly Migrator _migrator;

        public Program(IApplicationEnvironment appEnv, ILoggerProvider logProvider)
        {
            _logger = logProvider?.CreateLogger(this.GetType().ToString()) ?? new ConsoleLogger(this.GetType().ToString(), (catgory,level) => { return true; }, true);
            
            Configuration =
                new ConfigurationBuilder()
                .SetBasePath(appEnv.ApplicationBasePath)
                .AddJsonFile("config.json", true)
                .Build();

            _migrator = new Migrator(Configuration, appEnv.ApplicationName);
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
                    _migrator.AddMigration(name.Value, GetConfiguration(connectionString, providerName), ignoreChanges.HasValue());
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
                    return _migrator.UpdateDatabase(config);
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
            var connectionString = connectionStringOption.HasValue() ? connectionStringOption.Value() : null;
            var providerName = providerNameOption.HasValue() ? providerNameOption.Value() : null;
            return _migrator.GetConfiguration(connectionString, providerName: providerName);
        }
    }
}
