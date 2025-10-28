using Newtonsoft.Json;

namespace OpenPolicyAgent.Opa.Authorization;

/// <summary>
/// Represents an OPA policy evaluation response.
/// </summary>
public class OpaResponse
{
    /// <summary>
    /// Gets or sets the authorization decision.
    /// </summary>
    [JsonProperty("allow")]
    public bool Decision { get; set; }

    /// <summary>
    /// Gets or sets the reason for the authorization decision.
    /// Can be a string or a dictionary of localized reasons.
    /// </summary>
    [JsonProperty("reason")]
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

        // If Reason is a dictionary, try to get the preferred key
        var reasonJson = JsonConvert.SerializeObject(Reason);
        var reasonDict = JsonConvert.DeserializeObject<Dictionary<string, string>>(reasonJson);

        if (reasonDict == null || reasonDict.Count == 0)
            return null;

        // Try to get the preferred key
        if (reasonDict.TryGetValue(preferredKey, out var preferredReason))
            return preferredReason;

        // Fall back to the first available reason
        return reasonDict.OrderBy(kvp => kvp.Key).First().Value;
    }
}
