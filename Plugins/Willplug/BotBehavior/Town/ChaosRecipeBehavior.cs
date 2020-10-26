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


namespace Willplug.BotBehavior.Town
{

    public class ItemToGrabFromStash
    {
        public int stashIndex;
        public RectangleF rect;
        public ItemTypes itemType;
        public SellitItem sellItItem;
        public ItemToGrabFromStash(int stashIndex, RectangleF rect, ItemTypes itemType, SellitItem sellItItem)
        {
            this.stashIndex = stashIndex;
            this.rect = rect;
            this.itemType = itemType;
            this.sellItItem = sellItItem;
        }
    }



    public static class ChaosRecipeBehavior
    {
        /* All stash info starts as null. You have to load each stash tab for them to show up with info stored in memory.
         */
        static private readonly Stopwatch ControlTimer = new Stopwatch();
        private static GameController GameController = WillBot.gameController;
        private static WillPlayer Me => WillBot.Me;
        private static int Latency => (int)GameController.Game.IngameState.CurLatency;

        private static List<string> chaosRecipeStashTabNamesList = new List<string>() { "cr1", "cr2", "cr3", "cr4" };
        private static List<int> chaosRecipeStashTabIndexes = new List<int>();

        public static Dictionary<ItemTypes, List<ItemToGrabFromStash>> completeSetDict = new Dictionary<ItemTypes, List<ItemToGrabFromStash>>();
        public static Dictionary<ItemTypes, List<ItemToGrabFromStash>> allItemsDict = new Dictionary<ItemTypes, List<ItemToGrabFromStash>>();
        public static List<ItemToGrabFromStash> itemsToGrabFromStash = new List<ItemToGrabFromStash>();

        private static int chaosRecipeSetsSoldThisCycle = 0;
        private static ItemToGrabFromStash currentItemToGrab = null;

        private static bool hasInitializedSets = false;

        private static bool hasOpenedCrStashTabs = false;

        public static void ResetData()
        {
            itemsToGrabFromStash.Clear();
            completeSetDict.Clear();
            allItemsDict.Clear();
            hasInitializedSets = false;
            currentItemToGrab = null;
            chaosRecipeSetsSoldThisCycle = 0;
            hasOpenedCrStashTabs = false;
        }



        //public static bool DoChaosRecipe()
        //{
        //    if (!Me.HasPerformedChaosRecipeThisTownCycle && GetNumberOfCrRecipeSetsInStash() >= 1)
        //    {
        //        return true;
        //    }
        //    return false;
        //}

        public delegate ItemToGrabFromStash ItemToGrabFromStashDelegate(object context);


        public delegate Dictionary<ItemTypes, List<ItemToGrabFromStash>> ItemsToGrabDelegate(object context);



        public static Composite DoChaosRecipeStashesInit()
        {
            return new Decorator(delegate
            {
                GetIndexesOfCrStashTabs();
                if (hasOpenedCrStashTabs == false)
                {
                    return true;
                }
                return false;
            }, new Sequence(
                TownBehavior.OpenStash(),
                StashBehavior.SwitchToTab(x => chaosRecipeStashTabIndexes[0]),
                StashBehavior.SwitchToTab(x => chaosRecipeStashTabIndexes[1]),
                StashBehavior.SwitchToTab(x => chaosRecipeStashTabIndexes[2]),
                StashBehavior.SwitchToTab(x => chaosRecipeStashTabIndexes[3]),
                new Action(delegate
                {
                    hasOpenedCrStashTabs = true;
                    return RunStatus.Success;
                })
                )
            );
        }


        public static Composite ChaosRecipeHandler()
        {
            return new PrioritySelector(DoChaosRecipeStashesInit(), GetCrRecipeItemsFromStash(), SellCrItemsToVendor());
        }

