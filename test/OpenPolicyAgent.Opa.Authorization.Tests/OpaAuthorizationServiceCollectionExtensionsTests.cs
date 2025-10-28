using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;

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
