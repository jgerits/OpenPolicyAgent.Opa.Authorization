package authz.debug

import rego.v1

# This policy file demonstrates comprehensive debugging and logging practices
# Use this as a reference when troubleshooting authorization issues

# Default deny
default allow := false

# Decision log with complete input context for debugging
# This helps track what data was available during policy evaluation
decision_log := {
	"timestamp": time.now_ns(),
	"subject": {
		"id": input.subject.id,
		"type": input.subject.type,
		"claims_count": count(input.subject.claims),
		"has_token": object.get(input.subject, "token", null) != null,
	},
	"resource": {
		"id": input.resource.id,
		"type": input.resource.type,
	},
	"action": {
		"name": input.action.name,
		"protocol": input.action.protocol,
		"headers_count": count(object.get(input.action, "headers", {})),
	},
	"context": {
		"type": input.context.type,
		"host": input.context.host,
		"ip": input.context.ip,
		"port": input.context.port,
		"has_metadata": object.get(input.context, "metadata", null) != null,
		"has_custom_data": object.get(input.context, "data", null) != null,
	},
	"evaluation": {
		"allow": allow,
		"reason": reason,
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
	input.action.name == "GET"
	startswith(input.resource.id, "/api/documents")
}

matched_rules contains "post_documents" if {
	input.action.name == "POST"
	startswith(input.resource.id, "/api/documents")
}

matched_rules contains "delete_documents" if {
	input.action.name == "DELETE"
	startswith(input.resource.id, "/api/documents")
}

# Extract all user roles for visibility
user_roles contains role if {
	some claim in input.subject.claims
	claim.type == "http://schemas.microsoft.com/ws/2008/06/identity/claims/role"
	role := claim.value
}

# Helper to check authentication status
is_authenticated if {
	input.subject.id != ""
}

# Helper to check admin status
is_admin if {
	has_role("admin")
}

# Helper function to check if user has a specific role
has_role(role) if {
	some claim in input.subject.claims
	claim.type == "http://schemas.microsoft.com/ws/2008/06/identity/claims/role"
	claim.value == role
}

# Authorization rules with detailed logging context
# Allow GET requests to /api/documents for authenticated users
allow if {
	input.action.name == "GET"
	startswith(input.resource.id, "/api/documents")
	is_authenticated
}

# Allow POST requests to /api/documents only for admin users
allow if {
	input.action.name == "POST"
	startswith(input.resource.id, "/api/documents")
	is_admin
}

# Allow DELETE requests to /api/documents only for admin users
allow if {
	input.action.name == "DELETE"
	startswith(input.resource.id, "/api/documents")
	is_admin
}

# Detailed denial reasons with context
reason["en"] := msg if {
	not allow
	not is_authenticated
	msg := sprintf("Authentication required. Subject ID: '%v'", [input.subject.id])
}

reason["en"] := msg if {
	not allow
	is_authenticated
	not is_admin
	input.action.name in ["POST", "DELETE"]
	msg := sprintf("Admin role required for %v. User '%v' has roles: %v", [
		input.action.name,
		input.subject.id,
		concat(", ", user_roles),
	])
}

reason["en"] := msg if {
	not allow
	msg := sprintf("Access denied by policy for user '%v' attempting %v on %v", [
		input.subject.id,
		input.action.name,
		input.resource.id,
	])
}

# Additional debugging metadata
debug_info := {
	"input_structure": {
		"has_subject": object.get(input, "subject", null) != null,
		"has_resource": object.get(input, "resource", null) != null,
		"has_action": object.get(input, "action", null) != null,
		"has_context": object.get(input, "context", null) != null,
	},
	"claim_types": {type | some claim in input.subject.claims; type := claim.type},
	"environment": {
		"opa_version": opa.runtime().version,
		"policy_evaluation_time_ns": time.now_ns(),
	},
}
