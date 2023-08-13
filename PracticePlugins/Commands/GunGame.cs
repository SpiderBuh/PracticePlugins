﻿using CommandSystem;
using CommandSystem.Commands.RemoteAdmin;
using CommandSystem.Commands.RemoteAdmin.Doors;
using CustomPlayerEffects;
using Interactables.Interobjects;
using Interactables.Interobjects.DoorUtils;
using InventorySystem.Items;
using InventorySystem.Items.Firearms;
using InventorySystem.Items.Firearms.Attachments;
using MapGeneration;
using PlayerRoles;
using PluginAPI.Core;
using PluginAPI.Core.Attributes;
using PluginAPI.Core.Doors;
using PluginAPI.Core.Zones;
using PluginAPI.Core.Zones.Entrance;
using PluginAPI.Enums;
using PluginAPI.Events;
using PluginAPI.Roles;
using Scp914;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.ProBuilder.Shapes;
using Utils;

namespace PracticePlugins
{

    [CommandHandler(typeof(RemoteAdminCommandHandler))] //Make sure to register events (EventManager.RegisterEvents<GunGameEventCommand>(this);) and also add "Gungame" to an EventType enum in Plugin.cs
    public class GunGameEventCommand : ICommand, IUsageProvider
    {
        public string Command => "gungame";

        public string[] Aliases => null;

        public string Description => "Starts the GunGame event";

        public string[] Usage { get; } = { "FFA? (y/[n])", "Zone? (L/[H]/E/S)" };


        public static bool FFA = false;
        public static MapGeneration.FacilityZone zone = MapGeneration.FacilityZone.HeavyContainment;


        public static Dictionary<Player, plrInfo> AllPlayers = new Dictionary<Player, plrInfo>(); //plrInfo stores: (NTF = true | Chaos = false) and Score
        public class plrInfo
        {
            public bool IsNtf { get; set; }
            public byte Score { get; set; }
        }

        public static byte Tntf = 0; //Number of NTF
        public static byte Tchaos = 0; //Number of chaos
        //public List<string> SpawnLocationNames = new List<string>() { "HczCheckpointA", "HczCheckpointB", "Hcz079", "Hcz096", "Hcz106", "Hcz939", "HczServers", "HczArmory", "HczWarhead", "HczMicroHID", "Hcz049" }; //List of the names of all possible spawn points

        public List<string> BlacklistRoomNames = new List<string>() { "LczCheckpointA", "LczCheckpointB", "LczClassDSpawn", "HczCheckpointToEntranceZone", "HczCheckpointToEntranceZone", "HczWarhead", "Hcz049", "Hcz106" };
        public static List<RoomIdentifier> BlacklistRooms = new List<RoomIdentifier>();

        public static List<Vector3> Spawns = new List<Vector3>(); //List of all possible spawnpoints
        public static Vector3 NTFSpawn; //Current NTF spawn
        public static Vector3 ChaosSpawn; //Current chaos spawn

        public static byte credits = 0; //Tracks time to next spawnpoint rotation
        public List<ItemType> AllAmmo = new List<ItemType>() { ItemType.Ammo12gauge, ItemType.Ammo44cal, ItemType.Ammo556x45, ItemType.Ammo762x39, ItemType.Ammo9x19 }; //All ammo types for easy adding
        public static List<ItemType> Weapons = new List<ItemType>(); //List of all weapons
      //public static List<ItemType> EasyWeapons = new List<ItemType>() { ItemType.GunLogicer, ItemType.Jailbird, ItemType.ParticleDisruptor };
      //public static List<ItemType> NormalWeapons = new List<ItemType>() { ItemType.GunRevolver, ItemType.GunCrossvec, ItemType.GunE11SR, ItemType.GunShotgun };
      //public static List<ItemType> HardWeapons = new List<ItemType>() { ItemType.GunAK, ItemType.GunCom45, ItemType.GunFSP9 };
      //public static List<ItemType> VeryHardWeapons = new List<ItemType>() { ItemType.GunCOM15, ItemType.GunCOM18, ItemType.MicroHID };

