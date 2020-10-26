using ExileCore;
using ExileCore.PoEMemory.Elements;
using ExileCore.Shared.Enums;
using SharpDX;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TreeRoutine.TreeSharp;
using Action = TreeRoutine.TreeSharp.Action;

namespace Willplug.BotBehavior
{




    static public class SellBehavior
    {
        static private LabelOnGround vendorLabelOnGround;

        static private readonly Stopwatch timer = new Stopwatch();


        static private RectangleF VendorClickPositionRect
        {
            get
            {
                return vendorLabelOnGround.Label.GetClientRectCache;
                //var gameWindowRect = WillBot.gameController.Window.GetWindowRectangle();
                //var clickPosition = gameWindowRect.TopLeft + vendorLabelOnGround.Label.GetClientRectCache.Center;
                //return clickPosition;
            }
        }



        public static bool DoSelling()
        {
            //var inventory = WillBot.gameController.Game.IngameState.IngameUi.InventoryPanel[InventoryIndex.PlayerInventory];
            //inventory.VisibleInventoryItems.Count > 8)
            bool isInHideout = WillBot.Plugin.Cache.InHideout == true;
            if (isInHideout && !WillBot.Me.HasSoldItemsThisTownCycle)
            {
                return true;
            }
            return false;
        }


        static public Composite CheckForItemsToSell()
        {
            return new Action(delegate
            {
                var inventory = WillBot.gameController.Game.IngameState.IngameUi.InventoryPanel[InventoryIndex.PlayerInventory];
                var invItems = inventory.VisibleInventoryItems;
                var itemsToSell = WillBot.Me.sellit.GetItemsToSell(invItems);
                if (itemsToSell.Count == 0)
                {


                }
            });

        }

        static public Composite OpenVendorSellScreen()
        {
            return new Action(delegate
            {
                int latency = (int)WillBot.gameController.IngameState.CurLatency;
                Mouse.SetCursorPosAndLeftOrRightClick(WillBot.gameController.IngameState.IngameUi.VendorPanelSellOption.GetClientRectCache, latency,randomClick:true);
                timer.Restart();
                while (timer.ElapsedMilliseconds < 3000 && WillBot.gameController.IngameState.IngameUi.SellWindow.IsVisible == false)
                {
                    Thread.Sleep(200);
                }
                if (WillBot.gameController.IngameState.IngameUi.SellWindow.IsVisible == false) return RunStatus.Failure;
                return RunStatus.Success;
            });
        }
        static public Composite SellItems(bool sellUnidCrItems = false)
        {

            return new Decorator(x => DoSelling(),

             new Sequence(
                 new Action(delegate
                 {
                     InputWrapper.ResetMouseButtons();
                     // InputWrapper.KeyPress(System.Windows.Forms.Keys.Escape);
                     return RunStatus.Success;
                 }),
                 FindVendor(),
                 MoveToAndOpenVendor(),
                OpenVendorSellScreen(),

                 new Action(delegate
                 {
                     var inventory = WillBot.gameController.Game.IngameState.IngameUi.InventoryPanel[InventoryIndex.PlayerInventory];
                     var invItems = inventory.VisibleInventoryItems;
                     var itemsToSell = WillBot.Me.sellit.GetItemsToSell(invItems, sellUnidCrItems);
                     if (itemsToSell.Count == 0)
                     {
                         WillBot.Me.HasSoldItemsThisTownCycle = true;
                         InputWrapper.KeyPress(System.Windows.Forms.Keys.Escape);
                         Thread.Sleep(200);
                         if (WillBot.gameController.IngameState.IngameUi.VendorPanel.IsVisible)
                         {
                             InputWrapper.KeyPress(System.Windows.Forms.Keys.Escape);
                         }

                         return RunStatus.Success;
                     }
                     int latency = (int)WillBot.gameController.IngameState.CurLatency;
                     Input.KeyDown(System.Windows.Forms.Keys.LControlKey);
                     foreach (var item in itemsToSell)
                     {
                         Mouse.SetCursorPosAndLeftOrRightClick(item.Rect, latency,randomClick:true);
                         Thread.Sleep(200);
                     }

                     Input.KeyUp(System.Windows.Forms.Keys.LControlKey);

                     var acceptButton = WillBot.gameController.IngameState.IngameUi.VendorAcceptButton;
                     //var acceptButton = WillBot.gameController.IngameState.IngameUi.SellWindow.GetChildAtIndex(3).GetChildAtIndex(5);
                     Mouse.SetCursorPosAndLeftOrRightClick(acceptButton.GetClientRectCache, latency, randomClick: true);

                     Thread.Sleep(900);
                     if (WillBot.gameController.IngameState.IngameUi.VendorPanel.IsVisible)
                     {
                         InputWrapper.KeyPress(System.Windows.Forms.Keys.Escape);
                     }
                     WillBot.Me.HasSoldItemsThisTownCycle = true;
                     WillBot.Me.IsPlayerInventoryFull = false;
                     return RunStatus.Success;
                 })

                 ));

        }




        public static Composite MoveToAndOpenVendor()
        {
            return new PrioritySelector(
                   new Decorator(x => vendorLabelOnGround.ItemOnGround.DistancePlayer < 70,
                   ClickVendorLabelAction()),
                   CommonBehavior.MoveTo(x => vendorLabelOnGround.ItemOnGround.GridPos, spec: CommonBehavior.DefaultMovementSpec),
                    ClickVendorLabelAction()
                   );
        }

        private static Action ClickVendorLabelAction()
        {
            return new Action(delegate
            {
                int latency = (int)WillBot.gameController.IngameState.CurLatency;
                Mouse.SetCursorPosAndLeftOrRightClick(VendorClickPositionRect, latency,randomClick:true);
                timer.Restart();
                while (timer.ElapsedMilliseconds < 4000 && WillBot.gameController.IngameState.IngameUi.VendorPanelSellOption == null)
                {
                    Thread.Sleep(200);
                }
                if (WillBot.gameController.IngameState.IngameUi.VendorPanelSellOption == null)
                {
                    return RunStatus.Failure;
                }
                return RunStatus.Success;
            });
        }

        public static Composite FindVendor()
        {
            return new PrioritySelector(
                new Action(delegate
                {
                    if (WillBot.Me.ZanaHideoutVendor == null)
                    {
                        return RunStatus.Failure;
                    }
                    vendorLabelOnGround = WillBot.Me.ZanaHideoutVendor;
                    return RunStatus.Success;
                }),
                new Action(delegate
                {
                    if (WillBot.Me.NavaliHideoutVendor == null)
                    {
                        return RunStatus.Failure;
                    }
                    vendorLabelOnGround = WillBot.Me.NavaliHideoutVendor;
                    return RunStatus.Success;
                }));
        }


    }





}
