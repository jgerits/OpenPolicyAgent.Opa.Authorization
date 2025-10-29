package authz

import rego.v1

# Default deny all
default allow := false

# Allow GET requests to /api/documents for authenticated users
allow if {
    input.action.name == "GET"
    startswith(input.resource.id, "/api/documents")
    input.subject.id != ""
}

# Allow POST requests to /api/documents only for admin users
# Can also check input.context.metadata for extra information
allow if {
    input.action.name == "POST"
    startswith(input.resource.id, "/api/documents")
    has_role("admin")
    # Optional: Check metadata if present
    # input.context.metadata == "CreateDocument"
}

# Allow DELETE requests to /api/documents only for admin users
# Can also check input.context.metadata for extra information
allow if {
    input.action.name == "DELETE"
    startswith(input.resource.id, "/api/documents")
    has_role("admin")
    # Optional: Check metadata if present
    # input.context.metadata == "DeleteDocument"
}

# Helper function to check if user has a specific role
has_role(role) if {
    some claim in input.subject.claims
    claim.type == "http://schemas.microsoft.com/ws/2008/06/identity/claims/role"
    claim.value == role
}

# Provide a reason for denial
reason["en"] := msg if {
    not allow
    not input.subject.id
    msg := "Authentication required"
}

reason["en"] := msg if {
    not allow
    input.subject.id != ""
    not has_role("admin")
    input.action.name in ["POST", "DELETE"]
    msg := "Admin role required for this action"
}

reason["en"] := msg if {
    not allow
    msg := "Access denied by policy"
}
