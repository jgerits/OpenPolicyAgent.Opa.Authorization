using System.Net;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace OpenPolicyAgent.Opa.Authorization.Tests;

public class OpaHealthCheckTests
{
    [Fact]
    public async Task CheckHealthAsync_WhenAuthorizationDisabled_ShouldReturnHealthyWithoutHttpCall()
    {
        // Arrange
        var handler = new TestHttpMessageHandler(HttpStatusCode.InternalServerError);
        var healthCheck = CreateHealthCheck(handler, options => options.DisableAuthorization = true);

        // Act
        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        // Assert
        Assert.Equal(HealthStatus.Healthy, result.Status);
        Assert.Equal(0, handler.CallCount);
    }

    [Fact]
    public async Task CheckHealthAsync_WhenHealthEndpointReturnsSuccess_ShouldReturnHealthy()
    {
        // Arrange
        var handler = new TestHttpMessageHandler(HttpStatusCode.OK);
        var healthCheck = CreateHealthCheck(handler);

        // Act
        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        // Assert
        Assert.Equal(HealthStatus.Healthy, result.Status);
        Assert.Equal(new Uri("http://localhost:8181/health"), handler.LastRequestUri);
    }

    [Fact]
    public async Task CheckHealthAsync_WhenHealthEndpointReturnsFailure_ShouldReturnUnhealthy()
    {
        // Arrange
        var handler = new TestHttpMessageHandler(HttpStatusCode.ServiceUnavailable);
        var healthCheck = CreateHealthCheck(handler);

        // Act
        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        // Assert
        Assert.Equal(HealthStatus.Unhealthy, result.Status);
    }

    private static OpaHealthCheck CreateHealthCheck(
        TestHttpMessageHandler handler,
        Action<OpaAuthorizationOptions>? configureOptions = null)
    {
        var options = new OpaAuthorizationOptions
        {
            OpaUrl = "http://localhost:8181",
            RequestTimeout = TimeSpan.FromSeconds(5)
        };
        configureOptions?.Invoke(options);

        var httpClientFactory = new TestHttpClientFactory(new HttpClient(handler));
        return new OpaHealthCheck(
            Options.Create(options),
            httpClientFactory,
            NullLogger<OpaHealthCheck>.Instance);
    }

    private sealed class TestHttpClientFactory : IHttpClientFactory
    {
        private readonly HttpClient _httpClient;

        public TestHttpClientFactory(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public HttpClient CreateClient(string name)
        {
            return _httpClient;
        }
    }

    private sealed class TestHttpMessageHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode _statusCode;

        public TestHttpMessageHandler(HttpStatusCode statusCode)
        {
            _statusCode = statusCode;
        }

        public int CallCount { get; private set; }

        public Uri? LastRequestUri { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            CallCount++;
            LastRequestUri = request.RequestUri;

            return Task.FromResult(new HttpResponseMessage(_statusCode));
        }
    }
}
