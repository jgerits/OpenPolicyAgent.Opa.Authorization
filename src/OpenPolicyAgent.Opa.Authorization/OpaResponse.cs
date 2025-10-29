using System.Text.Json;
using System.Text.Json.Serialization;

namespace OpenPolicyAgent.Opa.Authorization;

/// <summary>
/// Represents an OPA policy evaluation response.
/// </summary>
public class OpaResponse
{
    /// <summary>
    /// Gets or sets the authorization decision.
    /// </summary>
    [JsonPropertyName("allow")]
    public bool Decision { get; set; }

    /// <summary>
    /// Gets or sets the reason for the authorization decision.
    /// Can be a string or a dictionary of localized reasons.
    /// </summary>
    [JsonPropertyName("reason")]
    public object? Reason { get; set; }

    /// <summary>
    /// Gets the reason for the decision, using the preferred language key if available.
    /// </summary>
    /// <param name="preferredKey">The preferred language key (e.g., "en").</param>
    /// <returns>The reason string, or null if not available.</returns>
    public string? GetReasonForDecision(string preferredKey = "en")
    {
        if (Reason == null)
            return null;

        // If Reason is a string, return it directly
        if (Reason is string reasonString)
            return reasonString;

        // If Reason is a JsonElement, handle it appropriately
        if (Reason is JsonElement jsonElement)
        {
            if (jsonElement.ValueKind == JsonValueKind.String)
                return jsonElement.GetString();

            if (jsonElement.ValueKind == JsonValueKind.Object)
            {
                var reasonDict = JsonSerializer.Deserialize<Dictionary<string, string>>(jsonElement.GetRawText());
                if (reasonDict == null || reasonDict.Count == 0)
                    return null;

                // Try to get the preferred key
                if (reasonDict.TryGetValue(preferredKey, out var preferredReason))
                    return preferredReason;

                // Fall back to the first available reason
                return reasonDict.OrderBy(kvp => kvp.Key).First().Value;
            }
        }

        // If Reason is a dictionary, try to get the preferred key
        var reasonJson = JsonSerializer.Serialize(Reason);
        var reasonDict2 = JsonSerializer.Deserialize<Dictionary<string, string>>(reasonJson);

        if (reasonDict2 == null || reasonDict2.Count == 0)
            return null;

        // Try to get the preferred key
        if (reasonDict2.TryGetValue(preferredKey, out var preferredReason2))
            return preferredReason2;

        // Fall back to the first available reason
        return reasonDict2.OrderBy(kvp => kvp.Key).First().Value;
    }
}
