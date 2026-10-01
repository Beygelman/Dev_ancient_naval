using DevAncientNaval.Core.Battle;
using Godot;

namespace DevAncientNaval.Presentation.UI;
/// <summary>Static generated harbor artwork; no hidden background animation.</summary>
public partial class MenuHarborView : Control
{
    public FleetColor FleetColor { get; set; } = FleetColor.Blue;

    public override void _Ready()
    {
        var artwork = new TextureRect
        {
            Name = "CityShipArtwork",
            Texture = GD.Load<Texture2D>("res://assets/ui/menu-harbor-0.16.png"),
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCovered,
            MouseFilter = MouseFilterEnum.Ignore
        };
        AddChild(artwork);
        artwork.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        var shade = new ColorRect
        {
            Color = new Color(.01f, .04f, .06f, .16f),
            MouseFilter = MouseFilterEnum.Ignore
        };
        AddChild(shade);
        shade.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
    }
}
