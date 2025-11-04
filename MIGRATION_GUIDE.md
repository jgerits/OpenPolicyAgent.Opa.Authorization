# OPA Input Structure Migration Guide

This document explains the changes to the OPA input structure, inspired by Trino's OPA integration but adapted for .NET/ASP.NET Core.

## What Changed

The OPA input structure has been reorganized to be more aligned with industry standards like Trino's OPA integration, while still making sense for .NET applications.

### Before (Old Structure)

```json
{
  "subject": {
    "type": "aspnetcore_authentication",
    "id": "<user identity name>",
    "claims": [/* array of user claims */],
    "token": "<authorization header value, if enabled>"
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
    "data": {/* custom context data */},
    "metadata": "<extra information>"
  }
}
```

### After (New Structure)

```json
{
  "context": {
    "identity": {
      "user": "<user identity name>",
      "claims": [/* array of user claims with type, value, valueType, issuer */],
      "groups": [/* array of role values extracted from claims */],
      "token": "<authorization header value, if enabled>"
    },
    "requestId": "<unique request identifier (trace ID)>",
    "softwareStack": {
      "framework": "aspnetcore",
      "frameworkVersion": "<.NET runtime version>"
    },
    "http": {
      "host": "<request host>",
      "ip": "<remote IP address>",
      "port": <remote port>
    },
    "data": {/* custom context data */},
    "metadata": "<extra information>"
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

## Migration Guide for Policies

If you have existing OPA policies, you'll need to update them to use the new structure.

### User Identity

**Before:**
```rego
input.subject.id
input.subject.claims
```

**After:**
```rego
input.context.identity.user
input.context.identity.claims
input.context.identity.groups  # New! Extracted role values
```

### Resource/Endpoint

**Before:**
```rego
input.resource.id
input.resource.type
```

**After:**
```rego
input.action.resource.endpoint.path
input.action.resource.endpoint.type
```

### Action/Operation

**Before:**
```rego
input.action.name
input.action.protocol
input.action.headers
```

**After:**
```rego
input.action.operation
input.action.protocol
input.action.headers
```

### HTTP Context

**Before:**
```rego
input.context.type
input.context.host
input.context.ip
input.context.port
```

**After:**
```rego
input.context.http.host
input.context.http.ip
input.context.http.port
```

### Custom Data and Metadata

These remain in the same location:

```rego
input.context.data      # Still here
input.context.metadata  # Still here
```

## Example Policy Migration

### Before

```rego
package authz

import rego.v1

default allow := false

allow if {
    input.action.name == "GET"
    startswith(input.resource.id, "/api/documents")
    input.subject.id != ""
}

allow if {
    input.action.name == "POST"
    startswith(input.resource.id, "/api/documents")
    has_role("admin")
}

has_role(role) if {
    some claim in input.subject.claims
    claim.type == "http://schemas.microsoft.com/ws/2008/06/identity/claims/role"
    claim.value == role
}
```

### After

```rego
package authz

import rego.v1

default allow := false

allow if {
    input.action.operation == "GET"
    startswith(input.action.resource.endpoint.path, "/api/documents")
    input.context.identity.user != ""
}

allow if {
    input.action.operation == "POST"
    startswith(input.action.resource.endpoint.path, "/api/documents")
    has_role("admin")
}

# New: Check groups first (simpler and faster)
has_role(role) if {
    some group in input.context.identity.groups
    group == role
}

# Backward compatible: also check claims
has_role(role) if {
    some claim in input.context.identity.claims
    claim.type == "http://schemas.microsoft.com/ws/2008/06/identity/claims/role"
    claim.value == role
}
```

## Benefits of the New Structure

1. **Industry Alignment**: Inspired by Trino's OPA integration, making it easier for teams using multiple systems
2. **Better Organization**: Identity information is grouped together under `context.identity`
3. **Groups Extraction**: Role values are automatically extracted to `groups` array for easier access
4. **Request Tracking**: New `requestId` field for correlating authorization decisions with requests
5. **Software Stack Info**: Version information for debugging and policy decisions
6. **Clearer Hierarchy**: Resource information is nested under action, showing the relationship

## Trino Comparison

Our structure is inspired by Trino but adapted for .NET:

| Trino | Our .NET Structure |
|-------|-------------------|
| `context.identity.user` | `context.identity.user` ✓ |
| `context.identity.groups` | `context.identity.groups` ✓ |
| `context.queryId` | `context.requestId` (adapted) |
| `context.softwareStack.trinoVersion` | `context.softwareStack.framework` + `frameworkVersion` (adapted) |
| `action.operation` | `action.operation` ✓ |
| `action.resource.table.*` | `action.resource.endpoint.*` (adapted for web APIs) |

The key differences reflect the different domains:
- Trino works with database objects (tables, schemas, catalogs)
- Our library works with web endpoints (paths, HTTP methods)
