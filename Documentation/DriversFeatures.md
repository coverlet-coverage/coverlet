# Driver feature differences

All Coverlet drivers share the same coverage engine. Since version 3.0.0, we've consolidated versioning across drivers, so for every new release, all drivers will have the same version number.

We think that keeping the versions in sync better expresses the set of features every release will have. This does not mean that all drivers will support every functionality/feature or have the same behaviors, since they are limited by the context they're running in.

In the table below we keep track of main differences:

| Feature                            | MSBuild       | .NET Tool    | VS DataCollector  | MTP Extension |
|:-----------------------------------|:--------------|:-------------|:------------------|:--------------|
| .NET Core support(>= 8.0)          | Yes           | Yes          | Yes               | Yes           |
| .NET Framework support(>= 4.7.2)   | Yes           | Yes          | Yes(since 3.0.0)  | Yes           |
| Show result on console             | Yes           | Yes          | No                | No            |
| Deterministic reports output folder| Yes           | Yes          | No                | No            |
| Merge reports                      | Yes           | Yes          | No                | No            |
| Coverage threshold validation      | Yes           | Yes          | No                | No            |
| Deterministic build support        | Yes           | No           | Yes               | Yes           |

> [!TIP]
> The new coverlet.MTP extension should be used for new test projects. This package supports the modern **Microsoft Test Platform** (see [Microsoft.Testing.Platform and VSTest comparison](https://learn.microsoft.com/en-us/dotnet/core/testing/microsoft-testing-platform-vs-vstest))

> [!NOTE]
> When possible, we advice you to use the collectors integration (VSTest engine integration), since it is fully integrated inside the test pipeline and does not suffer from the [known issues](KnownIssues.md) of the other drivers.
