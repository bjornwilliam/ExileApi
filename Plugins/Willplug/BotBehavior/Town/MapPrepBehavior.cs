using ExileCore;
using ExileCore.PoEMemory.Components;
using ExileCore.PoEMemory.Elements.InventoryElements;
using ExileCore.Shared.Enums;
using ExileCore.Shared.Nodes;
using SharpDX;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using TreeRoutine.DefaultBehaviors.Actions;
using TreeRoutine.TreeSharp;
using Willplug.BotBehavior.Town;
using Willplug.SellItems;
using Action = TreeRoutine.TreeSharp.Action;


namespace Willplug.BotBehavior
{
    public static class MapPrepBehavior
    {
        private static GameController GameController = WillBot.gameController;
        private static int Latency => (int)GameController.Game.IngameState.CurLatency;
        public static WillPlayer Me { get => WillBot.Me; }
        static private readonly Stopwatch ControlTimer = new Stopwatch();

        public static int minimumMapTierToRun = 3;
        public static int maximumMapTierToRun = 14;

        public static List<List<string>> unwantedMapModifiers = new List<List<string>>() {
           new List<string>() {"reflect","elemental damage"},
           new List<string>() {"reflect","physical damage"},
           new List<string>() {"players cannot regenerate life,mana or energy shield"},
        };
        public delegate string StringDelegate(object context);
        public delegate string IntDelegate(object context);

        public delegate int RectangleFDelegate(object context);

        private static NormalInventoryItem tempMapItem;
        private static RectangleF tempMapInInventoryRectangle;
        public static Composite GetOpenAndEnterMap()
        {
            return new Sequence(
                TownBehavior.OpenStash(),
                new DecoratorContinue(delegate
                {
                    var ret = GetMapDataForValidMapInInventory();
                    if (ret.Item1 == true)
                    {
                        tempMapInInventoryRectangle = ret.Item2;
                        return false;
                    }
                    return true;
                }, new Sequence(
                        StashBehavior.SwitchToTab(x => StashBehavior.GetIndexOfMapStash()),
                        new UntilSuccess(new Sequence(
                        StashBehavior.OpenMapStashTier(x => minimumMapTierToRun, y => maximumMapTierToRun),
                        StashBehavior.GetFirstPossibleMap())))
                ),
                //As of now there might not be a valid map in invetory when you get here
                RollMapInInventoryUntilNoMoreCurrencyOrOk(),
                new Action(delegate { Thread.Sleep(200); return RunStatus.Success; }),
                new UseHotkeyAction(WillBot.KeyboardHelper, x => WillBot.Settings.CloseAllPanelsKey),
                 new Action(delegate { Thread.Sleep(500); return RunStatus.Success; }),
                TownBehavior.OpenMapDevice(),
                MoveMapFromInventoryToMapDevice(),
                ClickMapDeviceActivateButton(),
                EnterMapTownPortal()
                );
        }

        public static Composite EnterMapTownPortal()
        {
            return new Action(delegate
            {
                var townPortalRect = WillBot.Me.TownPortal.Label.GetClientRectCache;
                Mouse.SetCursorPosAndLeftOrRightClick(townPortalRect, Latency, randomClick: true);
                Thread.Sleep(5000);
                return RunStatus.Success;
            });
        }

        public static Composite MoveMapFromInventoryToMapDevice()
        {
            return new Action(delegate
            {
                if (GameController.Game.IngameState.IngameUi.MapDeviceTopLeftReceptacle?.ChildCount > 1)
                {
                    return RunStatus.Success;
                }
                var foundMap = GetMapDataForValidMapInInventory();
                if (foundMap.Item1 == false) return RunStatus.Failure;
                Input.KeyDown(Keys.LControlKey);
                Thread.Sleep(10);
                Mouse.SetCursorPosAndLeftOrRightClick(foundMap.Item2, Latency, randomClick: true);
                Thread.Sleep(15);
                Input.KeyUp(Keys.LControlKey);
                Thread.Sleep(Latency + 50);

                if (GameController.Game.IngameState.IngameUi.MapDeviceTopLeftReceptacle?.ChildCount > 1)
                {
                    return RunStatus.Success;
                }
                return RunStatus.Failure;
            });
        }

