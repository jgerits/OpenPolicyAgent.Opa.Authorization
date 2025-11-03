# Sample Web API with OPA Authorization

This sample demonstrates how to use the `OpenPolicyAgent.Opa.Authorization` package in an ASP.NET Core Web API.

## Features

- JWT Bearer authentication with a simple demo mode
- OPA-based authorization using the `[OpaAuthorize]` attribute
- Multiple policy paths demonstrated
- Role-based access control via OPA policies

## Prerequisites

1. .NET 8.0 SDK
2. OPA installed (https://www.openpolicyagent.org/docs/latest/#running-opa)

## Running the Sample

### 1. Start OPA Server

In the `policies` directory, start OPA with the sample policies:

```bash
cd policies
opa run --server --addr localhost:8181 policy.rego documents.rego debug_policy.rego
```

For debugging with decision logs enabled:

```bash
opa run --server --addr localhost:8181 \
  --set decision_logs.console=true \
  policy.rego documents.rego debug_policy.rego
```

### 2. Run the Web API

In a separate terminal:

```bash
cd ..
dotnet run
```

The API will be available at `https://localhost:5001` (or the port shown in the console).

### 3. Test the API

#### Test as a regular user

```bash
# This should succeed - authenticated users can GET documents
curl -H "Authorization: Bearer simple-john" https://localhost:5001/api/documents

# This should succeed - authenticated users can GET a specific document
curl -H "Authorization: Bearer simple-john" https://localhost:5001/api/documents/1

# This should fail - only admins can POST documents
curl -X POST -H "Authorization: Bearer simple-john" \
     -H "Content-Type: application/json" \
     -d '{"title":"New Doc","content":"Content","isPublic":true}' \
     https://localhost:5001/api/documents
```

#### Test as an admin user

```bash
# This should succeed - admins can do everything
curl -X POST -H "Authorization: Bearer simple-admin" \
     -H "Content-Type: application/json" \
     -d '{"title":"New Doc","content":"Content","isPublic":true}' \
     https://localhost:5001/api/documents

# This should succeed - admins can delete
curl -X DELETE -H "Authorization: Bearer simple-admin" \
     https://localhost:5001/api/documents/1
```

#### Test without authentication

```bash
# This should fail - authentication required
curl https://localhost:5001/api/documents
```

## How It Works

### 1. Authentication

The sample uses a simplified JWT Bearer authentication for demonstration. In `Program.cs`:

- Tokens starting with `Bearer simple-` are treated as valid authentication
- The username is extracted from the token
- Users with "admin" in their username get the "admin" role
- Other users get the "user" role

In a real application, you would use proper JWT token validation with signing keys, issuers, etc.

### 2. OPA Authorization

The `[OpaAuthorize]` attribute on controller actions triggers OPA policy evaluation:

```csharp
[OpaAuthorize]  // Uses default policy path: authz/allow
[HttpGet]
public IActionResult GetAll() { ... }

[OpaAuthorize("authz/documents/read")]  // Uses custom policy path
[HttpGet("{id}")]
public IActionResult GetById(int id) { ... }

[OpaAuthorize("authz/allow", "CreateDocument")]  // With extra metadata
[HttpPost]
public IActionResult Create([FromBody] CreateDocumentRequest request) { ... }
```

The extra information parameter (second parameter) is available in OPA policies as `input.context.metadata`.

### 3. OPA Policies

Three policy files demonstrate different authorization scenarios:

**policy.rego** - Main authorization policy using modern Rego syntax:
- Uses `import rego.v1` for future-proof policies
- Uses `if` keyword for clearer rule definitions
- Allows GET requests for authenticated users
- Allows POST/DELETE only for users with admin role
- Provides localized denial reasons

**documents.rego** - Document-specific policy:
- Used by the `GetById` action with custom policy path
- Requires authentication to read documents

**debug_policy.rego** - Comprehensive debugging example:
- Logs complete call information for troubleshooting
- Tracks which rules matched during evaluation
- Extracts user roles and authentication status
- Provides detailed denial reasons with context
- Includes decision log with timestamps

All policies follow [OPA best practices](https://www.openpolicyagent.org/docs/policy-language) with:
- `import rego.v1` for modern Rego syntax
- Default deny for security
- Explicit `if` keywords for clarity
- `:=` for assignments
- `some` for explicit iteration

For more information, see:
- [OPA Policy Language Documentation](https://www.openpolicyagent.org/docs/policy-language)
- [Rego Cheat Sheet](https://docs.styra.com/opa/rego-cheat-sheet)

## OPA Input/Output

### Input sent to OPA

```json
{
  "subject": {
    "type": "aspnetcore_authentication",
    "id": "john",
    "claims": [
      {"type": "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name", "value": "john"},
      {"type": "http://schemas.microsoft.com/ws/2008/06/identity/claims/role", "value": "user"},
      ...
    ]
  },
  "resource": {
    "type": "endpoint",
    "id": "/api/documents"
  },
  "action": {
    "name": "GET",
    "protocol": "HTTP/1.1",
    "headers": {...}
  },
  "context": {
    "type": "http",
    "host": "localhost:5001",
    "ip": "127.0.0.1",
    "port": 12345
  }
}
```

### Expected output from OPA

```json
{
  "allow": true,
  "reason": {
    "en": "Access granted"
  }
}
```

Or when denied:

```json
{
  "allow": false,
  "reason": {
    "en": "Admin role required for this action"
  }
}
```

## Debugging Authorization Issues

### Using the Debug Policy

The sample includes a comprehensive debug policy that logs all evaluation details. To test it:

```bash
# Query the debug policy to see complete decision information
curl -X POST http://localhost:8181/v1/data/authz/debug \
  -H 'Content-Type: application/json' \
  -d '{
    "input": {
      "subject": {"id": "john", "claims": [...]},
      "resource": {"id": "/api/documents"},
      "action": {"name": "GET"},
      "context": {"host": "localhost"}
    }
  }'
```

Response includes comprehensive debugging information:

```json
{
  "result": {
    "allow": true,
    "decision_log": {
      "timestamp": 1699123456789,
      "subject": {"id": "john", "claims_count": 3},
      "resource": {"id": "/api/documents"},
      "action": {"name": "GET"},
      "evaluation": {
        "allow": true,
        "matched_rules": ["authenticated_user", "get_documents"],
        "user_roles": ["user"],
        "is_authenticated": true,
        "is_admin": false
      }
    },
    "reason": {"en": "Access granted"}
  }
}
```

### Testing Policies Directly

Test OPA policies without running the application:

```bash
# Test a policy with sample input
echo '{
  "input": {
    "subject": {"id": "admin", "claims": [
      {"type": "http://schemas.microsoft.com/ws/2008/06/identity/claims/role", "value": "admin"}
    ]},
    "resource": {"id": "/api/documents"},
    "action": {"name": "POST"}
  }
}' | opa eval -d policies/policy.rego -I 'data.authz.allow'
```

### Enable Verbose Logging

Configure logging in `appsettings.json`:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "OpenPolicyAgent.Opa.Authorization": "Debug"
    }
  }
}
```

Debug logging will show:
- Complete OPA input payloads
- OPA responses
- Policy evaluation results
- Authorization decisions

## Customization

### Add Custom Context Data

Create a context data provider:

```csharp
public class CustomContextDataProvider : IOpaContextDataProvider
{
    public object GetContextData(HttpContext context)
    {
        return new
        {
            tenant_id = context.Request.Headers["X-Tenant-Id"].ToString(),
            custom_field = "value"
        };
    }
}

// Register in Program.cs
builder.Services.AddOpaContextDataProvider<CustomContextDataProvider>();
```

This data will be available in OPA policies under `input.context.data`.

### Configure Different OPA URL

Via configuration (appsettings.json):

```json
{
  "OpaUrl": "http://my-opa-server:8181"
}
```

Or via environment variable:

```bash
export OPA_URL=http://my-opa-server:8181
```