        public static Composite SellCrItemsToVendor()
        {
            return new Decorator(delegate
            {
                if (GetNumberOfCrRecipeSetsInInventory() == 1 && chaosRecipeSetsSoldThisCycle < 10)
                {
                    return true;
                }
                return false;
            },
            new Sequence(
                CommonBehavior.CloseOpenPanels(),
                 SellBehavior.FindVendor(),
                 SellBehavior.MoveToAndOpenVendor(),
                 SellBehavior.OpenVendorSellScreen(),
                 new Action(delegate
                 {
                     Input.KeyDown(Keys.LControlKey);
                     Thread.Sleep(10);
                     foreach (var item in GameController.Game.IngameState.IngameUi.InventoryPanel[InventoryIndex.PlayerInventory].VisibleInventoryItems)
                     {
                         if (IsCrItem(item))
                         {
                             Mouse.SetCursorPosAndLeftOrRightClick(item.GetClientRectCache, Latency, randomClick: true);
                             Thread.Sleep(Latency);
                         }
                     }
                     Input.KeyUp(Keys.LControlKey);
                     Thread.Sleep(Latency + 100);

                     if (IsNpcOfferValid())
                     {
                         // click accept button
                         Console.WriteLine("Offer correct");
                         chaosRecipeSetsSoldThisCycle += 1;
                         hasInitializedSets = false;
                         var acceptButton = WillBot.gameController.IngameState.IngameUi.VendorAcceptButton;
                         Mouse.SetCursorPosAndLeftOrRightClick(acceptButton.GetClientRectCache, Latency, randomClick: true);
                         return RunStatus.Success;
                     }
                     else
                     {
                         Console.WriteLine("Offer incorrect");
                         // need to fail gracefully somehow..
                         return RunStatus.Failure;
                     }
                 }),
                 // Testing purposes.. Create a loop. 
                 // Close panels.
                 // Run stashie
                 // 
                 CommonBehavior.CloseOpenPanels(),
                  CommonBehavior.CloseOpenPanels(),
                   CommonBehavior.CloseOpenPanels()
                //new Action(delegate { WillBot.Me.HasStashedItemsThisTownCycle = false; return RunStatus.Success; }),
                //TownBehavior.Stashie()

                )
            );
        }

        public static Composite GetCrRecipeItemsFromStash()
        {
            // 
            return new Decorator(delegate
            {
                if (hasInitializedSets == false)
                {
                    hasInitializedSets = true;
                    MapChaosRecipeItems();
                    DisableOrEnableLootingOfItems();
                    Console.WriteLine("Initializing cr sets data");
                }
                if (itemsToGrabFromStash.Count > 0 && currentItemToGrab == null)
                {
                    Console.WriteLine("Finding item to grab");
                    currentItemToGrab = itemsToGrabFromStash[0];
                    itemsToGrabFromStash.RemoveAt(0);
                    return true;
                }
                else if (currentItemToGrab != null)
                {
                    Console.WriteLine("Already have an item to grab");
                    return true;
                }
                Console.WriteLine("Unable to get cr items from stash");
                return false;

            }, GrabItem(x => currentItemToGrab));
        }


        public static Composite MoveItemFromStashToInventory(ItemToGrabFromStashDelegate itemToGrabFromStashDelegate)
        {
            return new Sequence(
                new Action(delegate (object context)
                {
                    var item = itemToGrabFromStashDelegate(context);
                    int itemCountInPlayerInventoryBeforeMovingItem = GameController.Game.IngameState.IngameUi.InventoryPanel[InventoryIndex.PlayerInventory].VisibleInventoryItems.Count;
                    Console.WriteLine("item address init: {0}", item.sellItItem.NormalInventoryItem.Address);
                    Input.KeyDown(Keys.LControlKey);
                    Thread.Sleep(10);
                    Mouse.SetCursorPosAndLeftOrRightClick(item.rect, Latency, randomClick: true);
                    Thread.Sleep(15);
                    Input.KeyUp(Keys.LControlKey);
                    ControlTimer.Restart();
                    // Will the NormalInventoryItem be set to null?? what happens to it when I move it
                    while (ControlTimer.ElapsedMilliseconds < 2000)
                    {
                        int itemCountInPlayerInventoryAfterMovingItem = GameController.Game.IngameState.IngameUi.InventoryPanel[InventoryIndex.PlayerInventory].VisibleInventoryItems.Count;
                        if (itemCountInPlayerInventoryAfterMovingItem != itemCountInPlayerInventoryBeforeMovingItem)
                        {
                            Console.WriteLine("player inventory contains the item now");
                            return RunStatus.Success;
                        }
                        Thread.Sleep(50);
                    }
                    Console.WriteLine("player inventory DOES NOT scontain the item");
                    return RunStatus.Failure;
                })
                );
        }
        public static Composite GrabItem(ItemToGrabFromStashDelegate itemToGrabFromStashDelegate)
        {
            return new Sequence(
                TownBehavior.OpenStash(),
                StashBehavior.SwitchToTab(x => itemToGrabFromStashDelegate(x).stashIndex),
                MoveItemFromStashToInventory(itemToGrabFromStashDelegate),
                new Action(delegate
                {
                    Thread.Sleep(100);
                    currentItemToGrab = null;
                    return RunStatus.Success;
                })
                );
        }


