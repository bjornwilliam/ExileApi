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
        private static BuffDebuffAbility frostBiteCurseAbility = new BuffDebuffAbility();
        private static BuffDebuffAbility steelSkinAbility = new BuffDebuffAbility();
        static CharacterAbilityTrees()
        {
            frostBiteCurseAbility.buffDebuffName = CharAbilities.frostbiteBuff;
            frostBiteCurseAbility.minimumIntervalBetweenUsagesMs = 100;
            frostBiteCurseAbility.activationComposite = CharAbilities.ComboHotkey(x => Keys.None, x => Keys.A);
            
            
            steelSkinAbility.activationComposite = CharAbilities.ComboHotkey(x => Keys.LControlKey, x => Keys.W);
            steelSkinAbility.skillCooldownMs = 3000;



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




        public static Composite CreateVortexCharAbilityTree()
        {
            return new Decorator(delegate
            {
                return WillBot.Plugin.TreeHelper.CanTickMap();
            },
             new PrioritySelector(
                CharAbilities.CreateBerserkComposite(),
                //CharAbilities.CreateUseFrostbiteComposite(),
                CharAbilities.CreateUseCurseComposite(frostBiteCurseAbility),
                CharAbilities.CreateUseGuardSkillComposite(steelSkinAbility)

                ));

        }



    }
}
