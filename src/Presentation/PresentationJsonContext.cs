using System.Collections.Generic;
using System.Text.Json.Serialization;
using DevAncientNaval.Presentation.Diagnostics;
using DevAncientNaval.Presentation.UI;

namespace DevAncientNaval.Presentation;

// iOS NativeAOT cannot discover JSON constructors through runtime reflection.
// Metadata generation preserves the existing v1 envelope, tutorial sidecar and
// localization shapes; Core still owns the embedded BattleSave contract.
[JsonSourceGenerationOptions(GenerationMode = JsonSourceGenerationMode.Metadata, UseStringEnumConverter = true,
    Converters = new[] { typeof(DevAncientNaval.Core.Battle.BattleRulesJsonConverter) })]
[JsonSerializable(typeof(SessionSave))]
[JsonSerializable(typeof(SaveStore.SessionWrite))]
[JsonSerializable(typeof(TutorialAdvice.Progress))]
[JsonSerializable(typeof(Dictionary<string, string>), TypeInfoPropertyName = "LocalizationCatalog")]
[JsonSerializable(typeof(Dictionary<string, TraceMetric>), TypeInfoPropertyName = "TraceMetrics")]
internal partial class PresentationJsonContext : JsonSerializerContext;
