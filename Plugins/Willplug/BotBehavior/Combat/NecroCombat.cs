using ExileCore;
using ExileCore.PoEMemory.Components;
using ExileCore.PoEMemory.MemoryObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using TreeRoutine.DefaultBehaviors.Actions;
using TreeRoutine.TreeSharp;
using Action = TreeRoutine.TreeSharp.Action;

namespace Willplug.Combat
{
    public static class NecroCombat
    {

        public static WillPlayer Me { get => WillBot.Me; }

        private static GameController GameController => WillBot.gameController;

        public static Camera Camera => WillBot.gameController.IngameState.Camera;

        private static DateTime previousEnduringTime = DateTime.Now;
        private static DateTime previousEarthShatterTime = DateTime.Now;
        private static DateTime previousEnduringCryToBlowUpSpikes = DateTime.Now;
        private static DateTime previousConvocationUseTime = DateTime.Now;

        public static Composite NecroCombatComposite()
        {
            // Add death marking of rares + uniques
            // Only death mark x seconds (wont have to check for enemy debuffs etc)
            return new PrioritySelector(
                //Dash into center of pack
                //attack
                CloseDistance(),
                 CommonBehavior.MoveTo(x => Me.enemies.ClosestMonsterEntity?.GridPos, x => Me.enemies.ClosestMonsterEntity?.Pos,
                 CommonBehavior.CloseNavigationSpec),
                UseConvocation());
        }



        public static Composite UseConvocation()
        {
            return new Decorator(delegate
            {
                if (DateTime.Now.Subtract(previousConvocationUseTime).TotalMilliseconds > 3050)
                {
                    return true;
                }
                return false;

            },
            new Sequence(
                new Action(delegate
                {
                    previousConvocationUseTime = DateTime.Now;
                    return RunStatus.Success;
                }), new UseHotkeyAction(WillBot.KeyboardHelper, x => Keys.Space)
                    )
            );
        }

        public static Composite CloseDistance()
        {
            return new Decorator(delegate
            {
                if (Me.enemies.ClosestMonsterEntity?.DistancePlayer > 30)
                {
                    return true;
                }
                return false;
            },
           new Sequence(

                new Action(delegate
                {
                    try
                    {
                        var mobPos = Camera.WorldToScreen(Me.enemies.ClosestMonsterEntity.Pos);
                        Mouse.SetCursorPosAndLeftOrRightClick(mobPos, 20, clickType: Mouse.MyMouseClicks.NoClick);
                        return RunStatus.Success;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.ToString());
                        return RunStatus.Failure;
                    }      
                }),
                    new Inverter(new UseHotkeyAction(WillBot.KeyboardHelper, x => Keys.E))
                )


        );


        }



        //new TreeRoutine.TreeSharp.Action(delegate
        //    {
        //        if (CommonBehavior.DoCombat())
        //        {
        //            Mouse.SetCursorGridPosAndRightClick(Me.enemies.ClosestMonsterEntity.GridPos,2);
        //            WillBot.SetCursorPositionOnTarget(Me.enemies.ClosestMonsterEntity?.Pos ?? WillBot.gameController.Player.Pos);
        //            Mouse.RightClick(15);

        //            InputWrapper.RightMouseButtonDown();
        //            WillBot.LogMessageCombo($"Current monster {Me.enemies.ClosestMonsterEntity?.ToString()}, distance: {Me.enemies.ClosestMonsterEntity?.DistancePlayer}");
        //            return RunStatus.Running;
        //        }
        //        else
        //        {
        //            InputWrapper.RightMouseButtonUp();
        //            return RunStatus.Failure;
        //        }

        //    });

    }
}
