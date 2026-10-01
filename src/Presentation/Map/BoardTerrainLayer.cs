using System;
using Godot;

namespace DevAncientNaval.Presentation.Map;
internal partial class BoardTerrainLayer : Node2D
{
    internal Action<Node2D>? DrawWorld { get; set; }

    public override void _Draw() => DrawWorld?.Invoke(this);
}
