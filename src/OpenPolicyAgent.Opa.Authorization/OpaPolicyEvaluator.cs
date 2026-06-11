using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenPolicyAgent.Opa;

namespace OpenPolicyAgent.Opa.Authorization;

/// <summary>
/// Default OPA policy evaluator backed by the OpenPolicyAgent.Opa SDK.
/// </summary>
public class OpaPolicyEvaluator : IOpaPolicyEvaluator
{
    private readonly OpaAuthorizationOptions _options;
    private readonly ILogger<OpaClient> _logger;
    private OpaClient? _opaClient;

    /// <summary>
    /// Initializes a new instance of the <see cref="OpaPolicyEvaluator"/> class.
    /// </summary>
    /// <param name="options">The OPA authorization options.</param>
    /// <param name="logger">The logger used by the underlying OPA client.</param>
    public OpaPolicyEvaluator(
        IOptions<OpaAuthorizationOptions> options,
        ILogger<OpaClient> logger)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        _options = options.Value;
        _logger = logger;
    }

    /// <inheritdoc />
    public Task<OpaResponse?> EvaluateDefaultAsync(object input, CancellationToken cancellationToken = default)
    {
        return EvaluateWithTimeoutAsync(
            () => GetOpaClient().EvaluateDefault<OpaResponse>(input),
            cancellationToken);
    }

    /// <inheritdoc />
    public Task<OpaResponse?> EvaluateAsync(string policyPath, object input, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(policyPath);

        return EvaluateWithTimeoutAsync(
            () => GetOpaClient().Evaluate<OpaResponse>(policyPath, input),
            cancellationToken);
    }

    private OpaClient GetOpaClient()
    {
        if (_opaClient != null)
        {
            return _opaClient;
        }

        var opaUrl = OpaServerUrlResolver.Resolve(_options);
        _opaClient = new OpaClient(opaUrl, _logger);

        return _opaClient;
    }

    private async Task<OpaResponse?> EvaluateWithTimeoutAsync(
        Func<Task<OpaResponse>> evaluate,
        CancellationToken cancellationToken)
    {
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(_options.RequestTimeout);

        return await evaluate().WaitAsync(timeoutCts.Token).ConfigureAwait(false);
    }
}
