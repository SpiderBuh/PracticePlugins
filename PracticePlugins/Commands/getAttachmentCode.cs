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
    public class GetAttachmentCodeCommand : ICommand, IUsageProvider
    {
        public string Command => "gcode";

        public string[] Aliases => null;

        public string Description => "Returns the attachment code of the current gun";

        public string[] Usage { get; } = { "set code" };



        public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            response = "Not holding a gun";
            Firearm temp;
            try
            {
                Player plr = Player.Get(sender);
                if (plr.CurrentItem is Firearm)
                {
                    temp = (Firearm)plr.CurrentItem;
                    if (arguments.Count == 0)
                        response = "code: " + temp.GetCurrentAttachmentsCode().ToString("X");
                    else
                    {
                        temp.Status = new FirearmStatus(temp.AmmoManagerModule.MaxAmmo, FirearmStatusFlags.MagazineInserted, uint.Parse(arguments.ElementAt(0), System.Globalization.NumberStyles.HexNumber));
                        response = "Applied code " + arguments.ElementAt(0);
                    }
                }

                return true;
            }
            catch (Exception e) { response = e.Message; return false; }
        }


    }
}
