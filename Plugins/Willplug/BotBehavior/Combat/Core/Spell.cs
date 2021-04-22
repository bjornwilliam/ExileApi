using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TreeRoutine.TreeSharp;

namespace Willplug.Combat.Core
{
    public static class Spell
    {
        public delegate T Selection<out T>(object context);

        public delegate WillPlayer UnitSelectionDelegate(object context);
        private static WillPlayer Me
        {
            get { return WillBot.Me; }
        }

        public static Composite Buff(string spell, Selection<bool> reqs = null)
        {       
            return
                new Decorator(
                    ret => (reqs == null || reqs(ret)) && !Me.playerHasBuffs( new List<string> { spell }),
                    Cast(spell, ret => Me, ret => true));
        }

        //public static Composite Cast(string spell, Selection<bool> reqs = null)
        //{
        //    return Cast(spell, ret => Me.CurrentTarget, reqs);
        //}

        public static Composite Cast(string spell, UnitSelectionDelegate onUnit, Selection<bool> reqs = null)
        {
            return
                new Decorator(
                    ret =>
                        onUnit != null && onUnit(ret) != null && (reqs == null || reqs(ret)),
            //&& AbilityManager.CanCast(spell, onUnit(ret)),
                    new PrioritySelector(
                        new TreeRoutine.TreeSharp.Action(delegate
                        {
                            //added current target health percent check
                            //Logger.Write(">> Casting <<   " + spell);
                            return RunStatus.Failure;
                        }),
                        new TreeRoutine.TreeSharp.Action(ret => RunStatus.Success)) // AbilityManager.Cast(spell, onUnit(ret))))
                    );
        }
    }
}
