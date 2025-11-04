package authz

import rego.v1

# Default deny all - deny by default is a security best practice
default allow := false

# Allow GET requests to /api/documents for authenticated users
allow if {
	input.action.operation == "GET"
	startswith(input.action.resource.endpoint.path, "/api/documents")
	input.context.identity.user != ""
}

# Allow POST requests to /api/documents only for admin users
# Can also check input.context.metadata for extra information
allow if {
	input.action.operation == "POST"
	startswith(input.action.resource.endpoint.path, "/api/documents")
	has_role("admin")
	# Optional: Check metadata if present
	# input.context.metadata == "CreateDocument"
}

# Allow DELETE requests to /api/documents only for admin users
# Can also check input.context.metadata for extra information
allow if {
	input.action.operation == "DELETE"
	startswith(input.action.resource.endpoint.path, "/api/documents")
	has_role("admin")
	# Optional: Check metadata if present
	# input.context.metadata == "DeleteDocument"
}

# Helper function to check if user has a specific role
# Using 'some' for explicit iteration over groups
has_role(role) if {
	some group in input.context.identity.groups
	group == role
}

# Backward compatibility: also check claims for roles
has_role(role) if {
	some claim in input.context.identity.claims
	claim.type == "http://schemas.microsoft.com/ws/2008/06/identity/claims/role"
	claim.value == role
}

# Provide localized reasons for denial
# Multiple reason rules allow for specific denial messages
reason["en"] := msg if {
	not allow
	not input.context.identity.user
	msg := "Authentication required"
}

reason["en"] := msg if {
	not allow
	input.context.identity.user != ""
	not has_role("admin")
	input.action.operation in ["POST", "DELETE"]
	msg := "Admin role required for this action"
}

reason["en"] := msg if {
	not allow
	msg := "Access denied by policy"
}
