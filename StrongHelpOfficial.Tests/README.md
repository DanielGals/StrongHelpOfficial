# StrongHelpOfficial Test Suite

## Overview
Comprehensive test suite for the StrongHelpOfficial loan management system.

## Test Structure

### Controllers/
- **HomeControllerTests.cs**: Tests for public pages (Index, About, Contact, etc.)
- **AuthControllerTests.cs**: Tests for authentication and session management
- **ApplyForLoanControllerTests.cs**: Tests for loan application functionality

### Models/
- **ViewModelTests.cs**: Tests for view model validation and properties

### Integration/
- **BasicIntegrationTests.cs**: End-to-end tests for public endpoints

## Running Tests

```bash
# Run all tests
dotnet test

# Run with detailed output
dotnet test --logger "console;verbosity=detailed"

# Run specific test class
dotnet test --filter "FullyQualifiedName~HomeControllerTests"

# Run with code coverage
dotnet test --collect:"XPlat Code Coverage"
```

## Test Coverage

Current test coverage includes:
- ✅ Home controller actions
- ✅ Authentication flow
- ✅ Session management
- ✅ View model validation
- ✅ Public endpoint integration tests
- ✅ Loan application submission

## Adding New Tests

1. Create test class in appropriate folder
2. Follow naming convention: `[ClassName]Tests.cs`
3. Use Arrange-Act-Assert pattern
4. Mock external dependencies (DB, configuration, etc.)

## Dependencies

- xUnit: Testing framework
- Moq: Mocking library
- Microsoft.AspNetCore.Mvc.Testing: Integration testing
