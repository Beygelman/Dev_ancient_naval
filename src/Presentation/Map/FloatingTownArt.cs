using System;
using Godot;

namespace DevAncientNaval.Presentation.Map;

/// <summary>One town vocabulary for the fleet and title close-up. Coordinates are
/// deck x/y and vertical height: houses stay upright while the hull turns.</summary>
internal static class FloatingTownArt
{
    private readonly record struct House(float X, float Y, float Z, float Height, float Width, int Tone);
    private static readonly House[] Homes =
    {
        new(-21,-8,2,12,5,0),new(-9,-14,5,17,5,3),new(8,-11,8,18,5,1),
        new(18,-6,4,12,5,4),new(-24,3,2,10,4,2),new(-12,-2,6,13,5,1),
        new(2,-2,9,16,5,4),new(22,6,3,11,4,0),new(-15,9,4,12,5,3),
        new(-1,10,5,9,5,2),new(12,9,3,10,5,1),new(-28,-6,1,8,4,2),
        new(-2,-15,6,12,4,0),new(27,0,1,9,4,3),new(-25,12,1,8,4,4),
        new(-5,3,8,11,4,1),new(17,-15,3,9,4,2),new(7,17,1,9,4,0)
    };

    private static void Polygon(CanvasItem c, Color color, params Vector2[] p)
    {
        // A terrace wall parallel to the screen's vertical axis projects to
        // a line. Do not submit such zero-area faces to native triangulation.
        if (p.Length == 4)
        {
            // Splitting planar faces avoids ill-conditioned quadrilateral
            // triangulation when a rotating wall becomes almost edge-on.
            Triangle(c, color, p[0], p[1], p[2]);
            Triangle(c, color, p[0], p[2], p[3]);
            return;
        }
        float area = 0;
        for (int i = 1; i < p.Length - 1; i++)
            area += (p[i] - p[0]).Cross(p[i + 1] - p[0]);
        if (MathF.Abs(area) > .03f) c.DrawColoredPolygon(p, color);
    }

    private static void Triangle(CanvasItem c, Color color, Vector2 a, Vector2 b, Vector2 d)
    {
        if (MathF.Abs((b - a).Cross(d - a)) > .03f)
            c.DrawPrimitive(new[] { a, b, d }, new[] { color }, Array.Empty<Vector2>());
    }
    public static void Draw(CanvasItem c, Func<float,float,float,Vector2> p, Color accent, float time, bool detailed)
    {
        // Asymmetric, receding decks; shared shapes make all terraces one vessel.
        for (int level = 0; level < 4; level++)
        {
            float rx = 35-level*7, ry = 21-level*4, x = level*1.8f-3, y = -level*2.1f, z = level*2.1f;
            var top = new Vector2[32]; var wall = new Vector2[32];
            for (int i=0;i<top.Length;i++)
            {
                float angle=i*Mathf.Tau/top.Length;
                float irregular=1+.05f*MathF.Sin(angle*3+level);
                top[i]=p(x+MathF.Cos(angle)*rx*irregular,y+MathF.Sin(angle)*ry,z);
                wall[i]=p(x+MathF.Cos(angle)*rx*irregular,y+MathF.Sin(angle)*ry,z-2.4f);
            }
            for(int i=0;i<top.Length;i++)
                Polygon(c,new Color(level%2==0?"958a69":"aaa080"),top[i],top[(i+1)%32],wall[(i+1)%32],wall[i]);
            Polygon(c,new Color(level%2==0?"d7c7a0":"c8b88f"),top);
            var closed=new Vector2[33];top.CopyTo(closed,0);closed[32]=top[0];
            c.DrawPolyline(closed,new Color("f1dfb5"),.8f,true);
        }
        int count=detailed?Homes.Length:11;
        for(int i=0;i<count;i++) DrawHouse(c,p,Homes[i],accent,i,detailed);
        DrawTemple(c,p,accent);

        // Courtyard market, lumber and crates.
        foreach(var (x,y) in new[]{(-28f,6f),(-8f,15f),(18f,13f),(7f,1f)})
        {
            Polygon(c,new Color("9c7452"),p(x,y,1),p(x+3,y,1),p(x+3,y+2,1),p(x,y+2,1));
            Polygon(c,new Color("bd9a68"),p(x,y,4),p(x+3,y,4),p(x+3,y+2,4),p(x,y+2,4));
            c.DrawLine(p(x,y,4),p(x+3,y+2,4),new Color("6e5941"),.65f,true);
        }
        for(int i=0;i<4;i++) c.DrawLine(p(-20+i,-1,3),p(-14+i,1,3),new Color("99734d"),1.2f,true);
        Polygon(c,accent.Darkened(.12f),p(-19,13,8),p(-11,13,8),p(-10,18,7),p(-20,18,7));
        c.DrawLine(p(-19,13,2),p(-19,13,8),new Color("77634c"),.8f,true);
        c.DrawLine(p(-10,18,1),p(-10,18,7),new Color("77634c"),.8f,true);

        // Laundry and pennants hang between actual rooftops.
        var a=p(-20,-8,14);var b=p(-8,-14,20);
        var sag=(a+b)*.5f+new Vector2(0,3);
        c.DrawPolyline(new[]{a,a.Lerp(sag,.55f),sag,sag.Lerp(b,.55f),b},new Color("746751"),.75f,true);
        for(int i=1;i<=4;i++)
        {
            var at=a.Lerp(b,i/5f)+new Vector2(0,MathF.Sin(i*Mathf.Pi/5)*3);
            float sway=MathF.Sin(time*1.2f+i)*.7f;
            Polygon(c,i%2==0?accent:new Color("f6ebcd"),at,at+new Vector2(2.6f,1),at+new Vector2(2+sway,4.5f),at+new Vector2(sway,4));
        }
        if(detailed)
        {
            for(int i=0;i<12;i++)
            {
                float progress=Mathf.PosMod(time*(.8f+i%3*.15f)+i*5.37f,50);
                float x=-25+progress,y=(i%3-1)*12;
                var foot=p(x,y,3+(i%3==1?4:0));
                c.DrawLine(foot,foot+new Vector2(0,-3.4f),i%3==0?accent:new Color("9b7e5b"),1.4f,true);
                c.DrawCircle(foot+new Vector2(0,-4),.85f,new Color("d5b588"));
            }
            for(int i=0;i<2;i++)
            {
                float x=-26+Mathf.PosMod(time*1.5f+i*27,50),y=i==0?15:-9;
                var cart=p(x,y,3);
                Polygon(c,new Color("a67e51"),cart+new Vector2(-2,-2),cart+new Vector2(3,-2),cart+new Vector2(4,1),cart+new Vector2(-1,1));
                c.DrawCircle(cart+new Vector2(-1,2),1.2f,new Color("554631"));
                c.DrawCircle(cart+new Vector2(3,2),1.2f,new Color("554631"));
            }
        }
        var flag=p(26,-7,25);
        c.DrawLine(p(26,-7,4),flag,new Color("d4ba87"),1.1f,true);
        Polygon(c,accent,flag,flag+new Vector2(11,2+MathF.Sin(time*1.5f)),flag+new Vector2(1,5));
    }

