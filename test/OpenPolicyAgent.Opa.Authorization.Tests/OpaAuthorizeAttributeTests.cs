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
    public void Constructor_WithPolicyName_ShouldSetPolicies()
    {
        // Arrange
        var policyName = "authz/myapp/allow";

        // Act
        var attribute = new OpaAuthorizeAttribute(policyName);

        // Assert
        Assert.Equal(OpaAuthorizationDefaults.PolicyName, attribute.Policy);
        Assert.Single(attribute.Policies);
        Assert.Equal(policyName, attribute.Policies[0]);
        Assert.Null(attribute.PolicyPath);
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
