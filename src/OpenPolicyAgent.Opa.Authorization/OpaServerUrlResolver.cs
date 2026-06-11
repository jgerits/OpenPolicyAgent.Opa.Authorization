namespace OpenPolicyAgent.Opa.Authorization;

internal static class OpaServerUrlResolver
{
    public static string Resolve(OpaAuthorizationOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        return options.OpaUrl ?? Environment.GetEnvironmentVariable("OPA_URL") ?? "http://localhost:8181";
    }
}
