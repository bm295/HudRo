using System.Net;
using System.Text;
using System.Text.Json;
using DataStructures.Infrastructure.Assistant;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace HudRo.UnitTests;

public sealed class OpenAiAssistantModelTests
{
  [Fact]
  public async Task SendsStrictSchemaAndParsesToolCall()
  {
    var handler = new Handler("""
      {"choices":[{"finish_reason":"tool_calls","message":{"tool_calls":[{"id":"c1","function":{"name":"get_menu","arguments":"{}"}}]}}]}
      """);
    var model = Create(handler);
    var turn = await model.CompleteAsync(new object[] { new { role = "user", content = "test" } }, default);
    Assert.Equal("get_menu", turn.ToolName);
    Assert.Equal("c1", turn.CallId);
    Assert.Equal("https://api.openai.com/v1/chat/completions", handler.Url);
    Assert.Equal("Bearer test-key", handler.Authorization);
    using var request = JsonDocument.Parse(handler.Body!);
    Assert.Equal("test-model", request.RootElement.GetProperty("model").GetString());
    Assert.False(request.RootElement.GetProperty("parallel_tool_calls").GetBoolean());
    Assert.True(request.RootElement.GetProperty("tools")[0].GetProperty("function").GetProperty("strict").GetBoolean());
    Assert.True(request.RootElement.GetProperty("response_format").GetProperty("json_schema").GetProperty("strict").GetBoolean());
  }

  [Fact]
  public async Task ParsesFinalContent()
  {
    var turn = await Create(new Handler("""{"choices":[{"finish_reason":"stop","message":{"content":"final"}}]}"""))
      .CompleteAsync(Array.Empty<object>(), default);
    Assert.Equal("final", turn.Content);
    Assert.Null(turn.ToolName);
  }

  [Theory]
  [InlineData("{}")]
  [InlineData("{\"choices\":[]}")]
  [InlineData("{\"choices\":null}")]
  [InlineData("{\"choices\":[{\"finish_reason\":\"length\",\"message\":{}}]}")]
  [InlineData("{\"choices\":[{\"finish_reason\":\"stop\",\"message\":{\"refusal\":\"no\"}}]}")]
  [InlineData("{\"choices\":[{\"finish_reason\":\"tool_calls\",\"message\":{\"tool_calls\":[]}}]}")]
  [InlineData("{\"choices\":[{\"finish_reason\":\"stop\",\"message\":{\"content\":42}}]}")]
  public async Task RejectsMalformedOrIncompleteEnvelope(string body)
    => await Assert.ThrowsAsync<InvalidDataException>(() => Create(new Handler(body)).CompleteAsync(Array.Empty<object>(), default));

  [Theory]
  [InlineData(401)]
  [InlineData(429)]
  [InlineData(500)]
  public async Task ProviderErrors_DoNotExposeResponseBody(int status)
  {
    var error = await Assert.ThrowsAsync<HttpRequestException>(() =>
      Create(new Handler("sensitive-provider-body", (HttpStatusCode)status)).CompleteAsync(Array.Empty<object>(), default));
    Assert.Equal((HttpStatusCode)status, error.StatusCode);
    Assert.DoesNotContain("sensitive", error.Message);
  }

  [Fact]
  public async Task Cancellation_ReachesHttpTransport()
  {
    using var cancelled = new CancellationTokenSource();
    cancelled.Cancel();
    await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
      Create(new Handler("{}")).CompleteAsync(Array.Empty<object>(), cancelled.Token));
  }

  private static OpenAiAssistantModel Create(Handler handler) => new(new HttpClient(handler),
    new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
    { ["Assistant:ApiKey"] = "test-key", ["Assistant:Model"] = "test-model" }).Build());

  private sealed class Handler(string body, HttpStatusCode status = HttpStatusCode.OK) : HttpMessageHandler
  {
    public string? Body { get; private set; }
    public string? Url { get; private set; }
    public string? Authorization { get; private set; }
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
      ct.ThrowIfCancellationRequested();
      Url = request.RequestUri!.ToString();
      Authorization = request.Headers.Authorization?.ToString();
      Body = await request.Content!.ReadAsStringAsync(ct);
      return new(status) { Content = new StringContent(body, Encoding.UTF8, "application/json") };
    }
  }
}
