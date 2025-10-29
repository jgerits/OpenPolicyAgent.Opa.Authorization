using Microsoft.AspNetCore.Http;

namespace OpenPolicyAgent.Opa.Authorization;

/// <summary>
/// Provides context data asynchronously for OPA authorization requests.
/// </summary>
public interface IOpaAsyncContextDataProvider
{
    /// <summary>
    /// Gets additional context data asynchronously to be included in the OPA authorization request.
    /// This data will be available under input.context.data in the OPA policy.
    /// </summary>
    /// <param name="context">The HTTP context for the current request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that represents the asynchronous operation and contains an object with additional context data.</returns>
    Task<object> GetContextDataAsync(HttpContext context, CancellationToken cancellationToken = default);
}
