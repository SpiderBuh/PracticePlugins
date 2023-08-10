using CommandSystem;
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
using PluginAPI.Core.Zones;
using PluginAPI.Enums;
using PluginAPI.Events;
using PluginAPI.Roles;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.ProBuilder.Shapes;
using Utils;
using static PracticePlugins.miscExtras;

namespace PracticePlugins
{
    public class GunGame
    {

        [CommandHandler(typeof(RemoteAdminCommandHandler))]
        public class GunGameEventCommand : ICommand, IUsageProvider
        {
            public string Command => "gungame";

            public string[] Aliases => null;

            public string Description => "Starts the GunGame event";

            public string[] Usage { get; } = { };


            public static Dictionary<Player, plrInfo> AllPlayers = new Dictionary<Player, plrInfo>(); //plrInfo stores: (NTF = true | Chaos = false) and Score
            public class plrInfo
            {
                public bool IsNtf { get; set; }
                public byte Score { get; set; }
            }

            public static byte Tntf = 0; //Number of NTF
            public static byte Tchaos = 0; //Number of chaos
            public static List<string> SpawnLocationNames = new List<string>() { "HczCheckpointA", "HczCheckpointB", "Hcz079", "Hcz096", "Hcz106", "Hcz939", "HczServers", "HczArmory", "HczWarhead", "HczMicroHID", "Hcz049" }; //List of the names of all possible spawn points
            public static List<RoomIdentifier> Spawns = new List<RoomIdentifier>(); //List of all possible spawnpoints
            public static RoomIdentifier NTFSpawn = null; //Current NTF spawn
            public static RoomIdentifier ChaosSpawn = null; //Current chaos spawn
            public static byte credits = 0; //Tracks time to next spawnpoint rotation
            public static List<ItemType> AllAmmo = new List<ItemType>() { ItemType.Ammo12gauge, ItemType.Ammo44cal, ItemType.Ammo556x45, ItemType.Ammo762x39, ItemType.Ammo9x19 }; //All ammo types for easy adding
            public static List<ItemType> Weapons = new List<ItemType>(); //List of all weapons
            public static List<ItemType> EasyWeapons = new List<ItemType>() { ItemType.GunLogicer, ItemType.Jailbird, ItemType.ParticleDisruptor };
            public static List<ItemType> NormalWeapons = new List<ItemType>() { ItemType.GunRevolver, ItemType.GunCrossvec, ItemType.GunE11SR, ItemType.GunShotgun };
            public static List<ItemType> HardWeapons = new List<ItemType>() { ItemType.GunAK, ItemType.GunCom45, ItemType.GunFSP9 };
            public static List<ItemType> VeryHardWeapons = new List<ItemType>() { ItemType.GunCOM15, ItemType.GunCOM18, ItemType.MicroHID };
            public static RoleTypeId[,] Roles = new RoleTypeId[,] { { RoleTypeId.NtfCaptain, RoleTypeId.ChaosRepressor }, { RoleTypeId.NtfSergeant, RoleTypeId.ChaosMarauder }, { RoleTypeId.NtfPrivate, RoleTypeId.ChaosConscript }, { RoleTypeId.Scientist, RoleTypeId.ClassD } }; //Levels

            //public static List<ItemType> nonShuffledWeapons = new List<ItemType>() { ItemType.GunLogicer, /*ItemType.Jailbird, ItemType.ParticleDisruptor,*/ ItemType.GunRevolver, ItemType.GunCrossvec, ItemType.GunE11SR, ItemType.GunShotgun, ItemType.GunAK, ItemType.GunCom45, ItemType.GunFSP9, ItemType.GunCOM15, ItemType.GunCOM18, ItemType.MicroHID }; //List of non shuffled weapons

