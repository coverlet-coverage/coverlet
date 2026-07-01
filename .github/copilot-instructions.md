# GitHub Copilot Instructions

This is a .NET based repository that contains the coverlet projects for code coverage collection. Please follow these guidelines when contributing:

## Code Standards

You MUST follow all code-formatting and naming conventions defined in [`.editorconfig`](../.editorconfig).

In addition to the rules enforced by `.editorconfig`, you SHOULD:

- Favor style and conventions that are consistent with the existing codebase.
- Prefer file-scoped namespace declarations and single-line using directives.
- Ensure that the final return statement of a method is on its own line.
- Use pattern matching and switch expressions wherever possible.
- Use `nameof` instead of string literals when referring to member names.
- Always use `is null` or `is not null` instead of `== null` or `!= null`.
- Trust the C# null annotations and don't add null checks when the type system says a value cannot be null.
- Prefer `?.` if applicable (e.g. `scope?.Dispose()`).
- Use `ObjectDisposedException.ThrowIf` where applicable.
- Respect StyleCop.Analyzers rules, in particular:
  - SA1028: Code must not contain trailing whitespace
  - SA1316: Tuple element names should use correct casing
  - SA1518: File is required to end with a single newline character

## Testing Guidelines

- Tests for coverlet MUST use xunit.v3.
- Overall code test coverage for shipping projects (coverlet nuget packages) shall not be below 90%.

## Testing Requirements

- **Location**: `test/`
- **namespace** for tests must have the same root namespace as the code under test, with an additional `.Tests` suffix.
- **Coverage Requirement**: Overall 90% test coverage for all modules.
- **Best Practices**:
  - Follow existing test patterns.
  - New test samples for coverlet.core.coverage.tests must be added at the end of the source file, and existing tests should not be rearranged because line numbers are used within tests.

### Unit Test Guidelines (Critical Rules)

#### File System Abstraction (Mandatory)

**Unit tests MUST NOT use the file system directly.** Instead:

1. **Use `IFileSystem` abstraction** - All file operations must go through `Coverlet.Core.Abstractions.IFileSystem`.
2. **Mock file system operations** - Use `Mock<IFileSystem>` in tests.
3. **Simulate paths** - Use fake/simulated paths (e.g., `/fake/path/test.dll`) instead of creating real files.
4. **Verify behavior** - Verify that code calls the abstracted methods correctly.

**Examples:**

❌ **INCORRECT** - Direct file system usage:

```cs
// BAD - Creates real files and directories
File.WriteAllText("report.json", content); Directory.CreateDirectory("reports");
bool exists = File.Exists("test.dll");
```

✅ **CORRECT** - Mock file system:

```cs
// GOOD - Uses mocked abstraction with simulated paths
var mockFileSystem = new Mock<IFileSystem>();
mockFileSystem.Setup(x => x.Exists("/fake/path/test.dll")).Returns(true);
mockFileSystem.Setup(x => x.Exists("/fake/reports")).Returns(true);
mockFileSystem.Setup(x => x.WriteAllText(It.IsAny<string>(), It.IsAny<string>()));
// Verify the mock was called correctly
mockFileSystem.Verify(x => x.WriteAllText(It.Is<string>(path => path.EndsWith("report.json")), It.IsAny<string>()), Times.Once);
```

### Moq Testing Rules (Critical - Prevents Runtime Errors)

**NEVER use extension methods in Moq `Setup()` or `Verify()` calls.**

Extension methods are static methods that cannot be intercepted by Moq. Using them will result in a `System.NotSupportedException` at runtime with the message: "Unsupported expression: Extension methods may not be used in setup / verification expressions."

**Common Extension Methods That CANNOT Be Mocked:**
- `ILogger.LogInformation()`, `LogWarning()`, `LogError()`, `LogDebug()`, `LogTrace()`, `LogCritical()`
- LINQ extension methods on interfaces (e.g., `IEnumerable<T>.Where()`, `First()`, `Any()`)
- Any custom extension methods

**Solution:** Mock the underlying interface method that the extension method calls internally.

#### Example: Mocking ILogger

