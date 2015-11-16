using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Migrations;
using System.Linq;
using System.Reflection;

namespace Atquin.EntityFramework
{
    public class DbContextOperations
    {
        private string _assemblyName;
        public DbContextOperations(string startupAssembly)
        {
            _assemblyName = startupAssembly;
        }

        public IDictionary<Type, Func<DbMigrationsConfiguration>> FindConfigurations()
        {
            var startupAssembly = Assembly.Load(new AssemblyName(_assemblyName));
            var configurations = new Dictionary<Type, Func<DbMigrationsConfiguration>>();

            var types = GetConstructibleTypes(startupAssembly)
                .Select(i => i.AsType());
            var configTypes = types.Where(t => typeof(DbMigrationsConfiguration).IsAssignableFrom(t))
                .Distinct();
            foreach (var config in configTypes.Where(c => !configurations.ContainsKey(c)))
            {
                configurations.Add(
                    config,
                    (() => (DbMigrationsConfiguration)Activator.CreateInstance(config)));
            }
            return configurations;
        }

        public IDictionary<Type, Func<DbContext>> FindContextTypes()
        {
            var startupAssembly = Assembly.Load(new AssemblyName(_assemblyName));
            var contexts = new Dictionary<Type, Func<DbContext>>();

            var types = GetConstructibleTypes(startupAssembly)
                .Select(i => i.AsType());
            var contextTypes = types.Where(t => typeof(DbContext).IsAssignableFrom(t))
                .Distinct();
            foreach (var context in contextTypes.Where(c => !contexts.ContainsKey(c)))
            {
                contexts.Add(
                    context,
                    (() => (DbContext)Activator.CreateInstance(context)));
            }

            return contexts;
        }

        public static IEnumerable<TypeInfo> GetConstructibleTypes(Assembly assembly)
            => assembly.DefinedTypes.Where(
                t => !t.IsAbstract
                     && !t.IsGenericType
                     && t.DeclaredConstructors.Any(c => c.GetParameters().Length == 0 && c.IsPublic));

        public Type GetContextType()
        {
            return FindContextTypes().First().Key;
        }

        public DbMigrationsConfiguration GetMigrationConfiguration()
        {
            return FindConfigurations().First().Value();
        }
    }
}
