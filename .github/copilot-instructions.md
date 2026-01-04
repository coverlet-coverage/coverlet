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
- **Coverage Requirement**: Overall 90% test coverage for all modules
- **Best Practices**:
  - Follow existing test patterns

### Test Generation Verification (Critical Rule)

**Before generating any test, you MUST:**

1. **Read and analyze the actual implementation code** - Never generate tests without examining the source code first
2. **Understand the complete logic flow** - Identify all code paths, edge cases, and error handling
3. **Design comprehensive test cases** that cover:
   - Happy path scenarios
   - Edge cases and boundary conditions
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

## Pull Request guidelines


