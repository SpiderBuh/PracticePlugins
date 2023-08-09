/*using InventorySystem;
using PluginAPI.Core.Attributes;
using PluginAPI.Enums;
using System;
using System.Collections.Generic;
using InventorySystem.Items;
using PluginAPI.Events;

namespace CustomCommands
{
    public class SpiderBuhCoinCards
    {
        [PluginEvent(ServerEventType.PlayerCoinFlip)]
        public void CoinFlip(PlayerCoinFlipEvent args)
        {

            var plr = args.Player;
            bool isTails = args.IsTails;

            try //try catch to hopefully not crash the server
            {

                //plr.ReceiveHint("Coin flipped...", 1);
                Dictionary<ItemType, ItemType[]> Upgrades = new Dictionary<ItemType, ItemType[]>() //List of all cards and their possible changes
                {
                //  Current card being targeted             Card's target downgrade                                 Card's target upgrade
                    {ItemType.KeycardJanitor,               new ItemType[] { ItemType.Flashlight,                   ItemType.KeycardScientist } },
                    {ItemType.KeycardScientist,             new ItemType[] { ItemType.KeycardJanitor,               ItemType.KeycardResearchCoordinator } },
                    {ItemType.KeycardResearchCoordinator,   new ItemType[] { ItemType.KeycardScientist,             ItemType.KeycardContainmentEngineer } },
                    {ItemType.KeycardContainmentEngineer,   new ItemType[] { ItemType.KeycardResearchCoordinator,   ItemType.KeycardNTFCommander } },
                    {ItemType.KeycardGuard,                 new ItemType[] { ItemType.KeycardScientist,             ItemType.KeycardNTFOfficer } },
                    {ItemType.KeycardNTFOfficer,            new ItemType[] { ItemType.KeycardGuard,                 ItemType.KeycardNTFLieutenant } },
                    {ItemType.KeycardNTFLieutenant,         new ItemType[] { ItemType.KeycardNTFOfficer,            ItemType.KeycardNTFCommander} },
                    {ItemType.KeycardNTFCommander,          new ItemType[] { ItemType.KeycardNTFLieutenant,         ItemType.KeycardO5 } },
                    {ItemType.KeycardZoneManager,           new ItemType[] { ItemType.KeycardScientist,             ItemType.KeycardFacilityManager } },
                    {ItemType.KeycardFacilityManager,       new ItemType[] { ItemType.KeycardZoneManager,           ItemType.KeycardO5 } },
                    {ItemType.KeycardChaosInsurgency,       new ItemType[] { ItemType.KeycardNTFLieutenant,         ItemType.KeycardO5 } },
                    {ItemType.KeycardO5,                    new ItemType[] { ItemType.KeycardChaosInsurgency,       ItemType.Jailbird } }
                };


                ItemBase card = null;
                List<ItemBase> AllCards = new List<ItemBase>();
                System.Random rngCard = new System.Random();
                foreach (var item in plr.Items)
                {
                    if (Upgrades.ContainsKey(item.ItemTypeId)) //checking if item is a card
                    {
                        //plr.ReceiveHint("Card found...", 1);
                        //card = item;
                        //break; //uncomment if you dont want a random card
                        AllCards.Add(item);
                        card = AllCards[rngCard.Next(AllCards.Count)];
                    }
                }


                if (card != null)
                { //check if player has a keycard

                    ItemBase item = plr.CurrentItem; //gets coin item
                    MEC.Timing.CallDelayed(2, () =>
                    {
                        plr.ReferenceHub.inventory.ServerRemoveItem(item.ItemSerial, null); //deletes coin from inventory
                        plr.ReferenceHub.inventory.ServerRemoveItem(card.ItemSerial, null); //deletes old card from inventory
                        if (isTails)
                        {
                            plr.AddItem(Upgrades[card.ItemTypeId][0]); //Downgrades card
                            //plr.ReceiveHint("Tails! Downgrading card", 5);
                        }
                        else
                        {
                            plr.AddItem(Upgrades[card.ItemTypeId][1]); //Upgrades card
                            //plr.ReceiveHint("Heads! Upgrading card", 5);
                        }
                    });
                }
                //else plr.ReceiveHint("Card not found", 5);
            }
            catch (Exception ex) { plr.ReceiveHint("Oh shid oh fucc oh lawd   " + ex.Message, 10); } //idk this probably seems appropriate if something goes wrong

        }
    }
}*/

