using ExileCore;
using ExileCore.PoEMemory.MemoryObjects;
using ExileCore.Shared.Cache;
using ExileCore.Shared.Enums;
using ExileCore.Shared.Helpers;
using SharpDX;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using TreeRoutine.DefaultBehaviors.Actions;
using TreeRoutine.DefaultBehaviors.Helpers;
using TreeRoutine.TreeSharp;
using Willplug.BotBehavior;
using Willplug.Navigation;
using Action = TreeRoutine.TreeSharp.Action;

namespace Willplug
{
    public enum CancelReason
    {
        None,
        PathDistanceLessThanRange,
        PathDistanceLessThanRangeIfStraightPath,
    }
    public class MovementSpec
    {
        public MovementSpec(bool cancelForOpenables, bool cancelForMonster, bool cancelForLoot, CancelReason cancelReason, float cancelRange)
        {
            this.cancelForOpenables = cancelForOpenables;
            this.cancelForMonster = cancelForMonster;
            this.cancelForLoot = cancelForLoot;
            this.cancelReason = cancelReason;
            this.cancelRange = cancelRange;
            this.cancelRangeSquared = cancelRange * cancelRange;
        }
        public bool cancelForOpenables = false;
        public bool cancelForMonster = true;
        public bool cancelForLoot = true;
        public CancelReason cancelReason = CancelReason.None;
        public float cancelRange = 30f;
        public float cancelRangeSquared;
    }

    public static class CommonBehavior
    {

        static int Latency => (int)GameController.IngameState.CurLatency;
        static private readonly Stopwatch ControlTimer = new Stopwatch();
        static private GameController GameController = WillBot.gameController;

        static public MovementSpec LootingMovementSpec = new MovementSpec(cancelForOpenables: false, cancelForMonster: true, cancelForLoot: false,
            cancelReason: CancelReason.PathDistanceLessThanRangeIfStraightPath, 25);
        static public MovementSpec DefaultMovementSpec = new MovementSpec(cancelForOpenables: false, cancelForMonster: true, cancelForLoot: false,
            cancelReason: CancelReason.PathDistanceLessThanRangeIfStraightPath, 30);
        static public MovementSpec OpenablesMovementSpec = new MovementSpec(cancelForOpenables: false, cancelForMonster: true, cancelForLoot: true,
            cancelReason: CancelReason.PathDistanceLessThanRangeIfStraightPath, 15);

        static public MovementSpec ActivateProximityMovementSpec = new MovementSpec(cancelForOpenables: false, cancelForMonster: true, cancelForLoot: true,
    cancelReason: CancelReason.PathDistanceLessThanRangeIfStraightPath, 5);

        static public MovementSpec CloseNavigationSpec = new MovementSpec(cancelForOpenables: false, cancelForMonster: false, cancelForLoot: false,
    cancelReason: CancelReason.PathDistanceLessThanRangeIfStraightPath, 12);
        // Detect and handle unexpected item on cursor + destroy element -> return to safe state
        // IngameUielements ->cursor will have a child when there is an item on cursor

        public static Camera Camera => WillBot.gameController.IngameState.Camera;

        public static Composite HandleDestroyItemPopup()
        {
            return new PrioritySelector(new Decorator(delegate
            {

                if (WillBot.Me.DestroyItemOnCursosWindow?.IsVisible == true)
                {
                    return true;
                }
                return false;
            },
                new Action(delegate
                {
                    // keep =  child(0).child(3)
                    // destroy = child(0).child(2)
                    var keepButton = WillBot.Me.DestroyItemOnCursosWindow?.GetChildAtIndex(0)?.GetChildAtIndex(3)?.GetClientRect();
                    if (keepButton == null) return RunStatus.Failure;
                    Mouse.SetCursorPosAndLeftOrRightClick((RectangleF)keepButton, Latency);
                    return RunStatus.Failure;

                })
                ),
                new Decorator(delegate
                {
                    if (GameController.IngameState.IngameUi.Cursor.ChildCount > 0)
                    {
                        return true;
                    }
                    return false;
                },
                new Sequence(
                    OpenInventory(),
                    new Action(delegate
                    {
                        // Move item to safe place
                        var ret = BotInventory.GetRectangleOpenSpaceInInventory(2, 3);
                        var clickRect = ret.Item2;
                        Mouse.SetCursorPosAndLeftOrRightClick(clickRect, Latency);
                        return RunStatus.Failure;
                    }))
                )
                );
        }

