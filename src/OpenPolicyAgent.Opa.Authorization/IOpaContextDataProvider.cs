using Microsoft.AspNetCore.Http;

namespace OpenPolicyAgent.Opa.Authorization;

/// <summary>
/// Provides context data for OPA authorization requests.
/// </summary>
public interface IOpaContextDataProvider
{
    /// <summary>
    /// Gets additional context data to be included in the OPA authorization request.
    /// This data will be available under input.context.data in the OPA policy.
    /// </summary>
    /// <param name="context">The HTTP context for the current request.</param>
    /// <returns>An object containing additional context data.</returns>
    object GetContextData(HttpContext context);
}
