using System;
using System.Collections.Generic;
using System.Linq;
using DevAncientNaval.Core.Battle;
using Godot;
using Side = DevAncientNaval.Core.Units.Side;

namespace DevAncientNaval.Presentation.Map;

public partial class BoardView
{
    private readonly ShaderMaterial _townTargetInk = TargetHighlightArt.Material(false);
    private readonly ShaderMaterial _townLethalInk = TargetHighlightArt.Material(true);
    private readonly ShaderMaterial _townCaptureInk = TargetHighlightArt.Material(false, false, new Color(.95f,.8f,.4f,.85f));
    private IsometricProjection? _townRasterProjection;
    private Node? _townRasterRoot;
    private readonly Dictionary<(int Id, bool Front), TownRasterEntry> _townRasters = new();
    private readonly Dictionary<int, int> _townRasterBuilds = new();
    private sealed record TownRasterEntry(string Key, Rect2 Bounds, SceneryAtlasPage Viewport);
    internal int TownRasterBuildCount { get; private set; }
    internal int TownRasterCount => _townRasters.Count;
    internal bool TownRasterIdle => _townRasters.Values.All(v => v.Viewport.RenderTargetUpdateMode == SubViewport.UpdateMode.Disabled);
    internal bool TownRasterBounded => _townRasters.Values.All(v => v.Viewport.Size.X <= 512 && v.Viewport.Size.Y <= 512);
    internal int TownRasterBuildsFor(int id) => _townRasterBuilds.GetValueOrDefault(id);

    private void EnsureTownRasterWorld()
    {
        if (ReferenceEquals(_townRasterProjection, Projection)) return;
        _townRasterProjection = Projection;
        if (_townRasterRoot is not null)
        {
            RemoveChild(_townRasterRoot);
            _townRasterRoot.QueueFree();
        }
        _townRasters.Clear();
        _townRasterBuilds.Clear();
        _townRasterRoot = new Node { Name = "TownArtworkRasters" };
        AddChild(_townRasterRoot);
    }

    private Rect2 TownRasterBounds(Village town, bool front)
    {
        // Tall front corner towers extend above the compact walls; the rear
        // tower lives in the back layer. Leave room for every coastal fit.
        var bounds = front ? new Rect2(-58, -64, 116, 108) : new Rect2(-56, -92, 112, 106);
        if (town.HasPort)
        {
            var shore = PortShore(ObserveTownArt(town));
            // Pier ends, boats and their raised roof must fit in every direction.
            bounds = bounds.Expand(shore - new Vector2(42, 35)).Expand(shore + new Vector2(42, 35));
        }
        return bounds.Grow(2);
    }

    private void DrawCachedTown(Node2D canvas, Village town, bool front)
    {
        EnsureTownRasterWorld();
        var slot = (town.Id, front);
        _townRasters.TryGetValue(slot, out var entry);
        bool visible = Battle.Vision.IsVisible(Side.Player, town.Position);
        // Queued redraws may survive the transition into fog. Keep the last
        // observed texture instead of consulting a now-hidden owner's new art.
        if (!visible && entry is null) return;
        string key = $"{town.Owner}:{town.Level}:{town.IsFortified}:{town.HasPort}:{FleetPalette.For(Battle, town.Owner).ToHtml()}";
        if (visible && (entry is null || entry.Key != key))
        {
            entry?.Viewport.QueueFree();
            var bounds = TownRasterBounds(town, front);
            var observedArt = ObserveTownArt(town);
            var placement = VillagePlacement(observedArt);
            float scale = Math.Min(2, 512 / Math.Max(bounds.Size.X, bounds.Size.Y));
            var size = new Vector2I(Mathf.CeilToInt(bounds.Size.X * scale), Mathf.CeilToInt(bounds.Size.Y * scale));
            var page = new SceneryAtlasPage
            {
                Name = (front ? "TownFront" : "TownBack") + town.Id,
                Size = size,
                Disable3D = true,
                TransparentBg = true,
                World2D = new World2D(),
                RenderTargetClearMode = SubViewport.ClearMode.Always,
                RenderTargetUpdateMode = SubViewport.UpdateMode.Once
            };
            _townRasterRoot!.AddChild(page);
            page.SetPainter(scale, source =>
            {
                source.DrawSetTransform(-bounds.Position);
                if (!front)
                {
                    DrawTownGround(source, observedArt, Vector2.Zero);
                    if (observedArt.HasPort) DrawPortRoad(source, observedArt, Vector2.Zero);
                }
                if (placement.Scale > 0)
                {
                    source.DrawSetTransform(-bounds.Position + placement.Offset, 0, Vector2.One * placement.Scale);
                    if (front) DrawVillageForeground(source, observedArt, Vector2.Zero);
                    else DrawVillage(source, observedArt, Vector2.Zero);
                }
                source.DrawSetTransform(-bounds.Position);
                if (front)
                {
                    DrawTownFields(source, observedArt, Vector2.Zero);
                    // A port intentionally straddles the shore and sea; only
                    // the village itself uses the inset land footprint.
                    if (observedArt.HasPort) DrawPort(source, observedArt, Vector2.Zero);
                }
                source.DrawSetTransform(Vector2.Zero);
            });
            _townRasters[slot] = entry = new(key, bounds, page);
            TownRasterBuildCount++;
            _townRasterBuilds[town.Id] = _townRasterBuilds.GetValueOrDefault(town.Id) + 1;
        }
        bool attackable = visible && SelectedShipId is { } attackerId && Targets.Contains(town.Position) && Battle.CanAttackVillage(attackerId, town.Id);
        bool lethal = false;
        if (attackable && Battle.Find(SelectedShipId!.Value) is { } attacker)
        {
            double damage = (Battle.UsesMortar(attacker,town.Position) ? attacker.CurrentMortarDamage + Battle.Rules.Mortar.VillageDamageBonus : attacker.CurrentDamage) + attacker.ShotDamageBonus;
            lethal = damage * (town.IsFortified ? .75 : 1) * (Battle.CanDoubleSalvo(attacker.Id,town.Position) ? 2 : 1) >= town.Health;
        }
        bool capture = visible && Battle.CanCaptureVillage(Side.Player, town.Id);
        canvas.Material = attackable ? (lethal ? _townLethalInk : _townTargetInk) : capture ? _townCaptureInk : _sceneryMaterial;
        canvas.DrawTextureRect(entry!.Viewport.GetTexture(), entry.Bounds, false);
    }
}
