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
    public static class CharacterAbilityTrees
    {
        private static BuffDebuffAbility enfeebleCurseAbility = new BuffDebuffAbility();
        private static BuffDebuffAbility frostBiteCurseAbility = new BuffDebuffAbility();
        private static BuffDebuffAbility steelSkinAbility = new BuffDebuffAbility();
        private static BuffDebuffAbility bloodRageAbility = new BuffDebuffAbility();

        static CharacterAbilityTrees()
        {
            frostBiteCurseAbility.buffDebuffName = CharAbilities.frostbiteBuff;
            frostBiteCurseAbility.minimumIntervalBetweenUsagesMs = 2000;
            frostBiteCurseAbility.activationComposite = CharAbilities.ComboHotkey(x => Keys.LControlKey, x => Keys.Q);
            
            
            steelSkinAbility.activationComposite = CharAbilities.ComboHotkey(x => Keys.LControlKey, x => Keys.H);
            steelSkinAbility.skillCooldownMs = 500;

            enfeebleCurseAbility.buffDebuffName = CharAbilities.enfeebleBuff;
            enfeebleCurseAbility.minimumIntervalBetweenUsagesMs = 2000;
            enfeebleCurseAbility.activationComposite = CharAbilities.ComboHotkey(x => Keys.LControlKey, x => Keys.Q);
            
            
            bloodRageAbility.buffDebuffName = CharAbilities.bloodRageBuff;
            bloodRageAbility.minimumIntervalBetweenUsagesMs = 2000;
            bloodRageAbility.activationComposite = CharAbilities.ComboHotkey(x => Keys.LControlKey, x => Keys.Q);
        }
        public static Composite CreateNecroBuffTree()
        {
            return new Decorator(delegate
            {
                return WillBot.Plugin.TreeHelper.CanTickMap();
            },
             new PrioritySelector(
               CharAbilities.CreateUseEnduringCryComposite(),
                CharAbilities.CreateUseVortexComposite(),
                CharAbilities.CreateUseBoneArmorComposite(),
                CharAbilities.UseConvocation(),
                CharAbilities.CreateUseFrostbiteComposite(),
                CharAbilities.RaiseZombieComposite()
                ));
        }

        public static Composite ActivateNecroAuras()
        {
            return new PrioritySelector(
                CharAbilities.ActivateAura(x => CharAbilities.skitterBotsBuff, x => Keys.LControlKey, x => Keys.R),
                CharAbilities.ActivateAura(x => CharAbilities.hatredBuff, x => Keys.LControlKey, x => Keys.H),
                CharAbilities.ActivateAura(x => CharAbilities.carrionGolemBuff, x => Keys.None, x => Keys.W)
                
                );
        }
        public static Composite CreateBamaBuffTree()
        {
            return new Decorator(delegate
            {
                return WillBot.Plugin.TreeHelper.CanTickMap();
            },
             new PrioritySelector(
                    CharAbilities.CreateUseGuardSkillComposite(steelSkinAbility)
                    // Create snipers mark
                    // Create Flesh offering
                ));
        }





        public static Composite CreateVortexCharAbilityTree()
        {
            return new Decorator(delegate
            {
                return WillBot.Plugin.TreeHelper.CanTickMap();
            },
             new PrioritySelector(
                 //CharAbilities.SmokeMineMacroComposite()
                 //CharAbilities.CreateBerserkComposite()
                 //CharAbilities.CreateUseFrostbiteComposite()
                 //CharAbilities.CreateUseCurseComposite(frostBiteCurseAbility),
                 //LevelGemsBehavior.LevelGems()
                 //CharAbilities.CreateUseVortexComposite(),
                 //CharAbilities.CreateUseEnduringCryComposite()
                 //

                 CharAbilities.CreateUseGuardSkillComposite(steelSkinAbility),
                CharAbilities.CreateUseCurseComposite(enfeebleCurseAbility),
                CharAbilities.CreateUseVortexComposite(),
                CharAbilities.CreateBerserkComposite()
                )) ;

        }
        public static Composite CreateToxicRainTree()
        {
            return new Decorator(delegate
            {
                return WillBot.Plugin.TreeHelper.CanTickMap();
            },
             new PrioritySelector(
                 //CharAbilities.SmokeMineMacroComposite()
                 //CharAbilities.CreateBerserkComposite()
                 //CharAbilities.CreateUseFrostbiteComposite()
                 //CharAbilities.CreateUseCurseComposite(frostBiteCurseAbility),
                 //LevelGemsBehavior.LevelGems()
                 //CharAbilities.CreateUseVortexComposite(),
                 //CharAbilities.CreateUseEnduringCryComposite()
                 //

                 CharAbilities.CreateUseGuardSkillComposite(steelSkinAbility),
                 CharAbilities.ActivateBuffIfNotPresent(bloodRageAbility)
                //CharAbilities.CreateUseCurseComposite(enfeebleCurseAbility),
                //CharAbilities.CreateUseVortexComposite(),
                //CharAbilities.CreateBerserkComposite()
                ));

        }


    }
}