        public static List<ItemType> EasyWeapons = new List<ItemType>() { ItemType.GunLogicer, ItemType.ParticleDisruptor, ItemType.GunCrossvec, ItemType.GunE11SR, ItemType.GunShotgun };
        public static List<ItemType> NormalWeapons = new List<ItemType>() { ItemType.GunRevolver, ItemType.Jailbird, ItemType.GunAK, ItemType.GunCom45, ItemType.GunFSP9 };
        public static List<ItemType> HardWeapons = new List<ItemType>() { ItemType.GunCOM15, ItemType.GunCOM18, ItemType.MicroHID };


        public static RoleTypeId[,] Roles = new RoleTypeId[,] { { RoleTypeId.NtfCaptain, RoleTypeId.ChaosRepressor }, { RoleTypeId.NtfSergeant, RoleTypeId.ChaosMarauder }, { RoleTypeId.NtfPrivate, RoleTypeId.ChaosConscript }, { RoleTypeId.Scientist, RoleTypeId.ClassD } }; //Levels

        //public static List<ItemType> nonShuffledWeapons = new List<ItemType>() { ItemType.GunLogicer, /*ItemType.Jailbird, ItemType.ParticleDisruptor,*/ ItemType.GunRevolver, ItemType.GunCrossvec, ItemType.GunE11SR, ItemType.GunShotgun, ItemType.GunAK, ItemType.GunCom45, ItemType.GunFSP9, ItemType.GunCOM15, ItemType.GunCOM18, ItemType.MicroHID }; //List of non shuffled weapons

        public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            //   if (!Extensions.CanRun(sender, PlayerPermissions.PlayersManagement, arguments, Usage, out response))
            //       return false;
  
            try
            {
                if (arguments.Count > 0)
                    if (arguments.ElementAt(0).ToUpper().Equals("Y"))
                        FFA = true;
                    else
                        FFA = false;

                if (arguments.Count > 1)
                    switch (arguments.ElementAt(1).ToUpper())
                    {
                        case "L":
                            zone = MapGeneration.FacilityZone.LightContainment; break;
                        case "E":
                            zone = MapGeneration.FacilityZone.Entrance; break;
                        case "H":
                            zone = MapGeneration.FacilityZone.HeavyContainment; break;
                        case "S":
                            zone = MapGeneration.FacilityZone.Surface; break;
                    }

                Plugin.CurrentEvent = EventType.Gungame;
                Round.IsLocked = true;
 
                EasyWeapons.ShuffleList(); //Shuffles weapon levels
                NormalWeapons.ShuffleList();
                HardWeapons.ShuffleList();
                //VeryHardWeapons.ShuffleList();
                Weapons = EasyWeapons.Concat(NormalWeapons).Concat(HardWeapons)/*.Concat(VeryHardWeapons)*/.ToList(); //Combines weapon lists
                                                                                                                  //Weapons = nonShuffledWeapons;

                AllPlayers.Clear(); //Clears all values
                Spawns.Clear();
                Tntf = 0;
                Tchaos = 0;


                foreach (string room in BlacklistRoomNames) //Gets blacklisted room objects 
                    BlacklistRooms.AddRange(RoomIdentifier.AllRoomIdentifiers.Where(r => r.Name.ToString().Equals(room)));


                    //if (FFA)
                    foreach (var door in DoorVariant.AllDoors) //Adds every door in specified zone to spawns list
                    {
                        if (door.IsInZone(zone) && !(door is ElevatorDoor) && !door.Rooms.Any(x => BlacklistRooms.Any(y => y == x)))
                        {
                            Vector3 doorpos = door.gameObject.transform.position;
                            Spawns.Add(new Vector3(doorpos.x, doorpos.y+1, doorpos.z));
                            if (!door.RequiredPermissions.CheckPermissions(null, null)/* && !(door is PryableDoor)*//* && door is BreakableDoor /* && currRoom.Name != RoomName.Hcz106*/) //Opens locked doors
                                door.NetworkTargetState = true; 
                        }
                    }
                //else
                //    foreach (string room in SpawnLocationNames)
                //    { //Gets room objects 
                //        RoomIdentifier currRoom = RoomIdentifier.AllRoomIdentifiers.Where(r => r.Name.ToString().Equals(room)).First();
                //        //Spawns.Add(currRoom);
                //        tempSpawns.Add(new Vector3(currRoom.ApiRoom.Position.x, currRoom.ApiRoom.Position.y + 1, currRoom.ApiRoom.Position.z));

                //        foreach (var door in DoorVariant.DoorsByRoom[currRoom]) //opens keycard locked doors in spawn rooms, to not have people trapped
                //            if (!door.RequiredPermissions.CheckPermissions(null, null) && !(door is PryableDoor) && door is BreakableDoor /* && currRoom.Name != RoomName.Hcz106*/)
                //                door.NetworkTargetState = true;
                //    }


                RollSpawns(); //Shuffles spawns

                foreach (Player plr in Player.GetPlayers()) //Sets player teams
                {
                    if (plr.IsServer)
                        continue;
                    
                    AssignTeam(plr);

                    if (!SpawnPlayer(plr))
                        throw new Exception("Player " + plr.LogName + " could not be spawned");
                    plr.SendBroadcast("Welcome to GunGame! Race to the final weapon!", 10, shouldClearPrevious: true);
                }

                response = $"GunGame event has begun. FFA="+FFA+" | Zone: "+zone.ToString();
                return true;
            }
            catch (Exception e)
            {
                response = $"An error has occurred: " + e.Message;
                Round.IsLocked = false;
                return false;
            }
        }



