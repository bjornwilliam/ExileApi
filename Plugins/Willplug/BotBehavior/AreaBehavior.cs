using ExileCore;
using ExileCore.PoEMemory.MemoryObjects;
using ExileCore.Shared.Enums;
using ExileCore.Shared.Helpers;
using SharpDX;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using TreeRoutine.TreeSharp;
using Willplug.Navigation;
using Action = TreeRoutine.TreeSharp.Action;
using ExileCore.PoEMemory.Elements;
using ExileCore.PoEMemory.Components;

namespace Willplug.BotBehavior
{
    public static class AreaBehavior
    {

        private static WillPlayer Me => WillBot.Me;
        static private GameController GameController = WillBot.gameController;
        static private IngameUIElements IngameUIElements = GameController.IngameState.IngameUi;
        static private readonly Stopwatch ControlTimer = new Stopwatch();
        static private int Latency => (int)GameController.IngameState.CurLatency;


        static private uint areaHashBeforeTransition;


        static private ZoneMap zoneMapBeforeTransition;
        static private SubMap subMapBeforeTransition;
        static private int islandIdBeforeTransition;
        static private string labelTextOfAreaTransition;


        static private AreaTransitionInfo areaTransitionBeforeTransition;

        static private AreaTransitionInfo areaTransitionAfterTransition;


        /*
         * Metamorph. Check for existence of spawn thane button ( this should only have isVisible == true if its a metamorph map)
         * if map is done, do metamorph. Check for "Construct metamorph" label 
         * 
         * 
         * 
         * Chosen body parts window: Metamorphwindow 0 -> 3
         *  0  = eyes, 1 = liver, 2 = lung, 3= heart, 4 = brain (Body part boxes)
         *  Bodypart box:
         *  0 -> 1  ( There is a body part chosen for this box IF isVisible == true
         * Metamorphwindow -> 1 = create button
         

        * Methamorph stash side of things
        * 
        * MetamorphBodyPartStashWindowElement
        * BodyPartName can be : Eyes, livers, lungs, brains, hearts
        * 
        * Metamorph body part element
        * 0 -> 0 ( if isVisible == false then there is no body part in the stash for this
            
         */




        public static bool DoOpenDeliriumMirror()
        {
            if (Me.DeliriumMirror != null && (Me.DeliriumMirror?.DistancePlayer < 25 || !CommonBehavior.DoCombat()))
            {
                return true;
            }
            return false;

        }

        public static bool AreThereAreaActionsToPerform()
        {
            if (Me.TimelessMonolith == null && Me.DeliriumMirror == null && Me.EssenceMonsterMonolith == null && Me.HarvestChest == null && Me.StrongBox == null)
            {
                return false;
            }
            return true;
        }

        public static Composite OpenDeliriumMirror()
        {
            return new Decorator(x => DoOpenDeliriumMirror(),
                CommonBehavior.MoveTo(x => Me.DeliriumMirror?.GridPos, x => Me.DeliriumMirror?.Pos, CommonBehavior.ActivateProximityMovementSpec)

               );
        }

        public static Composite InteractWithLabelOnGroundOpenable(Func<LabelOnGround> labelToOpenDel)
        {
            return new Decorator(delegate
            {
                var labelOnGround = labelToOpenDel();

                if (labelOnGround != null && !CommonBehavior.DoCombat() && !CommonBehavior.DoLooting()) return true;
                return false;
            },
            new PrioritySelector(
                CommonBehavior.MoveTo(x => labelToOpenDel()?.ItemOnGround?.GridPos, x => labelToOpenDel()?.ItemOnGround?.Pos,
                CommonBehavior.OpenablesMovementSpec),
                new Action(delegate
               {
                   var label = labelToOpenDel();
                   var chestComponent = label?.ItemOnGround.GetComponent<Chest>();
                   if (chestComponent != null && chestComponent.IsStrongbox == true && chestComponent.IsLocked == true)
                   {
                       ControlTimer.Restart();
                       while (ControlTimer.ElapsedMilliseconds < 3500 && chestComponent?.IsLocked == true)
                       {
                           Thread.Sleep(150);
                           Mouse.SetCursorPosAndLeftOrRightClick(label, Latency);
                       }
                       return RunStatus.Failure;
                   }
                   else if (chestComponent != null && chestComponent.IsStrongbox == false && chestComponent.DestroyingAfterOpen == true)
                   {

                       ControlTimer.Restart();
                       while (ControlTimer.ElapsedMilliseconds < 3500 && label != null)
                       {
                           Thread.Sleep(150);
                           Mouse.SetCursorPosAndLeftOrRightClick(label, Latency);
                       }
                       return RunStatus.Failure;

                   }
                   else
                   {
                       ControlTimer.Restart();
                       while (ControlTimer.ElapsedMilliseconds < 3500 && label != null)
                       {
                           Thread.Sleep(150);
                           Mouse.SetCursorPosAndLeftOrRightClick(label, Latency);
                       }
                       return RunStatus.Failure;
                   }
               })));
        }