❌ **INCORRECT** - Will throw `NotSupportedException`:

```cs
// This will FAIL at runtime
_mockLogger.Verify(x => x.LogInformation(It.IsAny<string>()), Times.Once);
_mockLogger.Verify(x => x.LogInformation(It.Is<string>(s => s.Contains("json"))), Times.Once);
_mockLogger.Setup(x => x.LogWarning(It.IsAny<string>()));
```

✅ **CORRECT** - Mocks the underlying `Log` method:

```cs
// Verify LogInformation was called once
_mockLogger.Verify(x => x.Log(LogLevel.Information, It.IsAny<EventId>(), It.IsAny<It.IsAnyType>(), It.IsAny<Exception?>(), It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
// Verify LogInformation was called with a message containing "json"
_mockLogger.Verify(x => x.Log(LogLevel.Information, It.IsAny<EventId>(), It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("json")), It.IsAny<Exception?>(), It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
// Setup LogWarning behavior
_mockLogger.Setup(x => x.Log(LogLevel.Warning, It.IsAny<EventId>(), It.IsAny<It.IsAnyType>(), It.IsAny<Exception?>(), It.IsAny<Func<It.IsAnyType, Exception?, string>>()));
```

#### Example: Mocking LogDebug

❌ **INCORRECT**:

```cs
// This will FAIL at runtime
_mockLogger.Verify(x => x.LogDebug(It.IsAny<string>()), Times.Once);
```

✅ **CORRECT** - Mocks the underlying `Log` method:

```cs
// Verify LogDebug was called once
_mockLogger.Verify(x => x.Log(LogLevel.Debug, It.IsAny<EventId>(), It.IsAny<It.IsAnyType>(), It.IsAny<Exception?>(), It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
// Verify LogDebug was called with a message containing "xml"
_mockLogger.Verify(x => x.Log(LogLevel.Debug, It.IsAny<EventId>(), It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("xml")), It.IsAny<Exception?>(), It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
// Setup LogError behavior
_mockLogger.Setup(x => x.Log(LogLevel.Error, It.IsAny<EventId>(), It.IsAny<It.IsAnyType>(), It.IsAny<Exception?>(), It.IsAny<Func<It.IsAnyType, Exception?, string>>()));
```

**Key Points:**
1. Always use `ILogger.Log()` with the appropriate `LogLevel` instead of extension methods.
2. Use `It.IsAny<It.IsAnyType>()` for the state parameter.
3. Use `It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("text"))` to verify message content.
4. Include all required parameters: `LogLevel`, `EventId`, state, `Exception?`, and formatter function.
5. This applies to **all** extension methods, not just logging - if it's an extension method, mock the underlying interface method instead.

#### API Verification Before Use (Mandatory)

**ALWAYS verify external API signatures before using them in tests.** Do NOT assume API structure.

**Before mocking any external API:**
1. Use `get_symbols_by_name` to find the actual interface definition.
2. Use `code_search` to find existing usage patterns in the codebase.
3. Verify parameter types, return types, and method signatures.
4. Check if the API uses synchronous or asynchronous methods.
5. Distinguish between different logger interfaces (they have different signatures).

**Critical Distinction - Two Different ILogger Interfaces:**

This codebase uses **TWO different ILogger interfaces** with different signatures:

1. **Microsoft.Testing.Platform.Logging.ILogger** (MTP Logger)
   - Uses **async methods** with simple signatures.
   - Methods: `LogInformationAsync(string)`, `LogErrorAsync(string)`, `LogWarningAsync(string)`, etc.
   - **NO EventId parameter**.
   - Used in: `coverlet.MTP` projects.

2. **Microsoft.Extensions.Logging.ILogger** (MEL Logger)
   - Uses `Log()` method with EventId.
   - Extension methods: `LogInformation()`, `LogError()`, etc. call underlying `Log()`.
   - Used in: Other logging scenarios.

**Common Pitfall - Microsoft.Testing.Platform.Logging.ILogger:**

