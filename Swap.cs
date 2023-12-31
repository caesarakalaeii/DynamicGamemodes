﻿using BattleBitAPI.Common;
using BBRAPIModules;
using DynamicGamemodes;

namespace DynamicGamemodes;

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