using BBRAPIModules;

namespace DynamicGamemode.GameModes;

public class Hardcore : GameMode
{
    public Hardcore(DynamicGameMode r) : base(r)
    {
        Name = "Hardcore";
    }

    public override RunnerPlayer OnPlayerSpawned(RunnerPlayer player)
    {
        player.Modifications.HitMarkersEnabled = false;
        player.Modifications.RunningSpeedMultiplier = 1.25f;
        player.Modifications.FallDamageMultiplier = 2f;
        player.Modifications.GiveDamageMultiplier = 2f;
        player.SetHP(50);
        return base.OnPlayerSpawned(player);
    }
}