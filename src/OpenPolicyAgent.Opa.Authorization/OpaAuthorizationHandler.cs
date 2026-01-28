using System.Linq;
using System.Security.Claims;
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
    private readonly OpaClient? _opaClient;
    private readonly OpaAuthorizationOptions _options;
    private readonly ILogger<OpaAuthorizationHandler> _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IOpaContextDataProvider? _contextDataProvider;



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

        // Initialize OPA client only if authorization is not disabled
        if (!_options.DisableAuthorization)
        {
            var opaUrl = _options.OpaUrl ?? Environment.GetEnvironmentVariable("OPA_URL") ?? "http://localhost:8181";
            _opaClient = new OpaClient(opaUrl);
            _logger.LogInformation("OpaAuthorizationHandler initialized with OPA URL: {OpaUrl}", opaUrl);
        }
        else
        {
            _logger.LogWarning("OPA Authorization is DISABLED. All authorization requests will be logged and allowed.");
        }
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

        // If authorization is disabled, log and succeed
        if (_options.DisableAuthorization)
        {
            var resourceId = httpContext.Request.Path;
            var actionName = httpContext.Request.Method;
            
            _logger.LogInformation(
                "OPA Authorization is DISABLED. Resource: {Resource}, Action: {Action}, Decision: Disabled - No authorization performed",
                resourceId,
                actionName);
            
            context.Succeed(requirement);
            return;
        }

        // Try to get OpaAuthorize attribute from endpoint metadata
        var endpoint = httpContext.GetEndpoint();
        var opaAttribute = endpoint?.Metadata.GetMetadata<OpaAuthorizeAttribute>();
        
        // Use attribute values if available, otherwise use requirement values
        var policyPath = opaAttribute?.PolicyPath ?? requirement.PolicyPath ?? _options.DefaultPolicyPath;
        var extraInformation = opaAttribute?.ExtraInformation ?? requirement.ExtraInformation;

        // Validate policy path is not empty or whitespace
        if (string.IsNullOrWhiteSpace(policyPath))
        {
            _logger.LogError("OPA policy path is not configured. Please set DefaultPolicyPath in options or specify a policy path in the OpaAuthorize attribute.");
            context.Fail();
            return;
        }

        try
        {
            // Build OPA input
            var input = BuildOpaInput(httpContext, context, extraInformation);
            
            if (_logger.IsEnabled(LogLevel.Debug))
            {
                _logger.LogDebug("OPA input for request: {Input}", JsonSerializer.Serialize(input));
            }

            // Ensure OPA client is initialized (should never be null at this point)
            if (_opaClient == null)
            {
                _logger.LogError("OPA client is not initialized. This should not happen.");
                context.Fail();
                return;
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
            _logger.LogError(ex, "Unexpected error during OPA authorization for policy '{PolicyPath}': {ErrorType}", 
                policyPath ?? "(default)", ex.GetType().Name);
            context.Fail();
        }
    }

    /// <summary>
    /// Builds the input object for OPA policy evaluation.
    /// The structure is inspired by Trino's OPA integration but adapted for .NET/ASP.NET Core.
    /// </summary>
    private Dictionary<string, object> BuildOpaInput(HttpContext httpContext, AuthorizationHandlerContext authContext, string? extraInformation)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        ArgumentNullException.ThrowIfNull(authContext);

        var userName = authContext.User.Identity?.Name ?? "";
        
        // Convert claims to a serializable format
        var claimsList = authContext.User.Claims.Select(c => new 
        { 
            type = c.Type, 
            value = c.Value,
            valueType = c.ValueType,
            issuer = c.Issuer
        }).ToList();
        
        // Extract groups from role claims based on configured claim types
        var groups = authContext.User.Claims
            .Where(c => _options.GroupClaimTypes.Contains(c.Type))
            .Select(c => c.Value)
            .ToList();

        // Build identity object
        var identity = new Dictionary<string, object>
        {
            { "user", userName },
            { "claims", claimsList },
            { "groups", groups }
        };

        // Add authorization token if configured
        if (_options.IncludeAuthorizationToken)
        {
            var authorizationHeader = httpContext.Request.Headers.Authorization.ToString();
            if (!string.IsNullOrEmpty(authorizationHeader))
            {
                identity.Add("token", authorizationHeader);
            }
        }

        // Generate request ID from trace identifier
        var requestId = httpContext.TraceIdentifier;

        // Get .NET runtime version
        var runtimeVersion = Environment.Version.ToString();

        // Build software stack information
        var softwareStack = new Dictionary<string, object>
        {
            { "framework", "aspnetcore" },
            { "runtimeVersion", runtimeVersion }
        };

        // Build HTTP connection details
        var httpInfo = new Dictionary<string, object>
        {
            { "host", httpContext.Request.Host.ToString() },
            { "ip", httpContext.Connection.RemoteIpAddress?.ToString() ?? "" },
            { "port", httpContext.Connection.RemotePort }
        };

        // Build context object
        var context = new Dictionary<string, object>
        {
            { "identity", identity },
            { "requestId", requestId },
            { "softwareStack", softwareStack },
            { "http", httpInfo }
        };

        // Add custom context data if provider is available
        if (_contextDataProvider != null)
        {
            try
            {
                object contextData = _contextDataProvider.GetContextData(httpContext);
                if (contextData != null)
                {
                    context.Add("data", contextData);
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
            context.Add("metadata", extraInformation);
        }

        // Build headers dictionary with security filtering
        Dictionary<string, string> headers;
        if (_options.IncludeHeaders)
        {
            headers = httpContext.Request.Headers
                .Where(kvp => !_options.ExcludedHeaders.Contains(kvp.Key, StringComparer.OrdinalIgnoreCase))
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value.ToString());
        }
        else
        {
            headers = new Dictionary<string, string>();
        }

        // Build resource object
        var resource = new Dictionary<string, object>
        {
            { "endpoint", new Dictionary<string, object>
                {
                    { "path", httpContext.Request.Path.ToString() },
                    { "type", "endpoint" }
                }
            }
        };

        // Build action object
        var action = new Dictionary<string, object>
        {
            { "operation", httpContext.Request.Method },
            { "resource", resource },
            { "protocol", httpContext.Request.Protocol },
            { "headers", headers }
        };

        // Build final input object
        var input = new Dictionary<string, object>
        {
            { "context", context },
            { "action", action }
        };

        return input;
    }
}
