using FluentAssertions;
using StressTestApp.Server.Shared.Primitives.Result;
using StressTestApp.Server.Shared.Primitives.Errors;

namespace StressTestApp.Tests.Shared.Primitives;

public class ResultTests
{
    [Fact]
    public void Ok_CreatesSuccessResult()
    {
        // Act
        var result = Result<int, Error>.Ok(42);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(42);
    }

    [Fact]
    public void Fail_CreatesFailureResult()
    {
        // Arrange
        var error = Error.Validation("Invalid input");

        // Act
        var result = Result<int, Error>.Fail(error);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(error);
    }

    [Fact]
    public void Value_OnFailure_ThrowsInvalidOperationException()
    {
        // Arrange
        var result = Result<int, Error>.Fail(Error.Unexpected());

        // Act
        var act = () => result.Value;

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Cannot access Value of a failed Result.");
    }

    [Fact]
    public void Error_OnSuccess_ThrowsInvalidOperationException()
    {
        // Arrange
        var result = Result<int, Error>.Ok(42);

        // Act
        var act = () => result.Error;

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Cannot access Error of a successful Result.");
    }

    [Fact]
    public void Match_OnSuccess_ExecutesOkFunction()
    {
        // Arrange
        var result = Result<int, Error>.Ok(42);

        // Act
        var output = result.Match(
            ok: value => $"Success: {value}",
            err: error => $"Error: {error.Message}");

        // Assert
        output.Should().Be("Success: 42");
    }

    [Fact]
    public void Match_OnFailure_ExecutesErrorFunction()
    {
        // Arrange
        var error = Error.Validation("Bad input");
        var result = Result<int, Error>.Fail(error);

        // Act
        var output = result.Match(
            ok: value => $"Success: {value}",
            err: error => $"Error: {error.Message}");

        // Assert
        output.Should().Be("Error: Bad input");
    }

    [Fact]
    public void Bind_OnSuccess_ChainsNextOperation()
    {
        // Arrange
        var result = Result<int, Error>.Ok(10);

        // Act
        var chained = result.Bind(value => Result<string, Error>.Ok($"Value is {value}"));

        // Assert
        chained.IsSuccess.Should().BeTrue();
        chained.Value.Should().Be("Value is 10");
    }

    [Fact]
    public void Bind_OnFailure_SkipsNextOperation()
    {
        // Arrange
        var error = Error.Validation("Invalid");
        var result = Result<int, Error>.Fail(error);
        var nextCalled = false;

        // Act
        var chained = result.Bind(value =>
        {
            nextCalled = true;
            return Result<string, Error>.Ok($"Value is {value}");
        });

        // Assert
        chained.IsSuccess.Should().BeFalse();
        chained.Error.Should().Be(error);
        nextCalled.Should().BeFalse("Bind should not execute on failure");
    }

    [Fact]
    public async Task Bind_Async_OnSuccess_ChainsAsyncOperation()
    {
        // Arrange
        var result = Result<int, Error>.Ok(10);

        // Act
        var chained = await result.Bind(async value =>
        {
            await Task.Delay(1);
            return Result<string, Error>.Ok($"Value is {value}");
        });

        // Assert
        chained.IsSuccess.Should().BeTrue();
        chained.Value.Should().Be("Value is 10");
    }

    [Fact]
    public async Task Bind_Async_OnFailure_SkipsAsyncOperation()
    {
        // Arrange
        var error = Error.Validation("Invalid");
        var result = Result<int, Error>.Fail(error);
        var nextCalled = false;

        // Act
        var chained = await result.Bind(async value =>
        {
            nextCalled = true;
            await Task.Delay(1);
            return Result<string, Error>.Ok($"Value is {value}");
        });

        // Assert
        chained.IsSuccess.Should().BeFalse();
        chained.Error.Should().Be(error);
        nextCalled.Should().BeFalse();
    }