        public static bool IsNpcOfferValid()
        {
            var npcTradingWindow = GameController.Game.IngameState.IngameUi.SellWindow;
            var npcOfferItems = npcTradingWindow.OtherOffer;

            foreach (var element in npcOfferItems.Children)
            {
                var item = element.AsObject<NormalInventoryItem>().Item;
                if (string.IsNullOrEmpty(item.Metadata))
                    continue;
                var itemName = GameController.Files.BaseItemTypes.Translate(item.Metadata).BaseName;
                if (itemName == "Chaos Orb")
                {
                    return true;
                }
            }
            return false;
        }
        public static void DisableOrEnableLootingOfItems()
        {
            const int smallThreshold = 5;
            const int mediumThreshold = 3;
            const int bigThreshold = 2;
            foreach (var keyValue in allItemsDict)
            {
                switch (keyValue.Key)
                {
                    case ItemTypes.Amulet:
                    case ItemTypes.Ring:
                        // Always loot these
                        break;
                    case ItemTypes.Belt:
                        WillBot.Plugin.Settings.RareBelts.Value = (keyValue.Value.Count > smallThreshold ? false : true);
                        break;
                    case ItemTypes.Helmet:
                        WillBot.Plugin.Settings.RareHelmets.Value = (keyValue.Value.Count > mediumThreshold ? false : true);
                        break;
                    case ItemTypes.Gloves:
                        WillBot.Plugin.Settings.RareGloves.Value = (keyValue.Value.Count > mediumThreshold ? false : true);
                        break;
                    case ItemTypes.Boots:
                        WillBot.Plugin.Settings.RareBoots.Value = (keyValue.Value.Count > mediumThreshold ? false : true);
                        break;
                    case ItemTypes.BodyArmour:
                        WillBot.Plugin.Settings.RareArmour.Value = (keyValue.Value.Count > bigThreshold ? false : true);
                        break;
                    case ItemTypes.TwoHandedWeapon:
                        WillBot.Plugin.Settings.RareTwoHandedWeapon.Value = (keyValue.Value.Count > bigThreshold ? false : true);
                        break;
                }
            }
        }

        public static bool MapChaosRecipeItems()
        {
            completeSetDict.Clear();
            allItemsDict.Clear();
            itemsToGrabFromStash.Clear();
            GetIndexesOfCrStashTabs();
            foreach (var crIndex in chaosRecipeStashTabIndexes)
            {
                var stashItems = GameController.IngameState.IngameUi.StashElement?.AllInventories[crIndex]?.VisibleInventoryItems;
                if (stashItems == null) return false;
                foreach (var item in stashItems)
                {
                    if (IsCrItem(item) == false) continue;
                    var parsedItem = new SellitItem(item, GameController.Files);
                    if (parsedItem.ItemType != ItemTypes.Other && parsedItem.ItemType != ItemTypes.OneHandedWeapon)
                    {
                        Console.WriteLine("Added {0} to dictionary", parsedItem.ItemType.ToString());
                        var itemToGrab = new ItemToGrabFromStash(crIndex, item.GetClientRectCache, parsedItem.ItemType, parsedItem);
                        if (allItemsDict.ContainsKey(parsedItem.ItemType))
                        {
                            allItemsDict[parsedItem.ItemType].Add(itemToGrab);
                        }
                        else
                        {
                            allItemsDict.Add(parsedItem.ItemType, new List<ItemToGrabFromStash>());
                            allItemsDict[parsedItem.ItemType].Add(itemToGrab);
                        }

                        if (completeSetDict.ContainsKey(parsedItem.ItemType))
                        {
                            var numberOfSameItems = completeSetDict[parsedItem.ItemType].Count;
                            if (parsedItem.ItemType == ItemTypes.Ring && numberOfSameItems < 2)
                            {
                                completeSetDict[parsedItem.ItemType].Add(itemToGrab);
                            }
                            else if (numberOfSameItems < 1)
                            {
                                completeSetDict[parsedItem.ItemType].Add(itemToGrab);
                            }
                        }
                        else
                        {
                            completeSetDict.Add(parsedItem.ItemType, new List<ItemToGrabFromStash>());
                            completeSetDict[parsedItem.ItemType].Add(itemToGrab);
                        }
                    }
                }
            }


            var numberOfSets = GetNumberOfCrRecipeSetsInStash();
            if (numberOfSets > 0)
            {
                foreach (var keyValue in completeSetDict)
                {
                    itemsToGrabFromStash.AddRange(keyValue.Value);
                }
                return true;
            }
            return false;

        }


