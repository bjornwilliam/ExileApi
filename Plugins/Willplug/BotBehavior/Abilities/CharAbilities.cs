﻿using System;
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
    public static class CharAbilities
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
        public static string boneArmourBuff = "bone_armour";
        public static DateTime lastBoneOfferingUseTime = DateTime.Now;
        public static int minimumBoneOfferingIntervalMs = 7300;
        public static string boneOfferingBuff = "offering_defensive";
        public static string skitterBotsBuff = "skitterbots_buff";
        public static string hatredBuff = "player_aura_cold_damage";

        public static string frostbiteBuff = "curse_cold_weakness";
        public static int maxZombies = 8;
        public static string raisedZombieName = "RaisedZombieStandard";
        public static string carrionGolemBuff = "bone_golem_buff";
        public static string bladeVortexCounterBuff = "blade_vortex_counter";


        public static WillPlayer Me { get => WillBot.Me; }

        public static Camera Camera => WillBot.gameController.IngameState.Camera;

        private static readonly Stopwatch ControlTimer = new Stopwatch();

        private static DateTime previousVortexUseTime = DateTime.Now;
        private static DateTime previousEnduringCryUseTime = DateTime.Now;
        private static DateTime previousBoneArmourUseTime = DateTime.Now;
        private static DateTime previousConvocationUseTime = DateTime.Now;

        private static DateTime previousTryRaiseZombieTime = DateTime.Now;


        private static DateTime previousUseDousingFlaskTime = DateTime.Now;



        public delegate Keys BuffHotkeyPrefix(object context);
        public delegate Keys BuffHotkeySuffix(object context);
        public delegate string BuffNameDelegate(object context);




        public static Composite UseConvocation()
        {
            return new Decorator(delegate
            {
                if (Me.isHealthBelowPercentage(85) && DateTime.Now.Subtract(previousConvocationUseTime).TotalMilliseconds > 3050)
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
                }),
                new Inverter(new UseHotkeyAction(WillBot.KeyboardHelper, x => Keys.Space))
                ));
        }




        public static Composite RaiseZombieComposite()
        {
            return new Decorator(delegate
            {
                if (DateTime.Now.Subtract(previousTryRaiseZombieTime).TotalMilliseconds > 1000)
                {
                    previousTryRaiseZombieTime = DateTime.Now;
                    if (Me.enemies.NearbyCorpses?.Count() > 0 && Me.CountPlayerDeployedObjectsWithName(raisedZombieName) < 8)
                    {
                        return true;
                    }
                }
                return false;
            }, new Action(delegate
            {
                try
                {
                    var screenCorpsePos = Camera.WorldToScreen(Me.enemies.NearbyCorpses.First().Pos);
                    Mouse.SetCursorPosAndLeftOrRightClick(screenCorpsePos, 40, clickType: Mouse.MyMouseClicks.RightClick);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                }
                return RunStatus.Failure;

            })

            );

        }
        //   int zombieCount = Me.CountPlayerDeployedObjectsWithName(raisedZombieName);


        private static bool ShouldEntityBeCursed(Entity entity, string curseBuffName)
        {
            if (entity == null) return false;
            bool entityIsCorrectRarity = entity.Rarity == ExileCore.Shared.Enums.MonsterRarity.Rare || entity.Rarity == MonsterRarity.Unique;
            bool entityIsCloseEnough = entity.DistancePlayer < 60;
            bool entityIsNotAlreadyCursed = Me.entityDoesNotHaveAnyOfBuffs(entity, new List<string>() { curseBuffName }) == true;

            if (entityIsCloseEnough && entityIsCorrectRarity && entityIsNotAlreadyCursed)
            {
                return true;
            }
            return false;
        }

        private static bool ShouldEntityBeFrostbiteCursed(ExileCore.PoEMemory.MemoryObjects.Entity entity)
        {
            if (entity == null) return false;
            if (entity.Rarity == ExileCore.Shared.Enums.MonsterRarity.Rare || entity.Rarity == ExileCore.Shared.Enums.MonsterRarity.Unique)
            {
                if (Me.entityDoesNotHaveAnyOfBuffs(entity, new List<string>() { frostbiteBuff }) == true)
                {
                    return true;
                }
            }
            return false;
        }

        private static (bool, Vector3) TryFindEntityToCurse(List<Entity> entities, string curseBuffName)
        {
            if (Me.enemies.NearbyMonsters == null || Me.enemies.NearbyMonsters.Count == 0) return (false, new Vector3());

            foreach (var entity in entities)
            {
                if (ShouldEntityBeCursed(entity, curseBuffName) == true)
                {
                    return (true, entity.Pos);
                }
            }
            return (false, new Vector3());
        }
        private static (bool, Vector3) TryFindEntityToFrostbite(List<Entity> entities)
        {
            if (Me.enemies.NearbyMonsters == null || Me.enemies.NearbyMonsters.Count == 0) return (false, new Vector3());

            foreach (var entity in entities)
            {
                if (ShouldEntityBeFrostbiteCursed(entity) == true)
                {
                    return (true, entity.Pos);
                }
            }
            return (false, new Vector3());
        }


        private static DateTime previousCheckForFrostbiteTime = DateTime.Now;
        private static Vector2 positionToFrostBite = new Vector2();
        private static Vector2 previousMousePosition = new Vector2();

        public static Composite CreateUseCurseComposite(BuffDebuffAbility abilityDesc) // string curseBuffName,Keys hotkey)
        {
            return new Decorator(delegate
            {
                if (DateTime.Now.Subtract(abilityDesc.previousTryToUseTime).TotalMilliseconds > abilityDesc.minimumIntervalBetweenUsagesMs)
                {
                    abilityDesc.previousTryToUseTime = DateTime.Now;

                }
                else return false;
                var ret = TryFindEntityToCurse(Me.enemies.NearbyMonsters, abilityDesc.buffDebuffName);
                if (ret.Item1 == true)
                {
                    var worldCoords = ret.Item2;
                    var mobScreenCoords = Camera.WorldToScreen(worldCoords);
                    abilityDesc.locationToUseAbility = mobScreenCoords;
                    return true;
                }
                return false;
            },
            new Sequence(
                new Action(delegate
                {
                    abilityDesc.leftMouseButtonPressedBeforeAbilityUsage = Input.GetKeyState(Keys.LButton);
                    Mouse.blockInput(true);
                    Mouse.LeftMouseUp();
                    abilityDesc.mousePositionBeforeAbilityUsage = Mouse.GetCursorPosition();
                    Mouse.SetCursorPosAndLeftOrRightClick(abilityDesc.locationToUseAbility, 15, clickType: Mouse.MyMouseClicks.NoClick);

                    return RunStatus.Success;
                }),
                    abilityDesc.activationComposite,
                  // new UseHotkeyAction(WillBot.KeyboardHelper, x => abilityDesc.hotKey),
                  new Action(delegate
                  {
                      Mouse.SetCursorPosAndLeftOrRightClick(abilityDesc.mousePositionBeforeAbilityUsage, 15, clickType: Mouse.MyMouseClicks.NoClick);
                      Mouse.SetCursorPos(abilityDesc.mousePositionBeforeAbilityUsage);
                      if (abilityDesc.leftMouseButtonPressedBeforeAbilityUsage == true)
                      {
                          Mouse.LeftMouseDown();
                      }
                      Mouse.blockInput(false);
                      return RunStatus.Success;
                  })
                  ));
        }

        public static Composite CreateUseFrostbiteComposite()
        {
            return new Decorator(delegate
            {
                if (DateTime.Now.Subtract(previousCheckForFrostbiteTime).TotalMilliseconds > 1000)
                {
                    previousCheckForFrostbiteTime = DateTime.Now;
                }
                else return false;
                var ret = TryFindEntityToFrostbite(Me.enemies.NearbyMonsters);
                if (ret.Item1 == true)
                {
                    var worldCoords = ret.Item2;
                    var mobScreenCoords = Camera.WorldToScreen(worldCoords);
                    positionToFrostBite = mobScreenCoords;
                    return true;
                }
                return false;
            },
            new Sequence(
                new Action(delegate
                {
                    previousMousePosition = Mouse.GetCursorPosition();
                    Mouse.SetCursorPosAndLeftOrRightClick(positionToFrostBite, 15, clickType: Mouse.MyMouseClicks.NoClick);
                    return RunStatus.Success;
                }),
                  new UseHotkeyAction(WillBot.KeyboardHelper, x => Keys.A),
                  new Action(delegate
                  {
                      Mouse.SetCursorPos(previousMousePosition);
                      return RunStatus.Success;
                  })
                  ));
        }
        public static Composite CreateUseBoneOfferingComposite()
        {
            return new Decorator(delegate
            {
                if (WillBot.Me.enemies.ClosestMonsterEntity != null && (/*DateTime.Now.Subtract(lastBoneOfferingUseTime).TotalMilliseconds > 9000 ||*/ Me.playerDoesNotHaveAnyOfBuffs(new List<string>() { boneOfferingBuff }) == true))
                {
                    return true;
                }
                return false;
                // var corpses = Me.enemies.GetCorpsesForDesecrate();
            },
            new Sequence(
                UseDesecrate(),
                new Action(delegate
                {
                    lastBoneOfferingUseTime = DateTime.Now;
                    return RunStatus.Success;
                }),
                new UseHotkeyAction(WillBot.KeyboardHelper, x => Keys.A)));
        }

        public static Composite UseDesecrate()
        {
            return new UseHotkeyAction(WillBot.KeyboardHelper, x => Keys.R);
            // Use desecrate on player coordinate
        }

        // Automote desecrate + corpse buff
        private static bool previousBoneArmourState = false;
        public static Composite CreateUseBoneArmorComposite()
        {
            return new Decorator(delegate
            {

                bool noBoneArmourBuff = Me.playerDoesNotHaveAnyOfBuffs(new List<string>() { boneArmourBuff });

                if (noBoneArmourBuff != previousBoneArmourState)
                {
                    previousBoneArmourUseTime = DateTime.Now;
                }
                previousBoneArmourState = noBoneArmourBuff;

                if (Me.isHealthBelowPercentage(85) && DateTime.Now.Subtract(previousBoneArmourUseTime).TotalMilliseconds > 3050)
                {
                    return true;
                }
                return false;
            },
                   ComboHotkey(x => Keys.LControlKey, y => Keys.Q)

              );
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
                if (Me.isHealthBelowPercentage(70) && DateTime.Now.Subtract(previousEnduringCryUseTime).TotalMilliseconds > 700 && Me.playerDoesNotHaveAnyOfBuffs(new List<string>() { enduringCryLifeRegenBuff }))
                {
                    return true;
                }
                return false;
            },
                new Sequence(
                    new Action(delegate
                    {
                        previousEnduringCryUseTime = DateTime.Now;
                        return RunStatus.Success;
                    }),

                    ComboHotkey(x => Keys.LControlKey, y => Keys.W))

               );
        }

        public static Composite CreateUseGuardSkillComposite(BuffDebuffAbility abilityDesc)
        {
            return new Decorator(delegate
            {
                if (Me.isHealthBelowPercentage(70) && DateTime.Now.Subtract(abilityDesc.previousTryToUseTime).TotalMilliseconds > abilityDesc.skillCooldownMs)
                {
                    abilityDesc.previousTryToUseTime = DateTime.Now;
                    return true;
                }
                return false;
            },

                abilityDesc.activationComposite
           );
        }


        public static Composite CreateUseVortexComposite()
        {
            return new Decorator(delegate
            {

                if (WillBot.Me?.enemies?.ClosestMonsterEntity?.DistancePlayer < 80 && DateTime.Now.Subtract(previousVortexUseTime).TotalMilliseconds > 1100)
                {
                    return true;
                }
                return false;
            },
                new Sequence(
                    new Action(delegate
                    {
                        previousVortexUseTime = DateTime.Now;
                        return RunStatus.Success;
                    }),
                     ComboHotkey(x => Keys.LControlKey, y => Keys.M))

               );
        }


        static public Composite CreateMoltenShellComposite()
        {
            return new Decorator(x => Me.isHealthBelowPercentage(55),
                new Decorator(x => Me.playerDoesNotHaveAnyOfBuffs(new List<string>() { moltenShellShieldBuff }),
                 new UseHotkeyAction(WillBot.KeyboardHelper, x => Keys.A)
               ));
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
            }, ComboHotkey(buffHotkeyPrefix, buffHotkeySuffix));
        }
        public static Composite ComboHotkey(BuffHotkeyPrefix buffHotkeyPrefix, BuffHotkeySuffix buffHotkeySuffix)
        {
            return new Action(delegate (object context)
            {
                try
                {
                    var keyPrefix = buffHotkeyPrefix?.Invoke(context);
                    if (keyPrefix != Keys.None)
                    {
                        Input.KeyDown((Keys)keyPrefix);
                    }
                    var keySuffix = buffHotkeySuffix?.Invoke(context);
                    if (keySuffix != Keys.None)
                    {
                        InputWrapper.KeyPress((Keys)keySuffix);
                    }
                    if (keyPrefix != Keys.None)
                    {
                        Input.KeyUp((Keys)keyPrefix);
                    }
                    return RunStatus.Success;
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                    return RunStatus.Failure;
                }
            });
        }

        public static Composite CreateUseBladeVortexComposite()
        {
            return new Decorator(delegate
            {
                // WillBot.LogMessageCombo("In create using enduring composite");
                //if (Me.playerDoesNotHaveAnyOfBuffs(new List<string>() { onslaughtBuff })) // add + some other condition
                //{
                //    return true;
                //}
                if (Me.GetChargesForBuff(bladeVortexCounterBuff) <= 3)
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

                    ComboHotkey(x => Keys.LControlKey, y => Keys.W))

               );
        }

        static int berserkCooldownMs = 3900;
        static bool berserkActiveLastCheck = false;
        static bool activateBerserkNow = false;
        static DateTime berserkEndedTime = DateTime.Now;
        static public Composite CreateBerserkComposite()
        {
            // If berserk was active last check and not active now: note time of end berserk
            return new Sequence(
                new Action(delegate
                {

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
                new Decorator(x => Me.playerDoesNotHaveAnyOfBuffs(new List<string>() { berserkBuff }) && DateTime.Now.Subtract(berserkEndedTime).TotalMilliseconds > berserkCooldownMs,
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

        public static Composite ActivateSoulOfArakaliDefensiveComposite()
        {
            return new Decorator(delegate
            {
                bool dousingFlaskHasUsesLeft = WillBot.Plugin.FlaskHelper.GetFlaskInfo(WillBot.Settings.DousingFlaskIndex.Value)?.TotalUses > 0;
                if (Me.isHealthBelowPercentage(90) && DateTime.Now.Subtract(previousUseDousingFlaskTime).TotalMilliseconds > 3400 && dousingFlaskHasUsesLeft)
                {
                    Console.WriteLine("using soul of arakaali");
                    return true;
                }
                return false;
            },
         new Sequence(
            ComboHotkey(x => WillBot.Settings.RighteousFirePrefixKey, y => WillBot.Settings.RighteousFireSuffixKey),
            new Action(delegate
            {
                previousUseDousingFlaskTime = DateTime.Now;
                Thread.Sleep(100);
                return RunStatus.Success;
            }),
            new UseHotkeyAction(WillBot.KeyboardHelper, x => WillBot.Settings.UseDousingFlaskKey)
         )
            );
        }
        public static Composite SmokeMineMacroComposite()
        {
            return new Decorator(delegate { return Me.Settings.SmokeMineMacroActivationKey.PressedOnce(); }, new Sequence(
                ComboHotkey(x => WillBot.Settings.SmokeMineMacroKeyPrefix, y => WillBot.Settings.SmokeMineMacroKeySuffix),
                new Action(delegate
                {
                    Thread.Sleep(280);
                    return RunStatus.Success;
                }),
                   new UseHotkeyAction(WillBot.KeyboardHelper, x => Keys.D)
                ));
        }


        /*Player -> Components -> actor -> actorskills
         * Enduring cry -> Cd 4 seconds , but check Stats-> VirtualCooldownSpeedPct
         * 
         */

        // Can have SharedCDSkillGroup


        // Can track if auras are on by checking for odd number of uses
        //Track auras by checking buff list..




    }
}
