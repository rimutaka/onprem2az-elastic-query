# Azure cross-DB compatibility script generator

This utility generates a set of scripts to enable existing MS SQL DBs to perform cross-DB read/write queries from stored procedures with minimal changes.

## Commands

* `init` - generates blank config files and copies templates.
* `config` - generates secondary config files from `config.json`.
* `key` - generates *CREATE MASTER KEY* statements.
* `source` - generates *CREATE EXTERNAL DATA SOURCE* statements
* `template` - generates multiple table-related scripts using a template.
* `interpolate` - generates generic scripts using a template.
* `fixtypes` - generate ALTER COLUMN scripts for incompatible SQL type, e.g. *image* or *text*.
* `sqlcmd` - prepare a PowerShell script for executing all *.sql* files in the specified directory with *SqlCmd* utility.
* `replace` - replaces parts of SQL code for refactoring.


### Parameters
* `-t template_name` - name of the file inside `templates` folder or an absolute path to a template file elsewhere
* `-g path_to_grep.txt` - an absolute path to the grep output from DB analysis step. 
The grep file must be located in the root folder of the DB solution.
* `-c config_file.json` - optional in most cases. Use just the name of the file inside `config` folder or an absolute path to a config file elsewhere.
The structure of config files differs for different commands.
* `-d target_directory` - tells the app where to find *.sql* files for the command to process.
Use an absolute path or a name of subfolder under `scripts`.
* `-o master|mirror|az` - where to run the script

### Specific behavior

1. Existing files are never overwritten. Delete the files you want to replace before running a command.
2. `replace` command updates existing *.sql* files in DB folders.   


## App folder structure
This program expects to find some files in predefined subfolders of the current working directory. This structure is created in the current directory by `init` command.

* `./config` - all the config files. The names of the files are hardcoded and should not be changed.
* `./scripts` - output directory for script generation.
* `./templates` - a default location for script templates


## Config files

Config files reside in `config` sub-folder, per DB folder. For example, you need to migrate 5 DBs with cross-DB access.
You will need to create 5 folders, one per DB and configure them all separately.

* `config.json` - the main config file that should be populated by hand to generate the rest of the config files.
* `MasterKey.json` - used to generate master keys. It only has to be done once per DB after it was restored on Azure.
This file is auto-generated from `config.json`. You may have to modify the file if credentials differ between DBs.
* `ExternalDataSource.json` - used to generate *Create External Data Source* statements, one per local-remote DB pair.
This file is auto-generated from `config.json`. You may have to modify this file if the credentials differ between DBs.
* `TablesMirror.json` - list of external tables that should be mirrored for writing locally.
* `TablesReadOnly.json` - list of external tables for remote read-only access.
* `SPsConfig.json` - list of external SPs accessed via a local proxy SP.

### Config property inheritance

Config files other than `config.json` are arrays of objects. Identical values for the same properties do not need to be repeated from object to object - they are copied from the previous known value.

E.g. 2nd and 3rd objects will get all the properties except for `localDB` copied from the 1st object:

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

### Config properties

`serverName` - Azure server name, e.g. `"serverName":"citi-sql-pool-test"`

`password`, `credential`, `identity`, `secret` - these correspond to parameters of *CREATE MASTER KEY* and *CREATE EXTERNAL DATA SOURCE* SQL statements.

`twoway: 1` - generates the first script as prescribed, then reverses `externalDB` <-> `localDB` and generates again. 
Use this option if the connections have identical properties.

`localServer` - the name of the local server with copies of the DBs to migrate to configure *sqlcmd* scripts. E.g. `"localServer": "."`.

`connections` - a list of MSSQL connection strings to all the DBs, one per line. Example:
```
"connections":"Persist Security Info=False;User ID=sa;Password=sapwd;Initial Catalog=central;Server=.
Persist Security Info=False;User ID=sa;Password=sapwd;Initial Catalog=pbl;Server=.
Persist Security Info=False;User ID=sa;Password=sapwd;Initial Catalog=pbl_location;Server=."
```

