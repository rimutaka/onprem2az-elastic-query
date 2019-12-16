# Azure Cross-DB compatibility generator

This utility generates a set of scripts to enable existing MS SQL DBs to perform cross-DB write queries from stored procedures with minimal changes.

Use the following format: AzurePoolCrossDbGenerator [command] [config file name (use absolute path)]

## Commands
* `init` - generates blank config files and copies script templates to the current folder
* `config` - generates `TablesConfig.json` from `config.json` to establish links between DBs 
* `key` - generates *CREATE MASTER KEY* statements 
* `source` - generates *CREATE EXTERNAL DATA SOURCE* statements
* `template` - generates a script using specified template. Accepts a file name from *templates* sub-folder or a fully-qualified file name.
* `sqlcmd` - prepare a batch file for executing all files in the specified directory with *SqlCmd* utility. Omit the path to process all subdirectories under `script`.
* `selfref` - removes all DB self-references and prepares a batch file for executing modified files with *SqlCmd* utility.

## Folder structure
`./config` - all the config files. The names are hardcoded.
`./scripts` - output directory for script generation.
`./templates` - a default location for script templates

Existing files are never overwritten.

## Config JSON files

All files are arrays. Identical values do not need to be repeated - they are copied from the previous known value.

E.g. 2nd and 3rd objects will get all the properties except for `localDB` copied from the 1st object in this example:

```
[
    {
        "password": "xyz",
        "credential": "ElasticDBQueryCred",
        "identity": "yyy",
        "secret": "xxx",
        "folder": "C:\\some-path\\",
        "localDB": "MyDbName1"
    },
    {
        "localDB": "MyDbName2"
    },
    {
        "localDB": "MyDbName3"
    }
]
```

`twoway: 1` - generates the first script as prescribed, then reverses `externalDB` <-> `localDB` and generates again. 
Use this option if the other properties of the connection are identical.

### Config file generation

Use command `init` to generate all possible config files with blank values.

Populate `config/config.json` with values and run `config` command to generate more config files with table data.

Values for `serverName`, `password`, `credential`, `identity`, `secret` are copied into the generated configs.

`connections` should contain connection strings, one per line. E.g. 
```
Persist Security Info=False;User ID=sa;Password=sapwd;Initial Catalog=central;Server=.
Persist Security Info=False;User ID=sa;Password=sapwd;Initial Catalog=pblciti;Server=.
Persist Security Info=False;User ID=sa;Password=sapwd;Initial Catalog=reporting;Server=.
```

`mirrorDB` - name of the single DB that mirrors tables from master DBs. Usually it is the customer DB writing to shared DBs.

`masterTables` - a list of 3-part table names being remotely update from the *mirror DB*. One table per line. E.g.
```
central.dbo.tbt_EstimatedCategory 
helpdesk.dbo.departments
PBLCITI..TBR_CHANNEL_AGENCY
```

`localServer` - name of the server to include into *SqlCmd* scripts.

## Script templates

The templates are text files with .Net string interpolation via *String.Format(...)*. Use `{n}` placeholders with the following numbers:

* {0} - mirrorDB
* {1} - masterDB
* {2} - masterTable
* {3} - table columns

## Security

The code is built for reuse of the same credential name for all DBs.
It may be a more secure set up if you use a separate credential for every ext data source. 
You may want to change the code to generate the cred names automatically.

## Applying the scripts

1. Master key
2. Data sources
3. ALT master table
4. Create mirrors
5. Create ext tables
6. Create SPs at both ends

## Bootsrapping DB export for Azure SQL Pool

Exporting a DB for Azure SQL Pool requires `.bacpac` file format. It is done with *SQlPackage.exe* utility (https://docs.microsoft.com/en-us/sql/tools/sqlpackage).

The utility will check all internal references before exporting the file and raise an error for any cross-DB reference. 
So we need to update the DBs to using *ElasticQuery*, but it is not possible with an on-prem SQL Server. It's a bit of a catch-22.

The workaround is to create temporary tables and SPs with the same names as the external tables and no references to external data sources.
Then we can update all existing cross-DB references with the new local ones, import into Azure and then replace the dummy objects with the proper ones.

# Modifying files in bulk

## Directory structure

All exported DB scripts should be checked into a repo and comply with this structure for script modifications to work: 

- /root/
 -/db name/
  -/script file name.sql/

Script file names should follow the default SSMS exported script convension. E.g. `dbo.MyUserFunctionMane.UserDefinedFunction.sql`.

## Remove self-references

There may be 3-part names in SQL statements that refer to the same DB they are in. E.g. DB `PBLCITI_LOCATION` may have a statement like this:
```
select * from PBLCITI_LOCATION..TBR_AgencyObjectType
```

It is redundant and is not allowed under Azure SQL rules. Use `selfref` command to remove all DB self-references.

1. Use `grep` to find all self references and output them into a file

```
grep -i -n 'db_name\.' ./db_name/*.sql > self-refs.txt
grep -i -n '\[db_name\]\.' ./db_name/*.sql >> self-refs.txt
```
The output file name `self-refs.txt` is arbitrary. Name it anything you like.

2. Review `self-refs.txt` and remove lines that don't need modifications. E.g. line #1 in this example doesn't need any mods:
```
./citi_ip_country/CITI_IP_COUNTRY.Database.sql:6:( NAME = N'CITI_IP_COUNTRY_data', FILENAME = N'C:\Program Files\Microsoft SQL Server\MSSQL13.MSSQLSERVER\MSSQL\DATA\CITI_IP_COUNTRY.mdf' , SIZE = 458432KB , MAXSIZE = UNLIMITED, FILEGROWTH = 1024KB )
./citi_ip_country/dbo.GetRentalpCountryCodeByIpNumber.UserDefinedFunction.sql:18:	from	citi_ip_country..tb_ip p, citi_ip_country..tb_location c
```

3. Place `self-refs.txt` in the root folder of all the DB scripts listed in it.

4. Use `selfref absolute-path-to-self-refs.txt` from the root folder of the utility.
This command will modify all files listed in `self-refs.txt` and create `self-refs.bat` to help you execute all modified files in one step.

5. Carefully diff the changes and commit.

6. Run `self-refs.bat`.

7. Review the output.

## INSERT INTO column mismatch

A common error is that the column names are not listed and it does *INSERT INTO ... SELECT * FROM ...*,
but `mirror_key` field is left unaccounted for. Add the following at the end of the list of select columns `NULL -- required for mirror_key column`.