        public static Composite ClickMapDeviceActivateButton()
        {
            // Town portals can exist already
            return new Action(delegate
            {
                string previousTownPortalText = "";
                if (WillBot.Me.TownPortal != null)
                {
                    previousTownPortalText = WillBot.Me.TownPortal.Label.Text;
                }
                bool townPortalsAlreadyPresent = WillBot.Me.TownPortal != null;
                var buttonRect = GameController.Game.IngameState.IngameUi.MapDeviceAcceptButton.GetClientRectCache;
                Mouse.SetCursorPosAndLeftOrRightClick(buttonRect, Latency, randomClick: true);
                Thread.Sleep(600);
                if (string.IsNullOrEmpty(previousTownPortalText))
                {
                    ControlTimer.Restart();
                    while (ControlTimer.ElapsedMilliseconds < 12000)
                    {

                        if (WillBot.Me.TownPortal != null && WillBot.Me.TownPortal.IsVisible == true)
                        {
                            return RunStatus.Success;
                        }
                        Thread.Sleep(200);
                    }
                }
                else
                {
                    ControlTimer.Restart();
                    while (ControlTimer.ElapsedMilliseconds < 12000)
                    {
                        if (WillBot.Me.TownPortal != null && WillBot.Me.TownPortal.IsVisible == true)
                        {
                            string currentText = WillBot.Me.TownPortal.Label.Text;
                            if (previousTownPortalText != currentText || ControlTimer.ElapsedMilliseconds > 5000)
                            {
                                return RunStatus.Success;
                            }
                        }
                        Thread.Sleep(200);
                    }

                }
                return RunStatus.Failure;
            });

        }


        public static Composite RollMapInInventoryUntilNoMoreCurrencyOrOk()
        {
            return new UntilSuccess(
                new PrioritySelector(

                    new Action(delegate
                    {
                        var ret = GetMapDataForValidMapInInventory();
                        if (ret.Item1 == false) return RunStatus.Success;
                        tempMapInInventoryRectangle = ret.Item2;
                        tempMapItem = ret.Item3;
                        return RunStatus.Failure;
                    }),
                    TryIdentifyMap(),
                    ReturnSuccessIfMapReady(),
                    TryRerollMap(),
                    ReturnSuccessIfMapReady(),
                    TryScourMap(),
                    TryAlchemyMap(),
                    ReturnSuccessIfMapReady()
                    // If it cant get the map rare with ok mods, try to run magic maps?
                    )

                );
        }

        public static Composite VerifyCurrencyLeftForOneMapRollIteration()
        {
            return new Action(delegate
            {
                bool hasChaos = StashBehavior.GetCount(ItemStrings.ChaosOrb) > 0;
                bool hasScour = StashBehavior.GetCount(ItemStrings.OrbOfScouring) > 0;
                bool hasAlchemy = StashBehavior.GetCount(ItemStrings.OrbOfAlchemy) > 0;

                if (hasChaos || (hasScour && hasAlchemy))
                {
                    return RunStatus.Failure;
                }
                return RunStatus.Success;
            });
        }

        public static Composite ReturnSuccessIfMapReady()
        {
            return new Action(delegate
            {
                var mapMods = tempMapItem.Item.GetComponent<Mods>();
                bool areMapModsOk = IsMapFreeOfUnwantedMods(unwantedMapModifiers, mapMods.HumanCraftedStats);
                bool isMapRare = mapMods.ItemRarity == ItemRarity.Rare;
                bool isMapId = mapMods.Identified == true;
                if (isMapId && isMapRare && areMapModsOk)
                {
                    WillBot.LogMessageCombo("Stopping map rolling, map is ready.");
                    return RunStatus.Success;
                }
                WillBot.LogMessageCombo("Continuing map rolling. Map is not ready");
                return RunStatus.Failure;
            });
        }

