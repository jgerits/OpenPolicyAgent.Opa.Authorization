using System.Text.Json;

namespace OpenPolicyAgent.Opa.Authorization.Tests;

public class OpaResponseTests
{
    [Fact]
    public void Decision_ShouldReturnCorrectValue()
    {
        // Arrange
        var response = new OpaResponse { Decision = true };

        // Act & Assert
        Assert.True(response.Decision);
    }

    [Fact]
    public void GetReasonForDecision_WithStringReason_ShouldReturnString()
    {
        // Arrange
        var response = new OpaResponse
        {
            Decision = false,
            Reason = "Access denied"
        };

        // Act
        var reason = response.GetReasonForDecision();

        // Assert
        Assert.Equal("Access denied", reason);
    }

    [Fact]
    public void GetReasonForDecision_WithDictionary_ShouldReturnPreferredKey()
    {
        // Arrange
        var reasonDict = new Dictionary<string, string>
        {
            { "en", "Access denied" },
            { "es", "Acceso denegado" },
            { "fr", "Accès refusé" }
        };
        var response = new OpaResponse
        {
            Decision = false,
            Reason = reasonDict
        };

        // Act
        var reason = response.GetReasonForDecision("en");

        // Assert
        Assert.Equal("Access denied", reason);
    }

    [Fact]
    public void GetReasonForDecision_WithDictionary_AndMissingPreferredKey_ShouldReturnFirst()
    {
        // Arrange
        var reasonDict = new Dictionary<string, string>
        {
            { "es", "Acceso denegado" },
            { "fr", "Accès refusé" }
        };
        var response = new OpaResponse
        {
            Decision = false,
            Reason = reasonDict
        };

        // Act
        var reason = response.GetReasonForDecision("en");

        // Assert
        Assert.Equal("Acceso denegado", reason); // "es" comes before "fr" alphabetically
    }

    [Fact]
    public void GetReasonForDecision_WithNullReason_ShouldReturnNull()
    {
        // Arrange
        var response = new OpaResponse
        {
            Decision = false,
            Reason = null
        };

        // Act
        var reason = response.GetReasonForDecision();

        // Assert
        Assert.Null(reason);
    }

    [Fact]
    public void Deserialization_ShouldWorkCorrectly()
    {
        // Arrange
        var json = @"{""result"": true, ""reason"": ""Access granted""}";

        // Act
        var response = JsonSerializer.Deserialize<OpaResponse>(json);

        // Assert
        Assert.NotNull(response);
        Assert.True(response.Decision);
        Assert.NotNull(response.Reason);
        Assert.Equal("Access granted", response.Reason.ToString());
    }

    [Fact]
    public void Deserialization_WithOnlyAllowField_ShouldWorkCorrectly()
    {
        // Arrange
        var json = @"{""allow"": false}";

        // Act
        var response = JsonSerializer.Deserialize<OpaResponse>(json);

        // Assert
        Assert.NotNull(response);
        Assert.False(response.Decision);
        Assert.Null(response.Reason);
    }

    [Fact]
    public void Deserialization_WithComplexReason_ShouldWorkCorrectly()
    {
        // Arrange - This is the format returned by OPA when querying a package (e.g., /v1/data/authz)
        var json = @"{""result"": true, ""reason"": {""en"": ""Access granted"", ""es"": ""Acceso concedido""}}";

        // Act
        var response = JsonSerializer.Deserialize<OpaResponse>(json);

        // Assert
        Assert.NotNull(response);
        Assert.True(response.Decision);
        Assert.NotNull(response.Reason);
        Assert.Equal("Access granted", response.GetReasonForDecision("en"));
        Assert.Equal("Acceso concedido", response.GetReasonForDecision("es"));
    }

    [Fact]
    public void Deserialization_WithExtraFields_ShouldIgnoreThemAndDeserializeCorrectly()
    {
        // Arrange - OPA policies may return extra fields alongside allow and reason
        var json = @"{
            ""result"": true,
            ""debug_info"": {""opa_version"": ""1.9.0""},
            ""is_authenticated"": true,
            ""matched_rules"": [""authenticated_user""],
            ""reason"": {},
            ""user_roles"": []
        }";

        // Act
        var response = JsonSerializer.Deserialize<OpaResponse>(json);

        // Assert
        Assert.NotNull(response);
        Assert.True(response.Decision);
        Assert.NotNull(response.Reason);
    }

    [Fact]
    public void Deserialization_WithEmptyReasonObject_ShouldReturnNullReason()
    {
        // Arrange
        var json = @"{""result"": true, ""reason"": {}}";

        // Act
        var response = JsonSerializer.Deserialize<OpaResponse>(json);

        // Assert
        Assert.NotNull(response);
        Assert.True(response.Decision);
        Assert.NotNull(response.Reason); // Reason object exists
        Assert.Null(response.GetReasonForDecision()); // But GetReasonForDecision returns null for empty dict
    }
}