    [Fact]
    public void Tap_OnSuccess_ExecutesAction()
    {
        // Arrange
        var result = Result<int, Error>.Ok(42);
        var sideEffect = 0;

        // Act
        var returned = result.Tap(value => sideEffect = value * 2);

        // Assert
        returned.Should().Be(result, "Tap should return original result");
        sideEffect.Should().Be(84);
    }

    [Fact]
    public void Tap_OnFailure_SkipsAction()
    {
        // Arrange
        var result = Result<int, Error>.Fail(Error.Unexpected());
        var sideEffect = 0;

        // Act
        var returned = result.Tap(value => sideEffect = value * 2);

        // Assert
        returned.IsSuccess.Should().BeFalse();
        sideEffect.Should().Be(0, "Tap should not execute on failure");
    }

    [Fact]
    public async Task Tap_Async_OnSuccess_ExecutesAsyncAction()
    {
        // Arrange
        var result = Result<int, Error>.Ok(42);
        var sideEffect = 0;

        // Act
        var returned = await result.Tap(async value =>
        {
            await Task.Delay(1);
            sideEffect = value * 2;
        });

        // Assert
        returned.IsSuccess.Should().BeTrue();
        sideEffect.Should().Be(84);
    }

    [Fact]
    public void TapError_OnFailure_ExecutesAction()
    {
        // Arrange
        var error = Error.Validation("Bad data");
        var result = Result<int, Error>.Fail(error);
        Error? capturedError = null;

        // Act
        var returned = result.TapError(err => capturedError = err);

        // Assert
        returned.Should().Be(result);
        capturedError.Should().Be(error);
    }

    [Fact]
    public void TapError_OnSuccess_SkipsAction()
    {
        // Arrange
        var result = Result<int, Error>.Ok(42);
        Error? capturedError = null;

        // Act
        var returned = result.TapError(err => capturedError = err);

        // Assert
        returned.IsSuccess.Should().BeTrue();
        capturedError.Should().BeNull("TapError should not execute on success");
    }

    [Fact]
    public void Finally_AlwaysExecutes()
    {
        // Arrange
        var successResult = Result<int, Error>.Ok(42);
        var failureResult = Result<int, Error>.Fail(Error.Unexpected());
        var executionCount = 0;

        // Act
        successResult.Finally(executionCount, count => executionCount = count + 1);
        failureResult.Finally(executionCount, count => executionCount = count + 1);

        // Assert
        executionCount.Should().Be(2, "Finally should execute for both success and failure");
    }

    [Fact]
    public void ImplicitConversion_FromValue_CreatesSuccessResult()
    {
        // Act
        Result<int, Error> result = 42; // Implicit conversion

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(42);
    }

    [Fact]
    public void ImplicitConversion_FromError_CreatesFailureResult()
    {
        // Arrange
        var error = Error.Validation("Invalid");

        // Act
        Result<int, Error> result = error; // Implicit conversion

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(error);
    }

    [Fact]
    public void Combine_AllSuccess_ReturnsSuccessWithTuple()
    {
        // Arrange
        var r1 = Result<int, Error>.Ok(1);
        var r2 = Result<string, Error>.Ok("two");
        var r3 = Result<bool, Error>.Ok(true);

        // Act
        var combined = Result.Combine(r1, r2, r3);

        // Assert
        combined.IsSuccess.Should().BeTrue();
        combined.Value.Should().Be((1, "two", true));
    }

    [Fact]
    public void Combine_FirstFails_ReturnsFirstError()
    {
        // Arrange
        var error1 = Error.Validation("Error 1");
        var r1 = Result<int, Error>.Fail(error1);
        var r2 = Result<string, Error>.Ok("two");
        var r3 = Result<bool, Error>.Ok(true);

        // Act
        var combined = Result.Combine(r1, r2, r3);

        // Assert
        combined.IsSuccess.Should().BeFalse();
        combined.Error.Should().Be(error1);
    }