`mirrorDB` - the name of the current DB the config file is for.

`masterTables` - a list of all tables accessed by the current DB (mirror) with *INSERT* statements.
Use 3-part names, one per line, like in this example:
```
"masterTables": "central.dbo.tbc_EstimatedCategory
central.dbo.tbt_estimatedcategory
helpdesk.dbo.departments
pbl..tbr_channel_agency"
```
It's OK to omit the schema name. The all will default it to `dbo`. The app will use this list to create a mirror table, an external table, 2 SPs and an ALTER TABLE script per master table to make the remote writes work.

`masterTablesRO` - a list of read-only remote tables. Use the same format as above. This list is used to generate an external table in the current DB (mirror DB).

`masterSPs` - a list of remote SPs. Use the same format as above. This list is used to generate proxy SPs in the current DB (mirror DB).

### Config file generation

Run `azpm init` to generate the directory structure and `config.json` with blank values.

Populate `config/config.json` with values and run `azpm config` to generate more config files with table data.

Check the generated config files and templates and make changes as necessary.

 Both `azpm init` and `azpm config` can be safely re-run at any stage. Existing files will not be overwritten.

 For example, if you need to add or remove a table:
 1. make changes to `config.json`
 2. delete the config files you want to regenerate
 3. run `azpm config` and review the newly generated files

## SQL script templates

The templates are text files with .Net string interpolation via *String.Format(...)*. Use `{n}` placeholders with the following numbers:

* {0} - mirrorDB
* {1} - masterDB
* {2} - masterTable
* {3} - table columns with their definitions
* {4} - list of SP param definitions
* {5} - list of SP params
* {6} - list of SP param assignments
* {7} - list of table column names, excluding identity columns

Originally, the templates reside in *templates* folder of the C# project and are copied to the working folder by `init` command.
You can modify them locally, copy, rename and then specify the file name to be used with `-t` param.

### How to update all templates

Sometimes you may need to update all the templates and re-run the script generation.
1. Update the template source in the C# project
2. Run a PowerShell script to delete templates from working folders
3. Run `azpm init`. It will only add the missing templates and will not overwrite any existing files.


# Commands in detail

There is a great chance that you will need to modify the source code as you go.
It may be easier to create a shortcut in your working folder pointing at `\bin\Debug\netcoreapp3.0\AzurePoolCrossDbGenerator.exe`.

* Shortcut name: `azpm`
* Start in: `.` to make it run in the current directory of the command line

### init
* **Params**: none
* **Action**: create subfolders, copy SQL templates, create `config.json` template.
* **Example**: `azpm init`

### config
* **Params**: optional `-c` to specify the source config from a file other than the default `config.json`
* **Action**: generate other config files from `config.json` or `-c` param
* **Example 1**: `azpm config` - uses default `config.json`
* **Example 2**: `azpm config -c c:\myfolder\my-config.json`

### keys
* **Params**: optional `-c` to specify the source config from a file other than the default `MasterKey.json`
* **Action**: generate *CREATE MASTER KEY* scripts for all DBs listed in `masterTables` and `masterTablesRO` using `CreateMasterKey.txt` template.
* **Example 1**: `azpm keys` - uses default `MasterKey.json`
* **Example 2**: `azpm keys -c c:\myfolder\my-MasterKey-config.json`

### sources
* **Params**: optional `-c` to specify the source config from a file other than the default `ExternalDataSource.json`
* **Action**: generate *CREATE EXTERNAL DATA SOURCE* scripts for all DBs listed in `masterTables` and `masterTablesRO` using `CreateExternalDataSource.txt` template.
* **Example 1**: `azpm sources` - uses default `ExternalDataSource.json`
* **Example 2**: `azpm sources -c c:\myfolder\my-ExternalDataSource-config.json`

