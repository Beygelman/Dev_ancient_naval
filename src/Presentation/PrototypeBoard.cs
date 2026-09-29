using System;
using DevAncientNaval.Core.World;

namespace DevAncientNaval.Presentation;
internal static class PrototypeBoard
{
    public static GameBoard Create(int? seed=null) => ArchipelagoGenerator.Create(seed ?? Random.Shared.Next());
	public static GameBoard Create(int? seed=null) => ArchipelagoGenerator.Create(seed ?? Random.Shared.Next());
}
