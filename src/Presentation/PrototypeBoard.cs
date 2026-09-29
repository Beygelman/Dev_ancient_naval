using System;
using DevAncientNaval.Core.World;

namespace DevAncientNaval.Presentation;

/// <summary>Hand-composed archipelago with irregular lobes, bays and a stepping-stone island chain.</summary>
internal static class PrototypeBoard
{
	public static GameBoard Create() => new(20,20,p =>
	{
		double x=p.X,y=p.Y;
		double Island(double cx,double cy,double rx,double ry,double phase)
		{
			double dx=(x-cx)/rx,dy=(y-cy)/ry,angle=Math.Atan2(dy,dx);
			return Math.Sqrt(dx*dx+dy*dy)-(1+.17*Math.Sin(3*angle+phase)+.12*Math.Cos(5*angle-phase));
		}
		bool land=Island(6,4,3.1,2.3,.8)<0 || Island(12.8,14.5,2.7,3.2,2.1)<0 ||
			Island(7,13,1.7,2.2,1.2)<0 || Island(13.8,3.3,1.8,1.3,2)<0 ||
			Island(10.2,7.1,1,.8,1)<0 || Island(11.5,9.4,.7,.9,2)<0 ||
			Island(13.5,10.5,1.1,.8,3)<0 || Island(4,16,1,.8,1)<0 || Island(17,16,.7,.7,0)<0;
		// Carve bays into the large islands; preserve clear starting harbours on both sides.
		if(Island(5.2,5.7,1.2,1,0)<0 || Island(11,13,1,1.4,1)<0) land=false;
		if((x<=4&&y>=8&&y<=12)||(x>=15&&y>=6&&y<=10)) land=false;
		return land?TerrainType.Land:TerrainType.Water;
	});
}
