﻿using Microsoft.Dnx.Runtime;
using Microsoft.Framework.Configuration;
using System;
using System.Data.Entity.Migrations;
using System.Data.Entity.Migrations.Design;
using System.IO;
using System.Resources;

namespace Atquin.EntityFramework.Migrations
{
    public class Migrator
    {
        private const string _defaultConnectionStringName = "Data:DefaultConnection:ConnectionString";
        private const string _defaultProviderName = "System.Data.SqlClient";
        private readonly IConfiguration _config;
        private readonly string _dataContextAssemblyName;

        public Migrator(IConfiguration configuration, string dataContextAssemblyName)
        {
            _config = configuration;
            _dataContextAssemblyName = dataContextAssemblyName;
        }

        public DbMigrationsConfiguration GetConfiguration(string connectionString = null, string connectionStringName = null, string providerName = null)
        {
            if (string.IsNullOrEmpty(providerName))
                providerName = _defaultProviderName;

            if (string.IsNullOrEmpty(connectionString))
            {
                if (string.IsNullOrEmpty(connectionStringName))
                    connectionStringName = _defaultConnectionStringName;
                connectionString = _config[connectionStringName];
            }

            var config = new DbContextOperations(_dataContextAssemblyName).GetMigrationConfiguration();
            config.TargetDatabase = new System.Data.Entity.Infrastructure.DbConnectionInfo(connectionString, providerName);
            return config;
        }

        public void AddMigration(string name, DbMigrationsConfiguration config, bool ignoreChanges)
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

        public int UpdateDatabase(DbMigrationsConfiguration config)
        {
            Console.WriteLine("Updating...");
            var migrator = new DbMigrator(config);
            migrator.Update();
            return 0;
        }
    }
}