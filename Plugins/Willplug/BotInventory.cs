using ExileCore;
using ExileCore.PoEMemory.Elements.InventoryElements;
using ExileCore.Shared.Enums;
using SharpDX;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Security.Policy;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using TreeRoutine.TreeSharp;
using Willplug.SellItems;
using Action = TreeRoutine.TreeSharp.Action;

namespace Willplug
{
    public static class BotInventory
    {
        private static GameController GameController = WillBot.gameController;


        public static RectangleF InventoryPositionToRectangle(int x, int y)
        {
            var inventoryRect = GameController.IngameState.IngameUi.InventoryPanel[InventoryIndex.PlayerInventory].GetClientRectCache;
            var rectangle = new RectangleF((inventoryRect.Width / 12) * x + inventoryRect.X, (inventoryRect.Height / 5) * y + inventoryRect.Y, inventoryRect.Width / 12, inventoryRect.Height / 5);
            return rectangle;
        }

        public static (bool, RectangleF) GetRectangleOpenSpaceInInventory(int itemWidth, int itemHeight)
        {
            var inventory = GetCurrentInventoryArray();
            for (int x = 0; x < 12; x++)
            {
                for (int y = 0; y < 5; y++)
                {
                    if (CheckForOpenSpaceFrom(x, y, itemWidth, itemHeight, inventory) == true)
                    {
                        var startRect = InventoryPositionToRectangle(x, y);
                        var endRect = InventoryPositionToRectangle(x + itemWidth, y + itemHeight);
                        return (true, new RectangleF(startRect.X, startRect.Y, endRect.X - startRect.X, endRect.Y - startRect.Y));
                    }
                }
            }
            return (false, new RectangleF());
        }

        public static bool IsThereRoomForItem(CustomItem customItem)
        {
            var inventory = GetCurrentInventoryArray();
            for (int x = 0; x < 12; x++)
            {
                for (int y = 0; y < 5; y++)
                {
                    if (CheckForOpenSpaceFrom(x, y, customItem.Width, customItem.Height, inventory) == true)
                    {
                        Console.WriteLine("There is room for item");
                        return true;
                    }
                }
            }
            Console.WriteLine("There is NO room for item");
            return false;
        }

        public static bool CheckForOpenSpaceFrom(int x, int y, int width, int height, int[,] inventory)
        {
            for (int i = 0; i < width; i++)
            {
                if ((x + i) > 11) return false;
                if (inventory[x + i, y] != 0) return false;
                for (int j = 0; j < height; j++)
                {
                    if ((y + j) > 4) return false;
                    if (inventory[x, y + j] != 0) return false;
                }
            }
            return true;
        }

        private static int[,] GetCurrentInventoryArray()
        {
            int[,] inventory = new int[12, 5];

            var items = GameController.IngameState.ServerData.PlayerInventories[0].Inventory.InventorySlotItems;
            //var items = GameController.Game.IngameState.IngameUi.InventoryPanel[InventoryIndex.PlayerInventory].VisibleInventoryItems;
            foreach (var item in items)
            {
                for (int i = item.PosX; i < item.PosX + item.SizeX; i++)
                {
                    for (int j = item.PosY; j < item.PosY + item.SizeY; j++)
                    {
                        inventory[i, j] = 1;
                    }
                }
            }
            return inventory;

        }

    }
}
