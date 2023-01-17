# Driver feature differences

All Coverlet drivers share the same coverage engine. Since version 3.0.0, we've consolidated versioning across drivers, so for every new release, all drivers will have the same version number.

We think that keeping the versions in sync better expresses the set of features every release will have. This does not mean that all drivers will support every functionality/feature or have the same behaviours, since they are limited by the context they're running in.

In the table below we keep track of main differences:

| Feature                            | MSBuild       | .NET Tool    |  DataCollectors  |
|:-----------------------------------|:--------------|--------------|------------------|
| .NET Core support(>= 2.0)          | Yes           | Yes          | Yes              |
| .NET Framework support(>= 4.6.1)   | Yes           | Yes          | Yes(since 3.0.0) |
| Show result on console             | Yes           | Yes          | No               |
| Deterministic reports output folder| Yes           | Yes          | No               |
| Merge reports                      | Yes           | Yes          | No               |
| Coverage threshold validation      | Yes           | Yes          | No               |
| Deterministic build support        | Yes           | No           | Yes              |

When possible, we advice you to use the collectors integration (VSTest engine integration), since it is fully integrated inside the test pipeline and does not suffer from the [known issues](KnownIssues.md) of the other drivers.
