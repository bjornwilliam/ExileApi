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

namespace Willplug.BotBehavior
{
    public class BuffDebuffAbility
    {
        public DateTime previousTryToUseTime = DateTime.Now;
        public int minimumIntervalBetweenUsagesMs;
        public string buffDebuffName;

        public int skillCooldownMs;
        public Composite activationComposite;

        public Vector2 locationToUseAbility;
        public Vector2 mousePositionBeforeAbilityUsage;

        public bool leftMouseButtonPressedBeforeAbilityUsage = false;


    }
}
