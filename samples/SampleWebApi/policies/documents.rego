package authz.documents

import rego.v1

# Default deny - always deny by default for security
default allow := false

# Allow reading documents for authenticated users
allow if {
	input.context.identity.user != ""
}

# Provide localized reason for denial
reason["en"] := "Document access requires authentication" if {
	not allow
	not input.context.identity.user
}
