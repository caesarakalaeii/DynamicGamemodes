using BattleBitAPI.Common;
using BBRAPIModules;
using DynamicGamemodes;

namespace DynamicGamemodes;

public class MeleeOnly : GameMode
{
    public MeleeOnly(DynamicGameModes r) : base(r)
    {
        Name = "MeleeOnly";
    }

    public override Returner OnPlayerSpawning(RunnerPlayer player, OnPlayerSpawnArguments request)
    {
        player.SetLightGadget("Pickaxe", 0, true);
        player.Modifications.RunningSpeedMultiplier = 1.25f;
        player.Modifications.FallDamageMultiplier = 0f;
        player.Modifications.JumpHeightMultiplier = 1.5f;
        return base.OnPlayerSpawning(player, request);
    }
}