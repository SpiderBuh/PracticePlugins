using AdminToys;
using CommandSystem;
using CommandSystem.Commands.RemoteAdmin.Decontamination;
using CustomPlayerEffects;
using Interactables.Interobjects;
using Interactables.Interobjects.DoorUtils;
using InventorySystem;
using InventorySystem.Items;
using InventorySystem.Items.Firearms;
using InventorySystem.Items.Firearms.Attachments;
using InventorySystem.Items.Jailbird;
using InventorySystem.Items.MicroHID;
using InventorySystem.Items.Pickups;
using InventorySystem.Items.Usables.Scp244;
using LightContainmentZoneDecontamination;
using MapGeneration;
using Mirror;
using PlayerRoles;
using PlayerRoles.PlayableScps.Subroutines;
using PlayerStatsSystem;
using PluginAPI.Core;
using PluginAPI.Core.Attributes;
using PluginAPI.Core.Doors;
using PluginAPI.Core.Items;
using PluginAPI.Enums;
using PluginAPI.Events;
using Scp914;
using Subtitles;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Security.Cryptography.Xml;
using UnityEngine;
using UnityEngine.ProBuilder.Shapes;
using UnityEngine.UIElements;
using Utils;

namespace PracticePlugins
{

    [CommandHandler(typeof(RemoteAdminCommandHandler))]
    public class GunGameEventCommand : ICommand, IUsageProvider
    {
        public string Command => "gungame";

        public string[] Aliases => null;

        public string Description => "Starts the GunGame event. (Args optional, [X]=default)";

        public string[] Usage { get; } = { "FFA? ([y]/n)", "Zone? ([L]/H/E/S)", "Kills to win? [27]"/*, "Friendly fire? ([y]/n)"*/ };


        public static bool FFA = true;
        public static FacilityZone zone = FacilityZone.LightContainment;


        public static Dictionary<Player, PlrInfo> AllPlayers = new Dictionary<Player, PlrInfo>(); //plrInfo stores: (NTF = true | Chaos = false) and Score
        public class PlrInfo
        {
            public bool IsNtfTeam { get; set; }
            public byte Score { get; set; }

            public PlrInfo(bool isNtfTeam, byte score = 0)
            {
                IsNtfTeam = isNtfTeam;
                Score = score;
            }
        }

        public class Gat
        {
            public ItemType ItemType { get; set; }
            public uint AttachmentCode { get; set; } = 0;

            public Gat(ItemType itemType, uint attachmentCode = 0)
            {
                ItemType = itemType;
                AttachmentCode = attachmentCode;
            }
        }

        public static byte Tntf = 0; //Number of NTF
        public static byte Tchaos = 0; //Number of chaos

        public List<string> BlacklistRoomNames = new List<string>() { "LczCheckpointA", "LczCheckpointB", "LczClassDSpawn", "HczCheckpointToEntranceZone", "HczCheckpointToEntranceZone", "HczWarhead", "Hcz049", "Hcz106", "Hcz079", "Lcz173" };
        public static List<RoomIdentifier> BlacklistRooms = new List<RoomIdentifier>();

        public static List<Vector3> Spawns = new List<Vector3>(); //List of all possible spawnpoints
        public static Vector3 NTFSpawn; //Current NTF spawn
        public static Vector3 ChaosSpawn; //Current chaos spawn

        public static byte credits = 0; //Tracks time to next spawnpoint rotation
        public List<ItemType> AllAmmo = new List<ItemType>() { ItemType.Ammo12gauge, ItemType.Ammo44cal, ItemType.Ammo556x45, ItemType.Ammo762x39, ItemType.Ammo9x19 }; //All ammo types for easy adding

        public byte NumGuns = 27;
        public static List<Gat> AllWeapons = new List<Gat>();

