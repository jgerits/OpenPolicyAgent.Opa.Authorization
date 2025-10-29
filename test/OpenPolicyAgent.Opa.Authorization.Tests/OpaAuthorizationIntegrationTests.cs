using Xunit;

namespace OpenPolicyAgent.Opa.Authorization.Tests;

/// <summary>
/// Unit tests that validate the OPA authorization configuration and service registration.
/// </summary>
public class OpaAuthorizationIntegrationTests
{
    [Fact]
    public void OpaAuthorizationOptions_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var options = new OpaAuthorizationOptions();

        // Assert
        Assert.Equal("http://localhost:8181", options.OpaUrl);
        Assert.Null(options.DefaultPolicyPath);
        Assert.Equal("en", options.ReasonKey);
        Assert.False(options.AllowUnauthenticated);
    }

    [Fact]
    public void OpaAuthorizationOptions_CanBeConfigured()
    {
        // Arrange
        var options = new OpaAuthorizationOptions
        {
            OpaUrl = "http://custom-opa:9999",
            DefaultPolicyPath = "custom/policy/path",
            ReasonKey = "fr",
            AllowUnauthenticated = true
        };

        // Assert
        Assert.Equal("http://custom-opa:9999", options.OpaUrl);
        Assert.Equal("custom/policy/path", options.DefaultPolicyPath);
        Assert.Equal("fr", options.ReasonKey);
        Assert.True(options.AllowUnauthenticated);
    }

    [Fact]
    public void OpaAuthorizationDefaults_PolicyName_IsCorrect()
    {
        // Assert
        Assert.Equal("OpaPolicy", OpaAuthorizationDefaults.PolicyName);
    }

    [Fact]
    public void OpaAuthorizationRequirement_WithPolicyPath_StoresPolicyPath()
    {
        // Arrange
        var policyPath = "test/policy/path";
        
        // Act
        var requirement = new OpaAuthorizationRequirement(policyPath);

        // Assert
        Assert.Equal(policyPath, requirement.PolicyPath);
    }

    [Fact]
    public void OpaAuthorizeAttribute_InheritsFromAuthorizeAttribute()
    {
        // Arrange
        var attribute = new OpaAuthorizeAttribute();

        // Assert
        Assert.IsAssignableFrom<Microsoft.AspNetCore.Authorization.AuthorizeAttribute>(attribute);
        Assert.Equal(OpaAuthorizationDefaults.PolicyName, attribute.Policy);
    }
}

