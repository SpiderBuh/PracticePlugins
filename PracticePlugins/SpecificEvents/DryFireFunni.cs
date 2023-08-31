using InventorySystem.Items.Firearms;
using PluginAPI.Core;
using PluginAPI.Core.Attributes;
using PluginAPI.Enums;
using PluginAPI.Events;
using System;
using System.Linq.Expressions;
using Utils;

namespace PracticePlugins.Plugins
{
    public class DryFireFunni
    {
        [PluginEvent(ServerEventType.PlayerDryfireWeapon)]
        public void DryFire(PlayerDryfireWeaponEvent args)
        {
            {
                Player plr = args.Player;
                Firearm gun = args.Firearm;
                Player target = null;

                if (!plr.IsTutorial) // Limits this plugin to tutorial players
                    return;

                    plr.ReceiveHint("Dryfire", 1);

                    if (gun.HitregModule.ClientCalculateHit(out var message)) //Maybe checks if hitreg occurred?
                    {
                        uint targetNID = message.TargetNetId; //I barely know what a netID is
                        foreach (Player plrTarget in Player.GetPlayers()) //Loops through each player
                        {
                            if (plrTarget.ReferenceHub.netId == targetNID && !plrTarget.IsServer) //Checks if the player is being targeted
                                target = plrTarget;
                        }
                        if (target != null && target != plr)
                        {
                            ExplosionUtils.ServerExplode(target.ReferenceHub); //Explodes target
                            plr.ReceiveHint("Exploded the nerd", 5);
                        }
                        //else
                          //  plr.ReceiveHint("Hitreg activated, but was blocked", 5);
                    }
                    else
                        plr.ReceiveHint("Did not hit a player", 5);
                     
                


                //return;
            }
        }
    }
}
