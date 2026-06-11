namespace OpenPolicyAgent.Opa.Authorization;

/// <summary>
/// Evaluates authorization input against Open Policy Agent.
/// </summary>
public interface IOpaPolicyEvaluator
{
    /// <summary>
    /// Evaluates the configured OPA default decision.
    /// </summary>
    /// <param name="input">The input object to send to OPA.</param>
    /// <param name="cancellationToken">A cancellation token for the evaluation.</param>
    /// <returns>The OPA authorization response.</returns>
    Task<OpaResponse?> EvaluateDefaultAsync(object input, CancellationToken cancellationToken = default);

    /// <summary>
    /// Evaluates a policy path.
    /// </summary>
    /// <param name="policyPath">The OPA policy path to evaluate.</param>
    /// <param name="input">The input object to send to OPA.</param>
    /// <param name="cancellationToken">A cancellation token for the evaluation.</param>
    /// <returns>The OPA authorization response.</returns>
    Task<OpaResponse?> EvaluateAsync(string policyPath, object input, CancellationToken cancellationToken = default);
}
