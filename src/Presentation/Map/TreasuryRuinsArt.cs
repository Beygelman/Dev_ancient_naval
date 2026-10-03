using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Side = DevAncientNaval.Core.Units.Side;

namespace DevAncientNaval.Presentation.Map;

public partial class BoardView
{
    private readonly Dictionary<DevAncientNaval.Core.Grid.GridPosition, Sprite2D> _treasuryRuinSprites = new();
    private ShaderMaterial? _treasuryReadyMaterial;
    internal int TreasuryRuinModelCount => _scenery.Count(p => p.Kind == 4);
    internal IEnumerable<int> TreasuryRuinVariants => _scenery.Where(p => p.Kind == 4).Select(p => (int)p.Shade);
    private void AddTreasuryRuins()
    {
        foreach (var treasury in Battle.TreasuryRuins)
        {
            int variant = (int)(((uint)Board.Seed + (uint)treasury.Id * 17) % 4);
            _scenery.Add(new(treasury.Position, Projection.GridToWorld(treasury.Position), 18, 4, variant));
        }
    }
    private static void DrawTreasuryRuin(CanvasItem canvas, Vector2 p, int variant)
    {
        if (variant == 0)
        {
            Vector2 P(float x, float y, float z) => p + new Vector2((x - y) * .8f, (x + y) * .38f - z);
            var stone = new Color("91a99a");
            FactionSanctuaryArt.Box(canvas, P, 0, 0, -4, 21, 18, 7, new Color("698b80"));
            FactionSanctuaryArt.Box(canvas, P, 0, 0, 2, 13, 12, 28, stone);
            FactionSanctuaryArt.Box(canvas, P, 0, 0, 30, 15, 14, 2, new Color("b6bd9e"));
            for (int notch = 0; notch < 4; notch++)
                FactionSanctuaryArt.Box(canvas, P, -5 + notch * 3.5f, -6, 32, 2, 2, notch == 1 ? 2 : 5, stone);
            for (int course = 0; course < 5; course++)
            {
                float z = 5 + course * 5;
                canvas.DrawLine(P(-6.5f, 6, z), P(6.5f, 6, z), new Color("627f74"), .7f, true);
                canvas.DrawLine(P(6.5f, -6, z), P(6.5f, 6, z), new Color("627f74"), .7f, true);
            }
            canvas.DrawLine(P(0, 6.2f, 15), P(0, 6.2f, 23), new Color("364d47"), 2, true);
            canvas.DrawColoredPolygon(new[] { P(-10, -8, 1), P(11, -8, 1), P(15, 10, 1), P(-14, 10, 1) }, new Color(.16f, .37f, .43f, .4f));
            canvas.DrawLine(P(-9, 10, 2), P(3, 10, 2), new Color(.74f, .88f, .81f, .5f), 1, true);
        }
        else if (variant == 1) IslandRuinsArt.Draw(canvas, p, .94f, 2, true);
        else if (variant == 2) IslandRuinsArt.DrawProw(canvas, p, 1);
        else IslandRuinsArt.Draw(canvas, p, .9f, 1, true);
    }
    private void RefreshTreasuryRuinHighlights()
    {
        foreach (var (cell, sprite) in _treasuryRuinSprites)
        {
            bool ready = Battle.Vision.IsVisible(Side.Player, cell) && Battle.TreasuryAt(cell) is not null &&
                Battle.At(cell) is { Owner: Side.Player } ship && Battle.CanLootTreasury(Side.Player, ship.Id);
            sprite.Material = ready ? (_treasuryReadyMaterial ??= TargetHighlightArt.Material(false, false, new Color("efe0a2"))) : _sceneryMaterial;
        }
    }
}
