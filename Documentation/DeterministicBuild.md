# Deterministic build

Support for deterministic builds is available **only** in the `msbuild` (`/p:CollectCoverage=true`) and `collectors` (`--collect:"XPlat Code Coverage"`) drivers. Deterministic builds are important because they enable verification that the resulting binary was built from the specified sources. For more information on how to enable deterministic builds, I recommend you to take a look at [Claire Novotny](https://github.com/clairernovotny)'s [guide](https://github.com/clairernovotny/DeterministicBuilds).

From a coverage perspective, deterministic builds create some challenges because coverage tools usually need access to complete source file metadata (ie. local path) during instrumentation and report generation. These files are reported inside the `.pdb` files, where debugging information is stored.

In local (non-CI) builds, metadata emitted to pdbs are not "deterministic", which means that source files are reported with their full paths. For example, when we build the same project on different machines we'll have different paths emitted inside pdbs, hence, builds are "non deterministic".

As explained above, to improve the level of security of generated artifacts (for instance, DLLs inside the NuGet package), we need to apply some signature (signing with certificate) and validate before usage to avoid possible security issues like tampering.

Finally, thanks to deterministic CI builds (with the `ContinuousIntegrationBuild` property set to `true`) plus signature we can validate artifacts and be sure that the binary was built from specific sources (because there is no hard-coded variable metadata, like paths from different build machines).

## Deterministic report

Coverlet supports also deterministic reports(for now only for cobertura coverage format).
If you include `DeterministicReport` parameters for `msbuild` and `collectors` integrations resulting report will be like:

```xml
<?xml version="1.0" encoding="utf-8"?>
<coverage line-rate="0.8571" branch-rate="0.5" version="1.9" timestamp="1612702997" lines-covered="6" lines-valid="7" branches-covered="1" branches-valid="2">
  <sources />
  <packages>
    <package name="MyLibrary" line-rate="0.8571" branch-rate="0.5" complexity="3">
      <classes>
        <class name="MyLibrary.Hello" filename="/_/MyLibrary/Hello.cs" line-rate="0.8571" branch-rate="0.5" complexity="3">
          <methods>
...
```

As you can see we have empty `<sources />` element and the `filename` start with well known deterministic fragment `/_/...`

You can follow our [step-by-step sample](Examples.md)

Feel free to file an issue in case you run into any trouble!
