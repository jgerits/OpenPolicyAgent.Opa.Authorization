package authz.documents

import rego.v1

# Default deny
default read := false

# Allow reading documents for authenticated users
read if {
    input.subject.id != ""
}

# Provide reason
reason["en"] := "Document access requires authentication" if {
    not read
    not input.subject.id
}
