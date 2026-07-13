using System.Text.Json.Serialization;

namespace Peep.Shared;

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(Settings))]
public partial class SettingsSerializerContext : JsonSerializerContext { }
