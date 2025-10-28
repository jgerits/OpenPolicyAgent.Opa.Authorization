namespace OpenPolicyAgent.Opa.Authorization.Tests;

public class OpaAuthorizationRequirementTests
{
    [Fact]
    public void Constructor_WithoutPolicyPath_ShouldHaveNullPolicyPath()
    {
        // Act
        var requirement = new OpaAuthorizationRequirement();

        // Assert
        Assert.Null(requirement.PolicyPath);
    }

    [Fact]
    public void Constructor_WithPolicyPath_ShouldSetPolicyPath()
    {
        // Arrange
        var policyPath = "authz/myapp/allow";

        // Act
        var requirement = new OpaAuthorizationRequirement(policyPath);

        // Assert
        Assert.Equal(policyPath, requirement.PolicyPath);
    }
}
