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
            try
            {
                Player plr = Player.Get(sender);
                response = plr.Position.ToString();
                return true;
            }
            catch (Exception e) { response = e.Message; return false; }
        }


    }
}