        public void RollSpawns() //Changes spawn rooms
        {
            if (!FFA)
                Cassie.Message(".G2", false, false, false); //Plays glitch sound when rooms shuffle
            Spawns.ShuffleList();
            NTFSpawn = Spawns.ElementAt(0);
            ChaosSpawn = Spawns.ElementAt(1);
            credits = 0;
        }

        public void AssignTeam(Player plr) //Assigns player to team
        {
            if (plr.IsServer || plr.Role == PlayerRoles.RoleTypeId.Overwatch || AllPlayers.ContainsKey(plr))
            {
                //plr.ReceiveHint("You already have a score...", 1);
                return;
            }
            //plr.ReceiveHint("Assigning team...", 1);
            AllPlayers.Add(plr, new plrInfo { IsNtf = (Tntf < Tchaos)&&!FFA, Score = 0 }); //Adds player to list, uses bool operations to determine teams
            if ((Tntf < Tchaos) && !FFA)
                Tntf++;
            else
                Tchaos++;

            //plr.ReceiveHint("Assigned team", 2); //Message for testing purposes
            //return;
        }

        public void RemovePlayer(Player plr) //Removes player from list
        {
            if (AllPlayers.TryGetValue(plr, out var stats))
            {
                if (stats.IsNtf)
                    Tntf--;
                else
                    Tchaos--;
                AllPlayers.Remove(plr);
            }
        }

        public bool SpawnPlayer(Player plr) //Spawns player
        {
            if (plr.IsServer || plr.Role == PlayerRoles.RoleTypeId.Overwatch || plr.Role == PlayerRoles.RoleTypeId.Tutorial || !AllPlayers.TryGetValue(plr, out var plrStats))
            {
                plr.ReceiveHint("You are unable to spawn. Try rejoining", 10);
                return false;
            }
            int level = 3;
            if (!FFA)    
                level = Mathf.Clamp((int)Math.Round(((double)plrStats.Score / (Weapons.Count)) * 4), 0, 3); //Sets player's class to represent level
            if (plrStats.IsNtf) //Spawns either NTF or Chaos based on bool
            {
                //plr.SetRole(Roles[level, 0]); //Gives role items
                plr.ReferenceHub.roleManager.ServerSetRole(Roles[level, 0], RoleChangeReason.Respawn, RoleSpawnFlags.None);
                plr.Position = NTFSpawn;
            }
            else
            {
                //plr.SetRole(Roles[level, 1]);
                plr.ReferenceHub.roleManager.ServerSetRole(Roles[level, 1], RoleChangeReason.Respawn, RoleSpawnFlags.None);
                plr.Position = ChaosSpawn;
            }
            plr.ClearInventory();
            plr.ReferenceHub.playerEffectsController.EnableEffect<SpawnProtected>(2, false);
            plr.ReferenceHub.playerEffectsController.ChangeState<MovementBoost>(10, 99999, false); //Movement effects
            plr.ReferenceHub.playerEffectsController.ChangeState<Scp1853>(10, 99999, false);
            plr.AddItem(ItemType.ArmorCombat);
            plr.AddItem(ItemType.Painkillers);
            foreach (ItemType ammo in AllAmmo) //Gives max ammo of all types
                plr.AddAmmo(ammo, (ushort)plr.GetAmmoLimit(ammo));
            GiveGun(plr);
            return true;
        }