        public static List<Gat> Tier1 = new List<Gat>() {
            new Gat(ItemType.Jailbird),

            new Gat(ItemType.GunLogicer, 0x1881), //Hex for attachment code much more compact than binary
            
            new Gat(ItemType.GunShotgun, 0x429),

            new Gat(ItemType.GunCrossvec, 0x8918),
            new Gat(ItemType.GunCrossvec, 0xA054),

            new Gat(ItemType.GunE11SR, 0x1220A42),
            new Gat(ItemType.GunE11SR, 0x50A510),

        };
        public static List<Gat> Tier2 = new List<Gat>() {
            new Gat(ItemType.ParticleDisruptor),

            new Gat(ItemType.GunCrossvec, 0x84A1),

            new Gat(ItemType.GunE11SR, 0x491424),
            new Gat(ItemType.GunE11SR),

            new Gat(ItemType.GunRevolver, 0x452),

            new Gat(ItemType.GunShotgun),
            new Gat(ItemType.GunShotgun, 0x292),

            new Gat(ItemType.GunLogicer),
        };
        public static List<Gat> Tier3 = new List<Gat>() {
            new Gat(ItemType.GunCOM15),

            new Gat(ItemType.MicroHID),

            new Gat(ItemType.GunRevolver, 0x22C),

            new Gat(ItemType.GunCOM18, 0x12A),

            new Gat(ItemType.GunFSP9, 0x2922),

            new Gat(ItemType.GunAK),
        };
        public static List<Gat> Tier3S = null;
        public static List<Gat> Tier4 = new List<Gat>() {
            new Gat(ItemType.GunCom45),

            new Gat(ItemType.GunCOM18),

            new Gat(ItemType.GunRevolver),

            new Gat(ItemType.GunFSP9, 0x1611),

            new Gat(ItemType.GunAK, 0x14901),

            new Gat(ItemType.GunCrossvec),
        };

        public static RoleTypeId[,] Roles = new RoleTypeId[,] { { RoleTypeId.ChaosRepressor, RoleTypeId.NtfCaptain }, { RoleTypeId.ChaosMarauder, RoleTypeId.NtfSergeant }, { RoleTypeId.ChaosConscript, RoleTypeId.NtfPrivate }, { RoleTypeId.ClassD, RoleTypeId.Scientist } }; //Player visual levels

        public static bool SpecialEvent = false; //Event after a zombie is spawned

        public static List<Vector3> SurfaceSpawns = new List<Vector3> { new Vector3(0f, 1001f, 0f), new Vector3(8f, 991.65f, -43f), new Vector3(10.5f, 997.47f, -23.25f), new Vector3(63.5f, 995.69f, -34f), new Vector3(63f, 991.65f, -51f), new Vector3(114.5f, 995.45f, -43f), new Vector3(137f, 995.46f, -60f), new Vector3(38.5f, 1001f, -52f), new Vector3(0f, 1001f, -42f) }; //Extra surface spawns

        public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            /*   if (!Extensions.CanRun(sender, PlayerPermissions.PlayersManagement, arguments, Usage, out response))
                     return false;*/

