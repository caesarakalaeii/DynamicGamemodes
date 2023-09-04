using BattleBitAPI.Common;
using BBRAPIModules;
using System.Threading.Tasks;
using DynamicGamemode;

namespace DynamicGamemodes;

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
    protected DynamicGameMode R;

    protected GameMode(DynamicGameMode r)
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