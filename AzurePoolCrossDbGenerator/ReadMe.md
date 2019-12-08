# Azure Cross-DB compatibility generator

This utility generates a set of scripts to enable existing MS SQL DBs to perform cross-DB write queries from stored procedures with minimal changes.

Use the following format: AzurePoolCrossDbGenerator [command] [config file name (use absolute path)]

## Commands
* `config` - generates blank config files in TEMPLATES subfolder of the soluton
* `key` - generates *CREATE MASTER KEY* statements 
* `source` - generates *CREATE EXTERNAL DATA SOURCE* statements
* `mirror` - generate SPs for master and mirror, ALT master table
* `ext-table` - create external tables at either end
* `mir-table` - create mirror tables at the mirror end
* `tables` - generate a proper json config from a list of 3-part table names, one per line. 
E.g.
```
central.dbo.tbt_EstimatedCategory 
helpdesk.dbo.departments
PBLCITI..TBR_CHANNEL_AGENCY
```

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

Use command `config` to generate all possible config files with blank values.

Populate `CreateTableList.json` with values and run `tables CreateTableList.json` command to generate 2 more config files with table data.

Values for `folder`, `serverName`, `password`, `credential`, `identity`, `secret` are copied into the generated configs.

`connections` should contain connection strings, one per line. E.g. 
```
Persist Security Info=False;User ID=sa;Password=sapwd;Initial Catalog=central;Server=.
Persist Security Info=False;User ID=sa;Password=sapwd;Initial Catalog=pblciti;Server=.
Persist Security Info=False;User ID=sa;Password=sapwd;Initial Catalog=reporting;Server=.
```

`mirrorDB` - name of the single DB that mirrors the tables in others. Usually it is the customer DB writing to shared DBs.

`masterTables` - a list of 3-part table names being remotely update from the *mirror DB*. One table per line. E.g.
```
central.dbo.tbt_EstimatedCategory 
helpdesk.dbo.departments
PBLCITI..TBR_CHANNEL_AGENCY
```

Use the output of `tables CreateTableList.json` to create external and mirror tables.

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

