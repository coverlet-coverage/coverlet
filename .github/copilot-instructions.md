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
- Overall code test coverage for shipping projects (coverlet nuget packages) shall not below 90%

## Testing Requirements

- **Location**: `test/`
- **namespace** for tests must have the same root namespace as the code under test, with an additional `.Tests` suffix
- **Coverage Requirement**: Overall 90% test coverage for all modules
- **Best Practices**:
  - Follow existing test patterns

### Unit Test Guidelines (Critical Rules)

#### File System Abstraction (Mandatory)

**Unit tests MUST NOT use the file system directly.** Instead:

1. **Use `IFileSystem` abstraction** - All file operations must go through `Coverlet.Core.Abstractions.IFileSystem`
2. **Mock file system operations** - Use `Mock<IFileSystem>` in tests
3. **Simulate paths** - Use fake/simulated paths (e.g., `/fake/path/test.dll`) instead of creating real files
4. **Verify behavior** - Verify that code calls the abstracted methods correctly

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
mockFileSystem.Verify( x => x.WriteAllText( It.Is<string>(path => path.EndsWith("report.json")), It.IsAny<string>()), Times.Once);
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
_mockLogger.Verify(x => x.LogInformation(It.Is<string>(s => s.Contains("json"))), Times.Once); _mockLogger.Setup(x => x.LogWarning(It.IsAny<string>()));
```

✅ **CORRECT** - Mocks the underlying `Log` method:

```cs
// Verify LogInformation was called once
_mockLogger.Verify( x => x.Log( LogLevel.Information, It.IsAny<EventId>(), It.IsAny<It.IsAnyType>(), It.IsAny<Exception?>(), It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
// Verify LogInformation was called with a message containing "json"
_mockLogger.Verify( x => x.Log( LogLevel.Information, It.IsAny<EventId>(), It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("json")), It.IsAny<Exception?>(), It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
// Setup LogWarning behavior
_mockLogger.Setup( x => x.Log( LogLevel.Warning, It.IsAny<EventId>(), It.IsAny<It.IsAnyType>(), It.IsAny<Exception?>(), It.IsAny<Func<It.IsAnyType, Exception?, string>>()));
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
_mockLogger.Verify( x => x.Log( LogLevel.Debug, It.IsAny<EventId>(), It.IsAny<It.IsAnyType>(), It.IsAny<Exception?>(), It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
// Verify LogDebug was called with a message containing "xml"
_mockLogger.Verify( x => x.Log( LogLevel.Debug, It.IsAny<EventId>(), It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("xml")), It.IsAny<Exception?>(), It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
// Setup LogError behavior
_mockLogger.Setup( x => x.Log( LogLevel.Error, It.IsAny<EventId>(), It.IsAny<It.IsAnyType>(), It.IsAny<Exception?>(), It.IsAny<Func<It.IsAnyType, Exception?, string>>()));
```

**Key Points:**
1. Always use `ILogger.Log()` with the appropriate `LogLevel` instead of extension methods
2. Use `It.IsAny<It.IsAnyType>()` for the state parameter
3. Use `It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("text"))` to verify message content
4. Include all required parameters: `LogLevel`, `EventId`, state, `Exception?`, and formatter function
5. This applies to **all** extension methods, not just logging - if it's an extension method, mock the underlying interface method instead


#### API Verification Before Use (Mandatory)

**ALWAYS verify external API signatures before using them in tests.** Do NOT assume API structure.

**Before mocking any external API:**
1. Use `get_symbols_by_name` to find the actual interface definition
2. Use `code_search` to find existing usage patterns in the codebase
3. Verify parameter types, return types, and method signatures
4. Check if the API uses synchronous or asynchronous methods
5. Distinguish between different logger interfaces (they have different signatures)

**Critical Distinction - Two Different ILogger Interfaces:**

This codebase uses **TWO different ILogger interfaces** with different signatures:

1. **Microsoft.Testing.Platform.Logging.ILogger** (MTP Logger)
   - Uses **async methods** with simple signatures
   - Methods: `LogInformationAsync(string)`, `LogErrorAsync(string)`, `LogWarningAsync(string)`, etc.
   - **NO EventId parameter**
   - Used in: `coverlet.MTP` projects

2. **Microsoft.Extensions.Logging.ILogger** (MEL Logger)  
   - Uses `Log()` method with EventId
   - Extension methods: `LogInformation()`, `LogError()`, etc. call underlying `Log()`
   - Used in: Other logging scenarios

**Common Pitfall - Microsoft.Testing.Platform.Logging.ILogger:**

❌ **INCORRECT** - Assumes `EventId` parameter (which doesn't exist in MTP Logger):


```cs
// BAD - Microsoft.Testing.Platform.Logging.ILogger does NOT have EventId
_mockLogger.Verify( x => x.Log( LogLevel.Information, It.IsAny<EventId>(), // ⚠️ EventId does NOT exist in MTP ILogger
     It.IsAny<It.IsAnyType>(), It.IsAny<Exception?>(), It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
```

✅ **CORRECT** - Uses actual MTP ILogger API signature (async methods):

```cs
// GOOD - Microsoft.Testing.Platform.Logging.ILogger uses simple async methods
_mockLogger.Verify(x => x.LogInformationAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
_mockLogger.Verify(x => x.LogErrorAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
_mockLogger.Verify(x => x.LogWarningAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
// To verify message content:
_mockLogger.Verify( x => x.LogInformationAsync( It.Is<string>(s => s.Contains("expected text")), It.IsAny<CancellationToken>()), Times.Once);
// For synchronous extension methods (from LoggerExtensions):
_mockLogger.Verify(x => x.LogInformation(It.IsAny<string>()), Times.Once);
_mockLogger.Verify( x => x.LogInformation(It.Is<string>(s => s.Contains("Coverage reports generated"))), Times.Once);
```


**Verification Checklist:**
- [ ] I have searched for the interface definition using `get_symbols_by_name`
- [ ] I have reviewed existing usage in the codebase using `code_search`
- [ ] I have verified the actual method signatures
- [ ] I have checked existing adapter implementations (e.g., `CoverletLoggerAdapter.cs`)
- [ ] I have confirmed which ILogger interface is being used (MTP vs MEL)
- [ ] My mock setup matches the actual API structure
- [ ] I am NOT confusing Microsoft.Testing.Platform.Logging.ILogger with Microsoft.Extensions.Logging.ILogger

#### Cross-Reference with Actual Implementations

When mocking interfaces, **reference actual adapter implementations** in the codebase:

- `src/coverlet.MTP/Logging/CoverletLoggerAdapter.cs` - Shows how to use `Microsoft.Testing.Platform.Logging.ILogger`
- `src/coverlet.collector/DataCollection/CoverletLogger.cs` - Shows how to use VSTest platform logger  
- `src/coverlet.console/Logging/ConsoleLogger.cs` - Shows coverlet's internal `ILogger` implementation
- `src/coverlet.core/Abstractions/ILogger.cs` - Coverlet's internal logger interface

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

### Moq Testing Rules (Critical - Prevents Runtime Errors)

**NEVER use extension methods in Moq `Setup()` or `Verify()` calls.**

Extension methods are static methods that cannot be intercepted by Moq. Using them will result in a `System.NotSupportedException` at runtime with the message: "Unsupported expression: Extension methods may not be used in setup / verification expressions."

**Common Extension Methods That CANNOT Be Mocked:**
- **Microsoft.Extensions.Logging.ILogger** extension methods: `LogInformation()`, `LogWarning()`, `LogError()`, `LogDebug()`, `LogTrace()`, `LogCritical()`
- LINQ extension methods on interfaces (e.g., `IEnumerable<T>.Where()`, `First()`, `Any()`)
- Any custom extension methods

**Important Note:** This limitation primarily affects **Microsoft.Extensions.Logging.ILogger**. The **Microsoft.Testing.Platform.Logging.ILogger** interface methods can be mocked directly (though it also has extension methods in LoggerExtensions that should not be mocked).

**Solution:** For Microsoft.Extensions.Logging.ILogger, mock the underlying `Log()` method that extension methods call internally.

#### Example: Mocking Microsoft.Extensions.Logging.ILogger

❌ **INCORRECT** - Will throw `NotSupportedException`:

```cs
// BAD - This will FAIL at runtime for Microsoft.Extensions.Logging.ILogger
// LogInformation is an extension method
_mockLogger.Verify(x => x.LogInformation(It.IsAny<string>()), Times.Once);
_mockLogger.Setup(x => x.LogWarning(It.IsAny<string>()));
```

✅ **CORRECT** - Mocks the underlying `Log` method:
```cs
// GOOD - For Microsoft.Extensions.Logging.ILogger, mock the Log method
// Verify LogInformation was called once
_mockLogger.Verify( x => x.Log( LogLevel.Information, It.IsAny<EventId>(), It.IsAny<It.IsAnyType>(), It.IsAny<Exception?>(), It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
// Verify LogInformation was called with a message containing "json"
_mockLogger.Verify( x => x.Log( LogLevel.Information, It.IsAny<EventId>(), It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("json")), It.IsAny<Exception?>(), It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
// Setup LogWarning behavior
_mockLogger.Setup( x => x.Log( LogLevel.Warning, It.IsAny<EventId>(), It.IsAny<It.IsAnyType>(), It.IsAny<Exception?>(), It.IsAny<Func<It.IsAnyType, Exception?, string>>()));
```

**Key Points:**
1. **Identify which ILogger** you're working with (Microsoft.Extensions.Logging vs Microsoft.Testing.Platform.Logging)
2. For **Microsoft.Extensions.Logging.ILogger**: Use `ILogger.Log()` with `LogLevel` and `EventId`
3. For **Microsoft.Testing.Platform.Logging.ILogger**: Use direct methods like `LogInformationAsync()` or sync extension methods
4. Use `It.IsAny<It.IsAnyType>()` for the state parameter (MEL only)
5. Use `It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("text"))` to verify message content (MEL only)
6. This applies to **all** extension methods, not just logging

### Test Generation Verification (Critical Rule)

**Before generating any test, you MUST:**

1. **Read and analyze the actual implementation code** - Never generate tests without examining the source code first
2. **Understand the complete logic flow** - Identify all code paths, edge cases, and error handling
3. **Design comprehensive test cases** that cover:
   - Happy path scenarios
   - Tests for issue resolution shall include edge cases and boundary conditions
   - Error conditions and exception handling
   - All branches and code paths
4. **Never generate placeholder or blind tests** - Each test must validate specific, understood behavior
5. **Preserve existing tests unless** you can demonstrate the replacement test provides:
   - Better coverage of the same scenario
   - More accurate assertions
   - Clearer test intent
6. **When modifying existing tests:**
   - Document why the change improves the test
   - Ensure no coverage is lost
   - Verify all assertions remain valid

**Red Flags for Duplicate Tests:**
- Multiple test methods testing the same validation with different input values (use `[Theory]` instead)
- Test names that differ only in the input value (should be parameterized)
- Tests that assert the same behavior on the same method
- Test classes with overlapping responsibilities for the same production code

**Validation Checklist Before Generating Tests:**
- [ ] I have searched for existing tests using `code_search`
- [ ] I have reviewed existing test files in the same test project
- [ ] I have identified which existing tests cover similar scenarios
- [ ] I have documented which proposed tests are redundant
- [ ] I can justify why each new test adds unique value
- [ ] I have considered refactoring existing tests instead of adding duplicates
- [ ] I have verified the actual API signatures being tested
- [ ] I have used the correct mocking approach for the specific ILogger interface
- [ ] I have used mocked file system abstractions instead of real file I/O

## Summary of Key Testing Rules

1. **Always use `IFileSystem` abstraction** - Never use `File`, `Directory`, or `Path` static methods directly in tests
2. **Always verify API signatures** - Use `get_symbols_by_name` and `code_search` before mocking external APIs
3. **Know your ILogger** - Microsoft.Testing.Platform.Logging.ILogger ≠ Microsoft.Extensions.Logging.ILogger
4. **Avoid extension methods in Moq** - They cannot be intercepted and will cause runtime exceptions
5. **Use simulated paths** - Always use fake paths like `/fake/path/test.dll` in test mocks
6. **Verify existing tests** - Check for duplicates before adding new test methods
7. **Use Theory for parameterized tests** - Don't create multiple test methods for different input values

### Test Generation Verification (Critical Rule)

**Before generating any test, you MUST:**

1. **Read and analyze the actual implementation code** - Never generate tests without examining the source code first
2. **Understand the complete logic flow** - Identify all code paths, edge cases, and error handling
3. **Design comprehensive test cases** that cover:
   - Happy path scenarios
   - Tests for issue resolution shall include edge cases and boundary conditions.
   - Error conditions and exception handling
   - All branches and code paths
4. **Never generate placeholder or blind tests** - Each test must validate specific, understood behavior
5. **Preserve existing tests unless** you can demonstrate the replacement test provides:
   - Better coverage of the same scenario
   - More accurate assertions
   - Clearer test intent
6. **When modifying existing tests:**
   - Document why the change improves the test
   - Ensure no coverage is lost
   - Verify all assertions remain valid

**Red Flags for Duplicate Tests:**
- Multiple test methods testing the same validation with different input values (use `[Theory]` instead)
- Test names that differ only in the input value (should be parameterized)
- Tests that assert the same behavior on the same method
- Test classes with overlapping responsibilities for the same production code

**Validation Checklist Before Generating Tests:**
- [ ] I have searched for existing tests using `code_search`
- [ ] I have reviewed existing test files in the same test project
- [ ] I have identified which existing tests cover similar scenarios
- [ ] I have documented which proposed tests are redundant
- [ ] I can justify why each new test adds unique value
- [ ] I have considered refactoring existing tests instead of adding duplicates

