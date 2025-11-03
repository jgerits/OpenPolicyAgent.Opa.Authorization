namespace OpenPolicyAgent.Opa.Authorization;

/// <summary>
/// Exception thrown when OPA authorization encounters an error.
/// </summary>
public class OpaAuthorizationException : Exception
{
    /// <summary>
    /// Gets the OPA policy path that was being evaluated when the error occurred.
    /// </summary>
    public string? PolicyPath { get; }

    /// <summary>
    /// Gets the HTTP status code returned by the OPA server, if applicable.
    /// </summary>
    public int? StatusCode { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="OpaAuthorizationException"/> class.
    /// </summary>
    public OpaAuthorizationException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="OpaAuthorizationException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public OpaAuthorizationException(string message) : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="OpaAuthorizationException"/> class with a specified error message and inner exception.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public OpaAuthorizationException(string message, Exception innerException) : base(message, innerException)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="OpaAuthorizationException"/> class with policy path and status code.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="policyPath">The OPA policy path being evaluated.</param>
    /// <param name="statusCode">The HTTP status code from OPA server.</param>
    public OpaAuthorizationException(string message, string? policyPath, int? statusCode = null) : base(message)
    {
        PolicyPath = policyPath;
        StatusCode = statusCode;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="OpaAuthorizationException"/> class with policy path, status code, and inner exception.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="policyPath">The OPA policy path being evaluated.</param>
    /// <param name="statusCode">The HTTP status code from OPA server.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public OpaAuthorizationException(string message, string? policyPath, int? statusCode, Exception innerException) 
        : base(message, innerException)
    {
        PolicyPath = policyPath;
        StatusCode = statusCode;
    }
}
