using System;
using DevAncientNaval.Core.Battle;
using DevAncientNaval.Core.Units;
using Godot;
using Side = DevAncientNaval.Core.Units.Side;

namespace DevAncientNaval.Presentation.Map;
public partial class FleetView
{
    private readonly ShaderMaterial _targetInk = TargetHighlightArt.Material(false, true);
    private readonly ShaderMaterial _lethalTargetInk = TargetHighlightArt.Material(true, true);
    private static readonly Rect2 TargetMaskBounds = new(-85, -135, 170, 190);
    private static void ReleaseTargetMask(SceneryAtlasPage? mask)
    {
        if (mask is null) return;
        mask.GetParent()?.RemoveChild(mask);
        mask.QueueFree();
    }
    private bool AttackHighlight(ShipSnapshot target, out bool lethal)
    {
        lethal = false;
        if (SelectedId is not { } id || Battle.Find(id) is not { } attacker ||
            Battle.FindObserved(Side.Player,target.Id) is not { } defender || !Battle.CanAttack(id,defender.Id)) return false;
        double volley = Battle.Damage(attacker, defender) * (Battle.CanDoubleSalvo(id,defender.Position) ? 2 : 1);
        lethal = volley >= defender.Health;
        return true;
    }
    private void UpdateAttackHighlight(HullCanvas hull, bool active, bool lethal)
    {
        if (!active) { if (hull.Glow is not null) hull.Glow.Visible = false; return; }
        if (hull.Glow is null)
        {
            hull.Glow = new Sprite2D { Name = "AttackContour"+hull.Ship.Id, Centered=false, Offset=TargetMaskBounds.Position,
                ZIndex=0, ShowBehindParent=true };
            hull.Canvas.AddChild(hull.Glow);
        }
        var key = $"{hull.Ship.Class}:{hull.Ship.Level}:{hull.Ship.HasMortar}:{hull.Ship.IsVeteran}:{hull.Heading:0.00}";
        if (hull.MaskKey != key)
        {
            ReleaseTargetMask(hull.Mask);
            var mask = new SceneryAtlasPage { Name="AttackMask"+hull.Ship.Id, Size=new(340,380), Disable3D=true, TransparentBg=true,
                World2D=new World2D(), RenderTargetUpdateMode=SubViewport.UpdateMode.Once };
            AddChild(mask);
            var snapshot = hull.Ship;
            mask.SetPainter(2, source =>
            {
                _ink=source;
                try { DrawShip(snapshot,-TargetMaskBounds.Position, silhouette:true); }
                finally { _ink=null; }
            });
            hull.Mask=mask; hull.MaskKey=key;
            hull.Glow.Texture=mask.GetTexture();
            hull.Glow.Scale=Vector2.One*.5f;
            // Offset is in texture pixels, while the parent uses world units.
            hull.Glow.Offset=TargetMaskBounds.Position*2;
        }
        hull.Glow.Material=lethal?_lethalTargetInk:_targetInk;
        hull.Glow.Visible=true;
    }
}
