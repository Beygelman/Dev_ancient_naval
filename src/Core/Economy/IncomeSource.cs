using DevAncientNaval.Core.Units;

namespace DevAncientNaval.Core.Economy;

/// <summary>Income is attributed to a source, not hard-wired to a ship type.
/// BoundShipId removes a ship source when its ship sinks; stationary sources have no ship binding.</summary>
public sealed record IncomeSource(string Id, Side Owner, int Amount, int? BoundShipId = null);