            try
            {
                if (arguments.Count > 0)
                    if (arguments.ElementAt(0).ToUpper().Equals("Y"))
                        FFA = true;
                    else
                        FFA = false;

                if (arguments.Count > 1)
                    switch (arguments.ElementAt(1).ToUpper().ElementAt(0))
                    {
                        case 'L':
                            zone = FacilityZone.LightContainment; break;
                        case 'E':
                            zone = FacilityZone.Entrance; break;
                        case 'H':
                            zone = FacilityZone.HeavyContainment; break;
                        case 'S':
                            zone = FacilityZone.Surface; break;
                    }

                SpecialEvent = false;
                DecontaminationController.Singleton.enabled = false;
                AllPlayers.Clear(); //Clears all values
                Spawns.Clear();
                Tntf = 0;
                Tchaos = 0;

                Tier3S = Tier3.ToList();
                if (zone == FacilityZone.Surface)
                {
                    Tier3S.RemoveAt(0);
                    Spawns = SurfaceSpawns.ToList();
                }

                NumGuns = (byte)(Tier1.Count + Tier2.Count + Tier3S.Count + Tier4.Count);

                byte NumTarget = NumGuns;
                if (arguments.Count > 2)
                    NumTarget = byte.Parse(arguments.ElementAt(2));
                SetNumWeapons(NumTarget);

                foreach (string room in BlacklistRoomNames) //Gets blacklisted room objects for current game
                    BlacklistRooms.AddRange(RoomIdentifier.AllRoomIdentifiers.Where(r => r.Name.ToString().Equals(room)));

                foreach (var door in DoorVariant.AllDoors) //Adds every door in specified zone to spawns list
                {
                    if (door.IsInZone(zone) && !(door is ElevatorDoor || door is CheckpointDoor) && !door.Rooms.Any(x => BlacklistRooms.Any(y => y == x)))
                    {
                        Vector3 doorpos = door.gameObject.transform.position;
                        Spawns.Add(new Vector3(doorpos.x, doorpos.y + 1, doorpos.z));
                        //if (FFA || !door.RequiredPermissions.CheckPermissions(null, null)) //Opens doors
                        door.NetworkTargetState = true;
                    }
                }

                RollSpawns(); //Shuffles spawns

                foreach (Player plr in Player.GetPlayers().OrderBy(w => Guid.NewGuid()).ToList()) //Sets player teams
                {
                    if (plr.IsServer)
                        continue;

                    AssignTeam(plr);
                    SpawnPlayer(plr);
                    plr.SendBroadcast("<b><color=red>Welcome to GunGame!</color></b> \n<color=blue>Race to the final weapon!</color>", 10, shouldClearPrevious: true);
                }

                if (zone == FacilityZone.Surface && !Plugin.EventInProgress && InventoryItemLoader.AvailableItems.TryGetValue(ItemType.SCP244a, out var gma) && InventoryItemLoader.AvailableItems.TryGetValue(ItemType.SCP244b, out var gpa)) //SCP244 obsticals on surface
                {
                    Scp244DeployablePickup Grandma = UnityEngine.Object.Instantiate(gma.PickupDropModel, new Vector3(72f, 992f, -43f), UnityEngine.Random.rotation) as Scp244DeployablePickup;
                    Grandma.NetworkInfo = new PickupSyncInfo
                    {
                        ItemId = ItemType.SCP244a,
                        WeightKg = gma.Weight,
                        Serial = ItemSerialGenerator.GenerateNext()
                    };
                    Grandma.State = Scp244State.Active;
                    NetworkServer.Spawn(Grandma.gameObject);

                    Scp244DeployablePickup Grandpa = UnityEngine.Object.Instantiate(gpa.PickupDropModel, new Vector3(11.3f, 997.47f, -35.3f), UnityEngine.Random.rotation) as Scp244DeployablePickup;
                    Grandpa.NetworkInfo = new PickupSyncInfo
                    {
                        ItemId = ItemType.SCP244b,
                        WeightKg = gpa.Weight,
                        Serial = ItemSerialGenerator.GenerateNext()
                    };
                    Grandpa.State = Scp244State.Active;
                    NetworkServer.Spawn(Grandpa.gameObject);
                }

                Plugin.CurrentEvent = EventType.Gungame;
                Round.IsLocked = true;
                Round.Start(); //Starts the round. Removes player cound at beginning if command used in lobby
                response = $"GunGame event has begun. \nFFA: {FFA} | Zone: {zone} | Levels: {NumTarget}";
                return true;
            }
            catch (Exception e)
            {
                response = $"An error has occurred: {e.Message} \n{e}";
                Round.IsLocked = false;
                return false;
            }
        }