            public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
            {
                //   if (!Extensions.CanRun(sender, PlayerPermissions.PlayersManagement, arguments, Usage, out response))
                //       return false;

                try
                {

                    Plugin.CurrentEvent = EventType.Gungame;
                    Round.IsLocked = true;

                    EasyWeapons.ShuffleList(); //Shuffles weapon levels
                    NormalWeapons.ShuffleList();
                    HardWeapons.ShuffleList();
                    VeryHardWeapons.ShuffleList();
                    Weapons = EasyWeapons.Concat(NormalWeapons).Concat(HardWeapons).Concat(VeryHardWeapons).ToList(); //Combines weapon lists
                    //Weapons = nonShuffledWeapons;

                    AllPlayers.Clear(); //Clears all values
                    Tntf = 0;
                    Tchaos = 0;

                    foreach (string room in SpawnLocationNames)
                    { //Gets room objects 
                        Spawns.Add(RoomIdentifier.AllRoomIdentifiers.Where(r => r.Name.ToString().Equals(room)).First());
                        RoomIdentifier foundRoom = Spawns.Last();
                        foreach (var door in DoorVariant.DoorsByRoom[foundRoom]) //opens keycard locked doors in spawn rooms, to not have people trapped
                            if (!door.RequiredPermissions.CheckPermissions(null, null) && !(door is PryableDoor pryable) && foundRoom.Name != RoomName.Hcz106)
                                door.NetworkTargetState = true;
                    }


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

                    response = $"GunGame event has begun";
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
                //Cassie.Message("new rooms", false, false, true); //announces when rooms are being rolled for testing
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
                AllPlayers.Add(plr, new plrInfo { IsNtf = (Tntf < Tchaos), Score = 0 });
                if (Tntf < Tchaos)
                    Tntf++;
                else
                    Tchaos++;

                //plr.ReceiveHint("Assigned team", 2); //Message for testing purposes
                return;
            }

            public void RemovePlayer(Player plr) //Removes player from team
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
                //  plr.ReceiveHint("Checking if you can spawn...", 1); //Message for testing purposes
                if (plr.IsServer || plr.Role == PlayerRoles.RoleTypeId.Overwatch || plr.Role == PlayerRoles.RoleTypeId.Tutorial || !AllPlayers.TryGetValue(plr, out var plrStats))
                {
                    plr.ReceiveHint("You are unable to spawn. Try rejoining", 10);
                    return false;
                }
                //plr.ReceiveHint("Attempting spawn...", 2); //Message for testing purposes
                int level = Mathf.Clamp((int)Math.Round(((double)plrStats.Score / (Weapons.Count)) * 4), 0, 3); //Sets player's class to represent level
                if (plrStats.IsNtf) //Spawns either NTF or Chaos based on bool
                {
                    //plr.SetRole(Roles[level, 0]); //Gives role items
                    plr.ReferenceHub.roleManager.ServerSetRole(Roles[level, 0], RoleChangeReason.Respawn, RoleSpawnFlags.None);
                    plr.Position = new Vector3(NTFSpawn.ApiRoom.Position.x, NTFSpawn.ApiRoom.Position.y + 1, NTFSpawn.ApiRoom.Position.z);
                }
                else
                {
                    //plr.SetRole(Roles[level, 1]);
                    plr.ReferenceHub.roleManager.ServerSetRole(Roles[level, 1], RoleChangeReason.Respawn, RoleSpawnFlags.None);
                    plr.Position = new Vector3(ChaosSpawn.ApiRoom.Position.x, ChaosSpawn.ApiRoom.Position.y + 1, ChaosSpawn.ApiRoom.Position.z);
                }
                plr.ClearInventory();
                plr.AddItem(ItemType.ArmorCombat);
                plr.AddItem(ItemType.Painkillers);
                foreach (ItemType ammo in AllAmmo) //Gives max ammo of all types
                    plr.AddAmmo(ammo, (ushort)plr.GetAmmoLimit(ammo));
                GiveGun(plr);
                return true;
            }