        public static Composite DiedBehavior()
        {
            return new Action(delegate
            {
                // Click resurect at checkpoint option.
                // Update State: player died, so that the bot tries to continue doing what it was doing
                var optionRect = IngameUIElements.YouHaveDiedWindow.ResurrectAtCheckPointOption.GetClientRectCache;
                Mouse.SetCursorPosAndLeftOrRightClick(optionRect, Latency);

                ControlTimer.Restart();
                while (ControlTimer.ElapsedMilliseconds < 1500 && IngameUIElements.YouHaveDiedWindow.IsVisible)
                {
                    Thread.Sleep(50);
                }
                return RunStatus.Success;
            });

        }

        public static void UpdateAreaTransitions()
        {
            if (Me.ClosestAreaTransitionLabel != null)
            {
                WillBot.LogMessageCombo("There is an area transition. Proceeding to trying to add it");
                WillBot.Mover.GetZoneMap()?.TryAddAreaTransition(GameController.Player.GridPos, Me.ClosestAreaTransitionLabel);
            }
        }

        public static Composite TryDoAreaTransition()
        {
            return new PrioritySelector(
                    new Decorator(delegate
                    {
                        var ret = HasValidAreaTransitionByType(AreaTransitionType.Local);
                        if (ret.Item1 == true)
                        {
                            areaTransitionBeforeTransition = ret.Item2;
                            return true;
                        }
                        return false;

                    }, DoLocalAreaTransition()),
                    new Decorator(delegate
                    {
                        var ret = HasValidAreaTransitionByType(AreaTransitionType.NormalToCorrupted);
                        if (ret.Item1 == true)
                        {
                            areaTransitionBeforeTransition = ret.Item2;
                            return true;
                        }
                        return false;

                    }, DoCorruptedZoneTransition())

                );
        }



        public static (bool, AreaTransitionInfo) HasValidAreaTransitionByType(AreaTransitionType transitionType)
        {
            int currentIslandId = WillBot.Mover.GetZoneMap()?.GetSubMap(GameController.Player.GridPos)?.IslandId ?? -1;
            if (currentIslandId == -1)
            {
                WillBot.LogMessageCombo($"Unable to get any island id when trying to get area transition.");
                return (false, null);
            }
            var closestAreaTransitionsWithinCurrentIsland = WillBot.Mover.GetZoneMap().FoundAreaTransitions.Where(x => x.Value.locatedInIslandWithId == currentIslandId)?.
      OrderBy(x => x.Value.areaTransitionGridPosition.DistanceSquared(GameController.Player.GridPos))?.ToList();
            if (closestAreaTransitionsWithinCurrentIsland == null) return (false, null);

            foreach (var transition in closestAreaTransitionsWithinCurrentIsland)
            {
                switch (transitionType)
                {
                    case AreaTransitionType.Normal:
                        break;
                    case AreaTransitionType.Local:
                        if (transition.Value.hasBeenEntered == false && transition.Value.leadsToIslandWithId < 1)
                        {
                            WillBot.LogMessageCombo($"Found transition which has not been entered and no valid leads to island id");
                            return (true, transition.Value);
                        }
                        // if transition leads to island with less explored than current -> go
                        else if (transition.Value.leadsToIslandWithId > 0 && transition.Value.leadsToIslandWithId != currentIslandId)
                        {
                            float currentSubMapExploration = WillBot.Mover.GetPercentOfCurrentSubMapExplored();
                            //   float currentSubMapExploration = WillBot.Mover.GetZoneMap().GetSubMap(currentIslandId)?.Exploration?.ExploredFactor ?? 0;
                            float transitionSubMapExploration = WillBot.Mover.GetZoneMap().GetSubMap(transition.Value.leadsToIslandWithId)?.Exploration?.ExploredFactor ?? 0;

                            if (transitionSubMapExploration < currentSubMapExploration && currentSubMapExploration > 80)
                            {
                                WillBot.LogMessageCombo($"Found transition that leads to a less exlored submap {transitionSubMapExploration}  than current {currentSubMapExploration} ");
                                return (true, transition.Value);
                            }
                        }
                        break;
                    case AreaTransitionType.NormalToCorrupted:

                        if (transition.Value.hasBeenEntered == false)
                        {
                            WillBot.LogMessageCombo($"Entering corrupted zone which has not been entered before");
                            return (true, transition.Value);
                        }
                        break;
                    case AreaTransitionType.CorruptedToNormal:
                        // Exit the corrupted zone after some goals have been accomplished...
                        // Perhaps check for monsters remaining == 0 ?
                        float currentZoneExplored = WillBot.Mover.GetPercentOfZoneExplored();
                        if (currentZoneExplored > 94)
                        {
                            return (true, transition.Value);
                        }
                        break;

                }


            }
            return (false, null);
        }




