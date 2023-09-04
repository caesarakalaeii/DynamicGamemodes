using BattleBitAPI.Common;
using BBRAPIModules;
using DynamicGamemode;

namespace DynamicGamemode;

public class LifeSteal : GameMode
{
    public LifeSteal(DynamicGameMode r) : base(r)
    {
        Name = "LifeSteal";
    }

    public override OnPlayerKillArguments<RunnerPlayer> OnAPlayerDownedAnotherPlayer(
        OnPlayerKillArguments<RunnerPlayer> args)
    {
        args.Killer.SetHP(100);
        args.Victim.Kill();
        return base.OnAPlayerDownedAnotherPlayer(args);
    }


    public override RunnerPlayer OnPlayerSpawned(RunnerPlayer player)
    {
        player.Modifications.RunningSpeedMultiplier = 1.25f;
        player.Modifications.FallDamageMultiplier = 0f;
        player.Modifications.JumpHeightMultiplier = 1.5f;
        player.Modifications.DisableBleeding();
        return base.OnPlayerSpawned(player);
    }
}