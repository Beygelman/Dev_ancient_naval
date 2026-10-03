using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DevAncientNaval.Core.Battle;
using DevAncientNaval.Core.Grid;
using DevAncientNaval.Core.Units;
using DevAncientNaval.Core.World;
using Godot;

namespace DevAncientNaval.Presentation.Map;
<<<<<<< Updated upstream
=======

>>>>>>> Stashed changes
public partial class FleetView
{
    private int _salvoSequence;
    internal int LastSalvoCount { get; private set; }
<<<<<<< Updated upstream
    internal float LastSalvoLaunchSpread { get; private set; }

    internal readonly List<(int Launched, int Landed)> CompletedSalvos = new();
    internal readonly List<float> CompletedLaunchSpreads = new();
    private readonly record struct RouteSample(Vector2 Position, float Distance, bool Visible);
    private async Task AnimateTravel(int shipId, IReadOnlyList<MovementFrame> frames)
    {
        var routeTrace = Diagnostics.PerformanceTrace.Measure("Animation.TravelRoute");
        var moving = Battle.Find(shipId)!;
        _movingShip = ShipSnapshot.From(moving);
        _movingId = shipId;
        _movingPosition = Projection.GridToWorld(frames[0].Position);
        if (!frames.Any(f => f.VisibleToPlayer))
        {
            routeTrace.Dispose();
            return;
        }
        var profile = ShipVisualProfile.For(moving.Definition.Class);
        var route = new List<RouteSample>
        {
            new(_movingPosition, 0, frames[0].VisibleToPlayer)
        };
=======
    internal readonly List<(int Launched, int Landed)> CompletedSalvos = new();
    private readonly record struct RouteSample(Vector2 Position, float Distance, bool Visible);

    private async Task AnimateTravel(int shipId, IReadOnlyList<MovementFrame> frames)
    {
        var moving = Battle.Find(shipId)!;
        _movingShip = ShipSnapshot.From(moving); _movingId = shipId;
        _movingPosition = Projection.GridToWorld(frames[0].Position);
        if (!frames.Any(f => f.VisibleToPlayer)) return;
        var profile = ShipVisualProfile.For(moving.Definition.Class);
        var route = new List<RouteSample> { new(_movingPosition, 0, frames[0].VisibleToPlayer) };
>>>>>>> Stashed changes
        float length = 0;
        for (int i = 0; i < frames.Count - 1; i++)
        {
            Vector2 Position(int n) => Projection.GridToWorld(frames[Math.Clamp(n, 0, frames.Count - 1)].Position);
<<<<<<< Updated upstream
            var a = Position(i);
            var b = Position(i + 1);
=======
            var a = Position(i); var b = Position(i + 1);
>>>>>>> Stashed changes
            for (int step = 1; step <= 12; step++)
            {
                float t = step / 12f;
                var p = a.CubicInterpolate(b, Position(i - 1), Position(i + 2), t);
                var cell = Projection.WorldToGrid(p);
                // Keep a smoothed corner in navigable space. Visual interpolation
                // must not make a correctly routed ship cut across an island.
<<<<<<< Updated upstream
                if (!Battle.Board.Contains(cell) || (!moving.IsAirborne && Battle.Board.GetTile(cell).Terrain == TerrainType.Land))
                    p = a.Lerp(b, t);
=======
                if (!Battle.Board.Contains(cell) || (!moving.IsAirborne && Battle.Board.GetTile(cell).Terrain == TerrainType.Land)) p = a.Lerp(b, t);
>>>>>>> Stashed changes
                length += route[^1].Position.DistanceTo(p);
                route.Add(new(p, length, frames[i].VisibleToPlayer && frames[i + 1].VisibleToPlayer));
            }
        }
<<<<<<< Updated upstream

        int index = 1;
        Vector2 previousDirection = Vector2.Zero;
        _sailingBank = 0;
        _sailingPitch = 0;
        _wakeClock = _clock;
        double duration = Math.Max(.3, profile.TravelSeconds * (frames.Count - 1) + profile.Size * .42f);
        routeTrace.Dispose();
        await TweenValue(duration, t =>
        {
            float distance = MotionProgress(t) * length;
            while (index < route.Count - 1 && route[index].Distance < distance)
                index++;
            var a = route[index - 1];
            var b = route[index];
            float local = Math.Clamp((distance - a.Distance) / Math.Max(.001f, b.Distance - a.Distance), 0, 1);
            var previous = _movingPosition;
            _movingPosition = a.Position.Lerp(b.Position, local);
            _movingVisible = b.Visible;
            var heading = (_movingPosition - previous).Normalized();
            if (heading != Vector2.Zero && !moving.IsAirborne)
                _deckAngles[shipId] = Mathf.LerpAngle(DeckAngle(shipId), DeckProjection.Heading(heading) - BaseHeading(moving.Definition.Class, moving.Owner), .12f / profile.Size);
            float speed = MathF.Sin(Mathf.Pi * t);
            speed *= speed;
            _sailingPitch = -.055f * speed * (_movingShip!.Owner == DevAncientNaval.Core.Units.Side.Player ? 1 : -1);
            float turn = previousDirection == Vector2.Zero ? 0 : previousDirection.Cross(heading);
            _sailingBank = Mathf.Lerp(_sailingBank, Math.Clamp(turn * 1.8f, -.12f, .12f) * speed, .2f);
            if (heading != Vector2.Zero)
                previousDirection = heading;
=======
        int index = 1; Vector2 previousDirection = Vector2.Zero; _sailingBank = 0; _sailingPitch = 0;
        _wakeClock = _clock;
        double duration = Math.Max(.3, profile.TravelSeconds * (frames.Count - 1) + profile.Size * .42f);
        await TweenValue(duration, t =>
        {
            float distance = MotionProgress(t) * length;
            while (index < route.Count - 1 && route[index].Distance < distance) index++;
            var a = route[index - 1]; var b = route[index];
            float local = Math.Clamp((distance - a.Distance) / Math.Max(.001f, b.Distance - a.Distance), 0, 1);
            var previous = _movingPosition;
            _movingPosition = a.Position.Lerp(b.Position, local); _movingVisible = b.Visible;
            var heading = (_movingPosition - previous).Normalized();
            if (heading != Vector2.Zero && !moving.IsAirborne)
                _deckAngles[shipId] = Mathf.LerpAngle(DeckAngle(shipId),heading.Angle()-BaseHeading(moving.Definition.Class, moving.Owner),.12f/profile.Size);
            float speed = MathF.Sin(Mathf.Pi * t); speed *= speed;
            _sailingPitch = -.055f * speed * (_movingShip!.Owner == DevAncientNaval.Core.Units.Side.Player ? 1 : -1);
            float turn = previousDirection == Vector2.Zero ? 0 : previousDirection.Cross(heading);
            _sailingBank = Mathf.Lerp(_sailingBank, Math.Clamp(turn * 1.8f, -.12f, .12f) * speed, .2f);
            if (heading != Vector2.Zero) previousDirection = heading;
>>>>>>> Stashed changes
            if (_movingVisible && !moving.IsAirborne && _clock - _wakeClock > .13f && t > .03f && t < .97f)
            {
                var direction = (_movingPosition - previous).Normalized();
                EmitRipple(_movingPosition - direction * 12 * profile.Size, profile.Wake * .72f, direction.Angle(), true);
                _wakeClock = _clock;
            }
        });
<<<<<<< Updated upstream
        if (_movingVisible && !moving.IsAirborne)
            EmitRipple(_movingPosition, profile.Wake * .65f);
        _movingId = 0;
        _movingShip = null;
        _sailingPitch = 0;
        _sailingBank = 0;
    }

    private async Task AnimateSalvo(ShipSnapshot attacker, GridPosition targetCell, ShipSnapshot? target, bool mortar, bool attackerVisible, bool targetVisible, int charges = 1)
    {
        if (!attackerVisible && !targetVisible)
            return;
        if (targetVisible && FocusTarget is not null) await FocusTarget(targetCell);
        var profile = ShipVisualProfile.For(attacker.Class);
        int guns = mortar ? 1 : Math.Max(1, profile.Cannonballs);
        int count = guns * charges;
=======
        if (_movingVisible && !moving.IsAirborne) EmitRipple(_movingPosition, profile.Wake * .65f);
        _movingId = 0; _movingShip = null; _sailingPitch = 0; _sailingBank = 0;
    }

    private async Task AnimateSalvo(ShipSnapshot attacker, GridPosition targetCell, ShipSnapshot? target,
        bool mortar, bool attackerVisible, bool targetVisible)
    {
        if (!attackerVisible && !targetVisible) return;
        var profile = ShipVisualProfile.For(attacker.Class);
        int count = mortar ? 1 : Math.Max(1, profile.Cannonballs);
>>>>>>> Stashed changes
        LastSalvoCount = count;
        var random = new Random(attacker.Id * 7919 + ++_salvoSequence * 173 + targetCell.X * 31 + targetCell.Y);
        var from = Projection.GridToWorld(attacker.Position) + new Vector2(0, -8);
        var center = Projection.GridToWorld(targetCell) + new Vector2(0, target?.Class == ShipClass.Balloon ? -62 : -5);
        var direction = (center - from).Normalized();
<<<<<<< Updated upstream
        await AimBattery(attacker, direction, mortar, attackerVisible);
        from += direction * profile.Size * 13;
        var ends = new Vector2[count];
        var starts = new Vector2[count];
        var launched = new bool[count];
        var landed = new bool[count];
=======
        await AimBattery(attacker,direction,mortar,attackerVisible);
        from += direction * profile.Size * 13;
        var ends = new Vector2[count]; var starts = new Vector2[count];
        var launched = new bool[count]; var landed = new bool[count];
>>>>>>> Stashed changes
        float targetSize = target is null ? .8f : ShipVisualProfile.For(target.Class).Size;
        for (int i = 0; i < count; i++)
        {
            // Bounded impacts on the deck, never a random miss around the tile.
            ends[i] = center + new Vector2((float)random.NextDouble() * 24 - 12, (float)random.NextDouble() * 7 - 3.5f) * targetSize;
<<<<<<< Updated upstream
            starts[i] = from + direction.Orthogonal() * (((i % guns) - (guns - 1) * .5f) * 5 * profile.Size + i / guns * 1.8f);
        }

        float distance = from.DistanceTo(center);
        float flight = mortar ? Math.Clamp(distance / 330f, .76f, 1.16f) : Math.Clamp(distance / 560f, .32f, .62f);
        float height = mortar ? Math.Clamp(distance * .95f, 130, 270) : Math.Clamp(1900f * flight * flight / 8, 28, 95);
        float interval = charges > 1 ? 0 : .035f;
        LastSalvoLaunchSpread = (count - 1) * interval;
=======
            starts[i] = from + direction.Orthogonal() * ((i - (count - 1) * .5f) * 5 * profile.Size);
        }
        float distance = from.DistanceTo(center);
        float flight = mortar ? Math.Clamp(distance / 330f, .76f, 1.16f) : Math.Clamp(distance / 560f, .32f, .62f);
        float height = mortar ? Math.Clamp(distance * .95f, 130, 270) : Math.Clamp(1900f * flight * flight / 8, 28, 95);
        const float interval = .035f;
>>>>>>> Stashed changes
        float duration = flight + (count - 1) * interval;
        Vector2 Ball(int i, float t) => starts[i].Lerp(ends[i], t) + new Vector2(0, -4 * height * t * (1 - t));
        await TweenValue(duration, progress =>
        {
            float elapsed = progress * duration;
<<<<<<< Updated upstream
            _projectiles.Clear();
            _muzzle = null;
            for (int i = 0; i < count; i++)
            {
                float age = elapsed - i * interval;
                if (age < 0)
                    continue;
                if (!launched[i])
                {
                    launched[i] = true;
                    if (attackerVisible)
                        GunEffect(attacker, starts[i], direction, mortar);
                }

                if (age < .045f && attackerVisible)
                    _muzzle = starts[i];
                if (age >= flight)
                {
                    if (!landed[i] && targetVisible)
                        HitEffect(target, ends[i], direction, mortar);
                    landed[i] = true;
                    continue;
                }

                float t = age / flight;
                if ((!attackerVisible && t < .82f) || (!targetVisible && t > .18f))
                    continue;
                _projectiles.Add((Ball(i, t), Ball(i, Math.Max(0, t - .035f)), mortar ? 5.2f : 3.4f, Projection.GridToWorld(attacker.Position).Lerp(Projection.GridToWorld(targetCell), t)));
            }

            ProjectilePosition = _projectiles.Count > 0 ? _projectiles[0].Point : null;
        });
        _projectiles.Clear();
        ProjectilePosition = null;
        _muzzle = null;
        CompletedSalvos.Add((launched.Count(value => value), landed.Count(value => value)));
        CompletedLaunchSpreads.Add((count - 1) * interval);
=======
            _projectiles.Clear(); _muzzle = null;
            for (int i = 0; i < count; i++)
            {
                float age = elapsed - i * interval;
                if (age < 0) continue;
                if (!launched[i])
                {
                    launched[i] = true;
                    if (attackerVisible) GunEffect(attacker, starts[i], direction, mortar);
                }
                if (age < .045f && attackerVisible) _muzzle = starts[i];
                if (age >= flight)
                {
                    if (!landed[i] && targetVisible) HitEffect(target, ends[i], direction, mortar);
                    landed[i] = true; continue;
                }
                float t = age / flight;
                _projectiles.Add((Ball(i, t), Ball(i, Math.Max(0, t - .035f)), mortar ? 5.2f : 3.4f, Projection.GridToWorld(attacker.Position).Lerp(Projection.GridToWorld(targetCell), t)));
            }
            ProjectilePosition = _projectiles.Count > 0 ? _projectiles[0].Point : null;
        });
        _projectiles.Clear(); ProjectilePosition = null; _muzzle = null;
        CompletedSalvos.Add((launched.Count(value => value), landed.Count(value => value)));
>>>>>>> Stashed changes
    }
}
