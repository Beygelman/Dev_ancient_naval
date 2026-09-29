using System;
using Godot;

namespace DevAncientNaval.Presentation.UI;

/// <summary>Equal annular sectors; the center stays transparent to map input.</summary>
public partial class SectorButton : Button
{
    public float CenterAngle { get; private set; }
    public float Sweep { get; private set; } = Mathf.Pi/4;
    public const float Inner = 29, Outer = 65;
    public static readonly Vector2 Center = new(68,68);
    public void SetSector(int index,int count)
    {
        Sweep=Mathf.Pi/4; CenterAngle=(count==2?0:-Mathf.Pi/2)+index*Mathf.Tau/count;
        QueueRedraw();
    }
    public Vector2 IconCenter => Center + Vector2.FromAngle(CenterAngle)*47;
    public override bool _HasPoint(Vector2 point)
    {
        var offset=point-Center; float radius=offset.Length();
        float angle=Mathf.Wrap(offset.Angle()-CenterAngle,-Mathf.Pi,Mathf.Pi);
        return radius>=Inner&&radius<=Outer&&Math.Abs(angle)<=Sweep/2-0.025f;
    }
    public override void _Draw()
    {
        const int steps=32;
        var polygon=new Vector2[(steps+1)*2];
        float start=CenterAngle-Sweep/2+0.025f, span=Sweep-0.05f;
        for(int i=0;i<=steps;i++)
        {
            float angle=start+span*i/steps;
            polygon[i]=Center+Vector2.FromAngle(angle)*Outer;
            polygon[polygon.Length-1-i]=Center+Vector2.FromAngle(angle)*Inner;
        }
        var color=Disabled?new Color(0.05f,0.14f,0.18f,0.48f):IsHovered()?new Color(0.2f,0.47f,0.53f,0.8f):new Color(0.06f,0.2f,0.26f,0.68f);
        DrawColoredPolygon(polygon,color);
        var outline=new Vector2[polygon.Length+1]; polygon.CopyTo(outline,0); outline[^1]=polygon[0];
        DrawPolyline(outline,new Color(0.55f,0.85f,0.9f,Disabled?0.18f:0.5f),1.2f,true);
    }
}
