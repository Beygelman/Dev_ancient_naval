using System;
using Godot;
using Side = DevAncientNaval.Core.Units.Side;

namespace DevAncientNaval.Presentation.Map;
public partial class WorldAmbience
{
    private readonly Vector2[] _whirlpoolArm = new Vector2[55];
    private void DrawWhirlpools()
    {
        foreach (var pool in _battle!.Whirlpools)
        {
            foreach (var cell in pool.Cells)
            {
                if (_battle.Vision.IsExplored(Side.Player, cell))
                {
                    DrawColoredPolygon(BoardView.Projection.Diamond(cell), new Color(.02f, .12f, .17f, .7f));
                }
            }

            if (!_battle.Vision.IsExplored(Side.Player, pool.Position))
                continue;
            var c = BoardView.Projection.GridToWorld(pool.Position);
            float radius = 0;
            foreach (var cell in pool.Cells)
            {
                radius = Math.Max(radius, BoardView.Projection.GridToWorld(cell).DistanceTo(c));
            }

            radius *= .9f;
            DrawSetTransform(c, 0, new Vector2(1, .5f));
            for (int layer = 12; layer >= 1; layer--)
                DrawCircle(Vector2.Zero, radius * layer / 12, new Color(.015f, .065f, .09f, .065f));
            DrawCircle(Vector2.Zero, radius * .25f, new Color("071d28"));
            for (int arm = 0; arm < 5; arm++)
            {
                var points = _whirlpoolArm;
                for (int i = 0; i < points.Length; i++)
                {
                    float t = i / (float)(points.Length - 1);
                    float angle = arm * Mathf.Tau / 5 + t * 5.5f - _time * .6f;
                    points[i] = Vector2.FromAngle(angle) * (6 + radius * t);
                }

                DrawPolyline(points, new Color(.45f, .75f, .77f, .32f), 2, true);
            }

            DrawSetTransform(Vector2.Zero);
        }
    }
}
