using System.Text.Json.Serialization;

namespace WallpaperPlatform;

internal sealed class WallpaperManifest
{
    [JsonPropertyName("name")]        public string             Name        { get; init; } = "";
    [JsonPropertyName("version")]     public string             Version     { get; init; } = "";
    [JsonPropertyName("description")] public string             Description { get; init; } = "";
    [JsonPropertyName("tier")]        public string             Tier        { get; init; } = "free";
    [JsonPropertyName("events")]      public WallpaperEventDef[] Events     { get; init; } = [];
}

internal sealed class WallpaperEventDef
{
    [JsonPropertyName("name")]       public string Name       { get; init; } = "";
    [JsonPropertyName("label")]      public string Label      { get; init; } = "";
    [JsonPropertyName("minSeconds")] public int    MinSeconds { get; init; } = 60;
    [JsonPropertyName("maxSeconds")] public int    MaxSeconds { get; init; } = 300;
}