❌ **INCORRECT** - Assumes `EventId` parameter (which doesn't exist in MTP Logger):

```cs
// BAD - Microsoft.Testing.Platform.Logging.ILogger does NOT have EventId
_mockLogger.Verify(x => x.Log(LogLevel.Information, It.IsAny<EventId>(), // ⚠️ EventId does NOT exist in MTP LOGGER
     It.IsAny<It.IsAnyType>(), It.IsAny<Exception?>(), It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
```

✅ **CORRECT** - Uses actual MTP ILogger API signature (async methods):

```cs
// GOOD - Microsoft.Testing.Platform.Logging.ILogger uses simple async methods
_mockLogger.Verify(x => x.LogInformationAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
_mockLogger.Verify(x => x.LogErrorAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
_mockLogger.Verify(x => x.LogWarningAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
// To verify message content:
_mockLogger.Verify(x => x.LogInformationAsync(It.Is<string>(s => s.Contains("expected text")), It.IsAny<CancellationToken>()), Times.Once);
 // For synchronous LoggerExtensions (extension methods):
 // NOTE: These are extension methods and cannot be verified with Moq; verify the underlying Log(...) call instead.
 _mockLogger.Verify(x => x.Log(LogLevel.Information, It.Is<string>(s => s.Contains("Coverage reports generated")), It.IsAny<Exception?>(), It.IsAny<Func<string, Exception?, string>>()), Times.Once);
```

**Verification Checklist:**
- [ ] I have searched for the interface definition using `get_symbols_by_name`.
- [ ] I have reviewed existing usage in the codebase using `code_search`.
- [ ] I have verified the actual method signatures.
- [ ] I have checked existing adapter implementations (e.g., `CoverletLoggerAdapter.cs`).
- [ ] I have confirmed which ILogger interface is being used (MTP vs MEL).
- [ ] My mock setup matches the actual API structure.
- [ ] I am NOT confusing Microsoft.Testing.Platform.Logging.ILogger with Microsoft.Extensions.Logging.ILogger.

#### Cross-Reference with Actual Implementations

When mocking interfaces, **reference actual adapter implementations** in the codebase:

- `src/coverlet.MTP/Logging/CoverletLoggerAdapter.cs` - Shows how to use `Microsoft.Testing.Platform.Logging.ILogger`.
- `src/coverlet.collector/DataCollection/CoverletLogger.cs` - Shows how to use VSTest platform logger.
- `src/coverlet.console/Logging/ConsoleLogger.cs` - Shows coverlet's internal `ILogger` implementation.
- `src/coverlet.core/Abstractions/ILogger.cs` - Coverlet's internal logger interface.

**Example from CoverletLoggerAdapter.cs:**

```csharp
// Shows actual MTP ILogger usage - simple methods, no EventId
public void LogInformation(string message, bool important = false)
{
    if (important)
    {
        _logger.LogInformation($"[Important] {message}");
    }
    else
    {
        _logger.LogInformation(message);
    }
}
```
### Test Generation Verification (Critical Rule)

**Before generating any test, you MUST:**

1. **Read and analyze the actual implementation code** - Never generate tests without examining the source code first.
2. **Understand the complete logic flow** - Identify all code paths, edge cases, and error handling.
3. **Design comprehensive test cases** that cover:
   - Happy path scenarios.
   - Tests for issue resolution shall include edge cases and boundary conditions.
   - Error conditions and exception handling.
   - All branches and code paths.
4. **Never generate placeholder or blind tests** - Each test must validate specific, understood behavior.
5. **Preserve existing tests unless** you can demonstrate the replacement test provides:
   - Better coverage of the same scenario.
   - More accurate assertions.
   - Clearer test intent.
6. **When modifying existing tests:**
   - Document why the change improves the test.
   - Ensure no coverage is lost.
   - Verify all assertions remain valid.

**Red Flags for Duplicate Tests:**
- Multiple test methods testing the same validation with different input values (use `[Theory]` instead).
- Test names that differ only in the input value (should be parameterized).
- Tests that assert the same behavior on the same method.
- Test classes with overlapping responsibilities for the same production code.

**Validation Checklist Before Generating Tests:**
- [ ] I have searched for existing tests using `code_search`.
- [ ] I have reviewed existing test files in the same test project.
- [ ] I have identified which existing tests cover similar scenarios.
- [ ] I have documented which proposed tests are redundant.
- [ ] I can justify why each new test adds unique value.
- [ ] I have considered refactoring existing tests instead of adding duplicates.
- [ ] I have verified the actual API signatures being tested.
- [ ] I have used the correct mocking approach for the specific ILogger interface.
- [ ] I have used mocked file system abstractions instead of real file I/O.

## Summary of Key Testing Rules

1. **Always use `IFileSystem` abstraction** - Never use `File`, `Directory`, or `Path` static methods directly in tests.
2. **Always verify API signatures** - Use `get_symbols_by_name` and `code_search` before mocking external APIs.
3. **Know your ILogger** - Microsoft.Testing.Platform.Logging.ILogger ≠ Microsoft.Extensions.Logging.ILogger.
4. **Avoid extension methods in Moq** - They cannot be intercepted and will cause runtime exceptions.
5. **Use simulated paths** - Always use fake paths like `/fake/path/test.dll` in test mocks.
6. **Verify existing tests** - Check for duplicates before adding new test methods.
7. **Use Theory for parameterized tests** - Don't create multiple test methods for different input values.

## Issue-Specific Guidelines

- For issue #1965, identify problematic assemblies before instrumentation and skip them, rather than relying on partial-restore/non-fatal restore behavior after failure.
- For assembly-level instrumentation viability, preflight logic should only check lock and resolvability, not PDB/source-based exclusion; PDB/source exclusion remains handled by existing assembly-without-sources filtering via CanInstrument/options.
- **Prefer calling `instrumenter.CanInstrument()` before preflight** so assemblies already excluded by existing coverage filters (no PDB/no local sources) skip preflight probing.

## Documentation Guidelines for Issue Resolution

### Documentation Limitation (Critical Rule)

**When resolving issues, limit documentation to ONE comprehensive document ONLY.**

#### Rule
- Create a **single, well-organized proposal or resolution document** per issue
- Document location: **`Documentation/Plans/Issue-[IssueNumber]-Resolution.md`**
- This folder (`Documentation/Plans/`) is Git-ignored and local-only; documents are NOT uploaded to GitHub
- No multiple summary documents, guides, or indices
- No separate reference files or supporting documentation

#### What Goes Into the Single Document
The one comprehensive document MUST include:

1. **Executive Summary** (top section)
   - Problem statement
   - Solution summary
   - Key metrics/results

2. **Technical Analysis**
   - Root cause analysis
   - Why it happens
   - IL/code examples where relevant

3. **Solution Design**
   - How the fix works
   - Architecture/approach
   - Integration points

4. **Implementation Details**
   - Code changes
   - Files modified/added
   - Line-by-line breakdown

5. **Test Coverage**
   - Test scenarios
   - Test code samples
   - Expected results

6. **Validation Strategy**
   - Before/after comparison
   - Coverage metrics
   - Build/test verification

7. **Deployment Path**
   - Steps to deploy
   - Backward compatibility notes
   - Related issues/PRs

#### Naming Convention
- **Format:** `Issue-[NUMBER]-Resolution.md`
- **Example:** `Documentation/Plans/Issue-1969-Resolution.md`
- **Location:** `Documentation/Plans/` (ignored by Git)

#### What NOT to Do
❌ Don't create multiple summary documents (`EXECUTIVE_BRIEFING.md`, `WORK_PACKAGE_SUBMISSION.md`, etc.)
❌ Don't create separate guides (`MASTER_GUIDE.md`, `QUICK_REFERENCE_GUIDE.md`, etc.)
❌ Don't create navigation indices or indices (`DOCUMENTATION_INDEX.md`, `COMPLETE_DOCUMENT_INDEX.md`, etc.)
❌ Don't create reference documents (`ANALYSIS_AND_RESOLUTION.md`, `IMPLEMENTATION_SUMMARY.md`, etc.)
❌ Don't create multiple specialized documents (`STATUS_REPORT.md`, `VERIFICATION_CHECKLIST.md`, etc.)

**Result:** Developers see a single, focused, comprehensive proposal in their workspace — not a sprawling documentation package.

#### Template Structure
```markdown
# Issue #[NUMBER]: [Short Description] - Resolution

## Executive Summary
- Problem
- Solution
- Results

## Technical Analysis
- Root cause
- Why it happens
- Examples

## Solution Design
- Approach
- Integration points
- Files affected

## Implementation
- Code changes
- Before/after examples

## Testing
- Test scenarios
- Expected results

## Validation
- Build status
- Test status
- Coverage metrics

## Deployment
- Steps
- Compatibility notes
```

#### Example
For Issue #1969, create one document:
- **File:** `Documentation/Plans/Issue-1969-Resolution.md` (~3-5 KB, comprehensive)
- **NOT:** 15+ separate markdown files in the root directory

This keeps the repository clean, the documentation focused, and makes it easy for reviewers to understand the issue and resolution without navigating multiple files.

### Documentation Creation (Critical Rule - Always Ask First)

**ALWAYS ask the user BEFORE creating any documentation files.**

#### Rule: Explicit Approval Required
- ❌ **NEVER create documentation files without explicit user request**
- ✅ **ALWAYS ask:** "Should I create documentation for [specific purpose]?"
- ✅ **Wait for approval** before generating any files
- ✅ **Clarify scope:** "Would you like: [brief description]?"
- ✅ **Suggest alternatives:** "Would code comments be sufficient instead?"

#### Rationale
Documentation is only valuable if explicitly requested. Unsolicited documentation:
- Creates unnecessary files
- Clutters the workspace
- Wastes developer time
- Adds maintenance burden
- Dilutes focus on code quality

#### What Counts as "Documentation"
The following require explicit approval:
- ✅ `.md` files (markdown, guides, proposals)
- ✅ `.txt` files (reference documents)
- ✅ `.rst` files (reStructuredText)
- ✅ Architecture diagrams or explanations
- ✅ README files or guides
- ✅ Summary or index documents
- ✅ Any file intended for human reading (not code)

#### What Does NOT Require Approval
The following are code and do not need explicit documentation approval:
- ✅ Source code files (`.cs`)
- ✅ Test files (`.cs`)
- ✅ Configuration files (`.json`, `.xml`, `.config`)
- ✅ Build scripts (`.ps1`, `.sh`)
- ✅ Code comments (part of code files)

#### Workflow Example

**❌ WRONG - Creating docs without asking:**
```
User: "Implement feature X"
Copilot: [Creates feature code]
         [Also creates: Feature_Guide.md, Architecture.md, Quick_Start.md]
         "Done!"
User: "I don't need all these docs..."
```

**✅ RIGHT - Asking before creating docs:**
```
User: "Implement feature X"
Copilot: [Creates feature code]
         "Code implementation complete. Would you like me to create 
          documentation? (e.g., usage guide, implementation details)"
User: "No, code is clear enough. Just add inline comments if needed."
Copilot: [Adds code comments where helpful]
         "Done!"
```

#### When to Ask

**Always ask BEFORE creating:**
1. Any `.md`, `.txt`, or `.rst` file
2. Summary or overview documents
3. Navigation guides or indices
4. Quick-reference materials
5. Analysis or proposal documents
6. Anything outside the primary code deliverable

**OK without asking:**
1. Code comments (within `.cs` files)
2. XML documentation (within `.cs` files)
3. Inline explanations (within code)
4. Test documentation (within test files)

#### Suggested Questions

When you're considering creating documentation, ask the user:

✅ "Should I create a [document type] explaining [purpose]?"
✅ "Would you like documentation for [specific feature/fix]?"
✅ "Should I document [aspect]? If so, what format?"
✅ "Rather than creating a guide, should I add code comments instead?"
✅ "Is a README needed for this component, or is the code self-explanatory?"

#### Exception: Auto-Generated Documentation

The only documentation created without asking:
- ✅ Code comments within source files (when improving code clarity)
- ✅ XML doc comments on public methods (when required by standards)
- ✅ Inline explanations for complex logic

All other documentation requires explicit user approval.
