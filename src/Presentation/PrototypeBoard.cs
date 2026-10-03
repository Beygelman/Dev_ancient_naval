using System;
using DevAncientNaval.Core.World;

namespace DevAncientNaval.Presentation;
internal static class PrototypeBoard
{
	public static GameBoard Create(int? seed = null, int opponentCount = 3, WorldKind kind = WorldKind.Oceans, MapSize? mapSize = null) =>
		ArchipelagoGenerator.Create(seed ?? Random.Shared.Next(), opponentCount, kind, mapSize);
}
