using System;
using System.IO;
using System.Text.Json;
using DevAncientNaval.Core.Battle;
using Godot;

namespace DevAncientNaval.Presentation;
public sealed record SessionSave(int Version, DateTime SavedUtc, JsonElement Battle, float CameraX, float CameraY, float Zoom);
/// <summary>One recoverable slot. Replace only after the complete new file reaches disk.</summary>
public sealed class SaveStore
{
    private sealed record SessionWrite(int Version, DateTime SavedUtc, BattleSave Battle, float CameraX, float CameraY, float Zoom);
    // Null means this process has not inspected an existing primary yet.
    private bool? _primaryKnownInvalid;
    private readonly object _ioGate = new();
    public string Path { get; }
    private string BackupPath => Path + ".bak";
    public bool Exists => File.Exists(Path) || File.Exists(BackupPath);

    public SaveStore(string path) => Path = System.IO.Path.GetFullPath(path);
    public void Write(BattleState battle, Vector2 camera, float zoom) => WriteSnapshot(battle.CaptureSnapshot(), camera, zoom);

    public void WriteNewGame(BattleSave snapshot, Vector2 camera, float zoom) => WriteSnapshot(snapshot, camera, zoom, newGame: true);

    public void WriteSnapshot(BattleSave snapshot, Vector2 camera, float zoom, bool newGame = false)
    {
        lock (_ioGate) WriteSnapshotCore(snapshot, camera, zoom, newGame);
    }

    private void WriteSnapshotCore(BattleSave snapshot, Vector2 camera, float zoom, bool newGame)
    {
        using var trace = Diagnostics.PerformanceTrace.Measure("Save.Write");
        ValidateCamera(camera.X, camera.Y, zoom);
        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(Path)!);
        var save = new SessionWrite(1, DateTime.UtcNow, snapshot, camera.X, camera.Y, zoom);
        byte[] data = JsonSerializer.SerializeToUtf8Bytes(save, SaveSerializationOptions);
        string temporary = Path + ".tmp";
        using (var stream = new FileStream(temporary, FileMode.Create, System.IO.FileAccess.Write, FileShare.None))
        {
            stream.Write(data);
            stream.Flush(true);
        }

        if (File.Exists(Path))
        {
            if (_primaryKnownInvalid is null)
            {
                try
                {
                    ReadFile(Path);
                    _primaryKnownInvalid = false;
                }
                catch (Exception exception) when (IsInvalidSave(exception))
                {
                    _primaryKnownInvalid = true;
                }
            }

            // A recovered .bak is the last known-good checkpoint. Do not replace it
            // with a corrupt primary, including New Game before any Read call.
            File.Replace(temporary, Path, newGame || _primaryKnownInvalid == true ? null : BackupPath);
        }
        else
            File.Move(temporary, Path);
        _primaryKnownInvalid = false;
        // New game owns the single Continue slot. No previous voyage may survive
        // as a fallback after this successful atomic replacement.
        if (newGame && File.Exists(BackupPath)) File.Delete(BackupPath);
    }

    private static readonly JsonSerializerOptions SaveSerializationOptions = new()
    {
        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
    };

    public (BattleState Battle, Vector2 Camera, float Zoom, bool Backup) Read()
    {
        Exception? error = null;
        foreach (string file in new[]
        {
            Path,
            BackupPath
        }

        )
        {
            if (!File.Exists(file))
                continue;
            try
            {
                var result = ReadFile(file);
                if (file == Path)
                    _primaryKnownInvalid = false;
                return (result.Battle, result.Camera, result.Zoom, file == BackupPath);
            }
            catch (Exception exception) when (IsInvalidSave(exception))
            {
                if (file == Path)
                    _primaryKnownInvalid = true;
                error = exception;
            }
        }

        throw new InvalidDataException("The saved battle could not be read. Start a new game or restore its backup.", error);
    }

    private static (BattleState Battle, Vector2 Camera, float Zoom) ReadFile(string file)
    {
        if (new FileInfo(file).Length > 10_000_000)
            throw new InvalidDataException("Save is too large.");
        var data = JsonSerializer.Deserialize<SessionSave>(File.ReadAllBytes(file)) ?? throw new InvalidDataException("Empty save.");
        if (data.Version != 1 || data.Battle.ValueKind != JsonValueKind.Object)
            throw new InvalidDataException("Unsupported save.");
        ValidateCamera(data.CameraX, data.CameraY, data.Zoom);
        return (BattleState.LoadJson(data.Battle.GetRawText()), new(data.CameraX, data.CameraY), data.Zoom);
    }

    private static void ValidateCamera(float x, float y, float zoom)
    {
        if (!float.IsFinite(x) || !float.IsFinite(y) || !float.IsFinite(zoom) || zoom <= 0)
            throw new InvalidDataException("Invalid saved camera.");
    }

    private static bool IsInvalidSave(Exception exception) => exception is IOException or JsonException or ArgumentException or InvalidOperationException;
}