        public void GiveGun(Player plr) //Gives player their next gun and equips it, and removes old gun
        {
            if (plr.IsServer || plr.Role == PlayerRoles.RoleTypeId.Overwatch || plr.Role == PlayerRoles.RoleTypeId.Tutorial || !AllPlayers.TryGetValue(plr, out var plrStats))
                return;

            if (plrStats.Score > 0)
                foreach (ItemBase item in plr.Items) //Removes last gun
                {
                    if (item.ItemTypeId == Weapons.ElementAt(plrStats.Score - 1))
                    {
                        plr.RemoveItem(item);
                        break;
                    }
                }

            ItemType currGun = Weapons.ElementAt(plrStats.Score);
            ItemBase weapon = plr.AddItem(currGun);
            if (IsGun(currGun))
            {
                Firearm firearm = weapon as Firearm;
                uint attachment_code = AttachmentsServerHandler.PlayerPreferences[plr.ReferenceHub][currGun];
                AttachmentsUtils.ApplyAttachmentsCode(firearm, attachment_code, true);
                firearm.Status = new FirearmStatus(firearm.AmmoManagerModule.MaxAmmo, FirearmStatusFlags.MagazineInserted, attachment_code);
            }
            MEC.Timing.CallDelayed((float)0.1/*MEC.Timing.WaitForOneFrame*/, () =>
            {
                plr.CurrentItem = weapon;
            });
        }


        public static bool IsGun(ItemType type)
        {
            bool result = false;
            switch (type)
            {
                case ItemType.GunCOM15:
                    result = true;
                    break;
                case ItemType.GunCOM18:
                    result = true;
                    break;
                case ItemType.GunCom45:
                    result = true;
                    break;
                case ItemType.GunFSP9:
                    result = true;
                    break;
                case ItemType.GunCrossvec:
                    result = true;
                    break;
                case ItemType.GunE11SR:
                    result = true;
                    break;
                case ItemType.GunAK:
                    result = true;
                    break;
                case ItemType.GunRevolver:
                    result = true;
                    break;
                case ItemType.GunShotgun:
                    result = true;
                    break;
                case ItemType.GunLogicer:
                    result = true;
                    break;
                case ItemType.ParticleDisruptor:
                    result = true;
                    break;
            }
            return result;
        }


        public void AddScore(Player plr) //Increases player's score
        {
            if (plr.IsServer || plr.Role == PlayerRoles.RoleTypeId.Overwatch || plr.Role == PlayerRoles.RoleTypeId.Tutorial || !AllPlayers.TryGetValue(plr, out var plrStats))
            {
                plr.ReceiveHint("You aren't registered as a player. Try rejoining", 5);
                return;
            }

            if (plrStats.Score >= Weapons.Count - 1) //Gun just before zombie
            {
                plr.ClearInventory();
                plr.ReferenceHub.roleManager.ServerSetRole(RoleTypeId.Scp0492, RoleChangeReason.RemoteAdmin, RoleSpawnFlags.AssignInventory); //Spawns zombie without increasing score
                return;
            }
            plrStats.Score++; //Adds 1 to score
            GiveGun(plr);
        }

        public void RemoveScore(Player plr)
        {
            if (plr.IsServer || plr.Role == PlayerRoles.RoleTypeId.Overwatch || plr.Role == PlayerRoles.RoleTypeId.Tutorial || !AllPlayers.TryGetValue(plr, out var plrStats))
            {
                plr.ReceiveHint("You aren't registered as a player. Try rejoining", 5);
                return;
            }

            if (plrStats.Score > 0)
                plrStats.Score--;
        }

        public void TriggerWin(Player plr) //Win event
        {
            Round.IsLocked = false;
            plr.Health = 42069;
            foreach (Player loser in Player.GetPlayers())
            {
                if (loser.IsServer)
                    continue;
                loser.SendBroadcast(plr.Nickname + " wins!", 10, shouldClearPrevious: true);
                if (loser != plr)
                    loser.ReferenceHub.playerEffectsController.EnableEffect<SeveredHands>();
            }
        }

