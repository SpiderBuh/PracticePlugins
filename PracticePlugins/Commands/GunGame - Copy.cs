/*using CommandSystem;
using PluginAPI.Core.Attributes;
using PluginAPI.Enums;
using PluginAPI.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using MapGeneration;
using PlayerRoles;
using PluginAPI.Core;
using UnityEngine;
using CustomPlayerEffects;

namespace PracticePlugins
{
        [CommandHandler(typeof(RemoteAdminCommandHandler))]
        public class GunGameEventCommand : ICommand, IUsageProvider
        {
            public string Command => "gungametest";

            public string[] Aliases => null;

            public string Description => "Starts the GunGame event";

            public string[] Usage { get; } = { };


            Dictionary<Player, plrInfo> AllPlayers = new Dictionary<Player, plrInfo>(); //plrInfo stores: (NTF = true | Chaos = false) and Score
            byte Tntf = 0; //Number of NTF
            byte Tchaos = 0; //Number of chaos
            string[] SpawnLocationNames = { "HczCheckpointToEntranceZone", "HczCheckpointA", "HczCheckpointB", "Hcz079", "Hcz096", "Hcz106", "Hcz939", "HczServers", "HczTestroom", "HczArmory" }; //List of the names of all possible spawn points
            List<RoomIdentifier> Spawns = new List<RoomIdentifier>(); //List of all possible spawnpoints
            RoomIdentifier NTFSpawn = null; //Current NTF spawn
            RoomIdentifier ChaosSpawn = null; //Current chaos spawn
            byte credits = 0; //Tracks time to next spawnpoint rotation
            ItemType[] AllAmmo = { ItemType.Ammo12gauge, ItemType.Ammo44cal, ItemType.Ammo556x45, ItemType.Ammo762x39, ItemType.Ammo9x19 }; //All ammo types for easy adding
            List<ItemType> Weapons = new List<ItemType>(); //List of all weapons
            List<ItemType> EasyWeapons = new List<ItemType>() { ItemType.GunLogicer,  ItemType.Jailbird, ItemType.ParticleDisruptor };
            List<ItemType> NormalWeapons = new List<ItemType>() { ItemType.GunRevolver, ItemType.GunCrossvec, ItemType.GunE11SR, ItemType.GunShotgun};
            List<ItemType> HardWeapons = new List<ItemType>() { ItemType.GunAK, ItemType.GunCom45, ItemType.GunFSP9};
            List<ItemType> VeryHardWeapons = new List<ItemType>() {  ItemType.GunCOM15, ItemType.GunCOM18, ItemType.MicroHID};
            RoleTypeId[,] Roles = new RoleTypeId[,] { { RoleTypeId.NtfCaptain, RoleTypeId.ChaosRepressor }, { RoleTypeId.NtfSergeant, RoleTypeId.ChaosMarauder }, { RoleTypeId.NtfPrivate, RoleTypeId.ChaosConscript }, { RoleTypeId.Scientist, RoleTypeId.ClassD } }; //Levels

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
                    Weapons = (List<ItemType>)EasyWeapons.Concat(NormalWeapons).Concat(HardWeapons).Concat(VeryHardWeapons); //Combines weapon lists

                    foreach (string room in SpawnLocationNames) //Gets room objects 
                        Spawns.Add(RoomIdentifier.AllRoomIdentifiers.Where(r => r.Name.ToString().Equals(room)).First());

                    RollSpawns(); //Shuffles spawns

                    foreach (Player plr in Player.GetPlayers()) //Sets player teams
                    {
                        if (plr.IsServer)
                            continue;
                        AssignTeam(plr);
                        SpawnPlayer(plr);
                        plr.SendBroadcast("Welcome to GunGame! Race to the final weapon!", 10, shouldClearPrevious: true);
                    }

                    response = $"GunGame event has begun";
                    return true;
                } catch (Exception e) {
                    response = $"An error has occurred: " + e.Message;
                    return false; 
                }
            }

            public class plrInfo
            {
                public bool IsNtf { get; set; }
                public byte Score { get; set; }
            }

            public void RollSpawns() //Changes spawn rooms
            {
                Spawns.ShuffleList();
                NTFSpawn = Spawns[0];
                ChaosSpawn = Spawns[1];
                credits = 0;
            }

            public void AssignTeam(Player plr) //Assigns player to team
            {
                if (plr.IsServer || plr.Role == PlayerRoles.RoleTypeId.Overwatch || AllPlayers.ContainsKey(plr))
                    return;

                AllPlayers.Add(plr, new plrInfo { IsNtf = (Tntf < Tchaos), Score = 0 } );
                if (Tntf < Tchaos)
                    Tntf++;
                else
                    Tchaos++;

                plr.ReceiveHint("Assigned team"); //Message for testing purposes
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

            public void SpawnPlayer(Player plr) //Spawns player
            {
                if (plr.IsServer || plr.Role == PlayerRoles.RoleTypeId.Overwatch || plr.Role == PlayerRoles.RoleTypeId.Tutorial || !AllPlayers.ContainsKey(plr))
                    return;
                plr.ReceiveHint("Attempting spawn..."); //Message for testing purposes
                int level = (int)Math.Round((double)AllPlayers[plr].Score / Weapons.Count); //Sets player's class to represent level
                if (AllPlayers[plr].IsNtf) //Spawns either NTF or Chaos based on bool
                {
                    plr.ReferenceHub.roleManager.ServerSetRole(Roles[level,0], RoleChangeReason.RemoteAdmin, RoleSpawnFlags.None);
                    plr.Position = new Vector3(NTFSpawn.ApiRoom.Position.x, NTFSpawn.ApiRoom.Position.y + 1, NTFSpawn.ApiRoom.Position.z);
                } else
                {
                    plr.ReferenceHub.roleManager.ServerSetRole(Roles[level,1], RoleChangeReason.RemoteAdmin, RoleSpawnFlags.None);
                    plr.Position = new Vector3(ChaosSpawn.ApiRoom.Position.x, ChaosSpawn.ApiRoom.Position.y + 1, ChaosSpawn.ApiRoom.Position.z);
                }
                plr.AddItem(ItemType.ArmorCombat);
                plr.AddItem(ItemType.Painkillers);
                foreach (ItemType ammo in AllAmmo) //Gives max ammo of all types (in theory)
                    plr.AddAmmo(ammo, (ushort)plr.GetAmmoLimit(ammo));
                GiveGun(plr);
            }

            public void GiveGun(Player plr) //Gives player their next gun and equips it, and removes old gun
            {
                if (plr.IsServer || plr.Role == PlayerRoles.RoleTypeId.Overwatch || plr.Role == PlayerRoles.RoleTypeId.Tutorial || !AllPlayers.ContainsKey(plr))
                    return;
                plr.ReceiveHint("Giving weapon..."); //Message for testing purposes
                plr.RemoveItems(Weapons[AllPlayers[plr].Score]-1); //Removes last gun (def doesnt work)
                plr.AddItem(Weapons[(int)AllPlayers[plr].Score]); //Gives next gun
                foreach (var item in plr.Items) //Finds and equips next weapon
                {
                    if (item.ItemTypeId == Weapons[(int)AllPlayers[plr].Score])
                    {
                        plr.CurrentItem = item;
                        break;
                    }
                }
            }

            [PluginEvent(ServerEventType.PlayerDeath)]
            public void PlayerDeath(PlayerDeathEvent args)
            {
                var plr = args.Player;
                var atckr = args.Attacker;
                plr.ReceiveHint(atckr.LogName + " killed you");
                atckr.ReceiveHint("You killed " + plr.LogName);
                if (atckr.Role == RoleTypeId.Scp0492) //Triggers win if player is zombie
                {
                    TriggerWin(atckr);
                    return;
                }
                AddScore(atckr);
                SpawnPlayer(plr);
                System.Random rnd = new System.Random();
                credits += (byte)rnd.Next(1, 25); //Adds random amount of credits
                if (credits >= 100) //Rolls next spawns if credits high enough
                {
                    credits -= 100;
                    RollSpawns();
                }
            }

            [PluginEvent (ServerEventType.PlayerDropItem)]
            public bool DropItem(PlayerDropItemEvent args) //Stops items from being dropped on death (def doesnt work)
            {
                return false;
            }

            public void AddScore(Player plr) //Increases player's score
            {
                if (plr.IsServer || plr.Role == PlayerRoles.RoleTypeId.Overwatch || plr.Role == PlayerRoles.RoleTypeId.Tutorial || !AllPlayers.ContainsKey(plr))
                    return;

                if (AllPlayers[plr].Score >= Weapons.Count) //Gun just before zombie
                {
                    plr.ReferenceHub.roleManager.ServerSetRole(RoleTypeId.Scp0492, RoleChangeReason.RemoteAdmin, RoleSpawnFlags.AssignInventory); //Spawns zombie without increasing score
                    return;
                }
                AllPlayers[plr].Score++; //Adds 1 to score
                GiveGun(plr);
            }

            public void TriggerWin(Player plr) //Win event
            {
                Round.IsLocked = true;
                foreach (Player loser in Player.GetPlayers())
                {
                    if (loser.IsServer)
                        continue;
                    loser.SendBroadcast(plr.LogName+" wins!", 10, shouldClearPrevious: true);
                    if (loser != plr)
                        loser.ReferenceHub.playerEffectsController.EnableEffect<SeveredHands>();
                }
            }

            [PluginEvent(ServerEventType.PlayerJoined)]
            public void PlayerJoined(PlayerJoinedEvent args) //Adding new player to the game 
            {
                AssignTeam(args.Player);
                SpawnPlayer(args.Player);
                args.Player.SendBroadcast("Welcome to GunGame! Race to the final weapon!", 10, shouldClearPrevious: true);
            }

            [PluginEvent(ServerEventType.PlayerLeft)]
            public void PlayerLeft(PlayerLeftEvent args) //Removing player that left from list
            {
                RemovePlayer(args.Player);
            }

            [PluginEvent(ServerEventType.PlayerChangeRole)]
            public void ChangeRole(PlayerChangeRoleEvent args) //Failsafes for admin shenanegens 
            {
                var newR = args.NewRole;
                if (newR == RoleTypeId.Overwatch || newR == RoleTypeId.Tutorial || newR == RoleTypeId.Filmmaker)
                {
                    RemovePlayer(args.Player);
                    return;
                }
                if (newR == RoleTypeId.Spectator && args.ChangeReason.Equals(RoleChangeReason.RemoteAdmin))
                {
                    AssignTeam(args.Player);
                    SpawnPlayer(args.Player);
                }
            }
        }
    }

*/