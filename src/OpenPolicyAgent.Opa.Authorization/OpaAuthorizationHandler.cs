using System.Linq;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenPolicyAgent.Opa;

namespace OpenPolicyAgent.Opa.Authorization;

/// <summary>
/// Authorization handler that evaluates OPA policies for authorization decisions.
/// </summary>
public class OpaAuthorizationHandler : AuthorizationHandler<OpaAuthorizationRequirement>
{
    private readonly OpaClient _opaClient;
    private readonly OpaAuthorizationOptions _options;
    private readonly ILogger<OpaAuthorizationHandler> _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IOpaContextDataProvider? _contextDataProvider;

    private const string SubjectType = "aspnetcore_authentication";
    private const string RequestResourceType = "endpoint";
    private const string RequestContextType = "http";

    /// <summary>
    /// Initializes a new instance of the <see cref="OpaAuthorizationHandler"/> class.
    /// </summary>
    /// <param name="options">The OPA authorization options.</param>
    /// <param name="httpContextAccessor">The HTTP context accessor.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="contextDataProvider">Optional context data provider.</param>
    public OpaAuthorizationHandler(
        IOptions<OpaAuthorizationOptions> options,
        IHttpContextAccessor httpContextAccessor,
        ILogger<OpaAuthorizationHandler> logger,
        IOpaContextDataProvider? contextDataProvider = null)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(httpContextAccessor);
        ArgumentNullException.ThrowIfNull(logger);

        _options = options.Value;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
        _contextDataProvider = contextDataProvider;

        // Validate options
        try
        {
            _options.Validate();
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Invalid OPA authorization configuration");
            throw;
        }

        // Initialize OPA client
        var opaUrl = _options.OpaUrl ?? Environment.GetEnvironmentVariable("OPA_URL") ?? "http://localhost:8181";
        _opaClient = new OpaClient(opaUrl);

