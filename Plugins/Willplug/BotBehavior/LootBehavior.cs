using System;
using ExileCore.PoEMemory.Components;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TreeRoutine.TreeSharp;
using Action = TreeRoutine.TreeSharp.Action;
using Input = ExileCore.Input;
using System.Threading;
using System.Diagnostics;
using SharpDX;
using ExileCore.Shared.Helpers;
using ExileCore;

namespace Willplug.BotBehavior
{
    public static class LootBehavior
    {
        public static WillPlayer Me { get => WillBot.Me; }

        private static readonly Stopwatch ControlTimer = new Stopwatch();

        private static GameController GameController = WillBot.gameController;
        private static int Latency => (int)GameController.IngameState.CurLatency;


        private static void TrackAndClickLoot(CustomItem item)
        {
            if (item?.LabelOnGround?.Label != null)
            {
                var itemRect = item.LabelOnGround.Label.GetClientRect();
                var gameWindowRect = GameController.Window.GetWindowRectangle();
                var clickPos = gameWindowRect.TopLeft + itemRect.Center;
                Mouse.MoveCursorToPosition(clickPos);
                if (item.IsTargeted())
                {
                    Mouse.LeftClick(1);
                    WillBot.LogMessageCombo("Clicking targeted loot");    
                }
            }

        }

        public static Composite TryToPickLootCloseToPlayer()
        {
            return new Action(delegate
            {

                var itemToLoot = Me.ClosestItemToLoot;
                if (itemToLoot == null) return RunStatus.Failure;
                if (BotInventory.IsThereRoomForItem(itemToLoot) == false)
                {
                    WillBot.LogMessageCombo("Cant pick up item");

                    Me.IsPlayerInventoryFull = true;
                    return RunStatus.Failure;
                }
                Mouse.SetCursorPosAndLeftOrRightClick(itemToLoot.LabelOnGround, Latency, useCache: false, randomClick: false);
                ControlTimer.Restart();
                var labelToCheck = Me.ClosestItemToLoot?.LabelOnGround;
                while (ControlTimer.ElapsedMilliseconds < 400)
                {
                    if (WillBot.gameController.Game.IngameState.IngameUi.ItemsOnGroundLabels.FirstOrDefault(
                      x => x.Address == labelToCheck?.Address) == null)
                    {
                        return RunStatus.Failure;
                    }
                    TrackAndClickLoot(itemToLoot);
                    Thread.Sleep(20);
                }
                return RunStatus.Failure;
            });

        }


        public static Composite BuildLootComposite()
        {
            return new Decorator(ret => CommonBehavior.DoLooting(),
                new Sequence(
                  //     new DecoratorContinue(delegate
                  //     {
                  //         if (Me.ClosestItemToLoot?.GridPos.Distance(WillBot.gameController.Player.GridPos) > 18)
                  //         {
                  //             return true;
                  //         }
                  //         return false;
                  //     },
                  //     new Inverter(CommonBehavior.MoveTo2(x => Me.ClosestItemToLoot?.GridPos ?? WillBot.gameController.Player.GridPos,
                  //y => WillBot.Me.ClosestItemToLoot?.LabelOnGround?.ItemOnGround?.Pos ?? WillBot.gameController.Player.Pos, CommonBehavior.LootingMovementSpec))),
                  //TryToPickLootCloseToPlayer()
                  new Inverter(CommonBehavior.MoveTo(x => Me.ClosestItemToLoot?.GridPos,
                  y => WillBot.Me.ClosestItemToLoot?.LabelOnGround?.ItemOnGround?.Pos, CommonBehavior.LootingMovementSpec)),

                  TryToPickLootCloseToPlayer()

                //    NavigateUntilVeryCloseDueToObstacles(),

                //  new Inverter(CommonBehavior.MoveTo(x => Me.ClosestItemToLoot?.GridPos ?? WillBot.gameController.Player.GridPos,
                //  y => WillBot.Me.ClosestItemToLoot?.LabelOnGround?.ItemOnGround?.Pos ?? WillBot.gameController.Player.Pos,
                //  cancelForLoot: false, cancelInRange: true, cancelRange: 18))),
                //TryToPickLootCloseToPlayer()

                ));

        }
    }
}
