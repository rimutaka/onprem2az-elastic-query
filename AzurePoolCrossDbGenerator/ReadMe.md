# Azure Cross-DB compatibility generator

This utility generates a set of scripts to enable existing MS SQL DBs to perform cross-DB write queries from stored procedures with minimal changes.

Use the following format: azpm `command` [-t `template file name or replacement pattern`] [-c `config file name`] [-g `full path to grep file`].

## Commands

* `init` - generates blank config files in `/config` folder and copies script templates to `/templates`.
* `config` - generates secondary config files from `config.json` to establish links between DBs.
* `key` - generates *CREATE MASTER KEY* statements 
* `source` - generates *CREATE EXTERNAL DATA SOURCE* statements
* `template -t ... -c ...` - generates a script using specified template. Accepts a file name from *templates* sub-folder or a fully-qualified file name.
* `sqlcmd -d path_to_folder` - prepare a PowerShell script for executing all *.sql* files in the specified directory with *SqlCmd* utility. 
Non-recursive. Optional param:  *-c path to some version of config.json*.
* `replace path_to_grep.txt` - removes all DB self-references and prepares a batch file for executing modified files with *SqlCmd* utility.
* `insertref path_to_grep.txt` - converts 3-part names to a single mirror table name following *INSERT INTO* in all *.sql* files under the path of the 2nd param.

**IMPORTANT**: Existing config or script files in subfolders are never overwritten. Delete the files you want to replace before running a command. 
On the contrary, `replace` command updates existing .sql files in DB folders.   

### Parameters
* `template_name` - name of the file inside `./templates` folder
* `path_to_grep.txt` - an absolute path or relative to the grep output with all the references located in the root folder of all the DB scripts.

## App folder structure
This program expects to find some files in predefined subfolders relative to the current working directory.

* `./config` - all the config files. The names of the files are hardcoded and should not be changed.
* `./scripts` - output directory for script generation.
* `./templates` - a default location for script templates


## Usage

There is a great chance that you will need to modify the source code to fit your unique situation.
It may be easier to create a shortcut in your working folder pointing to `\bin\Debug\netcoreapp3.0\AzurePoolCrossDbGenerator.exe`.

* Shortcut name: `azpm`
* Start in: `.` to make it run in the current directory of the command line


### Step 1: initialise the environment

Run `azpm init` to create blank config files. In most cases you only need to fill in details of `config.json`and delete the rest of the files.
The config files you delete will be regenerated with values from `config.json` at the next step.

The app will copy template files to your working directory. You can modify them as needed.

### Step 2: generate config

Run `azpm config` to generate config files based on the values in `config.json`. In most cases it all you need to get going.




## Config JSON files

Config files reside in `config` sub-folder, per DB folder.

* `config.json` - the main config file that should be populated by hand to generate the rest of the config files.
* `MasterKey.json` - used to generate master keys. It only has to be done once per DB after it was restored on Azure.
* `ExternalDataSource.json` - used to generate *Create External Data Source* statements, one per local-remote DB pair.
This file is auto-generated from `config.json`, but if the logins differ between servers it has to be modified by hand.
* `TablesMirror.json` - list of external tables that should be mirrored. 
Used to create mirrors, dummy external tables and external tables for INSERT statements.
* `TablesReadOnly.json` - list of external tables that are only read remotely from the mirror DB.
Used to create dummy external tables and external tables.
* `SearchAndReplace.json` - lists C# string formatting patterns for substitution in existing SQL code.
The formats must match those in the SQL templates. 

### Value inheritance

Some config files are arrays. Identical values do not need to be repeated - they are copied from the previous known value.

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

`masterTables` - a list of all master tables for this DB to mirror locally and create the SPs for remote writes.

`masterTablesRO` - a list of remote tables that are read only, so no need to create a local mirror.

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

Run `azpm template` with these params to generate scripts using a template:
* `-t` - an SQL template file name. It can be just the name of the file inside *templates* subfolder or the full path to a file eslewhere.
* `-c` - a config file name. It can be just the name of the file inside *configs* subfolder or the full path to a file eslewhere.
The config file must have the same format as *TablesMirror.json* or *TablesReadOnly.json*.
* `-o` - either `master` or `mirror` to indicate which DB to run this script on.

## Applying the scripts

Run `azpm sqlcmd` with these params to generate PowerShell scripts with `sqlcmd` calls:
* `-d` - path to the folder with SQL scripts. It can be either a full path or `target_folder_name` under `scripts` subfolder of the current directory.
* `-c` - a an optional config file name. It can be just the name of the file inside *configs* subfolder or the full path to a file eslewhere.
The config file must have the same format as *config.json*.


## Order of applying the scripts

Follow these steps. There are certain constaints that force this order onto us.

1. Create mirrors
2. Create dummy ext tables - *it's hard to create valid ext tables on-prem, so just create dummy ones as an interface*.
3. Create dummy SPs at both ends - *no body because ext tables are not ready and a remote call doesn't work on-prem*.
4. Move the DB to Azure Pool
5. Add Master Keys
6. Add Ext Data Sources - *ths and following steps can only be done on Azure*.
7. ALT master table - *remove mirror_key field from mirror table template if creating mirrors after altering master tables*.
8. Replace dummy ext tables with real ext tables
9. Replace dummy SPs with real SPs



## Security

The code is built for reuse of the same credential name for all DBs.
It may be a more secure set up if you use a separate credential for every ext data source. 
You may want to change the code to generate the cred names automatically.


## Bootsrapping DB export for Azure SQL Pool

Exporting a DB for Azure SQL Pool requires `.bacpac` file format. It is done with *SQlPackage.exe* utility (https://docs.microsoft.com/en-us/sql/tools/sqlpackage).

The utility will check all internal references before exporting the file and raise an error for any cross-DB reference. 
So we need to update the DBs to using *ElasticQuery*, but it is not possible with an on-prem SQL Server. It's a bit of a catch-22.

The workaround is to create temporary tables and SPs with the same names as the external tables and no references to external data sources.
Then we can update all existing cross-DB references with the new local ones, import into Azure and then replace the dummy objects with the proper ones.

# Modifying files in bulk

## Directory structure

This program modifies *.sql* scripts exported from the DBs you are migrating. 
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

## Replace INSERT INTO 3-part names with mirror tables

E.g. `insert into citi_reporting.dbo.tb_site` -> `insert into mr_citi_reporting__tb_site` where both tables have the same signature.

1. Use `grep` to extract all 3-part names after INSERT INTO. Run it on all the SQL files for all DBs.
```
grep -i -r -n --include '*.sql'  -E '\binsert\s*into\s*\[?CITI_\w+\]?\.\[?\w*\]?\.\[?\w*\]?' . > cross-db-insert-grep.txt
```
This example uses common DB prefix `CITI_`, which was specific to a particular project. Modify the Regex to suit yours.

2. Clean up the grep output to remove commented out lines, false positives and files like *Database.sql*.
3. Remove references to lines where INSERT INTO is followed by SELECT ... FROM 3-part-name in the same line. Those have to be dealt with separately.



### INSERT INTO column mismatch

A common error is that the column names are not listed and it does *INSERT INTO ... SELECT * FROM ...*,
but `mirror_key` field is left unaccounted for. Add the following at the end of the list of select columns `NULL -- required for mirror_key column`.


