using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using DataStructures.Application.Search;
using Microsoft.Extensions.Options;

namespace DataStructures.Infrastructure.Search;

public sealed class OpenAiTextEmbedding(HttpClient client, IOptions<SearchOptions> options) : ITextEmbedding
{
  public async Task<float[]> EmbedAsync(string text, CancellationToken ct)
  {
    var settings = options.Value;
    using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/embeddings");
    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", settings.ApiKey);
    request.Content = JsonContent.Create(new { model = settings.Model, input = text, dimensions = settings.Dimensions, encoding_format = "float" });
    using var response = await client.SendAsync(request, ct);
    response.EnsureSuccessStatusCode();
    using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
    try
    {
      var data = json.RootElement.GetProperty("data");
      if (data.GetArrayLength() != 1) throw new InvalidDataException("Expected one embedding.");
      var vector = data[0].GetProperty("embedding").EnumerateArray().Select(x => x.GetSingle()).ToArray();
      EmbeddingValidation.Check(vector, settings.Dimensions);
      return vector;
    }
    catch (Exception ex) when (ex is KeyNotFoundException or InvalidOperationException or FormatException)
    { throw new InvalidDataException("Malformed embedding response.", ex); }
  }
}
