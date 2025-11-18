using Microsoft.Extensions.Logging;
using Moq;

namespace Sazkomat.Tests.Helpers;

/// <summary>
/// Helper utilities for unit tests
/// </summary>
public static class TestHelpers
{
    /// <summary>
    /// Creates a mock ILogger for testing
    /// </summary>
    public static ILogger<T> CreateMockLogger<T>()
    {
        return new Mock<ILogger<T>>().Object;
    }

    /// <summary>
    /// Creates a mock ILogger with strict behavior for testing
    /// </summary>
    public static Mock<ILogger<T>> CreateStrictMockLogger<T>()
    {
        return new Mock<ILogger<T>>(MockBehavior.Strict);
    }
}
