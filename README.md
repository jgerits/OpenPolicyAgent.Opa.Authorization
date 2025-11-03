# OpenPolicyAgent.Opa.Authorization

A .NET NuGet package that provides attribute-based authorization for ASP.NET Core using [Open Policy Agent (OPA)](https://www.openpolicyagent.org/).

## Features

- **Attribute-based authorization**: Use `[OpaAuthorize]` attribute on controllers and methods
- **Seamless ASP.NET Core integration**: Works with existing authentication and authorization infrastructure
- **Policy-based decisions**: Delegate authorization logic to OPA policies
- **Flexible configuration**: Configure OPA URL, policy paths, and custom context data
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
    options.DefaultPolicyPath = "authz/allow";
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
    [OpaAuthorize("authz/documents/allow")]
    [HttpGet("{id}")]
    public IActionResult GetById(int id)
    {
        return Ok($"document{id}");
    }

    // Includes extra information (available as input.context.metadata in OPA)
    [OpaAuthorize("authz/documents/allow", "AdminOperation")]
    [HttpPost]
    public IActionResult Create([FromBody] object document)
    {
        return Created("", document);
    }
}
```

### 3. Create OPA Policy

Create a Rego policy file (e.g., `policy.rego`):

```rego
package authz

# Default deny
default allow = false

# Allow GET requests to /api/documents for authenticated users
allow {
    input.action.name == "GET"
    startswith(input.resource.id, "/api/documents")
    input.subject.id != ""
}

# Allow POST requests only for admin users
allow {
    input.action.name == "POST"
    startswith(input.resource.id, "/api/documents")
    some claim in input.subject.claims
    claim.type == "role"
    claim.value == "admin"
}
```

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
    options.DefaultPolicyPath = "authz/allow";

    // Preferred language key for access denial reasons (default: "en")
    options.ReasonKey = "en";

    // Allow unauthenticated requests (default: false)
    options.AllowUnauthenticated = false;

    // Include authorization token in OPA input (default: false)
    // When enabled, the Authorization header value is included in input.subject.token
    options.IncludeAuthorizationToken = false;
});
```

### Environment Variable Configuration

You can also configure the OPA URL via environment variable:

```bash
export OPA_URL=http://opa-server:8181
```

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

The package sends the following input to OPA:

```json
{
  "subject": {
    "type": "aspnetcore_authentication",
    "id": "<user identity name>",
    "claims": [/* array of user claims */],
    "token": "<authorization header value, if IncludeAuthorizationToken is enabled>"
  },
  "resource": {
    "type": "endpoint",
    "id": "<request path>"
  },
  "action": {
    "name": "<HTTP method>",
    "protocol": "<HTTP protocol>",
    "headers": {/* request headers */}
  },
  "context": {
    "type": "http",
    "host": "<request host>",
    "ip": "<remote IP address>",
    "port": <remote port>,
    "data": {/* custom context data, if provider registered */},
    "metadata": "<extra information from attribute, if provided>"
  }
}
```

**Note**: 
- The `token` field in `subject` is only included when `IncludeAuthorizationToken` is set to `true` in the options and an Authorization header is present.
- The `metadata` field is only included when using `[OpaAuthorize("policy/path", "Extra Information")]` with the second parameter.

## OPA Response Schema

The package expects the following response from OPA:

```json
{
  "allow": true,
  "reason": "Access granted" // or {"en": "Access granted", "es": "Acceso concedido"}
}
```

## Examples

See the [samples directory](./samples) for complete working examples.

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
