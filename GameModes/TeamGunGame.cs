using BattleBitAPI.Common;
using BBRAPIModules;
using GameModeModule;
using System.Collections.Generic;

namespace GameModeModule.GameModes;

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

    public TeamGunGame(DynamicGameMode r) : base(r)
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