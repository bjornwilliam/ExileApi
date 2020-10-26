using ExileCore;
using ExileCore.PoEMemory.Components;
using ExileCore.PoEMemory.Elements.InventoryElements;
using ExileCore.PoEMemory.MemoryObjects;
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
using Willplug.SellItems;
using Action = TreeRoutine.TreeSharp.Action;

namespace Willplug.BotBehavior.Town
{
    public static class TownBehavior
    {
        static private GameController GameController = WillBot.gameController;

        static private readonly Stopwatch ControlTimer = new Stopwatch();
        static private int numberOfItemsInInventoryBeforeRunningStashing = 0;



        public static NormalInventoryItem tempUniqueItemInInventory;
        public static Composite IdentifyUniquesInPlayerInventory()
        {
            // Until_x_  return success . Watch out in priorityselectors

            return new Inverter(new UntilFailure(
                new Sequence(
                    OpenStash(),
                    new Action(delegate
                    {
                        var ret = TryGetDataForUnidUniqueInInventory();
                        if (ret.Item1 == false)
                        {
                            WillBot.LogMessageCombo("Unable to find more unique items to identify");
                            return RunStatus.Failure;
                        }
                        tempUniqueItemInInventory = ret.Item3;
                        return RunStatus.Success;
                    }),
                    MapPrepBehavior.ApplyCurrencyItemToItem(() => ItemStrings.ScrollOfWisdom, () => tempUniqueItemInInventory.GetClientRect()),
                    new Action(delegate
                    {
                        Thread.Sleep(300);
                        return RunStatus.Success;
                    })
                    )

                ));


        }
        public static (bool, RectangleF, NormalInventoryItem) TryGetDataForUnidUniqueInInventory()
        {
            var inventory = GameController.Game.IngameState.IngameUi.InventoryPanel[InventoryIndex.PlayerInventory];
            var invItems = inventory.VisibleInventoryItems;
            foreach (var item in invItems)
            {
                var itemMods = item?.Item?.GetComponent<Mods>();
                if (itemMods != null && itemMods.ItemRarity == ItemRarity.Unique && itemMods.Identified == false)
                {
                    return (true, item.GetClientRect(), item);
                }
            }
            return (false, new RectangleF(), null);
        }



        public static void TestEnterBloodAqua()
        {
            var gameWindowRect = GameController.Window.GetWindowRectangle();
            int latency = (int)GameController.IngameState.CurLatency;
            RectangleF waypointRect = new RectangleF();
            foreach (var itemOnGround in GameController.Game.IngameState.IngameUi.ItemsOnGroundLabels)
            {
                if (itemOnGround.Label != null && itemOnGround.Label.ChildCount > 0)
                {

                    if (itemOnGround?.Label?.GetChildAtIndex(0)?.Text?.ToLower().Contains("waypoint") == true)
                    {
                        waypointRect = itemOnGround.Label.GetClientRectCache;
                        break;
                    }

                }
            }
            Mouse.SetCursorPosAndLeftOrRightClick(waypointRect, latency, randomClick: true);
            while (GameController.IngameState.IngameUi.WorldMapPart2.IsVisible == false)
            {
                Thread.Sleep(50);
            }
            Thread.Sleep(200);
            Mouse.SetCursorPosAndLeftOrRightClick(GameController.IngameState.IngameUi.WorldMapAct9Rect.GetClientRectCache, latency, randomClick: true);
            Thread.Sleep(300);
            Mouse.SetCursorPosAndLeftOrRightClick(GameController.IngameState.IngameUi.WorldMapPart2.GetClientRectCache, latency, randomClick: true);

            Thread.Sleep(200);
            var wp = GameController.IngameState.IngameUi.WorldMapBloodAquaductsWaypoint;
            // I only have topleft of the waypoint.
            // Add an offset
            SharpDX.Vector2 mapWaypointOffset = new SharpDX.Vector2(25, 25);
            var clickPoint = gameWindowRect.TopLeft + wp.GetClientRectCache.TopLeft + mapWaypointOffset;
            Input.KeyDown(Keys.LControlKey);
            Mouse.SetCursorPosAndLeftOrRightClick(clickPoint, latency);
            Input.KeyUp(Keys.LControlKey);

            Thread.Sleep(500);
            // Press new button


            Mouse.SetCursorPosAndLeftOrRightClick(WillBot.Me.AreaInstanceNewButton.GetClientRectCache, latency, randomClick: true);

        }
        public static Composite EnterBloodAquaducts()
        {
            // Find waypoint -> navigate to waypoint-> 
            return new Sequence(
                CommonBehavior.CloseOpenPanels(),
                new Action(delegate
                {
                    WillBot.Mover.RemoveNavigationDataForCurrentZone();
                    TestEnterBloodAqua();
                    Thread.Sleep(4000);
                    return RunStatus.Success;
                })
                );
        }