        public static Composite DoNormalZoneTransition()
        {
            return null;
        }



        public static Composite DoCorruptedZoneTransition()
        {
            return new Sequence(
                   new Inverter(CommonBehavior.MoveTo(x => areaTransitionBeforeTransition.areaTransitionGridPosition,
                   spec: CommonBehavior.OpenablesMovementSpec)),

                 new Action(delegate
                 {
                     // Test if were actually close to the area transition 
                     if (GameController.Player.GridPos.Distance(areaTransitionBeforeTransition.areaTransitionGridPosition) > 35)
                     {
                         WillBot.LogMessageCombo($"In trying to complete area transition. Movement to area transition must have been canceled");
                         return RunStatus.Failure;
                     }
                     areaHashBeforeTransition = GameController.Area.CurrentArea.Hash;

                     islandIdBeforeTransition = areaTransitionBeforeTransition.locatedInIslandWithId;
                     var closestAreaTransitionLabel = Me.ClosestAreaTransitionLabel;
                     if (closestAreaTransitionLabel == null)
                     {
                         WillBot.LogMessageCombo($"No area transition nearby");
                         return RunStatus.Failure;

                     }
                     labelTextOfAreaTransition = closestAreaTransitionLabel.Label.Text;
                     ControlTimer.Restart();

                     Vector2 prevGridPos = GameController.Player.GridPos;
                     while (ControlTimer.ElapsedMilliseconds < 4000)
                     {
                         Mouse.SetCursorPosAndLeftOrRightClick(closestAreaTransitionLabel.Label.GetClientRect(), Latency);
                         // Try to detect a jump in player gridpos
                         var currentGridPos = GameController.Player.GridPos;
                         if (currentGridPos.Distance(prevGridPos) > 20)
                         {
                             WillBot.LogMessageCombo($"detected large enough jump in grid pos. success");
                             Thread.Sleep(400);
                             lock (MyLocks.UpdateTerrainDataLock)
                             {
                                 //Issue is that AreaChange function run on another thread and will updateterraindata too..
                                 // Just wait until this can be aquired since that should mean its done updating terrain data
                             }
                             var zoneMapAfterTransition = WillBot.Mover.GetZoneMap();
                             var subMapAfterTransition = zoneMapAfterTransition.GetSubMap(GameController.Player.GridPos);
                             int islandIdAfterTransition = subMapAfterTransition.IslandId;
                             areaTransitionBeforeTransition.leadsToIslandWithId = islandIdAfterTransition;
                             areaTransitionBeforeTransition.hasBeenEntered = true;

                             if (Me.ClosestAreaTransitionLabel != null)
                             {
                                 // verify that this is another area transition
                                 var newKeyTuple = Me.ClosestAreaTransitionLabel.ItemOnGround.GridPos.ToIntTuple();
                                 if (zoneMapAfterTransition.FoundAreaTransitions.ContainsKey(newKeyTuple))
                                 {
                                     //Already registered this area transition 
                                     WillBot.LogMessageCombo($"already have this area transition");
                                 }
                                 else
                                 {
                                     var transition = new AreaTransitionInfo(Me.ClosestAreaTransitionLabel.Label.Text, Me.ClosestAreaTransitionLabel.ItemOnGround.GridPos, islandIdAfterTransition);
                                     transition.leadsToIslandWithId = islandIdBeforeTransition;
                                     transition.leadsToZoneWithAreaHash = areaHashBeforeTransition;
                                     transition.transitionType = Me.ClosestAreaTransitionLabel.ItemOnGround.GetComponent<AreaTransition>().TransitionType;
                                     zoneMapAfterTransition.FoundAreaTransitions.Add(newKeyTuple, transition);
                                 }
                             }
                             return RunStatus.Success;
                         }
                         prevGridPos = currentGridPos;
                         Thread.Sleep(100);
                     }
                     // time out can mean area transition that disappears + no movement during the transition
                     // if it timed out but the area transition is gone 
                     return RunStatus.Failure;
                 }));
        }



