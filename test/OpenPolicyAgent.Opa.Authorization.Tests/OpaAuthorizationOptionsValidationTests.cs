namespace OpenPolicyAgent.Opa.Authorization.Tests;

public class OpaAuthorizationOptionsValidationTests
{
    [Fact]
    public void Validate_WithValidOptions_ShouldNotThrow()
    {
        // Arrange
        var options = new OpaAuthorizationOptions
        {
            OpaUrl = "http://localhost:8181",
            ReasonKey = "en",
            RequestTimeout = TimeSpan.FromSeconds(30)
        };

        // Act & Assert
        var exception = Record.Exception(() => options.Validate());
        Assert.Null(exception);
    }

    [Fact]
    public void Validate_WithNullOpaUrl_ShouldThrow()
    {
        // Arrange
        var options = new OpaAuthorizationOptions
        {
            OpaUrl = null!
        };

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => options.Validate());
        Assert.Contains("OpaUrl cannot be null", exception.Message);
    }

    [Fact]
    public void Validate_WithEmptyOpaUrl_ShouldThrow()
    {
        // Arrange
        var options = new OpaAuthorizationOptions
        {
            OpaUrl = ""
        };

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => options.Validate());
        Assert.Contains("OpaUrl cannot be null", exception.Message);
    }

    [Fact]
    public void Validate_WithWhitespaceOpaUrl_ShouldThrow()
    {
        // Arrange
        var options = new OpaAuthorizationOptions
        {
            OpaUrl = "   "
        };

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => options.Validate());
        Assert.Contains("OpaUrl cannot be null", exception.Message);
    }

    [Fact]
    public void Validate_WithInvalidOpaUrl_ShouldThrow()
    {
        // Arrange
        var options = new OpaAuthorizationOptions
        {
            OpaUrl = "not-a-valid-url"
        };

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => options.Validate());
        Assert.Contains("not a valid absolute URI", exception.Message);
    }

    [Fact]
    public void Validate_WithRequireHttpsTrue_AndHttpUrl_ShouldThrow()
    {
        // Arrange
        var options = new OpaAuthorizationOptions
        {
            OpaUrl = "http://localhost:8181",
            RequireHttps = true
        };

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => options.Validate());
        Assert.Contains("must use HTTPS", exception.Message);
    }

    [Fact]
    public void Validate_WithRequireHttpsTrue_AndHttpsUrl_ShouldNotThrow()
    {
        // Arrange
        var options = new OpaAuthorizationOptions
        {
            OpaUrl = "https://opa.example.com:8181",
            RequireHttps = true
        };

        // Act & Assert
        var exception = Record.Exception(() => options.Validate());
        Assert.Null(exception);
    }

    [Fact]
    public void Validate_WithZeroTimeout_ShouldThrow()
    {
        // Arrange
        var options = new OpaAuthorizationOptions
        {
            RequestTimeout = TimeSpan.Zero
        };

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => options.Validate());
        Assert.Contains("RequestTimeout must be greater than zero", exception.Message);
    }

    [Fact]
    public void Validate_WithNegativeTimeout_ShouldThrow()
    {
        // Arrange
        var options = new OpaAuthorizationOptions
        {
            RequestTimeout = TimeSpan.FromSeconds(-1)
        };

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => options.Validate());
        Assert.Contains("RequestTimeout must be greater than zero", exception.Message);
    }

    [Fact]
    public void Validate_WithNullReasonKey_ShouldThrow()
    {
        // Arrange
        var options = new OpaAuthorizationOptions
        {
            ReasonKey = null!
        };

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => options.Validate());
        Assert.Contains("ReasonKey cannot be null", exception.Message);
    }

    [Fact]
    public void Validate_WithEmptyReasonKey_ShouldThrow()
    {
        // Arrange
        var options = new OpaAuthorizationOptions
        {
            ReasonKey = ""
        };

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => options.Validate());
        Assert.Contains("ReasonKey cannot be null", exception.Message);
    }

    [Fact]
    public void RequestTimeout_DefaultValue_ShouldBe30Seconds()
    {
        // Arrange & Act
        var options = new OpaAuthorizationOptions();

        // Assert
        Assert.Equal(TimeSpan.FromSeconds(30), options.RequestTimeout);
    }

    [Fact]
    public void RequireHttps_DefaultValue_ShouldBeFalse()
    {
        // Arrange & Act
        var options = new OpaAuthorizationOptions();

        // Assert
        Assert.False(options.RequireHttps);
    }
}