        public void SetNumWeapons(byte num = 20)
        {
            byte T1 = (byte)Math.Round((double)Tier1.Count / NumGuns * num);
            byte T2 = (byte)Math.Round((double)Tier2.Count / NumGuns * num);
            byte T3 = (byte)Math.Round((double)Tier3.Count / NumGuns * num);
            byte T4 = (byte)(num - T1 - T2 - T3);

            AllWeapons = ProcessTier(Tier1, T1).Concat(ProcessTier(Tier2, T2)).Concat(ProcessTier(Tier3S, T3)).Concat(ProcessTier(Tier4, T4)).ToList();
        }


        public List<Gat> ProcessTier(List<Gat> tier, byte target) //Takes in a list and returns a list of the target length comprising of the input's values
        {
            List<Gat> outTier = tier.OrderBy(w => Guid.NewGuid()).Take(target).ToList();
            short additionalCount = (short)(target - tier.Count);
            while (additionalCount > 0)
            {
                short index = (short)new System.Random().Next(tier.Count);
                outTier.Add(tier[index]);
                additionalCount--;
            }
            return outTier;
        }

        public void RollSpawns(byte stop = 0)
        {
            NTFSpawn = Spawns.RandomItem();
            ChaosSpawn = Spawns.RandomItem();

            if (stop < MaxRetryCount && !FFA && Vector3.SqrMagnitude(NTFSpawn - ChaosSpawn) < MinSpawnDistanceSquared)
            {
                RollSpawns((byte)(stop + 1));
                return;
            }

            if (!FFA)
                Cassie.Message(".G2", false, false, false);

            credits = 0;
        }
        const float MinSpawnDistanceSquared = 625; // 25 * 25
        const byte MaxRetryCount = 10;


        public void AssignTeam(Player plr) //Assigns player to team
        {
            if (plr.IsServer || plr.IsOverwatchEnabled || plr.IsTutorial || AllPlayers.ContainsKey(plr))
                return;

            AllPlayers.Add(plr, new PlrInfo((Tntf < Tchaos) && !FFA)); //Adds player to list, uses bool operations to determine teams
            if ((Tntf < Tchaos) && !FFA)
                Tntf++;
            else
                Tchaos++;
        }

        public void RemovePlayer(Player plr) //Removes player from list
        {
            if (AllPlayers.TryGetValue(plr, out var plrStats))
            {
                if (plrStats.IsNtfTeam)
                    Tntf--;
                else
                    Tchaos--;
                AllPlayers.Remove(plr);
            }
        }

        public void SpawnPlayer(Player plr) //Spawns player
        {
            if (!AllPlayers.TryGetValue(plr, out var plrStats))
            {
                plr.ReceiveHint("You are unable to spawn. Try rejoining", 10);
                return;
            }
            int level = 3;
            if (!FFA)
                level = Mathf.Clamp((int)Math.Round((double)plrStats.Score / AllWeapons.Count * 4), 0, 3); //Sets player's class to represent level
            plr.ReferenceHub.roleManager.ServerSetRole(Roles[level, Convert.ToInt32(plrStats.IsNtfTeam)], RoleChangeReason.Respawn, RoleSpawnFlags.None); //Uses bool in 2d array to determine spawn class
            plr.ClearInventory();
            plr.Position = new Vector3(-15.5f, 1014.5f, -31.5f); //Tower spawn point
            plr.ReferenceHub.playerEffectsController.ChangeState<MovementBoost>(25, 99999, false); //Movement effects
            plr.ReferenceHub.playerEffectsController.ChangeState<Scp1853>(10, 99999, false);
            plr.AddItem(ItemType.ArmorCombat);
            plr.AddItem(ItemType.Painkillers);
            foreach (ItemType ammo in AllAmmo) //Gives max ammo of all types
                plr.AddAmmo(ammo, (ushort)plr.GetAmmoLimit(ammo));
            plr.SendBroadcast($"Guns left: {AllWeapons.Count - plrStats.Score}", 5);

            MEC.Timing.CallDelayed(4, () =>
            {
                if (!plrStats.IsNtfTeam) //Spawns either NTF or Chaos based on bool
                    plr.Position = ChaosSpawn;
                else
                    plr.Position = NTFSpawn;

                plr.ReferenceHub.playerEffectsController.ChangeState<DamageReduction>(255, 2, false);
                GiveGun(plr);
                if (SpecialEvent) plr.EffectsManager.EnableEffect<Scp207>(9999);
                if (FFA) RollSpawns();
            });
        }