        public static Composite DoLocalAreaTransition()
        {
            //sepearte between local, to corrupted etc. Handle accordingly.
            return new Sequence(
                  new Inverter(CommonBehavior.MoveTo(x => areaTransitionBeforeTransition.areaTransitionGridPosition, spec: CommonBehavior.OpenablesMovementSpec)),

                new Action(delegate
           {
               // Test if were actually close to the area transition 
               if (GameController.Player.GridPos.Distance(areaTransitionBeforeTransition.areaTransitionGridPosition) > 35)
               {
                   WillBot.LogMessageCombo($"In trying to complete area transition. Movement to area transition must have been canceled");
                   return RunStatus.Failure;
               }
               areaHashBeforeTransition = GameController.Area.CurrentArea.Hash;

               islandIdBeforeTransition = areaTransitionBeforeTransition.locatedInIslandWithId;
               var closestAreaTransitionLabel = Me.ClosestAreaTransitionLabel;
               if (closestAreaTransitionLabel == null)
               {
                   WillBot.LogMessageCombo($"No area transition nearby");
                   return RunStatus.Failure;

               }
               labelTextOfAreaTransition = closestAreaTransitionLabel.Label.Text;
               ControlTimer.Restart();

               Vector2 prevGridPos = GameController.Player.GridPos;
               while (ControlTimer.ElapsedMilliseconds < 4000)
               {
                   Mouse.SetCursorPosAndLeftOrRightClick(closestAreaTransitionLabel.Label.GetClientRect(), Latency);
                   // Try to detect a jump in player gridpos
                   var currentGridPos = GameController.Player.GridPos;
                   if (currentGridPos.Distance(prevGridPos) > 20)
                   {
                       WillBot.LogMessageCombo($"detected large enough jump in grid pos. success");
                       Thread.Sleep(400);

                       // if local get zone map
                       // else initialize zone.. and add areatransition. 
                       var zoneMapAfterTransition = WillBot.Mover.GetZoneMap();
                       var subMapAfterTransition = zoneMapAfterTransition.GetSubMap(GameController.Player.GridPos);
                       int islandIdAfterTransition = subMapAfterTransition.IslandId;
                       areaTransitionBeforeTransition.leadsToIslandWithId = islandIdAfterTransition;
                       areaTransitionBeforeTransition.hasBeenEntered = true;

                       if (Me.ClosestAreaTransitionLabel != null)
                       {
                           // verify that this is another area transition
                           var newKeyTuple = Me.ClosestAreaTransitionLabel.ItemOnGround.GridPos.ToIntTuple();
                           if (zoneMapAfterTransition.FoundAreaTransitions.ContainsKey(newKeyTuple))
                           {
                               //Already registered this area transition 
                               WillBot.LogMessageCombo($"already have this area transition");
                           }
                           else
                           {
                               var transition = new AreaTransitionInfo(Me.ClosestAreaTransitionLabel.Label.Text, Me.ClosestAreaTransitionLabel.ItemOnGround.GridPos, islandIdAfterTransition);
                               transition.leadsToIslandWithId = islandIdBeforeTransition;
                               zoneMapAfterTransition.FoundAreaTransitions.Add(newKeyTuple, transition);
                           }
                       }
                       return RunStatus.Success;
                   }
                   prevGridPos = currentGridPos;
                   Thread.Sleep(100);
               }
               // time out can mean area transition that disappears + no movement during the transition
               // if it timed out but the area transition is gone 
               return RunStatus.Failure;
           })

            //new Action(delegate
            //{
            //    if (areaHashBeforeTransition != GameController.Area.CurrentArea.Hash)
            //    {
            //        areaTransitionBeforeTransition.isZoneTransition = true;
            //        return RunStatus.Success;
            //    }
            //    else
            //    {
            //        var zoneMapAfterTransition = WillBot.Mover.GetZoneMap();
            //        var subMapAfterTransition = zoneMapAfterTransition.GetSubMap(GameController.Player.GridPos);
            //        int islandIdAfterTransition = subMapAfterTransition.IslandId;
            //        areaTransitionBeforeTransition.leadsToIslandWithId = islandIdAfterTransition;
            //        areaTransitionBeforeTransition.hasBeenEntered = true;

            //        // Wait and try to add the area transition
            //        ControlTimer.Restart();
            //        while (ControlTimer.ElapsedMilliseconds < 6000)
            //        {
            //            areaTransitionAfterTransition = zoneMapAfterTransition.TryGetAreaTransition(Me.ClosestAreaTransitionLabel);
            //            if (areaTransitionAfterTransition != null)
            //            {
            //                break;
            //            }
            //            Thread.Sleep(200);
            //        }
            //        if (areaTransitionAfterTransition == null) return RunStatus.Failure;
            //        areaTransitionAfterTransition.hasBeenEntered = true;

            //        areaTransitionAfterTransition.leadsToIslandWithId = islandIdBeforeTransition;

            //        return RunStatus.Success;
            //    }
            //})

            );
        }


    }
}
