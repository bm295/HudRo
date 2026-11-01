using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using DataStructures.Application.Search;
using Microsoft.Extensions.Options;

namespace DataStructures.Infrastructure.Search;

public sealed class QdrantVectorRepository(HttpClient client, IOptions<SearchOptions> options) : IVectorRepository
{
  private string CollectionPath => "collections/" + Uri.EscapeDataString(options.Value.Collection);

  private async Task EnsureCollectionAsync(CancellationToken ct)
  {
    using var response = await client.GetAsync(CollectionPath, ct);
    if (response.StatusCode == HttpStatusCode.NotFound)
    {
      using var created = await client.PutAsJsonAsync(CollectionPath,
        new { vectors = new { size = options.Value.Dimensions, distance = "Cosine" } }, ct);
      // Another request may have created the collection concurrently; always verify its schema.
      if (!created.IsSuccessStatusCode && created.StatusCode != HttpStatusCode.Conflict)
        created.EnsureSuccessStatusCode();
      using var existing = await client.GetAsync(CollectionPath, ct);
      existing.EnsureSuccessStatusCode();
      await VerifySchemaAsync(existing, ct);
      return;
    }
    response.EnsureSuccessStatusCode();
    await VerifySchemaAsync(response, ct);
  }

  private async Task VerifySchemaAsync(HttpResponseMessage response, CancellationToken ct)
  {
    using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
    var vectors = json.RootElement.GetProperty("result").GetProperty("config").GetProperty("params").GetProperty("vectors");
    if (vectors.GetProperty("size").GetInt32() != options.Value.Dimensions || vectors.GetProperty("distance").GetString() != "Cosine")
      throw new InvalidDataException("Collection schema mismatch. Use a new collection and reindex.");
  }

  public async Task UpsertAsync(SearchDocument document, float[] vector, CancellationToken ct)
  {
    EmbeddingValidation.Check(vector, options.Value.Dimensions);
    await EnsureCollectionAsync(ct);
    using var response = await client.PutAsJsonAsync(CollectionPath + "/points?wait=true", new {
      points = new[] { new { id = document.Id, vector, payload = new { text = document.Text, source = document.Source } } }
    }, ct);
    response.EnsureSuccessStatusCode();
  }

  public async Task<IReadOnlyList<SearchHit>> SearchAsync(float[] vector, int topK, double minScore, CancellationToken ct)
  {
    EmbeddingValidation.Check(vector, options.Value.Dimensions);
    await EnsureCollectionAsync(ct);
    using var response = await client.PostAsJsonAsync(CollectionPath + "/points/query", new {
      query = vector, limit = topK, score_threshold = minScore, with_payload = true, with_vector = false
    }, ct);
    response.EnsureSuccessStatusCode();
    using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
    return json.RootElement.GetProperty("result").GetProperty("points").EnumerateArray().Select(point => {
      var payload = point.GetProperty("payload");
      return new SearchHit(point.GetProperty("id").GetGuid(), payload.GetProperty("text").GetString()!,
        payload.GetProperty("source").GetString()!, point.GetProperty("score").GetDouble());
    }).ToArray();
  }
}
