using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace OpenPolicyAgent.Opa.Authorization.Tests;

public class OpaAuthorizationHandlerTests
{
    [Fact]
    public async Task AuthorizeAsync_WhenOpaAllows_ShouldSucceedAndSendPolicyInput()
    {
        // Arrange
        var evaluator = new TestOpaPolicyEvaluator
        {
            Response = new OpaResponse { Decision = true }
        };
        var httpContext = CreateHttpContext();
        httpContext.SetEndpoint(new Endpoint(
            _ => Task.CompletedTask,
            new EndpointMetadataCollection(new OpaAuthorizeAttribute("document.read") { PolicyPath = "authz/documents" }),
            "test"));
        var authorizationService = CreateAuthorizationService(httpContext, evaluator);

        // Act
        var result = await authorizationService.AuthorizeAsync(
            httpContext.User,
            resource: null,
            OpaAuthorizationDefaults.PolicyName);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Equal("authz/documents", evaluator.LastPolicyPath);
        Assert.False(evaluator.UsedDefaultPolicy);

        var input = Assert.IsType<Dictionary<string, object>>(evaluator.LastInput);
        var policies = Assert.IsType<string[]>(input["policies"]);
        Assert.Equal(new[] { "document.read" }, policies);
    }

    [Fact]
    public async Task AuthorizeAsync_WhenOpaDenies_ShouldFail()
    {
        // Arrange
        var evaluator = new TestOpaPolicyEvaluator
        {
            Response = new OpaResponse { Decision = false, Reason = "No access" }
        };
        var httpContext = CreateHttpContext();
        var authorizationService = CreateAuthorizationService(httpContext, evaluator);

        // Act
        var result = await authorizationService.AuthorizeAsync(
            httpContext.User,
            resource: null,
            OpaAuthorizationDefaults.PolicyName);

        // Assert
        Assert.False(result.Succeeded);
    }

    [Fact]
    public async Task AuthorizeAsync_WhenOpaReturnsNull_ShouldFail()
    {
        // Arrange
        var evaluator = new TestOpaPolicyEvaluator();
        var httpContext = CreateHttpContext();
        var authorizationService = CreateAuthorizationService(httpContext, evaluator);

        // Act
        var result = await authorizationService.AuthorizeAsync(
            httpContext.User,
            resource: null,
            OpaAuthorizationDefaults.PolicyName);

        // Assert
        Assert.False(result.Succeeded);
    }

    [Fact]
    public async Task AuthorizeAsync_WithoutPolicyPath_ShouldUseDefaultOpaDecision()
    {
        // Arrange
        var evaluator = new TestOpaPolicyEvaluator
        {
            Response = new OpaResponse { Decision = true }
        };
        var httpContext = CreateHttpContext();
        var authorizationService = CreateAuthorizationService(
            httpContext,
            evaluator,
            options => options.DefaultPolicyPath = null);

        // Act
        var result = await authorizationService.AuthorizeAsync(
            httpContext.User,
            resource: null,
            OpaAuthorizationDefaults.PolicyName);

        // Assert
        Assert.True(result.Succeeded);
        Assert.True(evaluator.UsedDefaultPolicy);
    }

    [Fact]
    public async Task AuthorizeAsync_ShouldRespectIncludedAndExcludedClaimTypes()
    {
        // Arrange
        var evaluator = new TestOpaPolicyEvaluator
        {
            Response = new OpaResponse { Decision = true }
        };
        var httpContext = CreateHttpContext();
        var authorizationService = CreateAuthorizationService(
            httpContext,
            evaluator,
            options =>
            {
                options.IncludedClaimTypes.Add(ClaimTypes.Role);
                options.ExcludedClaimTypes.Add(ClaimTypes.Role);
            });

        // Act
        var result = await authorizationService.AuthorizeAsync(
            httpContext.User,
            resource: null,
            OpaAuthorizationDefaults.PolicyName);

        // Assert
        Assert.True(result.Succeeded);
        var input = Assert.IsType<Dictionary<string, object>>(evaluator.LastInput);
        var context = Assert.IsType<Dictionary<string, object>>(input["context"]);
        var identity = Assert.IsType<Dictionary<string, object>>(context["identity"]);
        var claims = Assert.IsAssignableFrom<IEnumerable<object>>(identity["claims"]).ToList();
        var groups = Assert.IsAssignableFrom<IEnumerable<string>>(identity["groups"]).ToList();

        Assert.Single(claims);
        Assert.Equal(new[] { "admin" }, groups);
    }

    private static IAuthorizationService CreateAuthorizationService(
        HttpContext httpContext,
        IOpaPolicyEvaluator evaluator,
        Action<OpaAuthorizationOptions>? configureOptions = null)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddOptions();
        services.Configure<OpaAuthorizationOptions>(options =>
        {
            options.OpaUrl = "http://localhost:8181";
            options.DefaultPolicyPath = "authz";
            configureOptions?.Invoke(options);
        });
        services.AddSingleton<IHttpContextAccessor>(new HttpContextAccessor { HttpContext = httpContext });
        services.AddSingleton(evaluator);
        services.AddSingleton<IAuthorizationHandler, OpaAuthorizationHandler>();
        services.AddAuthorization(options =>
        {
            options.AddPolicy(OpaAuthorizationDefaults.PolicyName, policy =>
            {
                policy.Requirements.Add(new OpaAuthorizationRequirement());
            });
        });

        return services.BuildServiceProvider().GetRequiredService<IAuthorizationService>();
    }

    private static DefaultHttpContext CreateHttpContext()
    {
        var httpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(
            [
                new Claim(ClaimTypes.Name, "alice"),
                new Claim(ClaimTypes.Role, "admin"),
                new Claim(ClaimTypes.Email, "alice@example.com")
            ], "test"))
        };

        httpContext.Request.Method = HttpMethods.Get;
        httpContext.Request.Path = "/documents/1";
        httpContext.Request.Headers.Authorization = "Bearer token";

        return httpContext;
    }

    private sealed class TestOpaPolicyEvaluator : IOpaPolicyEvaluator
    {
        public OpaResponse? Response { get; init; }

        public object? LastInput { get; private set; }

        public string? LastPolicyPath { get; private set; }

        public bool UsedDefaultPolicy { get; private set; }

        public Task<OpaResponse?> EvaluateDefaultAsync(object input, CancellationToken cancellationToken = default)
        {
            LastInput = input;
            UsedDefaultPolicy = true;
            return Task.FromResult(Response);
        }

        public Task<OpaResponse?> EvaluateAsync(string policyPath, object input, CancellationToken cancellationToken = default)
        {
            LastInput = input;
            LastPolicyPath = policyPath;
            return Task.FromResult(Response);
        }
    }
}
