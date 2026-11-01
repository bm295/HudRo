using DataStructures.Application.Assistant;
using DataStructures.Infrastructure;
using DataStructures.Infrastructure.Assistant;
using Xunit;
using Microsoft.Extensions.DependencyInjection;
using DataStructures.Infrastructure.DependencyInjection;

namespace HudRo.UnitTests;

public sealed class MenuAssistantTests
{
  [Fact]
  public void Templates_AreVersionedAndRejectUnknownNames()
  {
    Assert.Contains("get_menu", PromptTemplates.Render("menu-v1"));
    Assert.Contains("short sentence", PromptTemplates.Render("menu-concise-v1"));
    Assert.Throws<ArgumentException>(() => PromptTemplates.Render("../../secret"));
  }

  [Theory]
  [InlineData("{\"summary\":\"ok\"}")]
  [InlineData("{\"summary\":\"ok\",\"menuCodes\":[1]}")]
  [InlineData("{\"summary\":\"ok\",\"menuCodes\":[],\"extra\":true}")]
  public void Parse_RejectsInvalidContracts(string json)
    => Assert.Throws<InvalidDataException>(() => MenuAssistant.Parse(json));

  [Fact]
  public async Task Demo_ExecutesToolAndValidatesFinalOutput()
  {
    var assistant = new MenuAssistant(new DemoAssistantModel(), new InMemoryFnbReadAdapter(CreateStore()));
    var result = await assistant.RunAsync(new("Gợi ý món"), default);
    Assert.Equal("demo", result.Mode);
    Assert.Equal(new[] { "get_menu" }, result.Tools);
  }

  [Theory]
  [InlineData("delete_order", "{}")]
  [InlineData("get_menu", "{\"sql\":\"drop table\"}")]
  public async Task RejectsUnknownToolsAndArguments(string tool, string args)
  {
    var assistant = new MenuAssistant(new Stub(new(null, tool, args, "call-1")), new InMemoryFnbReadAdapter(CreateStore()));
    await Assert.ThrowsAsync<InvalidDataException>(() => assistant.RunAsync(new("test"), default));
  }

  [Fact]
  public async Task RejectsFinalAnswerBeforeReadingMenu()
  {
    var assistant = new MenuAssistant(new Stub(new("{}")), new InMemoryFnbReadAdapter(CreateStore()));
    await Assert.ThrowsAsync<InvalidDataException>(() => assistant.RunAsync(new("test"), default));
  }

  [Fact]
  public async Task RepeatedTools_StopAtFourModelTurns()
  {
    var assistant = new MenuAssistant(new Stub(new(null, "get_menu", "{}", "call")), new InMemoryFnbReadAdapter(CreateStore()));
    var error = await Assert.ThrowsAsync<InvalidDataException>(() => assistant.RunAsync(new("test"), default));
    Assert.Contains("limit", error.Message);
  }

  [Fact]
  public async Task RejectsInventedMenuCode()
  {
    var assistant = new MenuAssistant(new SequenceModel(), new InMemoryFnbReadAdapter(CreateStore()));
    await Assert.ThrowsAsync<InvalidDataException>(() => assistant.RunAsync(new("test"), default));
  }

  [Fact]
  public void RejectsDuplicateMenuCodes()
    => Assert.Throws<InvalidDataException>(() => MenuAssistant.Parse("""{"summary":"ok","menuCodes":["A","A"]}"""));

  private sealed class SequenceModel : IAssistantModel
  {
    public string Mode => "test";
    public Task<ModelTurn> CompleteAsync(IReadOnlyList<object> messages, CancellationToken ct)
      => Task.FromResult(messages.Count == 2 ? new ModelTurn(null, "get_menu", "{}", "call") :
        new ModelTurn("""{"summary":"ok","menuCodes":["INVENTED"]}"""));
  }

  private static InMemoryFnbStore CreateStore()
  {
    using var provider = new ServiceCollection().AddHudRoInMemoryStore().BuildServiceProvider();
    return provider.GetRequiredService<InMemoryFnbStore>();
  }

  private sealed class Stub(ModelTurn turn) : IAssistantModel
  {
    public string Mode => "test";
    public Task<ModelTurn> CompleteAsync(IReadOnlyList<object> messages, CancellationToken ct) => Task.FromResult(turn);
  }
}