        _logger.LogInformation("OpaAuthorizationHandler initialized with OPA URL: {OpaUrl}", opaUrl);
    }

    /// <summary>
    /// Makes a decision if authorization is allowed based on the OPA policy evaluation.
    /// </summary>
    /// <param name="context">The authorization context.</param>
    /// <param name="requirement">The OPA authorization requirement.</param>
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        OpaAuthorizationRequirement requirement)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null)
        {
            _logger.LogWarning("HttpContext is null, denying authorization");
            context.Fail();
            return;
        }

        // Check if user is authenticated
        if (!_options.AllowUnauthenticated && 
            (context.User.Identity == null || !context.User.Identity.IsAuthenticated))
        {
            _logger.LogTrace("User is not authenticated, denying authorization");
            context.Fail();
            return;
        }

        // Try to get OpaAuthorize attribute from endpoint metadata
        var endpoint = httpContext.GetEndpoint();
        var opaAttribute = endpoint?.Metadata.GetMetadata<OpaAuthorizeAttribute>();
        
        // Use attribute values if available, otherwise use requirement values
        var policyPath = opaAttribute?.PolicyPath ?? requirement.PolicyPath ?? _options.DefaultPolicyPath;
        var extraInformation = opaAttribute?.ExtraInformation ?? requirement.ExtraInformation;

        try
        {
            // Build OPA input
            var input = BuildOpaInput(httpContext, context, extraInformation);
            
            if (_logger.IsEnabled(LogLevel.Debug))
            {
                _logger.LogDebug("OPA input for request: {Input}", JsonSerializer.Serialize(input));
            }

            // Evaluate OPA policy
            OpaResponse? response;
            if (!string.IsNullOrEmpty(policyPath))
            {
                _logger.LogTrace("Evaluating OPA policy at path: {PolicyPath}", policyPath);
                response = await _opaClient.Evaluate<OpaResponse>(policyPath, input);
            }
            else
            {
                _logger.LogTrace("Evaluating default OPA policy");
                response = await _opaClient.EvaluateDefault<OpaResponse>(input);
            }

            if (response == null)
            {
                _logger.LogWarning("OPA returned null response, denying authorization");
                context.Fail();
                return;
            }

            if (_logger.IsEnabled(LogLevel.Debug))
            {
                _logger.LogDebug("OPA response: Allow={Decision}", response.Decision);
            }

            // Check authorization decision
            if (response.Decision)
            {
                _logger.LogTrace("OPA policy evaluation succeeded, allowing authorization");
                context.Succeed(requirement);
            }
            else
            {
                var reason = response.GetReasonForDecision(_options.ReasonKey) ?? "Access denied by policy";
                _logger.LogInformation("OPA policy evaluation failed: {Reason}", reason);
                context.Fail();
            }
        }
        catch (OpaException ex)
        {
            _logger.LogError(ex, "Error evaluating OPA policy at path '{PolicyPath}'. Message: {Message}", 
                policyPath ?? "(default)", ex.Message);
            context.Fail();
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error communicating with OPA server at {OpaUrl}", _options.OpaUrl);
            context.Fail();
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "Timeout communicating with OPA server at {OpaUrl}", _options.OpaUrl);
            context.Fail();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during OPA authorization");
            context.Fail();
        }
    }

    /// <summary>
    /// Builds the input object for OPA policy evaluation.
    /// </summary>
    private Dictionary<string, object> BuildOpaInput(HttpContext httpContext, AuthorizationHandlerContext authContext, string? extraInformation)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        ArgumentNullException.ThrowIfNull(authContext);

        var subjectId = authContext.User.Identity?.Name ?? "";
        
        // Convert claims to a serializable format
        var claimsList = authContext.User.Claims.Select(c => new 
        { 
            type = c.Type, 
            value = c.Value,
            valueType = c.ValueType,
            issuer = c.Issuer
        }).ToList();
        
        var subjectClaims = claimsList as object ?? new { };

        string resourceId = httpContext.Request.Path;
        string actionName = httpContext.Request.Method;
        string actionProtocol = httpContext.Request.Protocol;
        Dictionary<string, string> headers = httpContext.Request.Headers.ToDictionary(
            kvp => kvp.Key, 
            kvp => kvp.Value.ToString());

        string contextRemoteAddr = httpContext.Connection.RemoteIpAddress?.ToString() ?? "";
        string contextRemoteHost = httpContext.Request.Host.ToString();
        int contextRemotePort = httpContext.Connection.RemotePort;

        Dictionary<string, object> ctx = new Dictionary<string, object>
        {
            { "type", RequestContextType },
            { "host", contextRemoteHost },
            { "ip", contextRemoteAddr },
            { "port", contextRemotePort },
        };

        // Add custom context data if provider is available
        if (_contextDataProvider != null)
        {
            try
            {
                object contextData = _contextDataProvider.GetContextData(httpContext);
                if (contextData != null)
                {
                    ctx.Add("data", contextData);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error retrieving custom context data from provider");
            }
        }

        // Add extra information if available
        if (!string.IsNullOrEmpty(extraInformation))
        {
            ctx.Add("metadata", extraInformation);
        }

        Dictionary<string, object> subject = new Dictionary<string, object>
        {
            { "type", SubjectType },
            { "id", subjectId },
            { "claims", subjectClaims },
        };

        // Add authorization token if configured
        if (_options.IncludeAuthorizationToken)
        {
            var authorizationHeader = httpContext.Request.Headers.Authorization.ToString();
            if (!string.IsNullOrEmpty(authorizationHeader))
            {
                subject.Add("token", authorizationHeader);
            }
        }

        Dictionary<string, object> input = new Dictionary<string, object>
        {
            { "subject", subject },
            { "resource", new Dictionary<string, object>
                {
                    { "type", RequestResourceType },
                    { "id", resourceId },
                }
            },
            { "action", new Dictionary<string, object>
                {
                    { "name", actionName },
                    { "protocol", actionProtocol },
                    { "headers", headers },
                }
            },
            { "context", ctx },
        };

        return input;
    }
}
