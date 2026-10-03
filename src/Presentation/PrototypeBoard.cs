using System;
using DevAncientNaval.Core.World;

namespace DevAncientNaval.Presentation;
internal static class PrototypeBoard
{
<<<<<<< Updated upstream
	public static GameBoard Create(int? seed = null, int opponentCount = 3, WorldKind kind = WorldKind.Oceans) =>
		ArchipelagoGenerator.Create(seed ?? Random.Shared.Next(), opponentCount, kind);
=======
    public static GameBoard Create(int? seed = null, int opponentCount = 3) => ArchipelagoGenerator.Create(seed ?? Random.Shared.Next(), opponentCount);
>>>>>>> Stashed changes
}
