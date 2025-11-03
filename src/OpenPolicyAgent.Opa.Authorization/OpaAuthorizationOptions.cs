namespace OpenPolicyAgent.Opa.Authorization;

/// <summary>
/// Configuration options for OPA authorization.
/// </summary>
public class OpaAuthorizationOptions
{
    /// <summary>
    /// Gets or sets the OPA server URL. Defaults to http://localhost:8181 if not specified.
    /// </summary>
    public string OpaUrl { get; set; } = "http://localhost:8181";

    /// <summary>
    /// Gets or sets the default OPA policy path to evaluate.
    /// Example: "authz/allow" or "example/app/allow"
    /// </summary>
    public string? DefaultPolicyPath { get; set; }

    /// <summary>
    /// Gets or sets the preferred key where the access decision reason should be searched for 
    /// in the OPA response. A default value of 'en' is used. If the selected key is not present 
    /// in the response, the lexicographically first key is used instead.
    /// </summary>
    public string ReasonKey { get; set; } = "en";

    /// <summary>
    /// Gets or sets whether to allow unauthenticated requests. 
    /// If false (default), unauthenticated requests are automatically denied.
    /// </summary>
    public bool AllowUnauthenticated { get; set; } = false;

    /// <summary>
    /// Gets or sets whether to include the authorization token in the OPA input.
    /// If true, the Authorization header value will be included in the input sent to OPA.
    /// Default is false for backward compatibility.
    /// </summary>
    public bool IncludeAuthorizationToken { get; set; } = false;
}
