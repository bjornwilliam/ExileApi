using ExileCore;
using ExileCore.Shared.Enums;
using SharpDX;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using TreeRoutine.TreeSharp;
using Action = TreeRoutine.TreeSharp.Action;
using ExileCore.PoEMemory.Elements.InventoryElements;
using ExileCore.PoEMemory.Components;

namespace Willplug.BotBehavior.Town
{

    // Look at the naming convention to look for simplifications
    public enum Currency
    {
        OrbOfAlteration,
        ChaosOrb,

    }
    public static class StashBehavior
    {



        static private readonly Stopwatch ControlTimer = new Stopwatch();

        private static GameController GameController = WillBot.gameController;

        private static int stashcount = 0;
        private static int visibleStashIndex = -1;
        private const int MAXSHOWN_SIDEBARSTASHTABS = 32;
        private static int travelDistance = 0;

        public delegate string StringDelegate(object context);
        public delegate int StashIndexDelegate(object context);
        public delegate int IntDelegate(object context);

        private static int Latency => (int)GameController.Game.IngameState.CurLatency;


        public static int GetCount(string currencyString)
        {
            var ret = GetRectangleFForCurrencyItem(currencyString);
            if (ret.Item1 == false) return 0;
            return GetCount(ret.Item3);
        }
        public static int GetCount(ExileCore.PoEMemory.Elements.InventoryElements.NormalInventoryItem item)
        {
            string itemCountString = item?.GetChildAtIndex(0)?.Text;
            if (string.IsNullOrEmpty(itemCountString)) return 0;
            int count = 0;
            bool didParse = int.TryParse(itemCountString, out count);
            if (didParse == true) return count;
            return 0;
        }




        public static Composite ClickItemInPlayerInventory(Func<RectangleF> itemRectangleF)
        {
            return new Action(delegate
            {
                Mouse.SetCursorPosAndLeftOrRightClick(itemRectangleF(), 200);
                return RunStatus.Success;
            });
        }

        public static Composite RightClickCurrencyItemInCurrencyTab(Func<string> currencyString)
        {
            return new Action(delegate (object context)
            {

                var ret = GetRectangleFForCurrencyItem(currencyString());
                if (ret.Item1 == true)
                {
                    Mouse.SetCursorPosAndLeftOrRightClick(ret.Item2, 100, clickType: Mouse.MyMouseClicks.RightClick);
                    ControlTimer.Restart();
                    while (ControlTimer.ElapsedMilliseconds < 2000 && GameController?.Game?.IngameState?.IngameUi?.Cursor?.ChildCount == 0)
                    {
                        Thread.Sleep(50);
                    }
                    if (GameController?.Game?.IngameState?.IngameUi?.Cursor?.ChildCount == 1)
                    {
                        return RunStatus.Success;
                    }
                    else
                    {
                        return RunStatus.Failure;
                    }
                }
                return RunStatus.Failure;
            });
        }

        public static (bool, RectangleF, NormalInventoryItem) GetRectangleFForCurrencyItem(string currencyItemString)
        {
            try
            {
                var currencyStashTabItems = GameController?.Game?.IngameState?.IngameUi?.StashElement?.AllInventories[WillBot.Settings.CurrencyStashTabIndex]?.VisibleInventoryItems;
                if (currencyStashTabItems == null || currencyStashTabItems.Count == 0) return (false, new RectangleF(), null);
                //var currencyItem = currencyStashTabItems.Where(x => x?.Item?.Path?.Contains(currencyItemString) == true)?.FirstOrDefault();
                var currencyItem = currencyStashTabItems.Where(x => x?.Item?.GetComponent<Base>()?.Name?.ToLower()?.Contains(currencyItemString) == true)?.FirstOrDefault();
                if (currencyItem != null)
                {
                    return (true, currencyItem.GetClientRect(), currencyItem);
                }
                return (false, new RectangleF(), null);
            }
            catch (Exception ex)
            {
                WillBot.LogMessageCombo(ex.ToString());
                return (false, new RectangleF(), null);
            }
        }





