
using BattleBitAPI.Common;
using BattleBitBaseModules;
using BBRAPIModules;
using Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DynamicGamemodes;


[RequireModule(typeof(CommandHandler)), RequireModule(typeof(RichText))]
public class DynamicGameModes: BattleBitModule
{
    
    [ModuleReference]
    public CommandHandler CommandHandler { get; set; }
    
    private List<GameMode> mGameModes;
    private GameMode mCurrentGameMode;
    private bool _mCyclePlaylist;
    private int _mGameModeIndex;



    public DynamicGameModes()
    {
        _mCyclePlaylist = false;
        mGameModes = new List<GameMode>
        {
            new GunGame(this),
            new TeamGunGame(this),
            new LifeSteal(this),
            new Swap(this),
            new Hardcore(this),
            new MeleeOnly(this),
            new Csgo(this)
        };
        _mGameModeIndex = 0;
        mCurrentGameMode = mGameModes[_mGameModeIndex];
    }
    public override void OnModulesLoaded()
    {
        CommandHandler.Register(this);
        
    }


    
    //GAMEMODE PASSTHROUGH

    public override Task OnAPlayerDownedAnotherPlayer(OnPlayerKillArguments<RunnerPlayer> args)
    {
        
        mCurrentGameMode.OnAPlayerDownedAnotherPlayer(args);
        return Task.CompletedTask;
    }

    public override Task OnPlayerGivenUp(RunnerPlayer player)
    {
        mCurrentGameMode.OnPlayerGivenUp(player);
        return Task.CompletedTask;
    }
    
    public override Task OnPlayerConnected(RunnerPlayer player)
    {
        Server.SayToAllChat("<color=green>" + player.Name + " joined the game!</color>");
        player.Message($"Current GameMode is: {mCurrentGameMode.Name}", 4f);
        Console.Out.WriteLine("Connected: " + player);

        player.JoinSquad(Squads.Alpha);
        return Task.CompletedTask;
    }

    public override Task OnPlayerDisconnected(RunnerPlayer player)
    {
        Console.WriteLine($"{player.Name} disconnected");
        Server.SayToAllChat($"<color=red>{player.Name} left the game!</color>");
        mCurrentGameMode.OnPlayerDisconnected(player);
        return Task.CompletedTask;
    }
    
    
    //GAMEMODE COMMANDS
    
    
    [CommandCallback("nextGM", Description = "Selects the next Gamemode in Playlist and resets all Players")]
    public void NextGameModeCommand(RunnerPlayer commandSource)
    {
        mCurrentGameMode.Reset();
        _mGameModeIndex = (_mGameModeIndex + 1) % mGameModes.Count;
        mCurrentGameMode = mGameModes[_mGameModeIndex];
        mCurrentGameMode.Init();
        
        Server.AnnounceShort($"GameMode is now {mCurrentGameMode.Name}");
        Console.WriteLine($"GameMode is now {mCurrentGameMode.Name}");
    }
    [CommandCallback("SetGM", Description = "Sets the Gamemode to a specific one in Playlist and resets all Players")]
    public void SetGameModeCommand(RunnerPlayer commandSource, string gamemodeName)
    {
        try
        {
            mCurrentGameMode.Reset();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ERROR resetting GM: {ex}");
        }

        foreach (var gameMode in mGameModes.Where(gameMode => gameMode.Name == gamemodeName))
        {
            mCurrentGameMode = gameMode;
            _mGameModeIndex = mGameModes.IndexOf(gameMode);
        }

        try
        {
            mCurrentGameMode.Init();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ERROR initializing GM: {ex}");
        }

        Server.AnnounceShort($"GameMode is now {mCurrentGameMode.Name}");
    }
    [CommandCallback("GetGM", Description = "Announces the current selected GameMode")]
    public void GetGameModeCommand(RunnerPlayer commandSource)
    {
        Server.AnnounceShort($"GameMode is {mCurrentGameMode.Name}");
    }
    
    [CommandCallback("togglePlaylist", Description = "Toggles the GamemodePlaylist")]
    public void TogglePlaylistCommand(RunnerPlayer commandSource)
    {
        _mCyclePlaylist = !_mCyclePlaylist;
        
        Server.AnnounceShort($"Playlist is now {_mCyclePlaylist}");
        Console.WriteLine($"Playlist is now {_mCyclePlaylist}");
    }
}

