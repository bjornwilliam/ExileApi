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

namespace Willplug.BotBehavior
{
    public static class BuffBehavior
    {
        public static string enduringCryLifeRegenBuff = "usemax_life_regen";
        public static string moltenShellShieldBuff = "molten_shell_shield";
        public static string berserkBuff = "berserk";
        public static string rageBuff = "rage";
        public static string onslaughtBuff = "onslaught";

        public static string dreadBannerBuff = "banner_add_stage_on_impale";

        public static string prideAuraBuff = "player_physical_damage_aura";
        public static string bloodAndSandAuraBloodStance = "blood_armour";
        public static string bloodAndSandAuraSandStance = "blood_armour";

        public static WillPlayer Me { get => WillBot.Me; }

        private static readonly Stopwatch ControlTimer = new Stopwatch();

        private static DateTime previousBerserkUseTime = DateTime.Now;


        public delegate Keys BuffHotkeyPrefix(object context);
        public delegate Keys BuffHotkeySuffix(object context);
        public delegate string BuffNameDelegate(object context);

        public static Composite ActivateBerserkerAuras()
        {
            return new PrioritySelector(
                ActivateAura(x => prideAuraBuff, x => Keys.LControlKey, x => Keys.E),
                ActivateAura(x => bloodAndSandAuraBloodStance, x => Keys.LControlKey, x => Keys.R),
                ActivateAura(x => dreadBannerBuff, x => Keys.LControlKey, x => Keys.W)

                );

        }
        public static Composite ActivateAura(BuffNameDelegate buffNameDelegate, BuffHotkeyPrefix buffHotkeyPrefix, BuffHotkeySuffix buffHotkeySuffix)
        {
            return new Decorator(delegate (object context)
            {
                try
                {
                    var buffName = buffNameDelegate?.Invoke(context) ?? "";
                    if (string.IsNullOrEmpty(buffName)) return false;

                    if (Me.playerDoesNotHaveAnyOfBuffs(new List<string>() { buffName }) == true)
                    {
                        return true;
                    }
                    return false;
                }
                catch
                {
                    return false;
                }

            }, new Action(delegate (object context)
            {
                try
                {
                    var keyPrefix = buffHotkeyPrefix?.Invoke(context);
                    if (keyPrefix != null)
                    {
                        Input.KeyDown((Keys)keyPrefix);
                    }
                    var keySuffix = buffHotkeySuffix?.Invoke(context);
                    if (keySuffix != null)
                    {
                        InputWrapper.KeyPress((Keys)keySuffix);
                    }
                    Input.KeyUp((Keys)keyPrefix);
                    Thread.Sleep(300);
                    return RunStatus.Failure;
                }
                catch (Exception ex)
                {
                    Thread.Sleep(300);
                    Console.WriteLine(ex.ToString());
                    return RunStatus.Failure;
                }
            }));
        }

        public static Composite CreateBerserkerBuffTree()
        {
            return new Decorator(delegate
            {

                //WillBot.LogMessageCombo("In buffs decorator");
                //Console.WriteLine("In buffs decorator");    
                return WillBot.Plugin.TreeHelper.CanTickMap();
            },
             new PrioritySelector(
                CreateUseEnduringCryComposite(),
                CreateMoltenShellComposite(),
                CreateBerserkComposite()
                ));
        }


        public static Composite CreateUseEnduringCryComposite()
        {
            return new Decorator(delegate
            {
                // WillBot.LogMessageCombo("In create using enduring composite");
                //if (Me.playerDoesNotHaveAnyOfBuffs(new List<string>() { onslaughtBuff })) // add + some other condition
                //{
                //    return true;
                //}
                if (Me.isHealthBelowPercentage(80) && Me.playerDoesNotHaveAnyOfBuffs(new List<string>() { enduringCryLifeRegenBuff }))
                {
                    return true;
                }
                return false;
            },
                 new UseHotkeyAction(WillBot.KeyboardHelper, x => Keys.Space)
               );
        }

        static public Composite CreateMoltenShellComposite()
        {
            return new Decorator(x => Me.isHealthBelowPercentage(55),
                new Decorator(x => Me.playerDoesNotHaveAnyOfBuffs(new List<string>() { moltenShellShieldBuff }),
                 new UseHotkeyAction(WillBot.KeyboardHelper, x => Keys.A)
               ));
        }

        static int berserkCooldownMs = 3900;
        static bool berserkActiveLastCheck = false;
        static bool activateBerserkNow = false;
        static DateTime berserkEndedTime = DateTime.Now;
        static public Composite CreateBerserkComposite()
        {
            // If berserk was active last check and not active now: note time of end berserk
            return new Sequence(
                new Action(delegate {

                    if (Me.playerDoesNotHaveAnyOfBuffs(new List<string>() { berserkBuff }) && berserkActiveLastCheck == true)
                    {
                        berserkEndedTime = DateTime.Now;
                    }

                    if (Me.playerDoesNotHaveAnyOfBuffs(new List<string>() { berserkBuff }) == false)
                    {
                        berserkActiveLastCheck = true;
                    }
                    else
                    {
                        berserkActiveLastCheck = false;
                    }
                    return RunStatus.Success;
 
                }),
                new DecoratorContinue(x => Me.playerDoesNotHaveAnyOfBuffs(new List<string>() { berserkBuff }) && DateTime.Now.Subtract(berserkEndedTime).TotalMilliseconds > berserkCooldownMs,
                    new Sequence(
                            new Action(delegate
                            {
                                Console.WriteLine("Activating berserk");
                                //previousBerserkUseTime = DateTime.Now;
                                return RunStatus.Success;
                            }),
                            new UseHotkeyAction(WillBot.KeyboardHelper, x => Keys.W))


                    )

                );


        }

        //return new Decorator(x => DateTime.Now.Subtract(previousBerserkUseTime).TotalMilliseconds > 4800,
        //    new Decorator(x => Me.playerDoesNotHaveAnyOfBuffs(new List<string>() { berserkBuff }),
        //    new Decorator(x=> Me.GetChargesForBuff(rageBuff) > 15,
        //    //new Decorator(x => !Me.playerDoesNotHaveAnyOfBuffs(new List<string>() { rageBuff }),
        //    new Sequence(
        //        new Action(delegate
        //        {
        //            Console.WriteLine("Activating berserk");
        //            previousBerserkUseTime = DateTime.Now;
        //            return RunStatus.Success;
        //        }),
        //        new UseHotkeyAction(WillBot.KeyboardHelper, x => Keys.W)
        //   ))));
        //static public Composite CreateUseBerserkOnCdComposite()
        //{
        //}



        /*Player -> Components -> actor -> actorskills
         * Enduring cry -> Cd 4 seconds , but check Stats-> VirtualCooldownSpeedPct
         * 
         */

        // Can have SharedCDSkillGroup


        // Can track if auras are on by checking for odd number of uses
        //Track auras by checking buff list..




    }
}