        public static Composite OpenAndEnterTownPortal()
        {
            return new Sequence(
               new Action(delegate
                {
                    InputWrapper.ResetMouseButtons();
                    CommonBehavior.CloseOpenPanels();
                    return RunStatus.Success;
                }),
                OpenTownPortal(),
                EnterTownPortal(),
                new Action(delegate
                {
                    WillBot.Mover.RemoveNavigationDataForCurrentZone();
                    ControlTimer.Restart();
                    while (ControlTimer.ElapsedMilliseconds < 10000 && !WillBot.Plugin.TreeHelper.CanTickTown() && !WillBot.Plugin.TreeHelper.CanTickHideout())
                    {
                        Thread.Sleep(500);
                    }
                    if (WillBot.Plugin.TreeHelper.CanTickMap())
                    {
                        return RunStatus.Failure;
                    }
                    return RunStatus.Success;
                })
                );
        }

        public static Composite OpenTownPortal()
        {
            return new DecoratorContinue(x => WillBot.Me.TownPortal == null,
                new Sequence(
                    OpenInventory(),
                new Action(delegate
                {
                    Thread.Sleep(300);
                    var townPortalInInventory = WillBot.Me.TownPortalInInventory;
                    if (townPortalInInventory == null) return RunStatus.Failure;
                    Mouse.SetCursorPosAndLeftOrRightClick(townPortalInInventory.GetClientRect(), 200, clickType: Mouse.MyMouseClicks.RightClick);
                    ControlTimer.Restart();
                    while (ControlTimer.ElapsedMilliseconds < 4000)
                    {
                        if (WillBot.Me.TownPortal != null)
                        {
                            return RunStatus.Success;
                        }
                        Thread.Sleep(50);
                    }
                    return RunStatus.Failure;
                })
                ));
        }

        public static Composite EnterTownPortal()
        {
            return new Decorator(x => WillBot.Me.TownPortal != null,
             new Action(delegate
            {
                Mouse.SetCursorPosAndLeftOrRightClick(WillBot.Me.TownPortal.Label.GetClientRectCache, 200, randomClick: true);
                return RunStatus.Success;
            }));
        }

        public static Composite CloseOpenPanels()
        {
            return new UseHotkeyAction(WillBot.KeyboardHelper, x => WillBot.Settings.CloseAllPanelsKey.Value);
            //return new Action(delegate
            //{
            //    while (true)
            //    {
            //        bool leftPanelOpen = GameController.Game.IngameState.IngameUi.OpenLeftPanel?.Address != 0;
            //        bool rightPanelOpen = GameController.Game.IngameState.IngameUi.OpenRightPanel?.Address != 0;
            //        bool inventoryPanelOpen = GameController.Game.IngameState.IngameUi.InventoryPanel.IsVisible == true;

            //        bool vendorPanelOpen = WillBot.gameController.IngameState.IngameUi.VendorPanelSellOption != null;

            //        if (leftPanelOpen || rightPanelOpen || inventoryPanelOpen || vendorPanelOpen)
            //        {
            //            InputWrapper.KeyPress(Keys.Escape);
            //            Thread.Sleep(350);
            //        }
            //        else
            //        {
            //            return RunStatus.Success;
            //        }
            //    }
            //});
        }

