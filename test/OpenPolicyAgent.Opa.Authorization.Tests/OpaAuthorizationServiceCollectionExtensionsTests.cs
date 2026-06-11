using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace OpenPolicyAgent.Opa.Authorization.Tests;

public class OpaAuthorizationServiceCollectionExtensionsTests
{
    [Fact]
    public void AddOpaAuthorization_ShouldRegisterServices()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging(); // Add logging services

        // Act
        services.AddOpaAuthorization();
        var provider = services.BuildServiceProvider();

        // Assert
        var authHandler = provider.GetServices<IAuthorizationHandler>();
        Assert.Contains(authHandler, h => h is OpaAuthorizationHandler);

        var httpContextAccessor = provider.GetService<IHttpContextAccessor>();
        Assert.NotNull(httpContextAccessor);

        var opaPolicyEvaluator = provider.GetService<IOpaPolicyEvaluator>();
        Assert.NotNull(opaPolicyEvaluator);
    }

    [Fact]
    public void AddOpaAuthorization_WithOptions_ShouldConfigureOptions()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging(); // Add logging services
        var expectedUrl = "http://test-opa:8181";

        // Act
        services.AddOpaAuthorization(options =>
        {
            options.OpaUrl = expectedUrl;
        });
        var provider = services.BuildServiceProvider();

        // Assert
        var options = provider.GetService<Microsoft.Extensions.Options.IOptions<OpaAuthorizationOptions>>();
        Assert.NotNull(options);
        Assert.Equal(expectedUrl, options.Value.OpaUrl);
    }

    [Fact]
    public async Task AddOpaAuthorization_WhenDisabled_ShouldNotRequireValidOpaUrl()
    {
        // Arrange
        var httpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(
            [
                new Claim(ClaimTypes.Name, "alice")
            ], "test"))
        };
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IHttpContextAccessor>(new HttpContextAccessor { HttpContext = httpContext });

        // Act
        services.AddOpaAuthorization(options =>
        {
            options.DisableAuthorization = true;
            options.OpaUrl = "invalid-url";
        });
        var provider = services.BuildServiceProvider();
        var authorizationService = provider.GetRequiredService<IAuthorizationService>();
        var result = await authorizationService.AuthorizeAsync(
            httpContext.User,
            resource: null,
            OpaAuthorizationDefaults.PolicyName);

        // Assert
        Assert.True(result.Succeeded);
    }

    [Fact]
    public void AddOpaAuthorization_ShouldThrowWhenServicesIsNull()
    {
        // Arrange
        IServiceCollection? services = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            services!.AddOpaAuthorization());
    }

    [Fact]
    public void AddOpaAuthorization_ShouldThrowWhenConfigureOptionsIsNull()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            services.AddOpaAuthorization(null!));
    }

    [Fact]
    public void AddOpaContextDataProvider_ShouldRegisterProvider()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddOpaContextDataProvider<TestContextDataProvider>();
        var provider = services.BuildServiceProvider();

        // Assert
        var contextProvider = provider.GetService<IOpaContextDataProvider>();
        Assert.NotNull(contextProvider);
        Assert.IsType<TestContextDataProvider>(contextProvider);
    }

    private class TestContextDataProvider : IOpaContextDataProvider
    {
        public object GetContextData(HttpContext context)
        {
            return new { test = "data" };
        }
    }
}
