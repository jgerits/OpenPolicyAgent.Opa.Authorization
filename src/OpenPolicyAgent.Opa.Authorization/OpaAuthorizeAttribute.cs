using Microsoft.AspNetCore.Authorization;

namespace OpenPolicyAgent.Opa.Authorization;

/// <summary>
/// Specifies that the class or method that this attribute is applied to requires 
/// authorization via Open Policy Agent.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public class OpaAuthorizeAttribute : AuthorizeAttribute
{
    /// <summary>
    /// Gets or sets the OPA policy path to evaluate for this authorization decision.
    /// If not specified, the default policy path configured in OpaAuthorizationOptions will be used.
    /// </summary>
    public string? PolicyPath { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="OpaAuthorizeAttribute"/> class.
    /// </summary>
    public OpaAuthorizeAttribute()
    {
        Policy = OpaAuthorizationDefaults.PolicyName;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="OpaAuthorizeAttribute"/> class with a specified policy path.
    /// </summary>
    /// <param name="policyPath">The OPA policy path to evaluate.</param>
    public OpaAuthorizeAttribute(string policyPath)
    {
        Policy = OpaAuthorizationDefaults.PolicyName;
        PolicyPath = policyPath;
    }
}
