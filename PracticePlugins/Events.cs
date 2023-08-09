using AdminToys;
using CustomPlayerEffects;
using Interactables.Interobjects;
using Interactables.Interobjects.DoorUtils;
using InventorySystem;
using InventorySystem.Items.Firearms;
using InventorySystem.Items.Firearms.Attachments;
using MapGeneration.Distributors;
using Mirror;
using PlayerRoles;
using PlayerRoles.FirstPersonControl;
using PlayerStatsSystem;
using PluginAPI.Core;
using PluginAPI.Core.Attributes;
using PluginAPI.Enums;
using Scp914;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityStandardAssets.Effects;
using InventorySystem.Disarming;
using InventorySystem.Items.ThrowableProjectiles;
using InventorySystem.Items;
using Utils;
using Footprinting;
using InventorySystem.Items.Pickups;
using MapGeneration;
using Respawning;
using PluginAPI.Events;


namespace PracticePlugins
{
    public class Events
    {
        [PluginEvent(ServerEventType.RoundRestart)]
        public void OnRoundRestart()
        {
            Plugin.CurrentEvent = EventType.NONE;
        }





        //GUNGAME SPECIFIC EVENTS
        /*
        [PluginEvent(ServerEventType.PlayerDying), PluginPriority(LoadPriority.Highest)]
        public void PlayerDying(PlayerDyingEvent args)
        {
            var plr = args.Player;
            plr.ReceiveHint("You dieded" + 5);
            if (Plugin.CurrentEvent == EventType.Gungame)
            {
                var atckr = args.Attacker;

                if (atckr.Role == RoleTypeId.Scp0492) //Triggers win if player is zombie
                {
                    TriggerWin(atckr);
                    return;
                }
                if (!(atckr == plr) && args.DamageHandler is AttackerDamageHandler aDH)
                {
                    AddScore(atckr);
                    // plr.ReceiveHint(atckr.LogName + " killed you");
                    // atckr.ReceiveHint("You killed " + plr.LogName);
                }
                else
                    plr.ReceiveHint("Shrimply a krill issue", 1);
                System.Random rnd = new System.Random();
                credits += (byte)rnd.Next(1, 25); //Adds random amount of credits
                if (credits >= 100) //Rolls next spawns if credits high enough
                {
                    credits -= 100;
                    RollSpawns();
                }
                MEC.Timing.CallDelayed(2, () =>
                {
                    SpawnPlayer(plr);
                });
            }
        }



        [PluginEvent(ServerEventType.PlayerDropItem)]
        public bool DropItem(PlayerDropItemEvent args) //Stops items from being dropped on death (def doesnt work)
        {
            return false;
        }

        [PluginEvent(ServerEventType.PlayerJoined)]
        public void PlayerJoined(PlayerJoinedEvent args) //Adding new player to the game 
        {
            if (Plugin.CurrentEvent == EventType.Gungame)
            {
                AssignTeam(args.Player);
                SpawnPlayer(args.Player);
                args.Player.SendBroadcast("Welcome to GunGame! Race to the final weapon!", 10, shouldClearPrevious: true);
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
            if (Plugin.CurrentEvent == EventType.Gungame)
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
        }*/
    }
}
