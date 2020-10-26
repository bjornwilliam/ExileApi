using ExileCore;
using ExileCore.PoEMemory.Components;
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
    public class CombatRoutine
    {

        public WillPlayer Me { get => WillBot.Me; }

        private GameController GameController => WillBot.gameController;

        private Composite _combat;
        public Composite Combat { get => _combat; }

        private DateTime previousEnduringTime = DateTime.Now;
        private DateTime previousEarthShatterTime = DateTime.Now;
        private DateTime previousEnduringCryToBlowUpSpikes = DateTime.Now;

        public void Initialize()
        {
            _combat = new PrioritySelector(

                //Dash into center of pack
                //attack
                NavigateUntilVeryCloseDueToObstacles(),
                CloseDistance(),
                new Action(delegate
                {
                    WillBot.SetCursorPositionOnTarget(Me.enemies.ClosestMonsterEntity?.Pos ?? WillBot.gameController.Player.Pos);
                    Mouse.RightClick(50);
                    previousEarthShatterTime = DateTime.Now;
                    //InputWrapper.RightMouseButtonDown();
                    WillBot.LogMessageCombo($"Current monster {Me.enemies.ClosestMonsterEntity?.ToString()}, distance: {Me.enemies.ClosestMonsterEntity?.DistancePlayer}");
                    return RunStatus.Failure;
                }),
                new Decorator(delegate
                {
                    if (DateTime.Now.Subtract(previousEnduringTime).TotalMilliseconds > 1200 && DateTime.Now.Subtract(previousEarthShatterTime).TotalMilliseconds < 1500)
                    {
                        previousEnduringTime = DateTime.Now;
                        return true;
                    }
                    return false;
                },
                new Inverter(new UseHotkeyAction(WillBot.KeyboardHelper, x => Keys.Space))
                ));


        }

        public Composite NavigateUntilVeryCloseDueToObstacles()
        {
            return new Decorator(delegate
            {
                if (Me.enemies.ClosestMonsterEntity?.DistancePlayer > 12 && WillBot.Mover.GetZoneMap()?.CheckForUnpassableTerrain(GameController.Player.GridPos, Me.enemies.ClosestMonsterEntity.GridPos, 15) > 170)
                {
                    return true;
                }
                return false;
            },
             CommonBehavior.MoveTo(x => Me.enemies.ClosestMonsterEntity?.GridPos, x => Me.enemies.ClosestMonsterEntity?.Pos,
             CommonBehavior.CloseNavigationSpec)
);
        }


        public Composite CloseDistance()
        {
            return new Decorator(delegate
            {
                if (Me.enemies.ClosestMonsterEntity?.DistancePlayer > 20)
                {
                    return true;
                }
                return false;
            },


          new Inverter(new UseHotkeyAction(WillBot.KeyboardHelper, x => Keys.E))
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