### template
* **Params**: 
  * `-t` - template name from *templates* sub-folder, e.g. `-t CreateExtTable.txt` or `-t c:\myfolder\MyTemplate.txt`
  * `-c` - config file name from *config* sub-folder, e.g. `-c TablesMirror.json` or `-c c:\myfolder\TablesMirror-config.json`
  * `-o master` - the script will run on the DB listed as *master* in the config
  * `-o mirror` - the script will run on the DB listed as *mirror* in the config
  * all three `-t`, `-c` and `-o` params are required
  * the config file must be the same format as *TablesMirror.json* to correspond to *Configs.AllTables* class in the C# code.
* **Action**: generate SQL scripts using `-t` template with input from `-c` config, including table definitions.
* **Example**: `azpm template -t CreateExtTable.txt -c TablesMirror.json -o master`

### interpolate
* **Params**: 
  * required `-t` - template name from *templates* sub-folder, e.g. `-t AddErrorLogging.txt` or `-t c:\myfolder\MyTemplate.txt`
  * required `-c` - config file name from *config* sub-folder, e.g. `-c config.json` or `-c c:\myfolder\my-config.json`
  * the config file must be the same format as *config.json* to correspond to *Configs.InitialConfig* class in the C# code.
* **Action**: generate SQL scripts using `-t` template with input from `-c` config using `{{moustache}}` syntax for properties from *config.json*.
* **Example**: `azpm template -t AddErrorLogging.txt -c config.json`

The difference between `template` and `interpolate` is that *template* gathers table and SP data to generate mirror and external tables. *interpolate* is a generic script generator - it can only use data from *config.json*.

### fixtypes
* **Params**: 
  * required `-c` - config file name from *config* sub-folder, e.g. `-c TablesMirror.json` or `-c c:\myfolder\TablesMirror-config.json`
  * the config file must be the same format as *TablesMirror.json* to correspond to *Configs.AllTables* class in the C# code.
* **Action**: generate *ALTER TABLE COLUMN* SQL scripts for tables listed in the config file to change incompatible SQL types to compatible ones. E.g. *image* -> *varbinary(max)*.
* **Example**: `azpm fixtypes -c TablesReadOnly.json`

### sqlcmd
* **Params**: 
  * optional `-c` to specify the source config from a file other than the default `config.json`
  * required `-d` to specify the directory with SQL scripts, e.g. `-d AlterMasterTable` or `-d c:\myfolder`
  * optional `-o az` - the script will run on the Azure server listed in *serverName* in the config, otherwise use *localServer* value.