        public void GiveGun(Player plr) //Gives player their next gun and equips it, and removes old gun
        {
            if (plr.IsServer || plr.IsOverwatchEnabled || plr.IsTutorial || !AllPlayers.TryGetValue(plr, out var plrStats))
                return;

            if (plrStats.Score > 0)
                foreach (ItemBase item in plr.Items) //Removes last gun
                {
                    if (item.ItemTypeId == AllWeapons.ElementAt(plrStats.Score - 1).ItemType)
                    {
                        plr.RemoveItem(item);
                        item.PickupDropModel.DestroySelf(); //Might fix micro sound that keeps playing (Untested)
                        break;
                    }
                }


            Gat currGun = AllWeapons.ElementAt(plrStats.Score);
            ItemBase weapon = plr.AddItem(currGun.ItemType);
            if (weapon is Firearm)
            {
                Firearm firearm = weapon as Firearm;
                uint attachment_code = currGun.AttachmentCode;
                if (attachment_code == 0)
                    attachment_code = AttachmentsUtils.GetRandomAttachmentsCode(currGun.ItemType); //Random Weapon attachments
                                                                                                   //attachment_code = AttachmentsServerHandler.PlayerPreferences[plr.ReferenceHub][firearm.ItemTypeId]; //Player's chosen weapon attachments

                AttachmentsUtils.ApplyAttachmentsCode(firearm, attachment_code, true);
                firearm.Status = new FirearmStatus(firearm.AmmoManagerModule.MaxAmmo, FirearmStatusFlags.MagazineInserted, attachment_code);
            }

            MEC.Timing.CallDelayed(0.1f, () =>
            {
                plr.CurrentItem = weapon;
            });
        }

        public void AddScore(Player plr) //Increases player's score
        {
            if (plr.IsServer || plr.IsOverwatchEnabled || plr.IsTutorial || !AllPlayers.TryGetValue(plr, out var plrStats))
            {
                plr.ReceiveHint("You aren't registered as a player. Try rejoining", 5);
                return;
            }

            if (plrStats.Score >= AllWeapons.Count - 1) //Gun just before zombie
            {
                plr.ClearInventory();
                plr.ReferenceHub.roleManager.ServerSetRole(RoleTypeId.Scp0492, RoleChangeReason.Respawn, RoleSpawnFlags.AssignInventory); //Spawns zombie without increasing score
                plr.ReferenceHub.playerEffectsController.ChangeState<MovementBoost>(25, 99999, false);
                plr.ReferenceHub.playerEffectsController.ChangeState<Scp1853>(10, 99999, false);
                plr.ReferenceHub.playerEffectsController.ChangeState<Scp207>(1, 99999, false);
                plr.Health = 200;

                if (!SpecialEvent) { AlphaWarheadController.Singleton.enabled = true; SpecialEvent = true; }

                if (!FFA)
                    MEC.Timing.CallDelayed(0.1f, () =>
                    {
                        plr.Position = Spawns.RandomItem(); ;
                    });

                return;
            }

            plrStats.Score++; //Adds 1 to score
            GiveGun(plr);
            plr.SendBroadcast($"{AllWeapons.Count - plrStats.Score}", 1);
        }

