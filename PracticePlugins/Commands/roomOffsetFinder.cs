using CommandSystem;
using InventorySystem.Items.Firearms;
using InventorySystem.Items.Firearms.Attachments;
using MapGeneration;
using PlayerRoles.PlayableScps.HUDs;
using PluginAPI.Core;
using PluginAPI.Core.Interfaces;
using RemoteAdmin;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading.Tasks;
using Unity.Mathematics;
using UnityEngine;

namespace PracticePlugins.Commands
{
    [CommandHandler(typeof(RemoteAdminCommandHandler))]
    public class RoomOffsetFinderCommand : ICommand, IUsageProvider
    {
        public string Command => "getRoomOffset";

        public string[] Aliases => null;

        public string Description => "Utility thing to find an offset vector for a specific room";

        public string[] Usage { get; } = { "RoomName number" };



        public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            try
            {
                Player plr = Player.Get(sender);
                if (arguments.Count < 1)
                {
                    response = $"Room: {(int)RoomIdUtils.RoomAtPositionRaycasts(plr.Position).Name}";
                    return true;
                }
                
                if (!RoomIdUtils.TryFindRoom((RoomName)int.Parse(arguments.ElementAt(0)), FacilityZone.None, RoomShape.Undefined, out var foundRoom))
                    throw new ArgumentException("Could not find room");
                
                Vector3 offset = Quaternion.FromToRotation(foundRoom.transform.forward, Vector3.forward) * (plr.Position - foundRoom.transform.position);
                response = $"Room {foundRoom.Name}\noffset: {offset}\nRotation: {foundRoom.transform.rotation}";
                return true;
            }
            catch (Exception e) { response = $"{e.Message}\n{e.Source}\n{e.TargetSite}"; return false; }
        }


    }
}