        public static Composite OpenMapStashTier(IntDelegate minimumTierToRunDelegate, IntDelegate maximumTierToRunDelegate)
        {
            return new Action(delegate (object context)
            {
                Thread.Sleep(300);
                int minimumMapTier = minimumTierToRunDelegate(context);
                int maximumMapTier = maximumTierToRunDelegate(context);
                // Amount of maps in tier is at: tierbar-> child(x) -> child(1) -> child(0).Text

                for (int i = minimumMapTier; i <= maximumMapTier; i++)
                {
                    int index = i > 9 ? i - 10 : i - 1;
                    var validTierBar = i > 9 ? GameController.IngameState.IngameUi.MapStashBottomTierBar : GameController.IngameState.IngameUi.MapStashTopTierBar;
                    if (validTierBar?.IsVisible == false)
                    {
                        WillBot.LogMessageCombo($"tier bar is null or is visible == false");
                        return RunStatus.Failure;
                    }
                    int amountOfMapsForCurrentTier = 0;

                    int.TryParse(validTierBar.GetChildAtIndex(index)?.GetChildAtIndex(1)?.GetChildAtIndex(0)?.Text ?? "0", out amountOfMapsForCurrentTier);

                    if (amountOfMapsForCurrentTier != 0)
                    {
                        var tierButtonRect = validTierBar?.GetChildAtIndex(index)?.GetChildAtIndex(2)?.GetClientRect();
                        if (tierButtonRect != null)
                        {
                            WillBot.LogMessageCombo($"Trying to open map stash tier { i}");
                            Mouse.SetCursorPosAndLeftOrRightClick((RectangleF)tierButtonRect, Latency);
                            Thread.Sleep(400);
                            return RunStatus.Success;
                        }

                    }
                }
                return RunStatus.Failure;
            });
        }

        private static void EnsureMapStashMapButtonVisible(RectangleF mapButtonRect)
        {
            if (GameController.IngameState.IngameUi.MapStashMapIconsBoxVisibleTwoMapHeight.GetClientRect().Contains(mapButtonRect.Center))
            {
                return;
            }
            else
            {
                var rect = GameController.IngameState.IngameUi.MapStashMapIconsBoxVisibleTwoMapHeight.GetClientRect();
                Mouse.SetCursorPosAndLeftOrRightClick(rect, 200, clickType: Mouse.MyMouseClicks.NoClick);
                //Mouse.MoveCursorToPosition(GameController.IngameState.IngameUi.MapStashMapIconsBoxVisibleTwoMapHeight.GetClientRectCache.Center);
                Thread.Sleep(200);
                var mapStashBox = GameController.IngameState.IngameUi.MapStashMapIconsBoxVisibleTwoMapHeight.GetClientRect();
                if (mapButtonRect.TopLeft.Y < mapStashBox.TopLeft.Y)
                {
                    // Map is located in upwards scroll
                    int numberOfScrolls = (int)Math.Ceiling(Math.Abs((mapButtonRect.TopLeft.Y - mapStashBox.TopLeft.Y) / mapButtonRect.Height));
                    Mouse.VerticalScroll(true, numberOfScrolls);
                }
                else if (mapButtonRect.BottomLeft.Y > mapStashBox.BottomLeft.Y)
                {
                    // Map is located downards scroll
                    int numberOfScrolls = (int)Math.Ceiling(Math.Abs((mapButtonRect.BottomLeft.Y - mapStashBox.BottomLeft.Y) / mapButtonRect.Height));
                    Mouse.VerticalScroll(false, numberOfScrolls);
                }
                Thread.Sleep(200);
            }
        }

        public static Composite GetFirstPossibleMap()
        {

            return new Action(delegate
            {
                WillBot.LogMessageCombo("Trying to get first possible map");
                int index = GetIndexOfFirstMapwithMaps(GameController.IngameState.IngameUi.MapStashMapIconsBox.Children);
                if (index == -1)
                {
                    WillBot.LogMessageCombo($"No maps. Tier finding function must have failed.");
                    Thread.Sleep(200);
                    return RunStatus.Failure;

                }
                var mapButtonRect = GameController.IngameState.IngameUi.MapStashMapIconsBox.GetChildAtIndex(index)?.GetChildAtIndex(0)?.GetChildAtIndex(0)?.GetClientRect();
                WillBot.LogMessageCombo($"Found map button rect: {mapButtonRect}");
                if (mapButtonRect == null)
                {
                    return RunStatus.Failure;
                }
                else
                {
                    EnsureMapStashMapButtonVisible((RectangleF)mapButtonRect);
                    Mouse.SetCursorPosAndLeftOrRightClick((RectangleF)mapButtonRect, Latency, randomClick: true);
                }
                Thread.Sleep(600);

                var mapRect = GameController.IngameState.IngameUi?.StashElement?.VisibleStash?.VisibleInventoryItems?.FirstOrDefault()?.GetClientRect();
                if (mapRect == null)
                {
                    return RunStatus.Failure;
                }
                Input.KeyDown(Keys.LControlKey);
                Thread.Sleep(10);
                Mouse.SetCursorPosAndLeftOrRightClick((RectangleF)mapRect, Latency, randomClick: true);
                Thread.Sleep(15);
                Input.KeyUp(Keys.LControlKey);
                Thread.Sleep(350);
                return RunStatus.Success;
            });
        }


