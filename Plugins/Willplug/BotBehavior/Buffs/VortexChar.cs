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
using TreeRoutine.DefaultBehaviors.Actions;
using System.Windows.Forms;
using System.Runtime.Remoting.Messaging;
using Roy_T.AStar.Primitives;
using ExileCore.PoEMemory.MemoryObjects;
using ExileCore.Shared.Enums;


namespace Willplug.BotBehavior.Buffs
{
   
    public class VortexChar
    {
        public static WillPlayer Me { get => WillBot.Me; }

        public static string bladeVortexCounterBuff = "blade_vortex_counter";



        public static Composite CreateUseBladeVortexComposite()
        {
            return new Decorator(delegate
            {
                // WillBot.LogMessageCombo("In create using enduring composite");
                //if (Me.playerDoesNotHaveAnyOfBuffs(new List<string>() { onslaughtBuff })) // add + some other condition
                //{
                //    return true;
                //}
                if (Me.GetChargesForBuff(bladeVortexCounterBuff) <=3)
                {
                    return true;
                }
                return false;
            },
                new Sequence(
                    new Action(delegate
                    {
  
                        return RunStatus.Success;
                    }),
                    
                    NecroBuffs.ComboHotkey(x => Keys.LControlKey, y => Keys.W))

               );
        }

    }
}
