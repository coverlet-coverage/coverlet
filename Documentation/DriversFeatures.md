# Drivers features differences

Since the beginnig all coverlet drivers shared the same coverage engine.  
Since version 3.0.0 we decided to consolidate versioning across drivers so for every new release drivers will have same version.  
We think that keep version in sync express better the set of features every release will have.  
By the way not all drivers support all functionality/feature or have the same behaviours and this is related to the context they're running.  
In the table below we keep track of main differences

| Feature  | MsBuild       | .NET Tool    |  DataCollectors |
|----------|:-------------:|-------------:|----------------:|
| .NET Core support(>= 2.0) | Yes |    Yes        |Yes            |
| .NET Framework support(>= 4.6.1) | Yes |    Yes        |Yes(since 3.0.0)            |
| Show result on console |   Yes | Yes         |         No        |
| Deterministic reports output folder |    Yes   |   Yes        |No            |
| Merge reports |    Yes   |   Yes        |No            |
| Coverage threshold validation |    Yes   |   Yes        |No            |


If possible we advice to use collectors integration(vstest engine integration), because they're fully integrated inside test pipeline and does not suffer of [known issue](KnownIssues.md)