        public static Composite TryIdentifyMap()
        {
            return new Decorator(delegate
            {
                var mapMods = tempMapItem.Item.GetComponent<Mods>();
                bool hasWisdomScrolls = StashBehavior.GetCount(ItemStrings.ScrollOfWisdom) > 0;
                if (mapMods.Identified == false && hasWisdomScrolls) return true;
                return false;
            }, new Inverter(ApplyCurrencyItemToItem(() => ItemStrings.ScrollOfWisdom, () => tempMapInInventoryRectangle)));
        }
        public static Composite TryAlchemyMap()
        {
            return new Decorator(delegate
            {
                var mapMods = tempMapItem.Item.GetComponent<Mods>();
                bool hasAlchemy = StashBehavior.GetCount(ItemStrings.OrbOfAlchemy) > 0;
                if (mapMods.Identified == true && mapMods.ItemRarity == ItemRarity.Normal && hasAlchemy)
                {
                    return true;
                }
                return false;
            }, new Inverter(ApplyCurrencyItemToItem(() => ItemStrings.OrbOfAlchemy, () => tempMapInInventoryRectangle)));
        }

        public static Composite TryRerollMap()
        {
            return new Decorator(delegate
            {
                var mapMods = tempMapItem.Item.GetComponent<Mods>();
                bool hasChaos = StashBehavior.GetCount(ItemStrings.ChaosOrb) > 0;
                if (mapMods.Identified == true && mapMods.ItemRarity == ItemRarity.Rare && hasChaos)
                {
                    return true;
                }
                return false;
            }, new Inverter(ApplyCurrencyItemToItem(() => ItemStrings.ChaosOrb, () => tempMapInInventoryRectangle)));
        }
        public static Composite TryScourMap()
        {
            return new Decorator(delegate
            {
                var mapMods = tempMapItem.Item.GetComponent<Mods>();
                bool hasScour = StashBehavior.GetCount(ItemStrings.OrbOfScouring) > 0;

                if (mapMods.Identified == true && mapMods.ItemRarity != ItemRarity.Normal && hasScour)
                {
                    return true;
                }
                return false;
            }, new Inverter(ApplyCurrencyItemToItem(() => ItemStrings.OrbOfScouring, () => tempMapInInventoryRectangle)));
        }

        public static bool IsMapFreeOfUnwantedMods(List<List<string>> unwantedMapModifiers, List<string> mapModifiers)
        {
            if (mapModifiers == null) return true;

            foreach (var strList in unwantedMapModifiers)
            {
                foreach (var str in mapModifiers)
                {
                    if (strList.All( x=> str.Contains(x)) == true)
                    {
                        return true;
                    }
                }
            }
            return true;
        }



        public static (bool, RectangleF, NormalInventoryItem) GetMapDataForValidMapInInventory()
        {
            var inventory = GameController.Game.IngameState.IngameUi.InventoryPanel[InventoryIndex.PlayerInventory];
            var invItems = inventory.VisibleInventoryItems;
            foreach (var item in invItems)
            {

                if (item.Item.HasComponent<Map>() == true)
                {
                    var mapComponent = item.Item.GetComponent<Map>();
                    if (mapComponent.Tier >= minimumMapTierToRun && mapComponent.Tier <= maximumMapTierToRun)
                    {
                        return (true, item.GetClientRect(), item);
                    }
                }
            }
            return (false, new RectangleF(), null);
        }




        public static Composite ApplyCurrencyItemToItem(Func<string> currencyString, Func<RectangleF> itemToEditRectInInventory)
        {
            return new Sequence(
                //Ensure currency stash is selected
                TownBehavior.OpenStash(),
                StashBehavior.SwitchToTab(x => WillBot.Settings.CurrencyStashTabIndex),
                StashBehavior.RightClickCurrencyItemInCurrencyTab(currencyString),
                StashBehavior.ClickItemInPlayerInventory(itemToEditRectInInventory),
                new Action(delegate
                {
                    Thread.Sleep(350);
                    return RunStatus.Success;
                })
                );
            // Can just use inventory coordinates for the item.
            // Can test for applied currency by checking for item on cursor
        }
        // NEED TO HANDLE CURSOR ITEM: CURSOR->ACTION (FREE/ HoldItem / UseItem 
        // If UseItem press right click to remove, if holditem -> need to place the item somewhere









    }
}