        public static int GetIndexOfFirstMapwithMaps(IList<ExileCore.PoEMemory.Element> mapIcons)
        {
            for (int i = 0; i < mapIcons.Count; i++)
            {
                int numberOfMaps = -1;
                var foundNumber = int.TryParse(mapIcons[i]?.GetChildAtIndex(0)?.GetChildAtIndex(3)?.Text ?? "-1", out numberOfMaps);
                if (foundNumber == true && numberOfMaps > 0)
                {
                    return i;
                }
            }
            return -1;
        }

        public static int GetIndexOfMapStash()
        {
            var stashPanel = GameController.Game.IngameState?.IngameUi?.StashElement;
            var realNames = stashPanel.AllStashNames;
            var index = realNames.IndexOf("M");
            return index;
        }

        public static int GetIndexOfStashTabWithName(string name)
        {
            var stashPanel = GameController.Game.IngameState?.IngameUi?.StashElement;
            var realNames = stashPanel.AllStashNames;
            var index = realNames.IndexOf(name);
            return index;
        }
        public static int GetIndexOfStashTabWithType(InventoryType type)
        {
            // This will only work if the stash tab has been opened once before so that the stash tab is not null
            var stashInventories = GameController.Game.IngameState?.IngameUi?.StashElement?.AllInventories;
            if (stashInventories == null || stashInventories.Count == 0) return -1;
            for (int i = 0; i < stashInventories.Count; i++)
            {
                if (stashInventories[i]?.InvType == type)
                {
                    return i;
                }
            }
            return -1;
        }

        public static Composite SwitchToTab(StashIndexDelegate stashIndexDelegate)
        {
            return new DecoratorContinue(delegate (object context)
            {

                visibleStashIndex = GetIndexOfCurrentVisibleTab();
                var tabIndex = stashIndexDelegate(context);
                WillBot.LogMessageCombo($"Switching to stash tab index: {tabIndex} from index {visibleStashIndex}");
                travelDistance = Math.Abs(tabIndex - visibleStashIndex);
                if (travelDistance == 0)
                {
                    WillBot.LogMessageCombo("Already on correct stash tab");
                    return false;
                }
                return true;
            }, new Sequence(

            //new Decorator(delegate (object context)
            //    {
            //        var tabIndex = stashIndexDelegate(context);
            //        travelDistance = Math.Abs(tabIndex - visibleStashIndex);
            //        if (travelDistance > 3)
            //        {
            //            WillBot.LogMessageCombo("Switching with dropdown menu");
            //            return true;
            //        }
            //        WillBot.LogMessageCombo("Switching with arrow keys");
            //        return false;


            //    }, SwitchToTabViaDropdownMenu(stashIndexDelegate)),
            SwitchToTabViaArrowKeys(stashIndexDelegate),
            new Action(delegate
            {
                Thread.Sleep(500);
                return RunStatus.Success;
            }))
            );
        }


        private static Composite SwitchToTabViaArrowKeys(StashIndexDelegate stashIndexDelegate)
        {

            // Need to ensure that the all stash tab list is not visible. If its visible then arrow keys wont change tab
            return new Sequence(
                //new Action(delegate
                //{
                //    var viewAllTabsButton = GameController.Game.IngameState.IngameUi.StashElement.ViewAllStashButton;
                //    var dropdownMenu = GameController.Game.IngameState.IngameUi.StashElement.ViewAllStashPanel;
                //    var allTabsButton = viewAllTabsButton.GetClientRect();
                //    if (dropdownMenu.IsVisible)
                //    {
                //        Thread.Sleep(40);
                //        Mouse.SetCursorPosAndLeftOrRightClickInRectRandomly(allTabsButton, Latency);
                //        Thread.Sleep(100);
                //    }
                //    return RunStatus.Success;
                //}),
                new Action(delegate (object context)
            {
                var indexOfCurrentVisibleTab = GetIndexOfCurrentVisibleTab();
                var tabIndex = stashIndexDelegate(context);
                var difference = tabIndex - indexOfCurrentVisibleTab;
                var tabIsToTheLeft = difference < 0;
                var retry = 0;
                while (GetIndexOfCurrentVisibleTab() != tabIndex && retry < 3)
                {
                    for (var i = 0; i < Math.Abs(difference); i++)
                    {
                        Input.KeyDown(tabIsToTheLeft ? Keys.Left : Keys.Right);
                        Input.KeyUp(tabIsToTheLeft ? Keys.Left : Keys.Right);
                        Thread.Sleep(30);
                    }
                    Thread.Sleep(30);
                    retry++;
                }
                if (GetIndexOfCurrentVisibleTab() == tabIndex)
                {
                    return RunStatus.Success;
                }
                return RunStatus.Failure;
            }));


        }