* **Action**: generate a PowerShell script to run all the scripts in `-d` folder and stage them in GIT on success.
* **Example 1**: `azpm sqlcmd -d AlterMasterTable` - find SQL scripts in *./scripts/AlterMasterTable/* directory.
* **Example 2**: `azpm sqlcmd -d c:\myfolder\` - find SQL scripts in *c:\myfolder* directory.

### replace
* **Params**: 
  * required `-g` - absolute path to a grep output file with the list of strings to replace, e.g. `-g c:\myfolder\all-insert-grep.txt`
  * optional `-c` to specify the source config from a file other than the default `config.json`
  * required `-t` - a replacement template with `{0,1,2,3}` substitution groups, e.g. `ext_{1}__{2}`
  * substitution groups: `{0}` = the DB that owns the script, `{1}` = the DB name from the 3-part name being replaced, `{2}` = the table name, `{3}` = the schema name
* **Action 1**: 
  * replace 3-part names in the SQL scripts listed in the grep file with names built with `-t` template
  * generate a PowerShell script with *sqlcmd* to apply modified scripts and stage them in GIT on success
* **Example**: `replace -t ext_{1}__{2} -g C:\migration-repo\cross-db-read-grep-4v.txt`

`replace` command can be run from any working directory with `config.json`. It only updates files in subfolders relative to the grep file. 

#### Substitution group examples:

* cross-DB read: `ext_{1}__{2}` -> *ext_RemoteDbName__RemoteTableName*
* self-ref: `{3}.{2}` -> *schema.localTableName*


### About grep files

1. The grep file must be located in the root of the DB solution with DBs as sub-folders containing all the SQL scripts (tables, views, SPs, UFs, etc) extracted from the DBs. The app will use the location of the grep file as the base for relative paths to SQL scripts in the grep file.

**Grep example**:

```
./4val/dbo.ADD_MANUALRESERVATION_IN_STATS.StoredProcedure.sql:69:		LEFT OUTER JOIN CENTRAL.dbo.TB_Channel C ON  isnull(r.ID_Channel,0) = isnull(C.ID_Channel,0)
./4val/dbo.ADD_ONLINERESERVATION_DETAILS_IN_STATS.StoredProcedure.sql:102:		LEFT OUTER JOIN CENTRAL.dbo.TB_Channel C ON  isnull(R.ID_Channel,0) = isnull(C.ID_Channel,0)
./4val/dbo.ADD_ONLINERESERVATION_IN_STATS.StoredProcedure.sql:72:		LEFT OUTER JOIN CENTRAL.dbo.TB_Channel C ON  isnull(r.ID_Channel,0) = isnull(C.ID_Channel,0)
./4val/dbo.ADD_ONLINERESERVATION_IN_CITI_STATS.StoredProcedure.sql:144:		LEFT OUTER JOIN CENTRAL.dbo.TB_Channel C ON  isnull(r.ID_Channel,0) = isnull(C.ID_Channel,0)
```

2. The grep files must be cleaned up to make sure there are only lines that need to be processed at this time.
3. Line 1 from the snippet above will have `CENTRAL.dbo.TB_Channel` replaced with `ext_CENTRAL__TB_Channel` so that the 3-part name becomes a name of an external table which is created by one of the scripts generated with `azpm template -t CreateExtTableRO.txt -c TablesReadOnly.json -o mirror`
```
LEFT OUTER JOIN ext_CENTRAL__TB_Channel C ON  isnull(r.ID_Channel,0) = isnull(C.ID_Channel,0)
```

4. This program modifies *.sql* scripts exported from the DBs you are migrating. 
All exported DB scripts should be checked into a repo and comply with this structure for script modifications to work: 
```
- /root/
 -/db name/
  -/script file name.sql/
```


# Order of generating and applying the scripts

### Analysis

First of all, analyse and grep the SQL exported from the DBs you are migrating as much as possible. Look for:

* INSERT
* EXEC / EXECUTE
* UPDATE
* DELETE
* self-references (e.g. `this_db.dbo.some_table` should be just `some_table`)
* All the other 3-part names are likely to be from SQL FROM clauses, unless they are part of dynamic SQL

### Multistage approach

Some code can be run only on Azure and some only on a VM/bare metal. We are forced into a 2-stage approach:

1. Remove the 3-part names so that the DB can be exported into *.bacpac*
2. Upload the DBs to Azure
3. Create external data sources and replace the temporary code we put in place in step 1

### Step by step 

1. **Mirror tables**
2. **Dummy ext tables** - a local table that has the same definition as the external table
3. **Dummy SPs at both ends** - these SPs have no body because there are no external data sources yet
* *at this point there should be no 3-part names or any other issues preventing migration to Azure*
4. **Move the DBs to Azure Pool**
5. **Master Keys**
6. **Ext Data Sources**
7. **ALT master tables**
8. **Replace dummy ext tables** with real ext tables
9. **Replace dummy SPs** with real SPs
* *the DBs should have everything they need to continue operating on Azure the same way they operated on-prem*


# Security

The code is built for reuse of the same credential name for all DBs.
It may be a more secure set up if you use a separate credential for every ext data source. 
You may want to change the code to generate the cred names automatically.


# Testing for migration issues

Exporting a DB for Azure SQL Pool requires `.bacpac` file format. It is done with *SQlPackage.exe* utility (https://docs.microsoft.com/en-us/sql/tools/sqlpackage).

The utility will check all internal references before exporting the file and raise an error for any cross-DB reference.

```
sqlpackage.exe /Action:Export /ssn:127.0.0.1 /su:sa /sp:sapwd /sdn:4VAL /tf:4VAL.bacpac
```
