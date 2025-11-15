# .NET 10 Upgrade Plan

## Execution Steps

Execute steps below sequentially one by one in the order they are listed.

1. Validate that a .NET 10 SDK required for this upgrade is installed on the machine and if not, help to get it installed.
2. Ensure that the SDK version specified in global.json files is compatible with the .NET 10 upgrade.
3. Upgrade RequesterMini.csproj

## Settings

This section contains settings and data used by execution steps.

### Aggregate NuGet packages modifications across all projects

NuGet packages used across all selected projects or their dependencies that need version update in projects that reference them.

| Package Name                              | Current Version | New Version | Description                                   |
|:------------------------------------------|:---------------:|:-----------:|:----------------------------------------------|
| Microsoft.Extensions.DependencyInjection  |      8.0.0      |   10.0.0    | Recommended for .NET 10                       |
| Microsoft.Extensions.Http                 |      8.0.0      |   10.0.0    | Recommended for .NET 10                       |

### Project upgrade details

This section contains details about each project upgrade and modifications that need to be done in the project.

#### RequesterMini.csproj modifications

Project properties changes:
  - Target framework should be changed from `net8.0` to `net10.0-windows`

NuGet packages changes:
  - Microsoft.Extensions.DependencyInjection should be updated from `8.0.0` to `10.0.0` (*recommended for .NET 10*)
  - Microsoft.Extensions.Http should be updated from `8.0.0` to `10.0.0` (*recommended for .NET 10*)