        public static Composite CloseOpenPanelsIfOpenPanels()
        {
            return new Action(delegate
            {
                while (true)
                {
                    bool leftPanelOpen = GameController.Game.IngameState.IngameUi.OpenLeftPanel?.Address != 0;
                    bool rightPanelOpen = GameController.Game.IngameState.IngameUi.OpenRightPanel?.Address != 0;
                    bool inventoryPanelOpen = GameController.Game.IngameState.IngameUi.InventoryPanel.IsVisible == true;
                    bool vendorPanelOpen = WillBot.gameController.IngameState.IngameUi.VendorPanelSellOption != null;

                    bool betralWindowOpen = GameController.Game.IngameState.IngameUi.BetrayalWindow?.IsVisible == true;

                    if (leftPanelOpen || rightPanelOpen || inventoryPanelOpen || vendorPanelOpen || betralWindowOpen)
                    {
                        InputWrapper.KeyPress(WillBot.Settings.CloseAllPanelsKey.Value);
                        Thread.Sleep(350);
                    }
                    else
                    {
                        return RunStatus.Success;
                    }
                }
            });
        }

        public static Composite OpenInventory()
        {
            return new DecoratorContinue(x => GameController.Game.IngameState.IngameUi.InventoryPanel.IsVisible == false,
                new Action(delegate
                {
                    InputWrapper.KeyPress(Keys.I);
                    ControlTimer.Restart();
                    while (ControlTimer.ElapsedMilliseconds < 3000)
                    {
                        if (GameController.Game.IngameState.IngameUi.InventoryPanel.IsVisible == true)
                        {
                            return RunStatus.Success;
                        }
                        Thread.Sleep(50);
                    }
                    return RunStatus.Failure;
                })
                );
        }



        static public bool MapComplete()
        {
            var areaExplored = WillBot.Mover.GetPercentOfZoneExplored();

            if (areaExplored > WillBot.Settings.MapAreaExploration && WillBot.Me.enemies.ClosestMonsterEntity == null)
            {
                return true;
            }
            return false;
        }


        public static bool DoCombat()
        {
            // Scan pixels between player and monster to detect walls/ledges
            Vector3 playerWorldPos = WillBot.gameController.Player.Pos;
            var closestMonster = WillBot.Me.enemies.ClosestMonsterEntity;
            bool isMonsterInCombatRange = closestMonster != null && closestMonster.DistancePlayer < WillBot.Settings.InCombatRangeDistanceGrid;
            bool isZDifferenceOk = false;
            if (isMonsterInCombatRange)
            {
                isZDifferenceOk = Math.Abs(playerWorldPos.Z - closestMonster.Pos.Z) < WillBot.Settings.InCombatMaxZDifferenceToMonster;
            }
            return isMonsterInCombatRange && isZDifferenceOk && !DoLooting();
        }

        // Can this handle both moving to some valid monster + moving towards some unique monster with isTargetable == false etc
        public static bool DoMoveToMonster()
        {
            if (DoCombat() == true) return false;

            Vector3 playerWorldPos = WillBot.gameController.Player.Pos;
            bool isZDifferenceOk = false;

            var closestKillableMonster = WillBot.Me.enemies.ClosestMonsterEntity;


            if (closestKillableMonster != null)
            {

                isZDifferenceOk = Math.Abs(playerWorldPos.Z - closestKillableMonster.Pos.Z) < WillBot.Settings.NavigateToMonsterMaxZDifference;
                WillBot.LogMessageCombo($"In do move to monster. Z difference is {isZDifferenceOk} ");
                if (isZDifferenceOk)
                {
                    return true;
                }
            }


            return false;
        }
        public static bool DoMoveToNonKillableUniqueMonster()
        {
            if (DoCombat() == true || DoMoveToMonster()) return false;
            var uniqueMonsters = WillBot.Me.enemies.UniqueMonsters;
            if (uniqueMonsters != null && uniqueMonsters.Count > 0)
            {
                if (uniqueMonsters?.FirstOrDefault()?.IsAlive == true)
                {
                    return true;
                }
            }
            return false;
        }

        public static Composite MoveToUnkillableUniqueMonster()
        {
            // The point of this is mainly to activate unactivated map bosses. I need some way to identify map bosses to seperate them from non map boss unique stuff
            return new Decorator(x => DoMoveToNonKillableUniqueMonster(), MoveTo(ret => WillBot.Me.enemies.UniqueMonsters?.FirstOrDefault()?.GridPos, x => WillBot.Me.enemies.UniqueMonsters?.FirstOrDefault()?.Pos
                        , CommonBehavior.DefaultMovementSpec));
        }

