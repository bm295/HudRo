using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using DataStructures.Application.Assistant;

namespace DataStructures.Infrastructure.Assistant;

public sealed class OpenAiAssistantModel(HttpClient client, IConfiguration config) : IAssistantModel
{
  public string Mode => "openai";
  public async Task<ModelTurn> CompleteAsync(IReadOnlyList<object> messages, CancellationToken ct)
  {
    var key = config["Assistant:ApiKey"];
    var model = config["Assistant:Model"];
    if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(model))
      throw new InvalidOperationException("Configure Assistant:ApiKey and Assistant:Model.");
    using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/chat/completions");
    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", key);
    request.Content = JsonContent.Create(new {
      model, messages, parallel_tool_calls = false,
      tools = new[] { new { type = "function", function = new {
        name = "get_menu", description = "Read the current restaurant menu. No mutations.", strict = true,
        parameters = new { type = "object", properties = new { }, required = Array.Empty<string>(), additionalProperties = false }
      }}},
      response_format = new { type = "json_schema", json_schema = new {
        name = "menu_suggestion", strict = true, schema = new {
          type = "object", properties = new {
            summary = new { type = "string" },
            menuCodes = new { type = "array", items = new { type = "string" } }
          }, required = new[] { "summary", "menuCodes" }, additionalProperties = false
        }
      }}
    });
    using var response = await client.SendAsync(request, ct);
    if (!response.IsSuccessStatusCode) throw new HttpRequestException("LLM provider request failed.", null, response.StatusCode);
    using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
    var choice = body.RootElement.GetProperty("choices")[0];
    var message = choice.GetProperty("message");
    if (message.TryGetProperty("refusal", out var refusal) && refusal.ValueKind != JsonValueKind.Null)
      throw new InvalidDataException("Model refused the request.");
    var finish = choice.GetProperty("finish_reason").GetString();
    if (finish == "tool_calls" && message.TryGetProperty("tool_calls", out var calls) && calls.GetArrayLength() == 1)
    {
      var call = calls[0];
      var function = call.GetProperty("function");
      return new(null, function.GetProperty("name").GetString(), function.GetProperty("arguments").GetString(), call.GetProperty("id").GetString());
    }
    if (finish != "stop") throw new InvalidDataException("Incomplete model response.");
    return new(message.GetProperty("content").GetString());
  }
}
