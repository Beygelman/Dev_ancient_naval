using System.Text.Json.Serialization;

namespace DevAncientNaval.Core.Battle;

// The save and balance roots intentionally have different read options. Generate
// their complete DTO graphs at build time: iOS AOT cannot discover them by
// reflection or instantiate the non-generic enum converter at runtime.
[JsonSourceGenerationOptions(GenerationMode = JsonSourceGenerationMode.Metadata,
    WriteIndented = true, UseStringEnumConverter = true,
    Converters = new[] { typeof(BattleRulesJsonConverter) })]
[JsonSerializable(typeof(BattleSave))]
internal sealed partial class BattleSaveJsonContext : JsonSerializerContext;

[JsonSourceGenerationOptions(GenerationMode = JsonSourceGenerationMode.Metadata,
    PropertyNameCaseInsensitive = true, UseStringEnumConverter = true)]
[JsonSerializable(typeof(BattleRules))]
internal sealed partial class BattleRulesJsonContext : JsonSerializerContext;