public class Returner
{
    public OnPlayerSpawnArguments SpawnArguments;
    public ChatChannel Channel;
    public PlayerJoiningArguments JoiningArguments;
    public string Msg;
    public RunnerPlayer Player;
    public ulong SteamId;
}

public class GameMode
{
    public string Name = string.Empty;
    protected DynamicGameModes R;

    protected GameMode(DynamicGameModes r)
    {
        R = r;
    }

    public virtual void Init()
    {
    }

    public virtual void Reset()
    {
        foreach (var player in R.Server.AllPlayers) player.Kill();
    }

    public virtual void OnRoundEnded()
    {
        Reset();
    }

    public virtual OnPlayerKillArguments<RunnerPlayer> OnAPlayerDownedAnotherPlayer(
        OnPlayerKillArguments<RunnerPlayer> args)
    {
        return args;
    }

    public virtual RunnerPlayer OnPlayerGivenUp(RunnerPlayer player)
    {
        return player;
    }

    public virtual RunnerPlayer OnPlayerSpawned(RunnerPlayer player)
    {
        return player;
    }

    public virtual Returner OnPlayerSpawning(RunnerPlayer player, OnPlayerSpawnArguments request)
    {
        var re = new Returner
        {
            Player = player,
            SpawnArguments = request
        };
        return re;
    }

    public virtual void OnRoundStarted()
    {
    }

    public RunnerPlayer OnPlayerDisconnected(RunnerPlayer player)
    {
        return player;
    }

    public Returner OnPlayerJoiningToServer(ulong steamId, PlayerJoiningArguments args)
    {
        var re = new Returner
        {
            SteamId = steamId,
            JoiningArguments = args
        };
        return re;
    }

    public async Task<Returner> OnPlayerTypedMessage(RunnerPlayer player, ChatChannel channel, string msg)
    {
        var re = new Returner
        {
            Player = player,
            Channel = channel,
            Msg = msg
        };
        return re;
    }
}

public class GameModePlayerData
{

}


public class TeamGunGame : GameMode
{
    public int LevelA;
    public int LevelB;

    public List<WeaponItem> ProgressionList = new()
    {
        new WeaponItem
        {
            Tool = Weapons.FAL,
            MainSight = Attachments.RedDot,
            TopSight = null,
            CantedSight = null,
            Barrel = null,
            SideRail = null,
            UnderRail = null,
            BoltAction = null
        },

        new WeaponItem
        {
            Tool = Weapons.M249,
            MainSight = Attachments.Acog,
            TopSight = null,
            CantedSight = null,
            Barrel = null,
            SideRail = null,
            UnderRail = null,
            BoltAction = null
        },
        new WeaponItem
        {
            Tool = Weapons.M4A1,
            MainSight = Attachments.Holographic,
            TopSight = null,
            CantedSight = Attachments.CantedRedDot,
            Barrel = Attachments.Compensator,
            SideRail = Attachments.Flashlight,
            UnderRail = Attachments.VerticalGrip,
            BoltAction = null
        },
        new WeaponItem
        {
            Tool = Weapons.AK74,
            MainSight = Attachments.RedDot,
            TopSight = Attachments.DeltaSightTop,
            CantedSight = Attachments.CantedRedDot,
            Barrel = Attachments.Ranger,
            SideRail = Attachments.TacticalFlashlight,
            UnderRail = Attachments.Bipod,
            BoltAction = null
        },
        new WeaponItem
        {
            Tool = Weapons.SCARH,
            MainSight = Attachments.Acog,
            TopSight = Attachments.RedDotTop,
            CantedSight = Attachments.Ironsight,
            Barrel = Attachments.MuzzleBreak,
            SideRail = Attachments.TacticalFlashlight,
            UnderRail = Attachments.AngledGrip,
            BoltAction = null
        },
        new WeaponItem
        {
            Tool = Weapons.SSG69,
            MainSight = Attachments._6xScope,
            TopSight = null,
            CantedSight = Attachments.HoloDot,
            Barrel = Attachments.LongBarrel,
            SideRail = Attachments.Greenlaser,
            UnderRail = Attachments.VerticalSkeletonGrip,
            BoltAction = null
        },
        new WeaponItem
        {
            Tool = Weapons.M110,
            MainSight = Attachments.Acog,
            TopSight = Attachments.PistolRedDot,
            CantedSight = Attachments.FYouCanted,
            Barrel = Attachments.Heavy,
            SideRail = Attachments.TacticalFlashlight,
            UnderRail = Attachments.StubbyGrip,
            BoltAction = null
        },
        new WeaponItem
        {
            Tool = Weapons.PP2000,
            MainSight = Attachments.Kobra,
            TopSight = null,
            CantedSight = Attachments.Ironsight,
            Barrel = Attachments.MuzzleBreak,
            SideRail = Attachments.Flashlight,
            UnderRail = Attachments.AngledGrip,
            BoltAction = null
        }
    };