        public static int GetNumberOfCrRecipeSetsInStash()
        {
            var setList = new List<int>() { 0, 0, 0, 0, 0, 0, 0, 0 };
            GetIndexesOfCrStashTabs();
            foreach (var crIndex in chaosRecipeStashTabIndexes)
            {
                var stashItems = GameController.IngameState.IngameUi.StashElement.AllInventories[crIndex]?.VisibleInventoryItems;
                if (stashItems == null)
                {
                    Console.WriteLine("stashItems is null in GetNumberOfCrRecipeSetsInStash");
                    return 0;
                }
                foreach (var item in stashItems)
                {
                    if (IsCrItem(item) == false) continue;
                    var parsedItem = new SellitItem(item, GameController.Files);
                    setList[0] = (parsedItem.IsAmulet == true) ? setList[0] + 1 : setList[0];
                    setList[1] = (parsedItem.IsRing == true) ? setList[1] + 1 : setList[1];
                    setList[2] = (parsedItem.IsHelmet == true) ? setList[2] + 1 : setList[2];
                    setList[3] = (parsedItem.IsGloves == true) ? setList[3] + 1 : setList[3];
                    setList[4] = (parsedItem.IsBelt == true) ? setList[4] + 1 : setList[4];
                    setList[5] = (parsedItem.IsBoots == true) ? setList[5] + 1 : setList[5];
                    setList[6] = (parsedItem.IsBodyArmor == true) ? setList[6] + 1 : setList[6];
                    setList[7] = (parsedItem.IsTwoHandedWeapon == true) ? setList[7] + 1 : setList[7];
                    // setList[8] = (parsedItem.IsOneHandedWeapon == true) ? setList[8] + 1 : setList[8];
                }
            }
            setList[1] = setList[1] / 2;
            var numberOfSets = setList.Min();
            Console.WriteLine("Stash: Found {0} chaos recipe sets", numberOfSets);
            return numberOfSets;
        }
        public static int GetNumberOfCrRecipeSetsInInventory()
        {
            var setList = new List<int>() { 0, 0, 0, 0, 0, 0, 0, 0 };
            var inventoryItems = GameController.Game.IngameState.IngameUi.InventoryPanel[InventoryIndex.PlayerInventory].VisibleInventoryItems;
            foreach (var item in inventoryItems)
            {
                if (IsCrItem(item) == false) continue;
                var parsedItem = new SellitItem(item, GameController.Files);
                setList[0] = (parsedItem.IsAmulet == true) ? setList[0] + 1 : setList[0];
                setList[1] = (parsedItem.IsRing == true) ? setList[1] + 1 : setList[1];
                setList[2] = (parsedItem.IsHelmet == true) ? setList[2] + 1 : setList[2];
                setList[3] = (parsedItem.IsGloves == true) ? setList[3] + 1 : setList[3];
                setList[4] = (parsedItem.IsBelt == true) ? setList[4] + 1 : setList[4];
                setList[5] = (parsedItem.IsBoots == true) ? setList[5] + 1 : setList[5];
                setList[6] = (parsedItem.IsBodyArmor == true) ? setList[6] + 1 : setList[6];
                setList[7] = (parsedItem.IsTwoHandedWeapon == true) ? setList[7] + 1 : setList[7];
                // setList[8] = (parsedItem.IsOneHandedWeapon == true) ? setList[8] + 1 : setList[8];
            }
            setList[1] = setList[1] / 2;
            var numberOfSets = setList.Min();
            Console.WriteLine("Inventory: Found {0} chaos recipe sets", numberOfSets);
            return numberOfSets;
        }
        public static bool IsCrItem(NormalInventoryItem item)
        {
            var parsedItem = new SellitItem(item, GameController.Files);
            bool isCorrectILvl = parsedItem.ItemLevel >= 60 && parsedItem.ItemLevel <= 74;
            bool isCorrectItemType = parsedItem.IsArmour || parsedItem.IsWeapon || parsedItem.IsAmulet || parsedItem.IsRing;
            bool isRare = parsedItem.Rarity == ItemRarity.Rare;
            bool isUnidentified = parsedItem.IsIdentified == false;
            return isCorrectILvl && isCorrectItemType && isRare && isUnidentified;
        }



        private static bool hasInitializedIndexes = false;
        private static void GetIndexesOfCrStashTabs()
        {
            if (hasInitializedIndexes == false)
            {
                hasInitializedIndexes = true;
                foreach (var str in chaosRecipeStashTabNamesList)
                {
                    chaosRecipeStashTabIndexes.Add(StashBehavior.GetIndexOfStashTabWithName(str));
                }
            }
        }
    }
}