    private static void DrawHouse(CanvasItem c,Func<float,float,float,Vector2> p,House h,Color accent,int index,bool detailed)
    {
        float x=h.X,y=h.Y,z=h.Z,w=h.Width,height=h.Height;
        var plaster=new Color(new[]{"eadbb5","e2ccaa","dbc29b","f0e3c5","cdb996"}[h.Tone]);
        Polygon(c,plaster,p(x-w/2,y,z),p(x+w/2,y,z),p(x+w/2,y,z+height),p(x-w/2,y,z+height));
        Polygon(c,plaster.Darkened(.2f),p(x+w/2,y,z),p(x+w/2+3,y-2,z),p(x+w/2+3,y-2,z+height),p(x+w/2,y,z+height));
        Polygon(c,plaster.Lightened(.08f),p(x-w/2,y,z+height),p(x-w/2+3,y-2,z+height),p(x+w/2+3,y-2,z+height),p(x+w/2,y,z+height));
        int rows=detailed?3:2;
        for(int row=0;row<rows;row++)
        {
            float atX=x+(index%3-1)*.7f+MathF.Sin(index*7+row)*.45f;
            float atZ=z+3+row*(height-5)/rows;
            c.DrawLine(p(atX,y,atZ),p(atX,y,atZ+1.4f),new Color("6c6850"),.85f,true);
            if(detailed && (index+row)%2==0)c.DrawLine(p(x+1.6f,y,atZ+.7f),p(x+1.6f,y,atZ+2),new Color("77715b"),.65f,true);
        }
        if(index%3==0)
        {
            c.DrawLine(p(x-.8f,y,z+height),p(x-.8f,y,z+height+3),new Color("ac9674"),1.3f,true);
            c.DrawLine(p(x-1.5f,y,z+height+3),p(x,y,z+height+3),new Color("dcc8a6"),1.1f,true);
        }
        if(index%4==1)
        {
            c.DrawPolyline(new[]{p(x-1.8f,y,z+4),p(x,y,z+5.5f),p(x+1.8f,y,z+4),p(x,y,z+2.5f),p(x-1.8f,y,z+4)},accent.Darkened(.1f),.7f,true);
            c.DrawLine(p(x-1,y,z+7),p(x+1,y,z+7),accent,.8f,true);
        }
    }

    private static void DrawTemple(CanvasItem c,Func<float,float,float,Vector2> p,Color accent)
    {
        Polygon(c,new Color("f6f1df"),p(-5,-8,8),p(3,-8,8),p(3,-8,23),p(-5,-8,23));
        Polygon(c,new Color("d6d5bd"),p(3,-8,8),p(7,-11,8),p(7,-11,23),p(3,-8,23));
        Polygon(c,new Color("fff9e7"),p(-6,-8,23),p(-2,-11,23),p(8,-11,23),p(4,-8,23));
        var dome=p(1,-9,25);
        var scale=(p(3,-9,25)-p(1,-9,25)).Length()/2;
        c.DrawCircle(dome,3.8f*scale,new Color("ccac53"));
        c.DrawArc(dome,3.8f*scale,Mathf.Pi,Mathf.Tau,18,new Color("f2d886"),1,true);
        c.DrawLine(dome-new Vector2(0,3.6f*scale),dome-new Vector2(0,5.3f*scale),new Color("efcf73"),.9f,true);
        c.DrawLine(p(-1,-8,9),p(-1,-8,14),accent.Darkened(.5f),1.8f,true);
    }
}