    public TeamGunGame(DynamicGameModes r) : base(r)
    {
        Name = "TeamGunGame";
        LevelA = 0;
        LevelB = 0;
    }

    public override Returner OnPlayerSpawning(RunnerPlayer player, OnPlayerSpawnArguments request)
    {
        var level = 0;
        if (player.Team == Team.TeamA) level = LevelA;
        else if (player.Team == Team.TeamB) level = LevelB;

        request.Loadout.PrimaryWeapon = ProgressionList[level];
        request.Loadout.SecondaryWeapon = default;
        request.Loadout.LightGadget = null;
        request.Loadout.Throwable = null;
        request.Loadout.FirstAid = null;
        request.Loadout.HeavyGadget = new Gadget("Sledge Hammer");
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

    public override OnPlayerKillArguments<RunnerPlayer> OnAPlayerDownedAnotherPlayer(
        OnPlayerKillArguments<RunnerPlayer> args)
    {
        args.Victim.Kill();
        int level;
        if (args.Killer.Team == Team.TeamA)
        {
            LevelA++;
            level = LevelA;
        }
        else
        {
            LevelB++;
            level = LevelB;
        }

        if (level == ProgressionList.Count)
        {
            R.Server.AnnounceShort($"{args.Killer.Team.ToString()} only needs 1 more Kill");
        }
        else if (level > ProgressionList.Count)
        {
            R.Server.AnnounceLong($"{args.Killer.Team.ToString()} won the Game");
            R.Server.ForceEndGame();
            Reset();
        }

        return base.OnAPlayerDownedAnotherPlayer(args);
    }


    public override void Reset()
    {
        LevelA = 0;
        LevelB = 0;

        base.Reset();
    }
}


public class Swap : GameMode
{
    public Swap(DynamicGameModes r) : base(r)
    {
        Name = "Swappers";
    }

    public override Returner OnPlayerSpawning(RunnerPlayer player, OnPlayerSpawnArguments request)
    {
        player.Modifications.RunningSpeedMultiplier = 1.25f;
        player.Modifications.FallDamageMultiplier = 0f;
        player.Modifications.JumpHeightMultiplier = 1.5f;
        return base.OnPlayerSpawning(player, request);
    }

    public override OnPlayerKillArguments<RunnerPlayer> OnAPlayerDownedAnotherPlayer(
        OnPlayerKillArguments<RunnerPlayer> onPlayerKillArguments)
    {

        var victimPos = onPlayerKillArguments.VictimPosition;
        onPlayerKillArguments.Killer.Teleport(victimPos);
        onPlayerKillArguments.Victim.Kill();
        return base.OnAPlayerDownedAnotherPlayer(onPlayerKillArguments);
    }
}

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

    public GunGame(DynamicGameModes r) : base(r)
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
            _data.SetLevel(player, 0);
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
        Levels.Add(player.SteamID, 0);
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

public class Hardcore : GameMode
{
    public Hardcore(DynamicGameModes r) : base(r)
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

public class Csgo : GameMode
{
    public Csgo(DynamicGameModes r) : base(r)
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

public class LifeSteal : GameMode
{
    public LifeSteal(DynamicGameModes r) : base(r)
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