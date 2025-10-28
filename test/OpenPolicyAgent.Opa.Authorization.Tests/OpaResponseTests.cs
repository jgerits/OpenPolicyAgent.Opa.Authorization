using Newtonsoft.Json;

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
        var json = @"{""allow"": true, ""reason"": ""Access granted""}";

        // Act
        var response = JsonConvert.DeserializeObject<OpaResponse>(json);

        // Assert
        Assert.NotNull(response);
        Assert.True(response.Decision);
        Assert.Equal("Access granted", response.Reason);
    }
}
