# Entity Framework 6 Migrations for ASP.NET 5

1. Include the myget feed for this package, `https://www.myget.org/F/atquin`, or in global.json add the `src` directory of this repo to your `projects` list.
2. Reference Atquin.EntityFramework.Migrations in your DataContext xproj;
 
 ~~~
	"dependencies": {
		"EntityFramework6.Npgsql": "3.0.3", <!-- if you want npgsql -->
		"Atquin.EntityFramework.Migrations": "1.0.0-rc1-final"
	},
~~~

2. Open command prompt and execute; 
 ~~~
cd /your/datacontext/directory
dnx ef add MigrationName
~~~
3. VS2015 will then immediately generate a same-named *.cs for the *.resx file created.  Delete that *.cs file.
4. Now you can update the database;
~~~
dnx ef update
~~~
## Options
~~~
dnx ef add --help

Usage: ef add [arguments] [options]

Arguments:
  [name]  Migration name label

Options:
  -?|-h|--help           Show help information
  -c|--connectionstring  The connection string
  -p|--provider          The provider name
  -i|--ignore            Ignore model changes

dnx ef update --help

Usage: ef update [options]

Options:
  -?|-h|--help           Show help information
  -c|--connectionstring  The connection string
  -p|--provider          The provider name
~~~
