namespace DataStructures.Application.Search;

public sealed record SearchDocument(Guid Id, string Text, string Source);
public sealed record SearchRequest(string Query, int TopK = 5, double MinScore = 0);
public sealed record SearchHit(Guid Id, string Text, string Source, double Score);

public interface ITextEmbedding
{
  Task<float[]> EmbedAsync(string text, CancellationToken ct);
}

public interface IVectorRepository
{
  Task UpsertAsync(SearchDocument document, float[] vector, CancellationToken ct);
  Task<IReadOnlyList<SearchHit>> SearchAsync(float[] vector, int topK, double minScore, CancellationToken ct);
}

public sealed class SemanticSearch(ITextEmbedding embeddings, IVectorRepository vectors)
{
  public async Task IndexAsync(SearchDocument document, CancellationToken ct)
  {
    if (document.Id == Guid.Empty || string.IsNullOrWhiteSpace(document.Source) || document.Source.Length > 500)
      throw new ArgumentException("A nonempty document ID and source (1–500 characters) are required.");
    ValidateText(document.Text);
    await vectors.UpsertAsync(document, await embeddings.EmbedAsync(document.Text, ct), ct);
  }

  public async Task<IReadOnlyList<SearchHit>> SearchAsync(SearchRequest request, CancellationToken ct)
  {
    ValidateText(request.Query);
    if (request.TopK is < 1 or > 20 || !double.IsFinite(request.MinScore) || request.MinScore is < -1 or > 1)
      throw new ArgumentException("TopK must be 1–20 and MinScore must be -1–1.");
    return await vectors.SearchAsync(await embeddings.EmbedAsync(request.Query, ct), request.TopK, request.MinScore, ct);
  }

  private static void ValidateText(string text)
  {
    // Conservative character limit: callers split larger documents before indexing.
    if (string.IsNullOrWhiteSpace(text) || text.Length > 2000)
      throw new ArgumentException("Text/query must contain 1–2000 characters.");
  }
}
