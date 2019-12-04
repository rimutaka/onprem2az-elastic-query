# Azure Cross-DB compatibility generator

This utility generates a set of scripts to enable existing MS SQL DBs to perform cross-DB write queries from stored procedures with minimal changes.

Use the following format: AzurePoolCrossDbGenerator [command] [config file name (use absolute path)]

## Commands
* `config` - generates blank config files in TEMPLATES subfolder of the soluton
* `key` - generates *CREATE MASTER KEY* statements 
* `source` - generates *CREATE EXTERNAL DATA SOURCE* statements 

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

## Security

The code is built for reuse of the same credential name for all DBs.
It may be a more secure set up if you use a separate credential for every ext data source. 
You may want to change the code to generate the cred names automatically.