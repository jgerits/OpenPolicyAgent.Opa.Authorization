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
        Assert.False(options.IncludeAuthorizationToken);
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
            AllowUnauthenticated = true,
            IncludeAuthorizationToken = true
        };

        // Assert
        Assert.Equal("http://custom-opa:9999", options.OpaUrl);
        Assert.Equal("custom/policy/path", options.DefaultPolicyPath);
        Assert.Equal("fr", options.ReasonKey);
        Assert.True(options.AllowUnauthenticated);
        Assert.True(options.IncludeAuthorizationToken);
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
        var requirement = new OpaAuthorizationRequirement(policyPath: policyPath);

        // Assert
        Assert.Equal(policyPath, requirement.PolicyPath);
    }

    [Fact]
    public void OpaAuthorizationRequirement_WithPolicies_StoresPolicies()
    {
        // Arrange
        var policies = new[] { "policy1", "policy2" };

        // Act
        var requirement = new OpaAuthorizationRequirement(policies: policies);

        // Assert
        Assert.Equal(policies, requirement.Policies);
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

    [Fact]
    public void OpaAuthorizeAttribute_WithPolicies_StoresPolicies()
    {
        // Arrange
        var attribute = new OpaAuthorizeAttribute("policy1", "policy2");

        // Assert
        Assert.Equal(new[] { "policy1", "policy2" }, attribute.Policies);
        Assert.Null(attribute.PolicyPath); // PolicyPath should be null when using this constructor
    }

    [Fact]
    public void OpaAuthorizeAttribute_WithPolicyPath_CanBeSetExplicitly()
    {
        // Arrange
        var attribute = new OpaAuthorizeAttribute("policy1") { PolicyPath = "custom/path" };

        // Assert
        Assert.Single(attribute.Policies);
        Assert.Equal("policy1", attribute.Policies[0]);
        Assert.Equal("custom/path", attribute.PolicyPath);
    }

    [Fact]
    public void OpaAuthorizationOptions_IncludeAuthorizationToken_DefaultsToFalse()
    {
        // Arrange & Act
        var options = new OpaAuthorizationOptions();

        // Assert
        Assert.False(options.IncludeAuthorizationToken);
    }

    [Fact]
    public void OpaAuthorizationOptions_IncludeAuthorizationToken_CanBeEnabled()
    {
        // Arrange
        var options = new OpaAuthorizationOptions
        {
            IncludeAuthorizationToken = true
        };

        // Assert
        Assert.True(options.IncludeAuthorizationToken);
    }

    [Fact]
    public void OpaAuthorizationOptions_DisableAuthorization_DefaultsToFalse()
    {
        // Arrange & Act
        var options = new OpaAuthorizationOptions();

        // Assert
        Assert.False(options.DisableAuthorization);
    }

    [Fact]
    public void OpaAuthorizationOptions_DisableAuthorization_CanBeEnabled()
    {
        // Arrange
        var options = new OpaAuthorizationOptions
        {
            DisableAuthorization = true
        };

        // Assert
        Assert.True(options.DisableAuthorization);
    }

    [Fact]
    public void OpaAuthorizationOptions_GroupClaimTypes_HasDefaultValues()
    {
        // Arrange & Act
        var options = new OpaAuthorizationOptions();

        // Assert
        Assert.NotNull(options.GroupClaimTypes);
        Assert.Contains(System.Security.Claims.ClaimTypes.Role, options.GroupClaimTypes);
        Assert.Contains("role", options.GroupClaimTypes);
        Assert.Contains("groups", options.GroupClaimTypes);
    }

    [Fact]
    public void OpaAuthorizationOptions_GroupClaimTypes_CanBeCustomized()
    {
        // Arrange
        var options = new OpaAuthorizationOptions();
        
        // Act
        options.GroupClaimTypes.Clear();
        options.GroupClaimTypes.Add("custom-role");
        options.GroupClaimTypes.Add("custom-group");

        // Assert
        Assert.Equal(2, options.GroupClaimTypes.Count);
        Assert.Contains("custom-role", options.GroupClaimTypes);
        Assert.Contains("custom-group", options.GroupClaimTypes);
    }
}

