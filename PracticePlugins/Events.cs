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





        
    }
}
