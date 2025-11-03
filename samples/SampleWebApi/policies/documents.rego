package authz.documents

import rego.v1

# Default deny - always deny by default for security
default read := false

# Allow reading documents for authenticated users
read if {
	input.subject.id != ""
}

# Provide localized reason for denial
reason["en"] := "Document access requires authentication" if {
	not read
	not input.subject.id
}
