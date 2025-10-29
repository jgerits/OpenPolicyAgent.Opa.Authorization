using Microsoft.AspNetCore.Http;
using Xunit;

namespace OpenPolicyAgent.Opa.Authorization.Tests;

public class OpaAsyncContextDataProviderTests
{
    [Fact]
    public async Task GetContextDataAsync_ShouldReturnData()
    {
        // Arrange
        var provider = new TestAsyncContextDataProvider();
        var context = new DefaultHttpContext();

        // Act
        var data = await provider.GetContextDataAsync(context);

        // Assert
        Assert.NotNull(data);
        var dataDict = Assert.IsType<Dictionary<string, string>>(data);
        Assert.Equal("async_test_value", dataDict["test_key"]);
    }

    private class TestAsyncContextDataProvider : IOpaAsyncContextDataProvider
    {
        public async Task<object> GetContextDataAsync(HttpContext context, CancellationToken cancellationToken = default)
        {
            await Task.Delay(1, cancellationToken);
            return new Dictionary<string, string> { { "test_key", "async_test_value" } };
        }
    }
}