        private static Composite SwitchToTabViaDropdownMenu(StashIndexDelegate stashIndexDelegate)
        {

            return new Action(delegate (object context)
            {
                var tabIndex = stashIndexDelegate(context);
                var viewAllTabsButton = GameController.Game.IngameState.IngameUi.StashElement.ViewAllStashButton;
                var dropdownMenu = GameController.Game.IngameState.IngameUi.StashElement.ViewAllStashPanel;
                var allTabsButton = viewAllTabsButton.GetClientRect();
                var slider = stashcount > MAXSHOWN_SIDEBARSTASHTABS;
                if (!dropdownMenu.IsVisible)
                {
                    Thread.Sleep(100);
                    Mouse.SetCursorPosAndLeftOrRightClick(allTabsButton, Latency);
                    ControlTimer.Restart();
                    while (ControlTimer.ElapsedMilliseconds < 1500 && !dropdownMenu.IsVisible)
                    {
                        //WillBot.LogMessageCombo("Waiting for dropdownmenu visible. Waited for {0}", ControlTimer.ElapsedMilliseconds);
                        Thread.Sleep(50);
                    }
                    //wait for the dropdown menu to become visible
                    if (!dropdownMenu.IsVisible)
                    {
                        WillBot.basePlugin.LogError($"Error in opening DropdownMenu.", 5);
                        return RunStatus.Failure;
                    }
                }
                RectangleF tabPos;
                // Make sure that we are scrolled to the top in the menu.
                if (slider)
                {
                    for (int i = 0; i < stashcount - MAXSHOWN_SIDEBARSTASHTABS + 1; ++i)
                    {
                        Input.KeyDown(Keys.Left);
                        Thread.Sleep(1);
                        Input.KeyUp(Keys.Left);
                        Thread.Sleep(1);
                    }
                }
                //get clickposition of tab label
                for (int i = 0; i <= tabIndex; i++)
                {
                    Input.KeyDown(Keys.Right);
                    Thread.Sleep(2);
                    Input.KeyUp(Keys.Right);
                    Thread.Sleep(2);
                }
                //enter-key Method  (There is a bug where the stash rect does not highlight (not possible to enter press))
                //Input.KeyDown(Keys.Enter);
                //Thread.Sleep(1);
                //Input.KeyUp(Keys.Enter);
                //Thread.Sleep(1);

                //reset Sliderposition
                if (slider)
                {
                    if (!dropdownMenu.IsVisible)
                    {
                        //opening DropdownMenu
                        Input.SetCursorPos(allTabsButton.Center);
                        Thread.Sleep(10);
                        Input.MouseMove();
                        Input.LeftDown();
                        Thread.Sleep(1);
                        Input.LeftUp();
                        Thread.Sleep(10);
                        // wait for the dropdown menu to become visible.
                        for (int count = 0; !dropdownMenu.IsVisible && count <= 20; ++count)
                        {
                            Thread.Sleep(50);
                        }
                        if (!dropdownMenu.IsVisible)
                        {
                            WillBot.basePlugin.LogError($"Error in Scrolling back to the top.", 5);
                            return RunStatus.Failure;
                        }
                    }
                    //"scrolling" back up
                    for (int i = 0; i < stashcount - MAXSHOWN_SIDEBARSTASHTABS + 1; ++i)
                    {
                        Input.KeyDown(Keys.Left);
                        Thread.Sleep(1);
                        Input.KeyUp(Keys.Left);
                        Thread.Sleep(1);
                    }
                }
                if (GetIndexOfCurrentVisibleTab() == tabIndex)
                {
                    WillBot.LogMessageCombo("Switched to correct tab via drop down");
                    return RunStatus.Success;
                }
                return RunStatus.Failure;
            });
        }


        public static int GetIndexOfCurrentVisibleTab()
        {
            return GameController.Game.IngameState.IngameUi.StashElement.IndexVisibleStash;
        }

        private static InventoryType GetTypeOfCurrentVisibleStash()
        {
            var stashPanelVisibleStash = GameController.Game.IngameState.IngameUi?.StashElement?.VisibleStash;
            if (stashPanelVisibleStash != null) return stashPanelVisibleStash.InvType;

            return InventoryType.InvalidInventory;
        }


    }
}
