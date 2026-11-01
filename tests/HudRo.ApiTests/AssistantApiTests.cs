using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using DataStructures.Application.Assistant;
using DataStructures.Infrastructure.Assistant;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

namespace HudRo.ApiTests;

public sealed class AssistantApiTests
{
  [Theory]
  [InlineData("menu-v1")]
  [InlineData("menu-concise-v1")]
  public async Task Demo_ReturnsGroundedSuggestionWithoutChangingOperations(string template)
  {
    using var app = new Factory();
    using var client = app.CreateClient();
    var before = await client.GetStringAsync("/api/operations");
    using var response = await client.PostAsJsonAsync("/api/assistant/menu", new { message = "Gợi ý món cho hai người", template });
    response.EnsureSuccessStatusCode();
    var result = await response.Content.ReadFromJsonAsync<AssistantResult>();
    Assert.NotNull(result);
    Assert.Equal("demo", result.Mode);
    Assert.Equal(template, result.Template);
    Assert.Equal(new[] { "get_menu" }, result.Tools);
    using var operations = JsonDocument.Parse(before);
    var menu = operations.RootElement.GetProperty("menu").EnumerateArray()
      .Select(x => x.GetProperty("code").GetString()).ToArray();
    Assert.NotEmpty(result.Suggestion.MenuCodes);
    Assert.All(result.Suggestion.MenuCodes, code => Assert.Contains(code, menu));
    Assert.Equal(before, await client.GetStringAsync("/api/operations"));
  }

  [Theory]
  [InlineData("{}")]
  [InlineData("null")]
  [InlineData("{\"message\":null}")]
  [InlineData("{\"message\":\" \"}")]
  [InlineData("{\"message\":\"test\",\"template\":\"unknown\"}")]
  [InlineData("{\"message\":42}")]
  [InlineData("{broken")]
  public async Task InvalidInput_Returns400WithoutCallingModel(string body)
  {
    var model = new FailingModel(new Exception("Must not call provider"));
    using var app = new Factory(model);
    using var client = app.CreateClient();
    using var response = await client.PostAsync("/api/assistant/menu", new StringContent(body, Encoding.UTF8, "application/json"));
    Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    Assert.Equal(0, model.Calls);
  }

  [Fact]
  public async Task OversizedMessage_Returns400()
  {
    using var app = new Factory();
    using var client = app.CreateClient();
    using var response = await client.PostAsJsonAsync("/api/assistant/menu", new { message = new string('a', 2001) });
    Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
  }

  [Theory]
  [InlineData("provider", 502)]
  [InlineData("output", 502)]
  [InlineData("json", 502)]
  [InlineData("timeout", 504)]
  public async Task Failures_ReturnSanitizedProblemDetails(string failure, int status)
  {
    Exception error = failure switch
    {
      "provider" => new HttpRequestException("secret-provider-detail"),
      "output" => new InvalidDataException("secret-provider-detail"),
      "json" => new JsonException("secret-provider-detail"),
      _ => new TaskCanceledException("secret-provider-detail")
    };
    using var app = new Factory(new FailingModel(error));
    using var client = app.CreateClient();
    using var response = await client.PostAsJsonAsync("/api/assistant/menu", new { message = "test" });
    Assert.Equal(status, (int)response.StatusCode);
    Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    Assert.DoesNotContain("secret-provider-detail", await response.Content.ReadAsStringAsync());
  }

  [Fact]
  public async Task ParallelRequests_HaveIndependentToolConversations()
  {
    using var app = new Factory();
    using var client = app.CreateClient();
    var tasks = Enumerable.Range(0, 12).Select(async i =>
    {
      using var response = await client.PostAsJsonAsync("/api/assistant/menu", new { message = $"Gợi ý {i}" });
      response.EnsureSuccessStatusCode();
      var result = await response.Content.ReadFromJsonAsync<AssistantResult>();
      Assert.Equal(new[] { "get_menu" }, result!.Tools);
    });
    await Task.WhenAll(tasks);
  }

  private sealed class Factory(IAssistantModel? model = null) : WebApplicationFactory<Program>
  {
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
      builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(
        new Dictionary<string, string?> { ["Assistant:Mode"] = "Demo" }));
      builder.ConfigureServices(services =>
      {
        services.RemoveAll<IAssistantModel>();
        services.AddSingleton<IAssistantModel>(model ?? new DemoAssistantModel());
      });
    }
  }

  private sealed class FailingModel(Exception error) : IAssistantModel
  {
    public int Calls { get; private set; }
    public string Mode => "test";
    public Task<ModelTurn> CompleteAsync(IReadOnlyList<object> messages, CancellationToken ct)
    {
      Calls++;
      return Task.FromException<ModelTurn>(error);
    }
  }
}
