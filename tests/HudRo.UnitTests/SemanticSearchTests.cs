using DataStructures.Application.Search;
using DataStructures.Infrastructure.Search;
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace HudRo.UnitTests;

public sealed class SemanticSearchTests
{
  [Theory]
  [InlineData("", 5, 0)]
  [InlineData("query", 0, 0)]
  [InlineData("query", 21, 0)]
  [InlineData("query", 5, 2)]
  public async Task InvalidQueryNeverCallsProviders(string query, int topK, double score)
  {
    var provider = new FakeProviders();
    var service = new SemanticSearch(provider, provider);
    await Assert.ThrowsAsync<ArgumentException>(() => service.SearchAsync(new(query, topK, score), default));
    Assert.Equal(0, provider.Calls);
  }

  [Fact]
  public async Task IndexAndQueryUseSameEmbeddingProviderAndPreserveSource()
  {
    var provider = new FakeProviders();
    var service = new SemanticSearch(provider, provider);
    var document = new SearchDocument(Guid.NewGuid(), "Món chay", "menu/chay");
    await service.IndexAsync(document, default);
    var hits = await service.SearchAsync(new("vegetarian"), default);
    Assert.Equal(document.Id, Assert.Single(hits).Id);
    Assert.Equal(document.Source, hits[0].Source);
    Assert.Equal(2, provider.Calls);
  }

  [Fact]
  public async Task QdrantUsesWaitUpsertAndQueryPayload()
  {
    var id = Guid.NewGuid();
    var requests = new List<string>();
    using var client = new HttpClient(new Handler(async request => {
      var path = request.RequestUri!.PathAndQuery;
      requests.Add(path);
      if (request.Method == HttpMethod.Get)
        return Json(new { result = new { config = new { @params = new { vectors = new { size = 2, distance = "Cosine" } } } } });
      using var body = System.Text.Json.JsonDocument.Parse(await request.Content!.ReadAsStringAsync());
      if (request.Method == HttpMethod.Put)
      {
        Assert.Equal(id, body.RootElement.GetProperty("points")[0].GetProperty("id").GetGuid());
        return Json(new { result = new { status = "completed" } });
      }
      Assert.Equal(3, body.RootElement.GetProperty("limit").GetInt32());
      Assert.True(body.RootElement.GetProperty("with_payload").GetBoolean());
      return Json(new { result = new { points = new[] { new { id, score = 0.9, payload = new { text = "Món chay", source = "menu" } } } } });
    })) { BaseAddress = new Uri("http://localhost:6333/") };
    var repository = new QdrantVectorRepository(client, Options.Create(new SearchOptions { Dimensions = 2 }));
    await repository.UpsertAsync(new(id, "Món chay", "menu"), [1, 0], default);
    var hits = await repository.SearchAsync([1, 0], 3, 0.5, default);
    Assert.Equal(0.9, Assert.Single(hits).Score);
    Assert.Contains(requests, x => x.EndsWith("/points?wait=true"));
    Assert.Contains(requests, x => x.EndsWith("/points/query"));
  }

  [Fact]
  public async Task InvalidEmbeddingDimensionsAreRejected()
  {
    using var client = new HttpClient(new Handler(_ => Task.FromResult(Json(new { data = new[] { new { embedding = new[] { 1f } } } }))));
    var embedding = new OpenAiTextEmbedding(client, Options.Create(new SearchOptions { Dimensions = 2, ApiKey = "test" }));
    await Assert.ThrowsAsync<InvalidDataException>(() => embedding.EmbedAsync("query", default));
  }

  private static HttpResponseMessage Json(object body) => new(HttpStatusCode.OK) { Content = JsonContent.Create(body) };
  private sealed class Handler(Func<HttpRequestMessage, Task<HttpResponseMessage>> send) : HttpMessageHandler
  {
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct) => send(request);
  }
  private sealed class FakeProviders : ITextEmbedding, IVectorRepository
  {
    public int Calls { get; private set; }
    private SearchDocument? document;
    public Task<float[]> EmbedAsync(string text, CancellationToken ct) { Calls++; return Task.FromResult(new[] { 1f, 0f }); }
    public Task UpsertAsync(SearchDocument value, float[] vector, CancellationToken ct) { document = value; return Task.CompletedTask; }
    public Task<IReadOnlyList<SearchHit>> SearchAsync(float[] vector, int topK, double minScore, CancellationToken ct)
      => Task.FromResult<IReadOnlyList<SearchHit>>([new(document!.Id, document.Text, document.Source, 1)]);
  }
}
