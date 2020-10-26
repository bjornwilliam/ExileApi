using System;
using Willplug;

namespace TreeRoutine.DefaultBehaviors.Helpers
{
    public class TreeHelper<TSettings, TCache>
        where TSettings : BaseTreeSettings, new()
        where TCache : BaseTreeCache, new()
    {
        public BaseTreeRoutinePlugin<TSettings, TCache> Core { get; set; }

        public Boolean CanTick()
        {
            if (Core.GameController.IsLoading)
            {
                if (Core.Settings.Debug)
                    Core.LogMessage("Game is loading...", 0.2f);
                return false;
            }
            if (!Core.GameController.Game.IngameState.ServerData.IsInGame)
            {
                if (Core.Settings.Debug)
                    Core.LogMessage("Currently not in the game (Charactor selection maybe).", 0.2f);
                return false;
            }
            else if (Core.GameController.Player == null || Core.GameController.Player.Address == 0 || !Core.GameController.Player.IsValid)
            {
                if (Core.Settings.Debug)
                    Core.LogMessage("Cannot find player info.", 0.2f);
                return false;
            }
            else if (!Core.GameController.Window.IsForeground())
            {
                if (Core.Settings.Debug)
                    Core.LogMessage("Poe is minimized.", 0.2f);
                return false;
            }
            //else if (Core.Cache.InTown)
            //{
            //    if (Core.Settings.Debug)
            //        Core.LogMessage("Player is in town.", 0.2f);
            //    return false;
            //}
            if (Core.Settings.Debug)
                Core.LogMessage("Can tick!.", 0.2f);

            return true;
        }



        public bool CanTickTown()
        {
            return CanTick() && Core.Cache.InTown;
        }
        public bool CanTickHideout()
        {
            return CanTick() && Core.Cache.InHideout;
        }

        public bool CanTickMap() 
        {
            return CanTick() && !Core.Cache.InTown && !Core.Cache.InHideout && Core.GameController.Player.IsAlive == true;
        }
        public bool CanTickDead()
        {

            bool canTickDead = CanTick() && !Core.Cache.InTown && !Core.Cache.InHideout && Core.GameController.Player.IsAlive ==false;
            if (canTickDead)
            {
                //Console.WriteLine("Can tick dead");
            }
            return canTickDead;
        }


    }



}
