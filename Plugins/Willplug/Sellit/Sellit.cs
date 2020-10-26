using ExileCore;
using ExileCore.PoEMemory.Elements.InventoryElements;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Willplug.SellItems
{
    public class Sellit
    {

        private GameController gameController;

        public Sellit(GameController gameController)
        {
            this.gameController = gameController;
        }


        public List<SellitItem> GetItemsToSell(IList<NormalInventoryItem> inventoryItems, bool sellUnidCrItems=false)
        {
            var sellList = new List<SellitItem>();
            foreach (var inventoryItem in inventoryItems)
            {
                var sellItem = new SellitItem(inventoryItem, gameController.Files);
                bool rarityOk = sellItem.Rarity == ExileCore.Shared.Enums.ItemRarity.Magic || sellItem.Rarity == ExileCore.Shared.Enums.ItemRarity.Normal;
                bool sell1 = (sellItem.IsArmour && sellItem.LargestLink < 6);
                bool sell2 = sellItem.IsWeapon && sellItem.LargestLink < 6;
                bool itemIsIdentified = sellItem.IsIdentified == true;

                bool itemIsArmorOrWeapon = sellItem.IsArmour || sellItem.IsWeapon;


                bool itemIsRare = sellItem.Rarity == ExileCore.Shared.Enums.ItemRarity.Rare;

                bool sellIdentifiedRare = itemIsIdentified && (sell1 || sell2) && itemIsRare;

                bool sellWhiteAndMagicJewelry = (sellItem.IsAmulet || sellItem.IsRing) && (sellItem.Rarity == ExileCore.Shared.Enums.ItemRarity.Magic || sellItem.Rarity == ExileCore.Shared.Enums.ItemRarity.Normal);

                bool sell6Socket = (sellItem.IsArmour || sellItem.IsWeapon) && sellItem.LargestLink < 6 && sellItem.Sockets == 6 && sellItem.Rarity != ExileCore.Shared.Enums.ItemRarity.Unique;
                if (((sell1 || sell2) && rarityOk) || sell6Socket || sellIdentifiedRare)
                {
                    sellList.Add(sellItem);
                }

                if (sellWhiteAndMagicJewelry)
                {
                    sellList.Add(sellItem);
                }
                // If not doing chaos recipe, sell all unid rare items in inventory
                if (sellUnidCrItems == true && itemIsIdentified == false && itemIsRare && itemIsArmorOrWeapon)
                {
                    sellList.Add(sellItem);
                }

              

            }
            return sellList;
        }
    }
}
