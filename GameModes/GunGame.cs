using BattleBitAPI.Common;
using BBRAPIModules;
using System.Collections.Generic;

namespace DynamicGamemodes.GameModes;

public class GunGame : GameMode
{
    private readonly GunGamePlayerData _data = new();
    
    private readonly List<Weapon> _mGunGame = new()
    {
        Weapons.Glock18,
        Weapons.Groza,
        Weapons.ACR,
        Weapons.AK15,
        Weapons.AK74,
        Weapons.G36C,
        Weapons.HoneyBadger,
        Weapons.KrissVector,
        Weapons.L86A1,
        Weapons.L96,
        Weapons.M4A1,
        Weapons.M9,
        Weapons.M110,
        Weapons.M249,
        Weapons.MK14EBR,
        Weapons.MK20,
        Weapons.MP7,
        Weapons.PP2000,
        Weapons.SCARH,
        Weapons.SSG69
    };

    public GunGame(DynamicGameMode r) : base(r)
    {
        Name = "GunGame";
        GunGamePlayerData data = new();
    }

    public override void Init()
    {

        R.Server.ServerSettings.TeamlessMode = true;
    }


    // Gun Game
    public override Returner OnPlayerSpawning(RunnerPlayer player, OnPlayerSpawnArguments request)
    {
        UpdateWeapon(player);
        return base.OnPlayerSpawning(player, request);
    }

    public override RunnerPlayer OnPlayerSpawned(RunnerPlayer player)
    {
        player.Modifications.RespawnTime = 0f;
        player.Modifications.RunningSpeedMultiplier = 1.25f;
        player.Modifications.FallDamageMultiplier = 0f;
        player.Modifications.JumpHeightMultiplier = 1.5f;
        player.Modifications.DisableBleeding();
        return base.OnPlayerSpawned(player);
    }

    public int GetGameLenght()
    {
        return _mGunGame.Count;
    }

    public void UpdateWeapon(RunnerPlayer player)
    {
        var w = new WeaponItem
        {
            ToolName = _mGunGame[_data.GetLevel(player)].Name,
            MainSight = Attachments.RedDot
        };


        player.SetPrimaryWeapon(w, 10, true);
    }

    public override OnPlayerKillArguments<RunnerPlayer> OnAPlayerDownedAnotherPlayer(
        OnPlayerKillArguments<RunnerPlayer> onPlayerKillArguments)
    {
        var killer = onPlayerKillArguments.Killer;
        var victim = onPlayerKillArguments.Victim;
        _data.IncLevel(killer);
        if (_data.GetLevel(killer) == GetGameLenght()) R.Server.AnnounceShort($"{killer.Name} only needs 1 more Kill");
        if (_data.GetLevel(killer) > GetGameLenght())
        {
            R.Server.AnnounceShort($"{killer.Name} won the Game");
            R.Server.ForceEndGame();
        }

        killer.SetHP(100);
        victim.Kill();
        if (onPlayerKillArguments.KillerTool == "Sledge Hammer" && _data.GetLevel(victim) != 0) _data.DecLevel(victim);
        UpdateWeapon(killer);
        return base.OnAPlayerDownedAnotherPlayer(onPlayerKillArguments);
    }


    public override void Reset()
    {
        R.Server.SayToAllChat("Resetting GameMode");
        R.Server.ServerSettings.TeamlessMode = false;
        foreach (var player in R.Server.AllPlayers)
        {
            _data.SetLevel(player,0);
            player.Kill();
        }
    }
}

public class GunGamePlayerData : GameModePlayerData
{
    public Dictionary<ulong, int> Levels = new Dictionary<ulong, int>();


    public int GetLevel(RunnerPlayer player)
    {
        if (Levels.TryGetValue(player.SteamID, value: out var level)) return level;
        Levels.Add(player.SteamID,0);
        return 0;

    }

    public void SetLevel(RunnerPlayer player, int level)
    {
        if (!Levels.TryAdd(player.SteamID, level))
        {
            Levels[player.SteamID] = level;
        }
    }

    public void IncLevel(RunnerPlayer player)
    {
        var current = GetLevel(player);
        current++;
        SetLevel(player, current);
    }
    public void DecLevel(RunnerPlayer player)
    {
        var current = GetLevel(player);
        current--;
        SetLevel(player, current);
    }
}