        public void RemoveScore(Player plr)
        {
            if (plr.IsServer || plr.IsOverwatchEnabled || plr.IsTutorial || !AllPlayers.TryGetValue(plr, out var plrStats))
                return;

            if (plr.IsAlive)
                foreach (ItemBase item in plr.Items) //Removes last gun
                {
                    if (item.ItemTypeId == AllWeapons.ElementAt(plrStats.Score).ItemType)
                    {
                        plr.RemoveItem(item);
                        break;
                    }
                }

            if (plrStats.Score > 0)
            {
                plrStats.Score--;
                if (plr.IsAlive)
                    GiveGun(plr);
            }
        }

        public void TriggerWin(Player plr) //Win sequence
        {
            Round.IsLocked = false;
            Plugin.CurrentEvent = EventType.NONE;
            plr.Health = 42069;
            plr.ReferenceHub.playerEffectsController.DisableAllEffects();

            Firearm firearm = plr.AddItem(ItemType.GunLogicer) as Firearm;
            AttachmentsUtils.ApplyAttachmentsCode(firearm, 0x1881, true);
            firearm.Status = new FirearmStatus(firearm.AmmoManagerModule.MaxAmmo, FirearmStatusFlags.MagazineInserted, 0x1881);
            MEC.Timing.CallDelayed(0.1f, () =>
            {
                plr.CurrentItem = firearm;
            });

            foreach (Player loser in Player.GetPlayers())
            {
                if (loser.IsServer || loser.IsOverwatchEnabled || loser.IsTutorial || !AllPlayers.ContainsKey(loser))
                    continue;
                loser.SendBroadcast($"<b><color=yellow>{plr.Nickname} wins!</color></b>", 10, shouldClearPrevious: true);
                if (loser != plr)
                {
                    loser.SetRole(RoleTypeId.ClassD);
                    loser.ReferenceHub.playerEffectsController.EnableEffect<SeveredHands>();
                    loser.Position = plr.Position;
                }
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
                        plr.ReceiveHint($"{atckr.Nickname} killed you ({AllWeapons.Count - atckrStats.Score})", 2);

                        if (atckrStats.IsNtfTeam != plrStats.IsNtfTeam || FFA)
                        {
                            AddScore(atckr);
                            atckr.AddItem(ItemType.Medkit);
                            atckr.ReceiveHint($"You killed {plr.Nickname} ({AllWeapons.Count - plrStats.Score})", 2);
                        }
                        else
                        {
                            //RemoveScore(atckr); //Removes score if you kill a teammate
                            atckr.ReferenceHub.playerEffectsController.ChangeState<Sinkhole>(3, 5, false);
                            atckr.ReceiveHint($"<color=red>{plr.Nickname} is a teammate!</color>", 5);
                        }


                    }
                }
                else
                {
                    plr.ReceiveHint("Shrimply a krill issue", 3);
                    //RemoveScore(plr); //Removes a score if a player dies to natural means
                }
                if (!FFA)
                {
                    System.Random rnd = new System.Random();
                    credits += (byte)rnd.Next(1, 25); //Adds random amount of credits
                    if (credits >= Mathf.Clamp(Player.Count * 10, 30, 100)) //Rolls next spawns if credits high enough, based on player count
                        RollSpawns();
                }
                else RollSpawns();
                MEC.Timing.CallDelayed(1, () =>
                {
                    SpawnPlayer(plr);
                });
            }
        }


        [PluginEvent(ServerEventType.PlayerDropItem)]
        public bool DropItem(PlayerDropItemEvent args) //Stops items from being dropped
        {
            if (Plugin.EventInProgress && !args.Player.IsTutorial)
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
            if (Plugin.CurrentEvent == EventType.Gungame && !args.Player.IsTutorial)
                return false;
            return true;
        }

        [PluginEvent(ServerEventType.PlayerDropAmmo)]
        public bool DropAmmo(PlayerDropAmmoEvent args) //Stops ammo from being dropped 
        {
            if (Plugin.CurrentEvent == EventType.Gungame && !args.Player.IsTutorial)
                return false;
            return true;
        }

        [PluginEvent(ServerEventType.PlayerSearchPickup)]
        public bool PlayerPickup(PlayerSearchPickupEvent args)
        {
            var itemID = args.Item.Info.ItemId;
            if (Plugin.CurrentEvent == EventType.Gungame && !args.Player.IsTutorial)
                if (itemID == ItemType.Painkillers || itemID == ItemType.Medkit || itemID == ItemType.Adrenaline || itemID == ItemType.GrenadeFlash) //Allows only certain pickups
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
                args.Player.SendBroadcast("<b><color=red>Welcome to GunGame!</color></b> \n<color=blue>Race to the final weapon!</color>", 10, shouldClearPrevious: true);
                MEC.Timing.CallDelayed(3, () =>
                {
                    SpawnPlayer(args.Player);
                });
            }
        }

        //[PluginEvent(ServerEventType.PlayerLeft)]
        //public void PlayerLeft(PlayerLeftEvent args) //Removing player that left from list
        //{
        //    if (Plugin.CurrentEvent == EventType.Gungame)
        //        RemovePlayer(args.Player);
        //}

        [PluginEvent(ServerEventType.PlayerChangeRole)]
        public void ChangeRole(PlayerChangeRoleEvent args) //Failsafes for admin shenanegens 
        {
            if (Plugin.CurrentEvent == EventType.Gungame && args.ChangeReason.Equals(RoleChangeReason.RemoteAdmin))
            {
                var newR = args.NewRole;
                MEC.Timing.CallDelayed(0.1f, () =>
                {
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
            {
                args.Target.Health = 1;
                ExplosionUtils.ServerExplode(args.Player.ReferenceHub);
            }
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
            var knob = args.KnobSetting;
            if (Plugin.CurrentEvent == EventType.Gungame && !args.Player.IsTutorial)
            {
                if (args.Player.CurrentItem.ItemTypeId == ItemType.MicroHID && (knob == Scp914KnobSetting.OneToOne || knob == Scp914KnobSetting.Fine || args.KnobSetting == Scp914KnobSetting.VeryFine)) //Micro recharging
                    return true;
                return false;
            }
            return true;
        }

        [PluginEvent(ServerEventType.Scp914ProcessPlayer)]
        public void PlayerUpgrade(Scp914ProcessPlayerEvent args) //Custom effects for player upgrading
        {
            Player plr = args.Player;
            if (Plugin.CurrentEvent == EventType.Gungame && !plr.IsTutorial && !plr.IsSCP)
            {
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
                            plr.ReferenceHub.playerEffectsController.ChangeState<MovementBoost>(25, 99999, false);
                            plr.ReferenceHub.playerEffectsController.ChangeState<Scp1853>(10, 99999, false);
                        }
                        break;
                    case Scp914KnobSetting.VeryFine:
                        plr.EffectsManager.EnableEffect<Scp207>(9999); break;
                }
            }

        }

        [PluginEvent(ServerEventType.PlayerInteractScp330)]
        public void InfiniteCandy(PlayerInteractScp330Event args)
        {
            Player plr = args.Player;
            if (Plugin.CurrentEvent == EventType.Gungame && !plr.IsTutorial)
            {
                args.AllowPunishment = false;
                System.Random rnd = new System.Random();
                var SkillIssue = rnd.NextDouble();
                if (SkillIssue > 0.9f)
                    ExplosionUtils.ServerExplode(plr.ReferenceHub);
            }
            else args.AllowPunishment = true;
        }

        //[PluginEvent(ServerEventType.PlayerCoinFlip)] // For testing purposes when I don't have test subjects to experiment on
        //public void CoinFlip(PlayerCoinFlipEvent args)
        //{
        //    var plr = args.Player;
        //    plr.ReceiveHint("Cheater", 1);
        //    AddScore(plr);
        //}

        //[PluginEvent(ServerEventType.PlayerUnloadWeapon)]
        //public void GunUnload(PlayerUnloadWeaponEvent args)
        //{
        //    AddScore(args.Player);
        //}


    }


}

