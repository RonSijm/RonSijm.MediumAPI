using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace RonSijm.MediumAPI.Example.IntegrationTest;

public class HelloWorldEndpointTests(WebApplicationFactory<Program> factory)
	: IClassFixture<WebApplicationFactory<Program>>
{
	[Fact]
	public async Task Get_Hello_ReturnsOkWithHelloWorld()
	{
		var client = factory.CreateClient();

		var response = await client.GetAsync("/hello");

		response.EnsureSuccessStatusCode();
		var content = await response.Content.ReadAsStringAsync();
		Assert.Equal("\"Hello, World!\"", content);
	}
}
