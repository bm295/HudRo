using DataStructures.Application.Search;

namespace DataStructures.Presentation;

public static class SearchEndpoints
{
  public static void MapSemanticSearch(this WebApplication app)
  {
    app.MapPut("/api/search/documents/{id:guid}", (Guid id, DocumentBody body, SemanticSearch search, CancellationToken ct) =>
      Execute(async token => {
        await search.IndexAsync(new SearchDocument(id, body.Text, body.Source), token);
        return Results.NoContent();
      }, ct));
    app.MapPost("/api/search/query", (SearchRequest request, SemanticSearch search, CancellationToken ct) =>
      Execute(async token => Results.Ok(await search.SearchAsync(request, token)), ct));
  }

  private static async Task<IResult> Execute(Func<CancellationToken, Task<IResult>> action, CancellationToken ct)
  {
    using var deadline = CancellationTokenSource.CreateLinkedTokenSource(ct);
    deadline.CancelAfter(TimeSpan.FromSeconds(60));
    try { return await action(deadline.Token); }
    catch (ArgumentException ex) { return Results.BadRequest(new { error = ex.Message }); }
    catch (OperationCanceledException) when (!ct.IsCancellationRequested)
    { return Results.Problem("Semantic search timed out.", statusCode: 504); }
    catch (Exception ex) when (ex is HttpRequestException or InvalidDataException or System.Text.Json.JsonException or KeyNotFoundException or InvalidOperationException or FormatException)
    { return Results.Problem("Semantic search provider unavailable or invalid response.", statusCode: 502); }
  }

  public sealed record DocumentBody(string Text, string Source);
}
