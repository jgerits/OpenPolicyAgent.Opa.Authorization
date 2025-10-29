using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Xunit;

namespace OpenPolicyAgent.Opa.Authorization.Tests;

public class OpaAuthorizationOptionsTests
{
    [Fact]
    public void OpaAuthorizationOptions_DefaultTimeoutSeconds_Is30()
    {
        // Arrange & Act
        var options = new OpaAuthorizationOptions();

        // Assert
        Assert.Equal(30, options.TimeoutSeconds);
    }

    [Fact]
    public void OpaAuthorizationOptions_CanSetTimeoutSeconds()
    {
        // Arrange
        var options = new OpaAuthorizationOptions
        {
            TimeoutSeconds = 60
        };

        // Assert
        Assert.Equal(60, options.TimeoutSeconds);
    }

    [Fact]
    public void OpaAuthorizationHandler_WithInvalidUrl_ThrowsArgumentException()
    {
        // Arrange
        var options = Options.Create(new OpaAuthorizationOptions
        {
            OpaUrl = "invalid-url"
        });
        var httpContextAccessor = new HttpContextAccessor();
        var logger = new LoggerFactory().CreateLogger<OpaAuthorizationHandler>();

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
        {
            var handler = new OpaAuthorizationHandler(options, httpContextAccessor, logger);
        });

        Assert.Contains("Invalid OPA URL", exception.Message);
    }

    [Fact]
    public void OpaAuthorizationHandler_WithValidHttpUrl_DoesNotThrow()
    {
        // Arrange
        var options = Options.Create(new OpaAuthorizationOptions
        {
            OpaUrl = "http://localhost:8181"
        });
        var httpContextAccessor = new HttpContextAccessor();
        var logger = new LoggerFactory().CreateLogger<OpaAuthorizationHandler>();

        // Act & Assert - Should not throw
        var handler = new OpaAuthorizationHandler(options, httpContextAccessor, logger);
        Assert.NotNull(handler);
    }

    [Fact]
    public void OpaAuthorizationHandler_WithValidHttpsUrl_DoesNotThrow()
    {
        // Arrange
        var options = Options.Create(new OpaAuthorizationOptions
        {
            OpaUrl = "https://opa.example.com:8181"
        });
        var httpContextAccessor = new HttpContextAccessor();
        var logger = new LoggerFactory().CreateLogger<OpaAuthorizationHandler>();

        // Act & Assert - Should not throw
        var handler = new OpaAuthorizationHandler(options, httpContextAccessor, logger);
        Assert.NotNull(handler);
    }
}
