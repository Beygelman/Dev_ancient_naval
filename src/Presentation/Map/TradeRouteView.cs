using Godot;
using Side = DevAncientNaval.Core.Units.Side;

namespace DevAncientNaval.Presentation.Map;
public partial class BoardView
{
    private readonly SeaGeometryBatch _tradeInk = new();
    private void DrawTradeRoutes(Node2D canvas, Rect2 bounds)
    {
        var network = Battle.TradeRoutes(Side.Player);
        if (!ReferenceEquals(_tradeNetwork, network) || !ReferenceEquals(_tradeProjection, Projection) || _tradeVision != Battle.Vision.Revision)
        {
            _tradeNetwork = network;
            _tradeProjection = Projection;
            _tradeVision = Battle.Vision.Revision;
            BuildTradeInk(network);
        }
        _tradeInk.Submit(canvas, 1.7f);
    }
}