        public static bool DoLooting()
        {
            var closestMonster = WillBot.Me.enemies.ClosestMonsterEntity;

            bool noMonstersCloseToLoot = closestMonster == null ? true : (WillBot.Me.ClosestItemToLoot != null && WillBot.Me.ClosestItemToLoot.GridPos.Distance(closestMonster.GridPos) > 30);

            bool lootIsCloseEnough = WillBot.Me.ClosestItemToLoot != null && WillBot.Me.ClosestItemToLoot.GridPos.Distance(GameController.Player.GridPos) < 150;

            bool unOpenedStrongBox = WillBot.Me.StrongBox != null;
            if (noMonstersCloseToLoot && lootIsCloseEnough && !unOpenedStrongBox)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public static bool DoExploreZone()
        {
            bool isInMap = !WillBot.Plugin.Cache.InHideout && !WillBot.Plugin.Cache.InTown;
            // If no nearby monsters and no nearby objects to interact with
            if (WillBot.Me.ClosestItemToLoot == null && AreaBehavior.AreThereAreaActionsToPerform() == false && WillBot.Me.enemies.ClosestMonsterEntity == null)
            {
                return true;
            }
            return false;
            //   if (!DoCombat() && !DoMoveToMonster() && !DoLooting() && !DoMoveToNonKillableUniqueMonster()  && !)
        }



        public static Composite TownToHideout()
        {
            return new Action(delegate
            {
                InputWrapper.KeyPress(Keys.Enter);
                Thread.Sleep(100);
                Input.KeyDown(Keys.LControlKey);
                InputWrapper.KeyPress(Keys.A);
                Input.KeyUp(Keys.LControlKey);
                Thread.Sleep(50);
                SendKeys.SendWait("/hideout");
                InputWrapper.KeyPress(Keys.Enter);
                Thread.Sleep(2000);
                return RunStatus.Success;
            });

        }

        public delegate Vector2? PositionDelegate(object context);
        public delegate Vector3? XyzPositionDelegate(object context);


        public static Composite MoveTo(PositionDelegate positionDelegate, XyzPositionDelegate xyzPositionDelegate = null, MovementSpec spec = null)
        {
            return new Sequence(
                new Action(delegate (object context)
                {
                    var position = positionDelegate(context);
                    if (position == null)
                    {
                        WillBot.LogMessageCombo($"Unable to set path to null position");
                        return RunStatus.Failure;
                    }
                    bool didSetPath = WillBot.Mover.SetPath((Vector2)position);
                    if (didSetPath == false)
                    {
                        WillBot.LogMessageCombo($"Unable to set path to {position}");
                        return RunStatus.Failure;
                    }
                    else
                    {
                        WillBot.LogMessageCombo($"Successfully found path to {position}");
                        ControlTimer.Restart();
                        return RunStatus.Success;
                    }
                }),
                new Action(delegate (object context)
                {
                    bool isZDifferenceOk = true;
                    var entityPos = xyzPositionDelegate?.Invoke(context);
                    var entityGridPos = positionDelegate?.Invoke(context);
                    if (entityPos != null)
                    {
                        isZDifferenceOk = Math.Abs(GameController.Player.Pos.Z - entityPos.GetValueOrDefault().Z) < WillBot.Settings.MovementCancelingForLootZThreshold;
                    }
                    // if zdifferencenotOk, but the path is relatively straight to the object and close -> use movement skill
                    WillBot.LogMessageCombo($"Remaining path distance squared : {WillBot.Mover.pathFindingWrapper.RemainingPathDistanceSquaredToTarget}");
                    var airDistanceSquared = entityGridPos.GetValueOrDefault().DistanceSquared(WillBot.gameController.Player.GridPos);
                    if (isZDifferenceOk == false && (WillBot.Mover.pathFindingWrapper.RemainingPathDistanceSquaredToTarget < 1300) &&
                         WillBot.Mover.pathFindingWrapper.RemainingPathDistanceSquaredToTarget < 1.07 * airDistanceSquared)
                    {
                        WillBot.LogMessageCombo($"Using movement skill to most likely jump a cliff which incorrectly is set to walkable");
                        InputWrapper.ResetMouseButtons();
                        var screenPos = Camera.WorldToScreen((Vector3)entityPos);
                        Mouse.SetCursorPosAndLeftOrRightClick(screenPos, 10, clickType: Mouse.MyMouseClicks.NoClick);
                        InputWrapper.KeyPress(Keys.E);
                        //MoverHelper.ClickToStopCharacter();
                        return RunStatus.Failure;
                    }
                    switch (spec.cancelReason)
                    {
                        case CancelReason.None:
                            break;
                        case CancelReason.PathDistanceLessThanRange:
                            if ((WillBot.Mover.pathFindingWrapper.RemainingPathDistanceSquaredToTarget < (spec.cancelRangeSquared)) && isZDifferenceOk)
                            {
                                WillBot.LogMessageCombo($"Canceled movement in range to object {spec.cancelRange}");
                                InputWrapper.ResetMouseButtons();
                                //MoverHelper.ClickToStopCharacter();
                                return RunStatus.Failure;
                            }
                            break;

                        case CancelReason.PathDistanceLessThanRangeIfStraightPath:
                            /* Cancel pathfinding if path distance to object is less than X and distance to object and path distance are roughly equal
                    * use 1: Looting, avoid having to pathfind really close to loot before clicking the label.
                    */
                            //var airDistanceSquared = entityGridPos.GetValueOrDefault().DistanceSquared(WillBot.gameController.Player.GridPos);

                            if ((WillBot.Mover.pathFindingWrapper.RemainingPathDistanceSquaredToTarget < spec.cancelRangeSquared) &&
                                 WillBot.Mover.pathFindingWrapper.RemainingPathDistanceSquaredToTarget < 1.07 * airDistanceSquared)
                            {
                                WillBot.LogMessageCombo($"Canceled movement in range to object with straight line {spec.cancelRange}");
                                InputWrapper.ResetMouseButtons();
                                //MoverHelper.ClickToStopCharacter();
                                return RunStatus.Failure;
                            }
                            break;
                    }

                    if (spec.cancelForMonster && DoCombat())
                    {
                        WillBot.LogMessageCombo($"Canceled movement to do combat");
                        InputWrapper.ResetMouseButtons();
                        return RunStatus.Failure;
                    }
                    if (spec.cancelForLoot && DoLooting())
                    {
                        WillBot.LogMessageCombo($"Canceled movement to do looting");
                        InputWrapper.ResetMouseButtons();
                        return RunStatus.Failure;
                    }
                    if (spec.cancelForOpenables && (WillBot.Me.StrongBox != null || WillBot.Me.HarvestChest != null || WillBot.Me.EssenceMonsterMonolith != null))
                    {
                        InputWrapper.ResetMouseButtons();
                        WillBot.LogMessageCombo($"Canceled movement to do openables");
                        return RunStatus.Failure;
                    }
                    if (ControlTimer.ElapsedMilliseconds > 3500 && StuckTracker.GetDistanceMoved() < 40)
                    {
                        InputWrapper.ResetMouseButtons();
                        WillBot.LogMessageCombo($"Canceled movement due to being stuck!. Using movement ability");
                        InputWrapper.KeyPress(WillBot.Settings.MovementAbilityKey);
                        return RunStatus.Failure;
                    }
                    // If movement last x duration less than .. return
                    return WillBot.Mover.FollowPath();
                })
                    );
        }

        //public static Composite MoveTo(PositionDelegate positionDelegate, XyzPositionDelegate xyzPositionDelegate = null, bool cancelForOpenables = false
        //, bool cancelForMonster = true, bool cancelForLoot = true,
        //    bool cancelInRange = false, float cancelRange = 30f)
        //{
        //    return new Sequence(
        //        new Action(delegate (object context)
        //        {
        //            var position = positionDelegate(context);
        //            bool didSetPath = WillBot.Mover.SetPath(position);
        //            if (didSetPath == false)
        //            {
        //                WillBot.LogMessageCombo($"Unable to set path to {position}");
        //                return RunStatus.Failure;
        //            }
        //            else
        //            {
        //                WillBot.LogMessageCombo($"Successfully found path to {position}");
        //                WillBot.LogMessageCombo($"Air distance: {GameController.Player.GridPos.DistanceSquared(position)}, path distance: {WillBot.Mover.pathFindingWrapper.RemainingPathDistanceSquaredToTarget}");
        //                ControlTimer.Restart();
        //                return RunStatus.Success;
        //            }
        //        }),
        //        new Action(delegate (object context)
        //            {
        //                bool isZDifferenceOk = true;
        //                var entityPos = xyzPositionDelegate?.Invoke(context);
        //                var entityGridPos = positionDelegate?.Invoke(context);
        //                if (entityPos != null)
        //                {
        //                    isZDifferenceOk = Math.Abs(GameController.Player.Pos.Z - entityPos.GetValueOrDefault().Z) < WillBot.Settings.MovementCancelingForLootZThreshold;
        //                }
        //                if (cancelInRange && (WillBot.Mover.pathFindingWrapper.RemainingPathDistanceSquaredToTarget < (cancelRange * cancelRange)) && isZDifferenceOk)
        //                {
        //                    WillBot.LogMessageCombo($"Canceled movement in range to object {cancelRange}");
        //                    InputWrapper.ResetMouseButtons();
        //                    //MoverHelper.ClickToStopCharacter();
        //                    return RunStatus.Failure;
        //                }
        //                if (cancelForMonster && DoCombat())
        //                {
        //                    WillBot.LogMessageCombo($"Canceled movement to do combat");
        //                    InputWrapper.ResetMouseButtons();
        //                    return RunStatus.Failure;
        //                }
        //                if (cancelForLoot && DoLooting())
        //                {
        //                    WillBot.LogMessageCombo($"Canceled movement to do looting");
        //                    InputWrapper.ResetMouseButtons();
        //                    return RunStatus.Failure;
        //                }
        //                if (cancelForOpenables && (WillBot.Me.StrongBox != null || WillBot.Me.HarvestChest != null || WillBot.Me.EssenceMonsterMonolith != null))
        //                {
        //                    InputWrapper.ResetMouseButtons();
        //                    WillBot.LogMessageCombo($"Canceled movement to do openables");
        //                    return RunStatus.Failure;
        //                }
        //                if (ControlTimer.ElapsedMilliseconds > 3500 && StuckTracker.GetDistanceMoved() < 40)
        //                {
        //                    InputWrapper.ResetMouseButtons();
        //                    WillBot.LogMessageCombo($"Canceled movement due to being stuck!");
        //                    return RunStatus.Failure;
        //                }
        //                // If movement last x duration less than .. return
        //                return WillBot.Mover.FollowPath();
        //            })
        //            );
        //}


        public static bool CouldNotFindValidPositionToExplore { get; set; }
        public static Composite DoExploringComposite()
        {
            return new Decorator(delegate
            {
                float areaExplored = WillBot.Mover.GetPercentOfCurrentSubMapExplored();
                WillBot.LogMessageCombo($"Area explored: {areaExplored}");
                if (DoExploreZone() == true)
                {
                    return true;
                }
                return false;

            }, new Sequence(
                new Action(delegate
                {
                    var foundPos = WillBot.Mover.GetUnexploredPosition();
                    WillBot.LogMessageCombo($"Unexplored coordinates {foundPos}");
                    if (foundPos.Item1)
                    {
                        CouldNotFindValidPositionToExplore = false;
                        WillBot.Me.IsCurrentPositionToExploreValid = true;
                        WillBot.Me.CurrentPositionToExplore = foundPos.Item2;
                        return RunStatus.Success;
                    }
                    else
                    {
                        WillBot.Me.IsCurrentPositionToExploreValid = false;
                        CouldNotFindValidPositionToExplore = true;
                        return RunStatus.Failure;
                    }
                }),
                MoveTo(ret => WillBot.Me.CurrentPositionToExplore, spec: CommonBehavior.DefaultMovementSpec)
                )
            );
        }





    }
}
