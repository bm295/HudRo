namespace DataStructures.Infrastructure.Search;

public sealed class SearchOptions
{
  public bool Enabled { get; set; }
  public string QdrantUrl { get; set; } = "http://localhost:6333/";
  public string? QdrantApiKey { get; set; }
  public string Collection { get; set; } = "hudro_text_embedding_3_small_1536_v1";
  public string Model { get; set; } = "text-embedding-3-small";
  public int Dimensions { get; set; } = 1536;
  public string ApiKey { get; set; } = "";
}

internal static class EmbeddingValidation
{
  public static void Check(float[] vector, int dimensions)
  {
    if (vector.Length != dimensions || vector.Any(x => !float.IsFinite(x)) || !vector.Any(x => x != 0))
      throw new InvalidDataException("Embedding has invalid dimensions or values.");
  }
}
