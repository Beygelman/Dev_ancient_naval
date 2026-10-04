using DevAncientNaval.Core.Grid;
using System.Linq;
using DevAncientNaval.Core.Units;
using DevAncientNaval.Presentation.Map;
using Godot;
using Side = DevAncientNaval.Core.Units.Side;

namespace DevAncientNaval.Presentation;

public partial class Main
{
    private ConstructionPreview? _construction;
    internal ConstructionPreview? Construction => _construction;
    private void ClearConstruction() => _construction?.ShowAt(BoardView,null,null);
    internal void PreviewConstructionAt(GridPosition cell)
    {
        _construction ??= CreateConstruction();
        bool legal = CanCommand && Mode == OrderMode.Build && _building is not null &&
            BoardView.Reachable.Contains(cell) && Battle.Board.Contains(cell) &&
            Battle.Vision.IsVisible(Side.Player,cell);
        _construction.ShowAt(BoardView,legal ? _building : null,legal ? cell : null);
    }
    private ConstructionPreview CreateConstruction()
    {
        var preview = new ConstructionPreview { Name="ConstructionBlueprint",ZIndex=6,Visible=false };
        AddChild(preview);
        return preview;
    }
}
