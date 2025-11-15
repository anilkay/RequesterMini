# .NET 10 Upgrade Report

## Project target framework modifications

| Project name           | Old Target Framework | New Target Framework | Commits                          |
|:-----------------------|:--------------------:|:--------------------:|----------------------------------|
| RequesterMini.csproj   | net8.0               | net10.0-windows      | 596c1e13                         |

## NuGet Packages

| Package Name                              | Old Version | New Version | Commit Id                        |
|:------------------------------------------|:-----------:|:-----------:|----------------------------------|
| Microsoft.Extensions.DependencyInjection  | 8.0.0       | 10.0.0      | ef76546e                         |
| Microsoft.Extensions.Http                 | 8.0.0       | 10.0.0      | ef76546e                         |

## All commits

| Commit ID | Description                                                                                              |
|:----------|:---------------------------------------------------------------------------------------------------------|
| 203b3177  | Commit upgrade plan                                                                                      |
| 596c1e13  | Update RequesterMini.csproj to target net10.0-windows                                                    |
| ef76546e  | Update dependency versions in RequesterMini.csproj                                                       |
| 89116eeb  | 'Upgrade RequesterMini.csproj' adımı için son değişiklikleri kaydedin                                   |

## Next steps

- Build and test your application to ensure everything works correctly with .NET 10
- Review any deprecation warnings that may appear during compilation
- Update your CI/CD pipelines to use .NET 10 SDK
- Consider reviewing the [.NET 10 breaking changes documentation](https://learn.microsoft.com/en-us/dotnet/core/compatibility/10.0) for any additional compatibility concerns