        public static Composite OpenMapDevice()
        {
            return new PrioritySelector(
        new Action(delegate
        {
            if (GameController.IngameState.IngameUi.MapDevice.IsVisible == false)
            {
                WillBot.LogMessageCombo($"Map device is not visible");
                return RunStatus.Failure;
            }
            WillBot.LogMessageCombo($"Map device is visible");
            return RunStatus.Success;
        }),
 new Decorator(x => WillBot.Me.MapDeviceLabelOnGround.ItemOnGround.DistancePlayer > 60,
                      CommonBehavior.MoveTo(x => WillBot.Me.MapDeviceLabelOnGround.ItemOnGround.GridPos,spec: CommonBehavior.DefaultMovementSpec)),
                     ClickMapDevice()
    );

        }

        public static Composite ClickMapDevice()
        {
            return new Action(
                delegate
                {
                    Thread.Sleep(400); // If moving, wait to stop
                    int latency = (int)GameController.IngameState.CurLatency;
                    Mouse.SetCursorPosAndLeftOrRightClick(WillBot.Me.MapDeviceLabelOnGround.Label.GetClientRect(), latency);
                    ControlTimer.Restart();
                    while (ControlTimer.ElapsedMilliseconds < 3000 && WillBot.gameController.IngameState.IngameUi.MapDevice.IsVisible == false)
                    {
                        Thread.Sleep(200);
                    }
                    if (WillBot.gameController.IngameState.IngameUi.MapDevice.IsVisible == true)
                    {
                        WillBot.LogMessageCombo($"Successfully clicked mapdevice");
                        return RunStatus.Success;
                    }
                    return RunStatus.Failure;
                }
                );
        }


        public static bool DoStashing()
        {
            bool isInHideout = WillBot.Plugin.Cache.InHideout == true;
            if (isInHideout && !WillBot.Me.HasStashedItemsThisTownCycle)
            {
                return true;
            }
            return false;
        }

        public static Composite OpenStash()
        {
            // Do a minimum sleep of x seconds here
            // due to some issues with memory loading stash 
            return new PrioritySelector(
                    new Action(delegate
                    {
                        if (GameController.IngameState.IngameUi.StashElement?.IsVisible == false)
                        {
                            WillBot.LogMessageCombo($"Stash element is not visible");
                            return RunStatus.Failure;
                        }
                        WillBot.LogMessageCombo($"Stash element is visible");
                        Thread.Sleep(1000);
                        return RunStatus.Success;
                    }),
                    new Decorator(x => WillBot.Me.StashLabel.ItemOnGround.DistancePlayer > 60,
                        CommonBehavior.MoveTo(x => WillBot.Me.StashLabel.ItemOnGround.GridPos, spec:CommonBehavior.DefaultMovementSpec)),
                      ClickStash(),
                      new Action(delegate
                      {
                          Thread.Sleep(2000);
                          return RunStatus.Success;
                      })
                );

        }
        public static Composite ClickStash()
        {
            return new Action(
                delegate
                {

                    int latency = (int)GameController.IngameState.CurLatency;
                    Mouse.SetCursorPosAndLeftOrRightClick(WillBot.Me.StashLabel.Label.GetClientRect(), latency);
                    ControlTimer.Restart();
                    while (ControlTimer.ElapsedMilliseconds < 4000 && WillBot.gameController.IngameState.IngameUi.StashElement.IsVisible == false)
                    {
                        Thread.Sleep(200);
                    }
                    if (WillBot.gameController.IngameState.IngameUi.StashElement.IsVisible == true)
                    {
                        WillBot.LogMessageCombo($"Clicked stash. StashElement is visible");
                        return RunStatus.Success;
                    }
                    WillBot.LogMessageCombo($"Unable to click stash. Stashelement is not visible");
                    return RunStatus.Failure;
                }
                );
        }

        public static Composite Stashie()
        {
            return new Decorator(x => DoStashing(),
                new Sequence(
                OpenStash(),
                new Action(delegate
                {
                   // Thread.Sleep(3000);
                    var inventory = WillBot.gameController.Game.IngameState.IngameUi.InventoryPanel[InventoryIndex.PlayerInventory];
                    var invItems = inventory.VisibleInventoryItems;

                    numberOfItemsInInventoryBeforeRunningStashing = (invItems == null) ? 0 : invItems.Count;
                    WillBot.Me.StashieHasCompletedStashing = false;
                    WillBot.LogMessageCombo($"Stashie: getting ready to press stashie hotkey");

                    //InputWrapper.KeyPress(Keys.F6);
                    //InputWrapper.KeyPress(Keys.F6);
                    Input.KeyDown(Keys.F6);
                    Thread.Sleep(40);
                    Input.KeyUp(Keys.F6);
                    ControlTimer.Restart();
                    return RunStatus.Success;

                }),
                new Action(delegate
                {
                    // Just set a timer ?                      
                    while (ControlTimer.ElapsedMilliseconds < 15000 && WillBot.Me.StashieHasCompletedStashing == false)
                    {
                        return RunStatus.Running;
                    }
                    Thread.Sleep(1000);
                    Input.KeyUp(Keys.LControlKey);
                    var inventory = WillBot.gameController.Game.IngameState.IngameUi.InventoryPanel[InventoryIndex.PlayerInventory];
                    var invItems = inventory.VisibleInventoryItems;

                    int numberOfInventoryItemsLeft = invItems == null ? 0 : invItems.Count;
                    WillBot.Me.HasStashedItemsThisTownCycle = true;
                    return RunStatus.Success;
                })
                ));
        }

    }
}
