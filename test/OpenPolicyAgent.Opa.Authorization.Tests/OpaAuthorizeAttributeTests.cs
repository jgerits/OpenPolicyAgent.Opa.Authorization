namespace OpenPolicyAgent.Opa.Authorization.Tests;

public class OpaAuthorizeAttributeTests
{
    [Fact]
    public void Constructor_Default_ShouldSetCorrectPolicy()
    {
        // Act
        var attribute = new OpaAuthorizeAttribute();

        // Assert
        Assert.Equal(OpaAuthorizationDefaults.PolicyName, attribute.Policy);
        Assert.Null(attribute.PolicyPath);
    }

    [Fact]
    public void Constructor_WithPolicyPath_ShouldSetPolicyPath()
    {
        // Arrange
        var policyPath = "authz/myapp/allow";

        // Act
        var attribute = new OpaAuthorizeAttribute(policyPath);

        // Assert
        Assert.Equal(OpaAuthorizationDefaults.PolicyName, attribute.Policy);
        Assert.Equal(policyPath, attribute.PolicyPath);
    }

    [Fact]
    public void PolicyPath_CanBeSetAfterConstruction()
    {
        // Arrange
        var attribute = new OpaAuthorizeAttribute();
        var policyPath = "authz/custom/allow";

        // Act
        attribute.PolicyPath = policyPath;

        // Assert
        Assert.Equal(policyPath, attribute.PolicyPath);
    }
}