        [PluginEvent(ServerEventType.PlayerDying), PluginPriority(LoadPriority.Highest)]
        public void PlayerDeath(PlayerDyingEvent args)
        {
            var plr = args.Player;
            if (Plugin.CurrentEvent == EventType.Gungame && AllPlayers.TryGetValue(plr, out var plrStats))
            {
                plr.ClearInventory();
                var atckr = args.Attacker;
                if (atckr != null && atckr != plr)
                {
                    if (atckr.Role == RoleTypeId.Scp0492) //Triggers win if player is zombie
                    {
                        TriggerWin(atckr);
                        return;
                    }
                    if (AllPlayers.TryGetValue(atckr, out var atckrStats))
                    {
                        plr.ReceiveHint(atckr.Nickname + " killed you", 2);
                        atckr.ReceiveHint("You killed " + plr.Nickname, 2);
                        if (atckrStats.IsNtf != plrStats.IsNtf)
                        {
                            AddScore(atckr);
                            atckr.AddItem(ItemType.Medkit);
                        }
                        else
                            if (!FFA)
                            RemoveScore(atckr);
                    }
                }
                else
                {
                    plr.ReceiveHint("Shrimply a krill issue", 3);
                    RemoveScore(plr);
                }
                if (!FFA)
                {
                    System.Random rnd = new System.Random();
                    credits += (byte)rnd.Next(1, 25); //Adds random amount of credits
                    if (credits >= Mathf.Clamp(Player.Count * 10, 30, 100)) //Rolls next spawns if credits high enough, based on player count
                        RollSpawns();
                }
                else RollSpawns();
                MEC.Timing.CallDelayed(3, () =>
                {
                    SpawnPlayer(plr);
                });
            }
        }


        [PluginEvent(ServerEventType.PlayerDropItem)]
        public bool DropItem(PlayerDropItemEvent args) //Stops items from being dropped
        {
            if (Plugin.EventInProgress)
            {
                if (Plugin.CurrentEvent == EventType.Gungame)
                    return false;
                else return true;
            }
            return true;
        }

        [PluginEvent(ServerEventType.PlayerThrowItem)]
        public bool ThrowItem(PlayerThrowItemEvent args) //Stops items from being throwed
        {
            if (Plugin.EventInProgress && !args.Player.IsTutorial)
            {
                if (Plugin.CurrentEvent == EventType.Gungame)
                    return false;
                else return true;
            }
            return true;
        }

        [PluginEvent(ServerEventType.PlayerDropAmmo)]
        public bool DropAmmo(PlayerDropAmmoEvent args) //Stops ammo from being dropped 
        {
            if (Plugin.EventInProgress && !args.Player.IsTutorial)
            {
                if (Plugin.CurrentEvent == EventType.Gungame)
                    return false;
                else return true;
            }
            return true;
        }

        [PluginEvent(ServerEventType.PlayerSearchPickup)]
        public bool PlayerPickup(PlayerSearchPickupEvent args)
        {
            var itemID = args.Item.Info.ItemId;
            if (Plugin.CurrentEvent == EventType.Gungame && !args.Player.IsTutorial)
                if (itemID == ItemType.Painkillers || itemID == ItemType.Medkit || itemID == ItemType.Adrenaline) //Allows only medical pickups
                    return true;
                else return false;

            return true;
        }

        [PluginEvent(ServerEventType.PlayerJoined)]
        public void PlayerJoined(PlayerJoinedEvent args) //Adding new player to the game 
        {
            if (Plugin.CurrentEvent == EventType.Gungame)
            {
                AssignTeam(args.Player);
                args.Player.SendBroadcast("Welcome to GunGame! Race to the final weapon!", 10, shouldClearPrevious: true);
                MEC.Timing.CallDelayed(3, () =>
                {
                    SpawnPlayer(args.Player);
                });
            }
        }

        [PluginEvent(ServerEventType.PlayerLeft)]
        public void PlayerLeft(PlayerLeftEvent args) //Removing player that left from list
        {
            if (Plugin.CurrentEvent == EventType.Gungame)
                RemovePlayer(args.Player);
        }

