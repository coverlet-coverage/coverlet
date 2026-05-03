# Coverlet Architecture Overview

This document summarizes dependencies, responsibilities, and constraints for the main Coverlet packages:

- `coverlet.core`
- `coverlet.collector`
- `coverlet.msbuild`
- `coverlet.console`
- `coverlet.MTP`

## Package dependency map

```mermaid
flowchart TB
    CORE["coverlet.core\n(instrumentation + reporting engine)"]

    COL["coverlet.collector\n(VSTest integration)"] --> CORE
    MSB["coverlet.msbuild\n(MSBuild integration)"] --> CORE
    CON["coverlet.console\n(Global tool)"] --> CORE
    MTP["coverlet.MTP\n(Microsoft Testing Platform integration)"] --> CORE

    VST["VSTest platform"] --> COL
    BUILD["MSBuild / dotnet test"] --> MSB
    CLI["CLI process wrapper"] --> CON
    MTPP["Microsoft.Testing.Platform"] --> MTP
```

## Responsibilities and constraints by package

| Package | Primary dependencies | Main functionality | Key limitations / constraints |
| :-- | :-- | :-- | :-- |
| `coverlet.core` | `Mono.Cecil`, globbing and reporting dependencies | Assembly instrumentation, hit tracking, filtering, line/branch/method coverage aggregation, multi-format report generation | Must operate on assemblies with symbols/PDBs; instrumentation modifies binaries during run; source resolution affects report quality |
| `coverlet.collector` | `coverlet.core`, VSTest Data Collector APIs | Hooks VSTest collector lifecycle, instruments before test execution, collects hits, emits attachments | Controlled by VSTest lifecycle and output layout (`TestResults/<guid>`); narrower feature set than msbuild/console in some scenarios |
| `coverlet.msbuild` | `coverlet.core`, MSBuild task APIs | Integrates through targets/tasks before and after tests, report generation, threshold enforcement, merge support | Sensitive to target ordering and command-line escaping; rebuilds between instrumentation and execution can invalidate coverage |
| `coverlet.console` | `coverlet.core`, `System.CommandLine` | Standalone orchestration around an external target command/process; instrumentation and report output | Requires no-rebuild execution flow; relies on graceful process shutdown for complete hit flushing |
| `coverlet.MTP` | `coverlet.core`, `Microsoft.Testing.Platform`, configuration stack | Extends MTP runner, instruments assemblies, exchanges state via environment/process lifecycle, generates reports | Architecture is tightly bound to MTP extension model and process lifecycle; current feature set has documented gaps (for example threshold/merge in current docs) |

## Integration execution pattern

```mermaid
sequenceDiagram
    participant I as Integration package
    participant C as coverlet.core
    participant T as Test execution host

    I->>C: Prepare instrumentation plan
    C->>C: Rewrite IL + create tracker metadata
    I->>T: Start test execution
    T-->>C: Persist hit data (during/after run)
    I->>C: Load hits + calculate coverage
    C-->>I: Coverage result(s)
    I-->>I: Write reports / apply integration-specific behavior
```
