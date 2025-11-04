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
    /// This should be the package path (e.g., "authz") not the rule path (e.g., "authz/allow").
    /// The package must contain "allow" and optionally "reason" fields.
    /// Example: "authz" or "example/app"
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

    /// <summary>
    /// Gets or sets the timeout for OPA policy evaluation requests.
    /// Default is 30 seconds.
    /// </summary>
    public TimeSpan RequestTimeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Gets or sets whether to enforce HTTPS for the OPA URL.
    /// When true, non-HTTPS URLs will cause a validation error.
    /// Default is false for development flexibility.
    /// </summary>
    public bool RequireHttps { get; set; } = false;

    /// <summary>
    /// Gets or sets the list of header names to exclude from the OPA input.
    /// This is useful for preventing sensitive information from being sent to OPA.
    /// Default includes common sensitive headers like Authorization, Cookie, and X-API-Key.
    /// </summary>
    public HashSet<string> ExcludedHeaders { get; set; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "Authorization",
        "Cookie",
        "X-API-Key",
        "X-Auth-Token"
    };

    /// <summary>
    /// Gets or sets whether to include request headers in the OPA input.
    /// When false, no headers are sent to OPA (overrides ExcludedHeaders).
    /// Default is true.
    /// </summary>
    public bool IncludeHeaders { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to disable OPA authorization entirely.
    /// When true, no calls are made to the OPA server and all authorization attempts are logged locally.
    /// Default is false to maintain backward compatibility.
    /// </summary>
    public bool DisableAuthorization { get; set; } = false;

    /// <summary>
    /// Validates the configuration options.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when configuration is invalid.</exception>
    public void Validate()
    {
        // Skip validation if authorization is disabled
        if (DisableAuthorization)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(OpaUrl))
        {
            throw new InvalidOperationException("OpaUrl cannot be null or whitespace.");
        }

        if (!Uri.TryCreate(OpaUrl, UriKind.Absolute, out var uri))
        {
            throw new InvalidOperationException($"OpaUrl '{OpaUrl}' is not a valid absolute URI.");
        }

        if (RequireHttps && uri.Scheme != Uri.UriSchemeHttps)
        {
            throw new InvalidOperationException($"OpaUrl must use HTTPS when RequireHttps is enabled. Current URL: {OpaUrl}");
        }

        if (RequestTimeout <= TimeSpan.Zero)
        {
            throw new InvalidOperationException("RequestTimeout must be greater than zero.");
        }

        if (string.IsNullOrWhiteSpace(ReasonKey))
        {
            throw new InvalidOperationException("ReasonKey cannot be null or whitespace.");
        }
    }
}
