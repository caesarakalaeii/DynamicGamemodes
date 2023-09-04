
using BattleBitAPI.Common;
using BattleBitBaseModules;
using BBRAPIModules;
using Commands;
using DynamicGamemodes.GameModes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DynamicGamemodes;


[RequireModule(typeof(CommandHandler)), RequireModule(typeof(RichText))]
public class DynamicGameMode: BattleBitModule
{
    
    [ModuleReference]
    public CommandHandler CommandHandler { get; set; }
    
    private List<GameMode> mGameModes;
    private GameMode mCurrentGameMode;
    private bool _mCyclePlaylist;
    private int _mGameModeIndex;



    public DynamicGameMode()
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

