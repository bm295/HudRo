using System.Text.Json;
using DataStructures.Application.Assistant;

namespace DataStructures.Infrastructure.Assistant;

// Deterministic protocol simulator, not an LLM. No mutable conversation state.
public sealed class DemoAssistantModel : IAssistantModel
{
  public string Mode => "demo";
  public Task<ModelTurn> CompleteAsync(IReadOnlyList<object> messages, CancellationToken ct)
  {
    ct.ThrowIfCancellationRequested();
    var last = JsonSerializer.SerializeToElement(messages[^1]);
    if (last.GetProperty("role").GetString() != "tool")
      return Task.FromResult(new ModelTurn(null, "get_menu", "{}", "demo-menu-1"));
    using var menu = JsonDocument.Parse(last.GetProperty("content").GetString()!);
    var codes = menu.RootElement.EnumerateArray().Take(2).Select(x => x.GetProperty("Code").GetString()).ToArray();
    return Task.FromResult(new ModelTurn(JsonSerializer.Serialize(new {
      summary = "Demo: gợi ý tối đa hai món đầu menu; không phân tích yêu cầu bằng LLM.", menuCodes = codes
    })));
  }
}