/*
            Above code is my own attempt, below code is the improvement by ThePheggHerself https://github.com/ThePheggHerself             

many thanks, Pheen :)
*/

using InventorySystem;
using PluginAPI.Core.Attributes;
using PluginAPI.Enums;
using System;
using System.Collections.Generic;
using InventorySystem.Items;
using PluginAPI.Events;

namespace PracticePlugins
{
    public class Pocket914Cards
    {
        Dictionary<ItemType, ItemType[]> Upgrades = new Dictionary<ItemType, ItemType[]>() //List of all cards and their possible changes
                {
                //  Current card being targeted             Card's target downgrade                                 Card's target upgrade
                    {ItemType.KeycardJanitor,               new ItemType[] { ItemType.Flashlight,                   ItemType.KeycardScientist } },
                    {ItemType.KeycardScientist,             new ItemType[] { ItemType.KeycardJanitor,               ItemType.KeycardResearchCoordinator } },
                    {ItemType.KeycardResearchCoordinator,   new ItemType[] { ItemType.KeycardScientist,             ItemType.KeycardContainmentEngineer } },
                    {ItemType.KeycardContainmentEngineer,   new ItemType[] { ItemType.KeycardResearchCoordinator,   ItemType.KeycardNTFCommander } },
                    {ItemType.KeycardGuard,                 new ItemType[] { ItemType.KeycardScientist,             ItemType.KeycardNTFOfficer } },
                    {ItemType.KeycardNTFOfficer,            new ItemType[] { ItemType.KeycardGuard,                 ItemType.KeycardNTFLieutenant } },
                    {ItemType.KeycardNTFLieutenant,         new ItemType[] { ItemType.KeycardNTFOfficer,            ItemType.KeycardNTFCommander} },
                    {ItemType.KeycardNTFCommander,          new ItemType[] { ItemType.KeycardNTFLieutenant,         ItemType.KeycardO5 } },
                    {ItemType.KeycardZoneManager,           new ItemType[] { ItemType.KeycardScientist,             ItemType.KeycardFacilityManager } },
                    {ItemType.KeycardFacilityManager,       new ItemType[] { ItemType.KeycardZoneManager,           ItemType.KeycardO5 } },
                    {ItemType.KeycardChaosInsurgency,       new ItemType[] { ItemType.KeycardNTFLieutenant,         ItemType.KeycardO5 } },
                    {ItemType.KeycardO5,                    new ItemType[] { ItemType.KeycardFacilityManager,       ItemType.Jailbird } }
                };


        [PluginEvent(ServerEventType.PlayerCoinFlip)]
        public void CoinFlip(PlayerCoinFlipEvent args)
        {

            var plr = args.Player;

            ItemBase card = null;
            List<ItemBase> AllCards = new List<ItemBase>();
            System.Random rngCard = new System.Random();
            foreach (var item in plr.Items)
            {
                if (Upgrades.ContainsKey(item.ItemTypeId)) //Checks if item is a card
                    AllCards.Add(item);
            }

            if (AllCards.Count < 1)
                return;

            card = AllCards[rngCard.Next(AllCards.Count)]; //Choses random card

            MEC.Timing.CallDelayed(2, () =>
            {
                plr.ReferenceHub.inventory.ServerRemoveItem(plr.CurrentItem.ItemSerial, null); //deletes coin from inventory
                plr.ReferenceHub.inventory.ServerRemoveItem(card.ItemSerial, null); //deletes old card from inventory

                if (args.IsTails)
                    plr.AddItem(Upgrades[card.ItemTypeId][0]); //Downgrades card

                else
                    plr.AddItem(Upgrades[card.ItemTypeId][1]); //Upgrades card

            });
        }
    }
}