            public void GiveGun(Player plr) //Gives player their next gun and equips it, and removes old gun
            {
                //plr.ReceiveHint("Checking if you can get a gun...", 1); //Message for testing purposes
                if (plr.IsServer || plr.Role == PlayerRoles.RoleTypeId.Overwatch || plr.Role == PlayerRoles.RoleTypeId.Tutorial || !AllPlayers.TryGetValue(plr, out var plrStats))
                    return;
                //plr.ReceiveHint("Giving weapon...", 2); //Message for testing purposes
                if (plrStats.Score > 0)
                    foreach (ItemBase item in plr.Items) //Removes last gun
                    {
                        if (item.ItemTypeId == Weapons.ElementAt(plrStats.Score - 1))
                        {
                            //plr.ReferenceHub.inventory.ServerRemoveItem(item.ItemSerial, null);
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
                //plr.ReceiveHint("Your new score is: " + plrStats.Score, 5);
                GiveGun(plr);
            }

            public void TriggerWin(Player plr) //Win event
            {
                Round.IsLocked = false;
                plr.Health = 69420;
                foreach (Player loser in Player.GetPlayers())
                {
                    if (loser.IsServer)
                        continue;
                    loser.SendBroadcast(plr.Nickname + " wins!", 10, shouldClearPrevious: true);
                    if (loser != plr)
                        loser.ReferenceHub.playerEffectsController.EnableEffect<SeveredHands>();
                }
            }

            //[PluginEvent(ServerEventType.PlayerDeath)]
            [PluginEvent(ServerEventType.PlayerDying), PluginPriority(LoadPriority.Highest)]
            public void PlayerDeath(PlayerDyingEvent args)
            {

                if (Plugin.CurrentEvent == EventType.Gungame)
                {
                    var plr = args.Player;
                    plr.ClearInventory();
                    //plr.ReceiveHint("You died", 1);
                    var atckr = args.Attacker;
                    if (atckr != null && atckr != plr)
                    {
                        if (atckr.Role == RoleTypeId.Scp0492) //Triggers win if player is zombie
                        {
                            TriggerWin(atckr);
                            return;
                        }
                        if (AllPlayers.ContainsKey(atckr))
                        {
                            AddScore(atckr);
                            plr.ReceiveHint(atckr.Nickname + " killed you", 3);
                            atckr.ReceiveHint("You killed " + plr.Nickname, 3);

                        }
                    }
                    else
                        plr.ReceiveHint("Shrimply a krill issue", 3);
                    System.Random rnd = new System.Random();
                    credits += (byte)rnd.Next(1, 25); //Adds random amount of credits
                    if (credits >= Mathf.Clamp(Player.Count * 10, 40, 100)) //Rolls next spawns if credits high enough, based on player count
                        RollSpawns();
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
            public bool ThrowItem(PlayerThrowItemEvent args)
            {
                if (Plugin.EventInProgress)
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
                if (Plugin.EventInProgress)
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
                if (Plugin.EventInProgress)
                {
                    if (Plugin.CurrentEvent == EventType.Gungame)
                        if (itemID == ItemType.Painkillers || itemID == ItemType.Medkit || itemID == ItemType.Adrenaline)
                            return true;
                        else return false;
                }
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
                {
                    RemovePlayer(args.Player);
                }
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
                if (Plugin.CurrentEvent == EventType.Gungame)
                    return false;
                return true;
            }

            [PluginEvent(ServerEventType.PlayerHandcuff)]
            public void PlayerHandcuff(PlayerHandcuffEvent args)
            {
                if (Plugin.CurrentEvent == EventType.Gungame)
                    ExplosionUtils.ServerExplode(args.Target.ReferenceHub);
            }

            [PluginEvent(ServerEventType.TeamRespawn)]
            public bool RespawnCancel(TeamRespawnEvent args)
            {
                if (Plugin.EventInProgress)
                    return false;
                return true;
            }

            /*   [PluginEvent(ServerEventType.PlayerCoinFlip)] // For testing purposes when I don't have test subjects to experiment on
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
               }*/


        }


    }
}
