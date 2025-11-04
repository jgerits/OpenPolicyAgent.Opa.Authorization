# OpenPolicyAgent.Opa.Authorization

A .NET NuGet package that provides attribute-based authorization for ASP.NET Core using [Open Policy Agent (OPA)](https://www.openpolicyagent.org/).

## Features

- **Attribute-based authorization**: Use `[OpaAuthorize]` attribute on controllers and methods
- **Seamless ASP.NET Core integration**: Works with existing authentication and authorization infrastructure
- **Policy-based decisions**: Delegate authorization logic to OPA policies
- **Flexible configuration**: Configure OPA URL, policy paths, and custom context data
- **Development mode**: Disable OPA authorization for local development and debugging
- **Compatible with OPA ecosystem**: Built on top of the official [OpenPolicyAgent.Opa](https://www.nuget.org/packages/OpenPolicyAgent.Opa) package

## Installation

Install the package via NuGet:

```bash
dotnet add package OpenPolicyAgent.Opa.Authorization
```

## Quick Start

### 1. Configure OPA Authorization

In your `Program.cs` or `Startup.cs`:

```csharp
using OpenPolicyAgent.Opa.Authorization;

var builder = WebApplication.CreateBuilder(args);

// Add authentication (required)
builder.Services.AddAuthentication(/* your authentication configuration */);

// Add OPA authorization
builder.Services.AddOpaAuthorization(options =>
{
    options.OpaUrl = "http://localhost:8181";
    options.DefaultPolicyPath = "authz";
});

builder.Services.AddControllers();

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
```

### 2. Use the OpaAuthorize Attribute

Apply the `[OpaAuthorize]` attribute to your controllers or actions:

```csharp
using Microsoft.AspNetCore.Mvc;
using OpenPolicyAgent.Opa.Authorization;

[ApiController]
[Route("api/[controller]")]
public class DocumentsController : ControllerBase
{
    // Uses the default policy path configured in options
    [OpaAuthorize]
    [HttpGet]
    public IActionResult GetAll()
    {
        return Ok(new[] { "document1", "document2" });
    }

    // Uses a custom policy path for this specific action
    [OpaAuthorize("authz/documents")]
    [HttpGet("{id}")]
    public IActionResult GetById(int id)
    {
        return Ok($"document{id}");
    }

    // Includes extra information (available as input.context.metadata in OPA)
    [OpaAuthorize("authz/documents", "AdminOperation")]
    [HttpPost]
    public IActionResult Create([FromBody] object document)
    {
        return Created("", document);
    }
}
```

### 3. Create OPA Policy

Create a Rego policy file (e.g., `policy.rego`) using modern Rego syntax:

```rego
package authz

import rego.v1

# Default deny - always deny by default for security
default allow := false

# Allow GET requests to /api/documents for authenticated users
allow if {
	input.action.operation == "GET"
	startswith(input.action.resource.endpoint.path, "/api/documents")
	input.context.identity.user != ""
}

# Allow POST requests only for admin users
allow if {
	input.action.operation == "POST"
	startswith(input.action.resource.endpoint.path, "/api/documents")
	has_role("admin")
}

# Helper function to check if user has a specific role
# Checks both groups (recommended) and claims (backward compatible)
has_role(role) if {
	some group in input.context.identity.groups
	group == role
}

has_role(role) if {
	some claim in input.context.identity.claims
	claim.type == "http://schemas.microsoft.com/ws/2008/06/identity/claims/role"
	claim.value == role
}
```

**Note**: This example uses modern Rego syntax with:
- `import rego.v1` for future-proof policies
- `if` keyword for clearer rule definitions
- `:=` for explicit assignment
- `some` for explicit iteration

For more information, see the [OPA Policy Language documentation](https://www.openpolicyagent.org/docs/policy-language) and [Rego Cheat Sheet](https://docs.styra.com/opa/rego-cheat-sheet).

### 4. Run OPA Server

Start an OPA server with your policy:

```bash
opa run --server --addr localhost:8181 policy.rego
```

## Configuration Options

### OpaAuthorizationOptions

```csharp
builder.Services.AddOpaAuthorization(options =>
{
    // OPA server URL (default: http://localhost:8181)
    options.OpaUrl = "http://localhost:8181";

    // Default policy path to evaluate (optional)
    // This should be the package path (e.g., "authz") not the rule path (e.g., "authz/allow")
    options.DefaultPolicyPath = "authz";

    // Preferred language key for access denial reasons (default: "en")
    options.ReasonKey = "en";

    // Allow unauthenticated requests (default: false)
    options.AllowUnauthenticated = false;

    // Include authorization token in OPA input (default: false)
    // When enabled, the Authorization header value is included in input.subject.token
    options.IncludeAuthorizationToken = false;

    // Request timeout for OPA calls (default: 30 seconds)
    options.RequestTimeout = TimeSpan.FromSeconds(30);

    // Require HTTPS for OPA URL (default: false)
    // When enabled, non-HTTPS URLs will cause validation errors
    options.RequireHttps = false;

    // Control which headers are sent to OPA (default: true)
    options.IncludeHeaders = true;

    // Customize which headers to exclude (default includes: Authorization, Cookie, X-API-Key, X-Auth-Token)
    options.ExcludedHeaders.Add("Custom-Sensitive-Header");
    // Or clear and start fresh:
    // options.ExcludedHeaders.Clear();

    // Customize which claim types are treated as groups (default includes standard role claim types)
    // These claims will be extracted and included in input.context.identity.groups in OPA policies
    options.GroupClaimTypes.Add("custom-group-claim");
    // Or replace the defaults entirely:
    // options.GroupClaimTypes = new HashSet<string> { "custom-role", "custom-group" };

    // Disable OPA authorization entirely (default: false)
    // When enabled, no calls are made to the OPA server and all authorization attempts are logged locally
    // Useful for development and debugging
    options.DisableAuthorization = false;
});
```

### Environment Variable Configuration

You can also configure the OPA URL via environment variable:

```bash
export OPA_URL=http://opa-server:8181
```

### Disabling OPA Authorization for Development

During development, you may want to disable OPA authorization entirely to simplify testing and debugging. When disabled, no calls are made to the OPA server, and all authorization attempts are logged locally with information about what would have been sent to OPA.

```csharp
builder.Services.AddOpaAuthorization(options =>
{
    options.DisableAuthorization = true; // Disable OPA entirely
    options.DefaultPolicyPath = "authz"; // Other options can still be set
});
```

**When `DisableAuthorization` is enabled:**
- No HTTP calls are made to the OPA server
- All authorization requests automatically succeed
- Each authorization attempt is logged at `Information` level with:
  - The resource (request path)
  - The action (HTTP method)
  - A message indicating authorization is disabled
- Configuration validation is skipped (you don't need a valid OPA URL)

**Example log output:**
```
[Information] OPA Authorization is DISABLED. All authorization requests will be logged and allowed.
[Information] OPA Authorization is DISABLED. Resource: /api/documents/123, Action: GET, Decision: Disabled - No authorization performed
```

**Use cases:**
- Local development without running an OPA server
- Testing application functionality without authorization constraints
- Debugging authorization-related issues
- CI/CD environments where OPA is not available

**Important:** This option should **never** be enabled in production environments. Always ensure `DisableAuthorization` is set to `false` (or omitted, as it defaults to `false`) in production configurations.

## Health Check

Add OPA connectivity health checks to your application:

```csharp
builder.Services.AddHealthChecks()
    .AddOpaHealthCheck(
        name: "opa",
        tags: new[] { "ready", "opa" });

// In your pipeline
app.MapHealthChecks("/health/ready");
```

The health check verifies that the OPA server is reachable and responding.

## Custom Context Data Provider

Inject additional context data into OPA evaluation:

```csharp
public class CustomContextDataProvider : IOpaContextDataProvider
{
    public object GetContextData(HttpContext context)
    {
        return new
        {
            tenant_id = context.Request.Headers["X-Tenant-Id"].ToString(),
            request_time = DateTime.UtcNow
        };
    }
}

// Register the provider
builder.Services.AddOpaContextDataProvider<CustomContextDataProvider>();
```

This data will be available under `input.context.data` in your OPA policy.

## OPA Input Schema

The package sends the following input to OPA (inspired by Trino's OPA integration, adapted for .NET/ASP.NET Core):

```json
{
  "context": {
    "identity": {
      "user": "<user identity name>",
      "claims": [/* array of user claims with type, value, valueType, issuer */],
      "groups": [/* array of role values extracted from claims */],
      "token": "<authorization header value, if IncludeAuthorizationToken is enabled>"
    },
    "requestId": "<unique request identifier (trace ID)>",
    "softwareStack": {
      "framework": "aspnetcore",
      "runtimeVersion": "<.NET runtime version>"
    },
    "http": {
      "host": "<request host>",
      "ip": "<remote IP address>",
      "port": <remote port>
    },
    "data": {/* custom context data, if provider registered */},
    "metadata": "<extra information from attribute, if provided>"
  },
  "action": {
    "operation": "<HTTP method>",
    "resource": {
      "endpoint": {
        "path": "<request path>",
        "type": "endpoint"
      }
    },
    "protocol": "<HTTP protocol>",
    "headers": {/* request headers */}
  }
}
```

**Note**: 
- The structure is inspired by Trino's OPA integration but adapted to make sense for .NET/ASP.NET Core applications.
- The `token` field in `context.identity` is only included when `IncludeAuthorizationToken` is set to `true` in the options and an Authorization header is present.
- The `metadata` field is only included when using `[OpaAuthorize("policy/path", "Extra Information")]` with the second parameter.
- The `headers` field in `action` respects the `IncludeHeaders` and `ExcludedHeaders` configuration. By default, sensitive headers like Authorization, Cookie, X-API-Key, and X-Auth-Token are excluded.

## OPA Response Schema

The package expects the following response from OPA:

```json
{
  "allow": true,
  "reason": "Access granted" // or {"en": "Access granted", "es": "Acceso concedido"}
}
```

**Important**: 
- Your Rego policy **must** define an `allow` rule that returns a boolean value
- The `reason` field is optional but recommended for providing denial explanations
- When using OPA's REST API, these fields appear under `result` (e.g., `{"result": {"allow": true, "reason": "..."}}`)
- The .NET code automatically extracts values from the `result` object
- Additional fields in your policy response (like `decision_log`, `debug_info`) are ignored but won't cause errors

## Examples

See the [samples directory](./samples) for complete working examples, including:
- Basic authorization with role-based access control
- Document-specific policies
- **Debug policy with comprehensive logging** - See `samples/SampleWebApi/policies/debug_policy.rego` for an example that logs complete call information for troubleshooting

## Debugging OPA Policies

### Using the Debug Policy

The sample includes a comprehensive debug policy (`debug_policy.rego`) that logs all evaluation details:

```rego
package authz.debug

import rego.v1

# Returns comprehensive decision log with all input data
decision_log := {
	"timestamp": time.now_ns(),
	"subject": {
		"id": input.subject.id,
		"claims_count": count(input.subject.claims),
	},
	"resource": {
		"id": input.resource.id,
		"type": input.resource.type,
	},
	"action": {
		"name": input.action.name,
	},
	"evaluation": {
		"matched_rules": matched_rules,
		"user_roles": user_roles,
		"is_authenticated": is_authenticated,
		"is_admin": is_admin,
	},
}
```

**Note**: The decision_log does not include `allow` and `reason` to avoid circular references. These are separate top-level fields in the policy response.

To use the debug policy:

1. **Configure your endpoint to use the debug policy path**:
   ```csharp
   [OpaAuthorize("authz/debug")]
   [HttpGet]
   public IActionResult GetDocument() { ... }
   ```

2. **Query the decision log** alongside your allow decision:
   ```bash
   curl -X POST http://localhost:8181/v1/data/authz/debug \
     -H 'Content-Type: application/json' \
     -d @input.json
   ```

3. **Enable OPA decision logging** for automatic audit trails:
   ```bash
   opa run --server --addr localhost:8181 \
     --set decision_logs.console=true \
     policy.rego
   ```

### Best Practices for Debugging

1. **Use `print()` statements during development**:
   ```rego
   allow if {
       print("Checking user:", input.subject.id)
       print("User roles:", user_roles)
       has_role("admin")
   }
   ```

2. **Test policies with sample inputs**:
   ```bash
   opa eval -d policy.rego -i input.json 'data.authz.allow'
   ```

3. **Enable verbose logging in this package**:
   ```json
   {
     "Logging": {
       "LogLevel": {
         "OpenPolicyAgent.Opa.Authorization": "Debug"
       }
     }
   }
   ```

## Security Considerations

### Header Filtering
By default, sensitive headers are excluded from being sent to OPA:
- `Authorization`
- `Cookie`
- `X-API-Key`
- `X-Auth-Token`

You can customize this list via `options.ExcludedHeaders` or disable header inclusion entirely with `options.IncludeHeaders = false`.

### HTTPS Enforcement
For production environments, consider enabling HTTPS enforcement:
```csharp
options.RequireHttps = true;
```

This ensures that the OPA URL uses HTTPS, preventing credentials or sensitive data from being transmitted over unencrypted connections.

### Token Handling
When `IncludeAuthorizationToken` is enabled, be aware that the authorization token will be sent to OPA. Ensure:
1. OPA is running in a secure environment
2. Network communication to OPA is encrypted (use HTTPS)
3. OPA policies do not log tokens or include them in policy decision responses

### Logging
The package uses structured logging at different levels:
- `Trace`: Detailed OPA evaluation information
- `Debug`: OPA input/output (may contain sensitive data - disable in production)
- `Information`: Authorization decisions
- `Warning`: Configuration or connectivity issues
- `Error`: Failures and exceptions

**Important**: Debug and Trace level logging may expose sensitive information. Configure log levels appropriately for your environment.

## Troubleshooting

### OPA Server Connection Issues

**Symptom**: Authorization always fails with HTTP connection errors.

**Solutions**:
1. Verify OPA server is running: `curl http://localhost:8181/health`
2. Check the OPA URL configuration
3. Ensure network connectivity between your app and OPA
4. Use health checks to monitor OPA connectivity: `builder.Services.AddHealthChecks().AddOpaHealthCheck()`

### Policy Evaluation Errors

**Symptom**: "Error evaluating OPA policy" in logs.

**Solutions**:
1. Verify the policy path is correct (e.g., "authz" for package authz with allow and reason rules)
2. Check that the policy exists in OPA: `curl http://localhost:8181/v1/policies`
3. Test the policy directly with curl:
   ```bash
   curl -X POST http://localhost:8181/v1/data/authz \
     -H 'Content-Type: application/json' \
     -d '{"input": {...}}'
   ```
4. Enable debug logging to see the exact input being sent to OPA

### Timeout Issues

**Symptom**: "Timeout communicating with OPA server" errors.

**Solutions**:
1. Increase the request timeout: `options.RequestTimeout = TimeSpan.FromSeconds(60)`
2. Optimize your OPA policies to execute faster
3. Check OPA server performance and resource availability
4. Consider using OPA policy compilation for complex policies

### Unexpected Authorization Denials

**Symptom**: Users are denied access when they should be allowed.

**Solutions**:
1. Enable debug logging to see the OPA input and response
2. Test the policy with the exact input being sent
3. Verify claims are being populated correctly
4. Check if `AllowUnauthenticated` should be enabled
5. Verify the policy is returning `{"allow": true}` (not `{"result": true}`)

### "Could not convert bool result to type OpaResponse" Error

**Symptom**: "Could not convert bool result to type OpenPolicyAgent.Opa.Authorization.OpaResponse" error in logs.

**Cause**: This occurs when the policy path points to a specific rule (e.g., `authz/allow`) instead of the package (e.g., `authz`).

**Solution**:
1. Change your policy path from `authz/allow` to `authz`:
   ```csharp
   options.DefaultPolicyPath = "authz";  // Correct - queries the package
   // NOT: options.DefaultPolicyPath = "authz/allow";  // Wrong - queries only the rule
   ```

2. Your OPA policy package should contain `allow` and optionally `reason` fields:
   ```rego
   package authz
   
   default allow := false
   
   allow if {
       # your rules
   }
   
   reason["en"] := "Access denied" if {
       not allow
   }
   ```

3. When querying `/v1/data/authz`, OPA returns:
   ```json
   {"result": {"allow": true, "reason": {"en": "..."}}}
   ```
   
   But when querying `/v1/data/authz/allow`, OPA returns:
   ```json
   {"result": true}  // Just the boolean value
   ```

The library expects the full object structure with `allow` and `reason` fields, not just a boolean.

### Headers Not Available in OPA

**Symptom**: Headers are missing in the OPA policy input.

**Solutions**:
1. Check if `IncludeHeaders` is set to `true` (default)
2. Verify the header is not in the `ExcludedHeaders` list
3. Remember that sensitive headers are excluded by default for security

## Performance Considerations

### Caching
The package does not include built-in caching of OPA decisions. For high-traffic applications, consider:
1. Using OPA's decision logging and caching features
2. Deploying OPA as a sidecar for minimal network latency
3. Implementing application-level caching if appropriate for your use case

### Policy Optimization
- Keep policies focused and efficient
- Use policy compilation for complex policies
- Consider partial evaluation for data filtering scenarios
- Monitor OPA performance metrics

## Dependencies

This package is built on top of:
- [OpenPolicyAgent.Opa](https://www.nuget.org/packages/OpenPolicyAgent.Opa) - Official OPA C# SDK
- ASP.NET Core 8.0

## Related Packages

- [OpenPolicyAgent.Opa.AspNetCore](https://www.nuget.org/packages/OpenPolicyAgent.Opa.AspNetCore) - Middleware-based OPA authorization for ASP.NET Core

## License

MIT

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.
