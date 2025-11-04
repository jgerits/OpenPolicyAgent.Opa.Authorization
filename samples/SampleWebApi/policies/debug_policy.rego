package authz.debug

import rego.v1

# This policy file demonstrates comprehensive debugging and logging practices
# Use this as a reference when troubleshooting authorization issues

# Default deny
default allow := false

# Decision log with complete input context for debugging
# This helps track what data was available during policy evaluation
# Note: Query this separately from 'allow' to avoid circular references
decision_log := {
	"timestamp": time.now_ns(),
	"identity": {
		"user": input.context.identity.user,
		"claims_count": count(object.get(input.context.identity, "claims", [])),
		"groups_count": count(object.get(input.context.identity, "groups", [])),
		"has_token": object.get(input.context.identity, "token", null) != null,
	},
	"resource": {
		"path": input.action.resource.endpoint.path,
		"type": input.action.resource.endpoint.type,
	},
	"action": {
		"operation": input.action.operation,
		"protocol": input.action.protocol,
		"headers_count": count(object.get(input.action, "headers", {})),
	},
	"context": {
		"requestId": input.context.requestId,
		"softwareStack": input.context.softwareStack,
		"http": input.context.http,
		"has_metadata": object.get(input.context, "metadata", null) != null,
		"has_custom_data": object.get(input.context, "data", null) != null,
	},
	"evaluation": {
		"user_groups": user_groups,
		"user_roles": user_roles,
		"is_authenticated": is_authenticated,
		"is_admin": is_admin,
		"matched_rules": matched_rules,
	},
}

# Track which authorization rules were evaluated and matched
matched_rules contains "authenticated_user" if {
	is_authenticated
}

matched_rules contains "admin_user" if {
	is_admin
}

matched_rules contains "get_documents" if {
	input.action.operation == "GET"
	startswith(input.action.resource.endpoint.path, "/api/documents")
}

matched_rules contains "post_documents" if {
	input.action.operation == "POST"
	startswith(input.action.resource.endpoint.path, "/api/documents")
}

matched_rules contains "delete_documents" if {
	input.action.operation == "DELETE"
	startswith(input.action.resource.endpoint.path, "/api/documents")
}

# Extract all user groups for visibility
user_groups contains group if {
	some group in object.get(input.context.identity, "groups", [])
}

# Extract all user roles from claims for visibility
# Safe iteration with default empty array
user_roles contains role if {
	some claim in object.get(input.context.identity, "claims", [])
	claim.type == "http://schemas.microsoft.com/ws/2008/06/identity/claims/role"
	role := claim.value
}

# Helper to check authentication status
is_authenticated if {
	input.context.identity.user != ""
}

# Helper to check admin status
is_admin if {
	has_role("admin")
}

# Helper function to check if user has a specific role
# First check groups (simpler and faster)
has_role(role) if {
	some group in object.get(input.context.identity, "groups", [])
	group == role
}

# Backward compatibility: also check claims for roles
has_role(role) if {
	some claim in object.get(input.context.identity, "claims", [])
	claim.type == "http://schemas.microsoft.com/ws/2008/06/identity/claims/role"
	claim.value == role
}

# Authorization rules with detailed logging context
# Allow GET requests to /api/documents for authenticated users
allow if {
	input.action.operation == "GET"
	startswith(input.action.resource.endpoint.path, "/api/documents")
	is_authenticated
}

# Allow POST requests to /api/documents only for admin users
allow if {
	input.action.operation == "POST"
	startswith(input.action.resource.endpoint.path, "/api/documents")
	is_admin
}

# Allow DELETE requests to /api/documents only for admin users
allow if {
	input.action.operation == "DELETE"
	startswith(input.action.resource.endpoint.path, "/api/documents")
	is_admin
}

# Detailed denial reasons with context
reason["en"] := msg if {
	not allow
	not is_authenticated
	msg := sprintf("Authentication required. User: '%v'", [input.context.identity.user])
}

reason["en"] := msg if {
	not allow
	is_authenticated
	not is_admin
	input.action.operation in ["POST", "DELETE"]
	msg := sprintf("Admin role required for %v. User '%v' has groups: %v, roles: %v", [
		input.action.operation,
		input.context.identity.user,
		concat(", ", user_groups),
		concat(", ", user_roles),
	])
}

reason["en"] := msg if {
	not allow
	msg := sprintf("Access denied by policy for user '%v' attempting %v on %v", [
		input.context.identity.user,
		input.action.operation,
		input.action.resource.endpoint.path,
	])
}

# Additional debugging metadata
# Safe iteration with default empty array for claims
debug_info := {
	"input_structure": {
		"has_context": object.get(input, "context", null) != null,
		"has_action": object.get(input, "action", null) != null,
		"has_identity": object.get(input.context, "identity", null) != null,
		"has_resource": object.get(input.action, "resource", null) != null,
	},
	"claim_types": {type | some claim in object.get(input.context.identity, "claims", []); type := claim.type},
	"environment": {
		"opa_version": opa.runtime().version,
		"policy_evaluation_time_ns": time.now_ns(),
	},
}
