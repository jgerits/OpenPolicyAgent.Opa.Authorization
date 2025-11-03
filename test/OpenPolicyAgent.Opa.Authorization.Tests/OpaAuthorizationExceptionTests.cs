namespace OpenPolicyAgent.Opa.Authorization.Tests;

public class OpaAuthorizationExceptionTests
{
    [Fact]
    public void Constructor_Default_ShouldCreateException()
    {
        // Act
        var exception = new OpaAuthorizationException();

        // Assert
        Assert.NotNull(exception);
        Assert.Null(exception.PolicyPath);
        Assert.Null(exception.StatusCode);
    }

    [Fact]
    public void Constructor_WithMessage_ShouldSetMessage()
    {
        // Arrange
        var message = "Test error message";

        // Act
        var exception = new OpaAuthorizationException(message);

        // Assert
        Assert.Equal(message, exception.Message);
        Assert.Null(exception.PolicyPath);
        Assert.Null(exception.StatusCode);
    }

    [Fact]
    public void Constructor_WithMessageAndInnerException_ShouldSetBoth()
    {
        // Arrange
        var message = "Test error message";
        var innerException = new Exception("Inner exception");

        // Act
        var exception = new OpaAuthorizationException(message, innerException);

        // Assert
        Assert.Equal(message, exception.Message);
        Assert.Same(innerException, exception.InnerException);
        Assert.Null(exception.PolicyPath);
        Assert.Null(exception.StatusCode);
    }

    [Fact]
    public void Constructor_WithPolicyPath_ShouldSetPolicyPath()
    {
        // Arrange
        var message = "Test error message";
        var policyPath = "authz/allow";

        // Act
        var exception = new OpaAuthorizationException(message, policyPath);

        // Assert
        Assert.Equal(message, exception.Message);
        Assert.Equal(policyPath, exception.PolicyPath);
        Assert.Null(exception.StatusCode);
    }

    [Fact]
    public void Constructor_WithPolicyPathAndStatusCode_ShouldSetBoth()
    {
        // Arrange
        var message = "Test error message";
        var policyPath = "authz/allow";
        var statusCode = 500;

        // Act
        var exception = new OpaAuthorizationException(message, policyPath, statusCode);

        // Assert
        Assert.Equal(message, exception.Message);
        Assert.Equal(policyPath, exception.PolicyPath);
        Assert.Equal(statusCode, exception.StatusCode);
    }

    [Fact]
    public void Constructor_WithAllParameters_ShouldSetAllProperties()
    {
        // Arrange
        var message = "Test error message";
        var policyPath = "authz/allow";
        var statusCode = 500;
        var innerException = new Exception("Inner exception");

        // Act
        var exception = new OpaAuthorizationException(message, policyPath, statusCode, innerException);

        // Assert
        Assert.Equal(message, exception.Message);
        Assert.Equal(policyPath, exception.PolicyPath);
        Assert.Equal(statusCode, exception.StatusCode);
        Assert.Same(innerException, exception.InnerException);
    }

    [Fact]
    public void Constructor_WithNullPolicyPath_ShouldAllowNull()
    {
        // Arrange
        var message = "Test error message";

        // Act
        var exception = new OpaAuthorizationException(message, policyPath: null);

        // Assert
        Assert.Equal(message, exception.Message);
        Assert.Null(exception.PolicyPath);
    }

    [Fact]
    public void Constructor_WithNullStatusCode_ShouldAllowNull()
    {
        // Arrange
        var message = "Test error message";
        var policyPath = "authz/allow";

        // Act
        var exception = new OpaAuthorizationException(message, policyPath, null);

        // Assert
        Assert.Equal(message, exception.Message);
        Assert.Equal(policyPath, exception.PolicyPath);
        Assert.Null(exception.StatusCode);
    }
}