        [PluginEvent(ServerEventType.PlayerChangeRole)]
        public void ChangeRole(PlayerChangeRoleEvent args) //Failsafes for admin shenanegens 
        {
            if (Plugin.CurrentEvent == EventType.Gungame && args.ChangeReason.Equals(RoleChangeReason.RemoteAdmin))
            {
                var newR = args.NewRole;
                MEC.Timing.CallDelayed(3, () =>
                {
                    /*       if (newR == RoleTypeId.Overwatch || newR == RoleTypeId.Tutorial || newR == RoleTypeId.Filmmaker)
                       {
                           RemovePlayer(args.Player);
                           return;
                       }*/
                    if (newR == RoleTypeId.Spectator)
                    {
                        AssignTeam(args.Player);
                        SpawnPlayer(args.Player);
                    }
                });
            }
        }

        [PluginEvent(ServerEventType.PlayerInteractElevator)]
        public bool PlayerInteractElevator(PlayerInteractElevatorEvent args)
        {
            if (Plugin.CurrentEvent == EventType.Gungame && !args.Player.IsTutorial)
                return false;
            return true;
        }

        [PluginEvent(ServerEventType.PlayerHandcuff)]
        public void PlayerHandcuff(PlayerHandcuffEvent args)
        {
            if (Plugin.CurrentEvent == EventType.Gungame)
                //ExplosionUtils.ServerExplode(args.Target.ReferenceHub);
                args.Target.Damage(420, args.Player);
        }

        [PluginEvent(ServerEventType.TeamRespawn)]
        public bool RespawnCancel(TeamRespawnEvent args)
        {
            if (Plugin.EventInProgress)
                return false;
            return true;
        }

        [PluginEvent(ServerEventType.Scp914UpgradeInventory)]
        public bool InventoryUpgrade(Scp914UpgradeInventoryEvent args)
        {
            if (Plugin.CurrentEvent == EventType.Gungame && !args.Player.IsTutorial)
                if (Weapons.Contains(args.Item.ItemTypeId))
                    return false;
            return true;
        }

        [PluginEvent(ServerEventType.Scp914ProcessPlayer)]
        public void PlayerUpgrade(Scp914ProcessPlayerEvent args)
        {
            Player plr = args.Player;
            if (Plugin.CurrentEvent == EventType.Gungame && !plr.IsTutorial) {
                switch (args.KnobSetting)
                {
                    case Scp914KnobSetting.Rough:
                        MEC.Timing.CallDelayed((float)0.1, () =>
                        { ExplosionUtils.ServerExplode(plr.ReferenceHub); }); break;
                    case Scp914KnobSetting.Coarse:
                        plr.EffectsManager.DisableAllEffects(); break;
                    case Scp914KnobSetting.OneToOne:
                        if (FFA)
                        {
                            plr.ReferenceHub.roleManager.ServerSetRole(RoleTypeId.Scientist, RoleChangeReason.RemoteAdmin, RoleSpawnFlags.None);
                            plr.ReferenceHub.playerEffectsController.ChangeState<MovementBoost>(10, 99999, false);
                            plr.ReferenceHub.playerEffectsController.ChangeState<Scp1853>(10, 99999, false);
                        }
                        break;
                    case Scp914KnobSetting.VeryFine:
                        plr.EffectsManager.EnableEffect<Scp207>(9999); break;
                } 
            }

        }

        /*
           [PluginEvent(ServerEventType.PlayerCoinFlip)] // For testing purposes when I don't have test subjects to experiment on
           public void CoinFlip(PlayerCoinFlipEvent args)
           {
               var plr = args.Player;
               //plr.ReceiveHint("Cheater.", 1);
               AddScore(plr);
               //if (AllPlayers.TryGetValue(plr, out var test))
               //    plr.ReceiveHint("You are in the dictionary, your score is " + test.Score, 5);
               //else
               //{
               //    plr.ReceiveHint("You are not in the dictionary. Attempting to add you now...", 1);
               //    AllPlayers.Add(plr, new plrInfo { IsNtf = (Tntf < Tchaos), Score = 0 });
               //}
           }

           [PluginEvent(ServerEventType.PlayerUnloadWeapon)]
           public void GunUnload(PlayerUnloadWeaponEvent args)
           {
               AddScore(args.Player);
           }
        */

    }


}

