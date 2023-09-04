using BattleBitAPI.Common;
using BBRAPIModules;
using DynamicGamemode;

namespace DynamicGamemodes;
public class Csgo : GameMode
{
    public Csgo(DynamicGameMode r) : base(r)
    {
        Name = "CSGO";
    }

    public override void Init()
    {
        R.Server.ServerSettings.PlayerCollision = true;
        R.Server.ServerSettings.FriendlyFireEnabled = true;
        if (R.Server.Gamemode != "Rush")
        {
            // dunno
        }
    }
    //Buy system maybe


    public override OnPlayerKillArguments<RunnerPlayer> OnAPlayerDownedAnotherPlayer(
        OnPlayerKillArguments<RunnerPlayer> args)
    {
        var victim = args.Victim;
        var killer = args.Killer;
        victim.Modifications.CanDeploy = false;
        victim.Modifications.CanSpectate = false;
        victim.Kill();
        return base.OnAPlayerDownedAnotherPlayer(args);
    }

    public override void Reset()
    {
        R.Server.ServerSettings.PlayerCollision = false;
        R.Server.ServerSettings.FriendlyFireEnabled = false;
        base.Reset();
    }
}