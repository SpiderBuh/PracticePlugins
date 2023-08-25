using CommandSystem;
using InventorySystem.Items.Firearms;
using InventorySystem.Items.Firearms.Attachments;
using PluginAPI.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PracticePlugins.Commands
{
    [CommandHandler(typeof(RemoteAdminCommandHandler))]
    public class GetPlayerPositionCommand : ICommand, IUsageProvider
    {
        public string Command => "getPos";

        public string[] Aliases => null;

        public string Description => "Returns your current x y and z values";

        public string[] Usage { get; } = { };



        public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            response = "If you see this, something went wrong";
            try
            {
                foreach (var plr in Player.GetPlayers())
                {
                    if (plr.LogName.Equals(sender.LogName))
                    {
                        response = plr.Position.ToString();
                        break;
                    }
                }
                return true;
            }
            catch (Exception e) { response = e.Message; return false; }
        }


    }
}
