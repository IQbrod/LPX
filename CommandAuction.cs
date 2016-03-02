﻿using Rocket.API;
using Rocket.API.Serialisation;
using Rocket.Core;
using Rocket.Core.Logging;
using Rocket.Core.Permissions;
using Rocket.Unturned;
using Rocket.Unturned.Chat;
using Rocket.Unturned.Commands;
using Rocket.Unturned.Player;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using UnityEngine;
using fr34kyn01535.Uconomy;

namespace LIGHT
{
    public class CommandAuction : IRocketCommand
    {       
        public string Name
        {
            get
            {
                return "auction";
            }
        } 
        public AllowedCaller AllowedCaller
        {
            get
            {
                return AllowedCaller.Player;
            }
        }
        public string Help
        {
            get
            {
                return "Allows you to auction your items from your inventory.";
            }
        }
        public string Syntax
        {
            get
            {
                return "<name or id>";
            }
        }
        public List<string> Aliases
        {
            get { return new List<string>(); }
        }
        public List<string> Permissions
        {
            get 
            {
                return new List<string>() {"auction"};
            }
        }
        public void Execute(IRocketPlayer caller, string[] command)
        {
            if(!LIGHT.Instance.Configuration.Instance.AllowAuction)
            {
                UnturnedChat.Say(caller, LIGHT.Instance.Translate("auction_disabled"));
                return;
            }
            UnturnedPlayer player = (UnturnedPlayer)caller;
            if (command.Length == 0)
            {
                UnturnedChat.Say(player, LIGHT.Instance.Translate("auction_command_usage"));
                return;
            }
            if(command.Length == 1)
            {
                switch (command[0])
                {
                    case ("add"):
                        UnturnedChat.Say(player, LIGHT.Instance.Translate("auction_addcommand_usage"));
                        return;
                    case ("list"):
                        string Message = "";
                        string[] ItemNameAndQuality = LIGHT.Instance.DatabaseAuction.GetAllItemNameWithQuality();
                        string[] AuctionID = LIGHT.Instance.DatabaseAuction.GetAllAuctionID();
                        string[] ItemPrice = LIGHT.Instance.DatabaseAuction.GetAllItemPrice();
                        for (int x = 0; x < ItemNameAndQuality.Length; x++)
                        {
                            if(x < ItemNameAndQuality.Length-1)
                                Message += AuctionID[x] + ": " + ItemNameAndQuality[x] + " for "+ ItemPrice[x] + Uconomy.Instance.Configuration.Instance.MoneyName +", ";
                            else
                                Message += AuctionID[x] + ": " + ItemNameAndQuality[x] + " for " + ItemPrice[x] + Uconomy.Instance.Configuration.Instance.MoneyName;
                        }
                        UnturnedChat.Say(player, Message);
                        break;
                }
            }
            if (command.Length == 2)
            {
                switch (command[0])
                {
                    case ("add"):
                        UnturnedChat.Say(player, LIGHT.Instance.Translate("auction_addcommand_usage2"));
                        return;
                }
            }
            if (command.Length > 2)
            {
                switch (command[0])
                {
                    case ("add"):
                        byte amt = 1;
                        ushort id;
                        string name = null;
                        ItemAsset vAsset = null;
                        string itemname = "";
                        for (int x = 1; x < command.Length - 1; x++)
                        {
                            itemname += command[x] + " ";
                        }
                        itemname = itemname.Trim();
                        if (!ushort.TryParse(itemname, out id))
                        {
                            Asset[] array = Assets.find(EAssetType.ITEM);
                            Asset[] array2 = array;
                            for (int i = 0; i < array2.Length; i++)
                            {
                                vAsset = (ItemAsset)array2[i];
                                if (vAsset != null && vAsset.Name != null && vAsset.Name.ToLower().Contains(itemname.ToLower()))
                                {
                                    id = vAsset.Id;
                                    name = vAsset.Name;
                                    break;
                                }
                            }
                        }
                        if (name == null && id == 0)
                        {
                            UnturnedChat.Say(player, LIGHT.Instance.Translate("could_not_find", itemname));
                            return;
                        }
                        else if (name == null && id != 0)
                        {
                            try
                            {
                                vAsset = (ItemAsset)Assets.find(EAssetType.ITEM, id);
                                name = vAsset.Name;
                            }
                            catch
                            {
                                UnturnedChat.Say(player, LIGHT.Instance.Translate("item_invalid"));
                                return;
                            }
                        }
                        if (player.Inventory.has(id) == null)
                        {
                            UnturnedChat.Say(player, LIGHT.Instance.Translate("not_have_item_auction", name));
                            return;
                        }
                        List<InventorySearch> list = player.Inventory.search(id, true, true);                        
                        if (vAsset.Amount > 1)
                        {
                            UnturnedChat.Say(player, LIGHT.Instance.Translate("auction_item_mag_ammo", name));
                            return;
                        }
                        decimal price = 0.00m;
                        if (LIGHT.Instance.Configuration.Instance.EnableShop)
                        {
                            price = LIGHT.Instance.ShopDB.GetItemCost(id);
                            if (price <= 0.00m)
                            {
                                UnturnedChat.Say(player, LIGHT.Instance.Translate("auction_item_notinshop", name));
                                price = 0.00m;
                            }
                        }
                        byte quality = 100;
                        switch (vAsset.Amount)
                        {
                            case 1:
                                // These are single items, not ammo or magazines
                                while (amt > 0)
                                {
                                    try
                                    {
                                        if (player.Player.Equipment.checkSelection(list[0].InventoryGroup, list[0].ItemJar.PositionX, list[0].ItemJar.PositionY))
                                        {
                                            player.Player.Equipment.dequip();
                                        }
                                    }
                                    catch
                                    {
                                        UnturnedChat.Say(player, LIGHT.Instance.Translate("auction_unequip_item", name));
                                        return;
                                    }
                                    quality = list[0].ItemJar.Item.Durability;
                                    player.Inventory.removeItem(list[0].InventoryGroup, player.Inventory.getIndex(list[0].InventoryGroup, list[0].ItemJar.PositionX, list[0].ItemJar.PositionY));
                                    list.RemoveAt(0);
                                    amt--;
                                }
                                break;
                            default:
                                UnturnedChat.Say(player, LIGHT.Instance.Translate("auction_item_mag_ammo", name));
                                return;
                        }
                        decimal SetPrice;
                        if(!decimal.TryParse(command[command.Length - 1], out SetPrice))
                            SetPrice = price;
                        if (LIGHT.Instance.DatabaseAuction.AddAuctionItem(LIGHT.Instance.DatabaseAuction.GetLastAuctionNo(), id.ToString(), name, SetPrice, price, (int)quality, player.Id))
                            UnturnedChat.Say(player, LIGHT.Instance.Translate("auction_item_succes", name, SetPrice, Uconomy.Instance.Configuration.Instance.MoneyName));
                        else
                            UnturnedChat.Say(player, LIGHT.Instance.Translate("auction_item_failed"));
                        break;
                }
                
            }
        }

    }

}