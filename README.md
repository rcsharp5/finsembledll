[![Build Status](https://dev.azure.com/chartiq/Finsemble-dll/_apis/build/status/ChartIQ.finsemble-dll?branchName=master)](https://dev.azure.com/chartiq/Finsemble-dll/_build/latest?definitionId=3&branchName=master)

# CI/CD system
## Master Branch
No tests are run, system only ensures that a build is made.

Finsemble.dll will be packaged up into a nuget package and published to https://www.nuget.org/packages/Finsemble/ with the version number that is set in AssembleInfo.cs.

Note: If an existing version is already on nuget (even in an unpublished or pending state) then the publish process will fail. This is expected behavoir. Be sure to bump the version number accordingly prior to merging to master.

## PR
As of now the system only checks that a build is made. There are no tests to run. No assets are published.

# Finsemble DLL
The project `Finsemble` will build Finsemble.dll

# finsemble-wpf


## WPF Example

Set a `wpfExampleRoot` variable in the manifest that points to folder where WPFExample.exe_ is built (e.g. _C:/Users/andy/Documents/SourceCode/finsemble-dll/WPFExample/bin/Debug_). Copy _wpfExample.json_ to _configs/applications_ in the seed project, and add the following to the `importConfig` array: `"$applicationRoot/configs/application/wpfExample.json"`.

```JSON
        "wpfExampleRoot": "C:/Users/andy/Documents/SourceCode/finsemble-dll/WPFExample/bin/Debug",
        "importConfig": [
            "$applicationRoot/configs/application/config.json",
            "$applicationRoot/configs/application/wpfExample.json"
        ],
```

## Multi-Window Example

Set a `multiWindowRoot` variable in the manifest that points to folder where _MultiWindowExample.exe_ is built (e.g. _C:/Users/andy/Documents/SourceCode/finsemble-dll/MultiWindowExample/bin/Debug_). Copy _multiWindowComponents.json_ to _configs/applications_ in the seed project, and add the following to the `importConfig` array: `"$applicationRoot/configs/application/multiWindowComponents.json"`.

```JSON
        "multiWindowRoot": "C:/Users/andy/Documents/SourceCode/finsemble-dll/MultiWindowExample/bin/Debug",
        "importConfig": [
            "$applicationRoot/configs/application/config.json",
            "$applicationRoot/configs/application/multiWindowComponents.json"
        ],
```