    [Fact]
    public void Combine_SecondFails_ReturnsSecondError()
    {
        // Arrange
        var error2 = Error.Validation("Error 2");
        var r1 = Result<int, Error>.Ok(1);
        var r2 = Result<string, Error>.Fail(error2);
        var r3 = Result<bool, Error>.Ok(true);

        // Act
        var combined = Result.Combine(r1, r2, r3);

        // Assert
        combined.IsSuccess.Should().BeFalse();
        combined.Error.Should().Be(error2);
    }

    [Fact]
    public async Task TryAsync_SuccessfulOperation_ReturnsSuccess()
    {
        // Arrange
        static async Task<int> Operation(CancellationToken ct)
        {
            await Task.Delay(1, ct);
            return 42;
        }

        // Act
        var result = await Result.TryAsync(Operation, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(42);
    }

    [Fact]
    public async Task TryAsync_ThrowingOperation_ReturnsFailure()
    {
        // Arrange
        static Task<int> Operation(CancellationToken ct)
        {
            throw new InvalidOperationException("Something went wrong");
        }

        // Act
        var result = await Result.TryAsync(Operation, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be(ErrorCode.Unknown.UnhandledException);
        result.Error.Message.Should().Contain("Something went wrong");
    }

    [Fact]
    public async Task Bind_TaskResult_ChainsCorrectly()
    {
        // Arrange
        var resultTask = Task.FromResult(Result<int, Error>.Ok(10));

        // Act
        var chained = await resultTask.Bind(async value =>
        {
            await Task.Delay(1);
            return Result<string, Error>.Ok($"Value: {value}");
        });

        // Assert
        chained.IsSuccess.Should().BeTrue();
        chained.Value.Should().Be("Value: 10");
    }

    [Fact]
    public async Task Match_TaskResult_ResolvesCorrectly()
    {
        // Arrange
        var resultTask = Task.FromResult(Result<int, Error>.Ok(42));

        // Act
        var output = await resultTask.Match(
            onSuccess: value => $"Got {value}",
            onFailure: error => $"Error: {error.Message}");

        // Assert
        output.Should().Be("Got 42");
    }

    [Fact]
    public void ResultChain_ComplexScenario_WorksCorrectly()
    {
        // Arrange - Simulate a pipeline
        var input = 10;

        // Act - Chain multiple operations
        var result = Result<int, Error>.Ok(input)
            .Bind(x => Result<int, Error>.Ok(x * 2))          // 20
            .Tap(x => Console.WriteLine($"Step 1: {x}"))
            .Bind(x => Result<int, Error>.Ok(x + 5))          // 25
            .Tap(x => Console.WriteLine($"Step 2: {x}"))
            .Bind(x => x > 20 
                ? Result<string, Error>.Ok($"Large: {x}") 
                : Result<string, Error>.Fail(Error.Validation("Too small")));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be("Large: 25");
    }

    [Fact]
    public void ResultChain_FailsEarly_ShortCircuits()
    {
        // Arrange
        var step2Called = false;
        var step3Called = false;

        // Act
        var result = Result<int, Error>.Ok(10)
            .Bind(x => Result<int, Error>.Fail(Error.Validation("Step 1 failed")))
            .Tap(x => step2Called = true)
            .Bind(x => 
            {
                step3Called = true;
                return Result<string, Error>.Ok($"Value: {x}");
            });

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Message.Should().Be("Step 1 failed");
        step2Called.Should().BeFalse("Chain should short-circuit after first failure");
        step3Called.Should().BeFalse("Chain should short-circuit after first failure");
    }

    [Fact]
    public void ImplicitConversion_AllowsCleanSyntax()
    {
        // Act - Use implicit conversion in method return
        Result<int, Error> GetValue(bool succeed)
        {
            return succeed 
                ? 42 // Implicitly converts to Result.Ok(42)
                : Error.Validation("Failed"); // Implicitly converts to Result.Fail(...)
        }

        // Assert
        var success = GetValue(true);
        success.IsSuccess.Should().BeTrue();
        success.Value.Should().Be(42);

        var failure = GetValue(false);
        failure.IsSuccess.Should().BeFalse();
        failure.Error.Message.Should().Be("Failed");
    }
}
