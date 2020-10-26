using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TreeRoutine.TreeSharp;
using ExileCore;
using ExileCore.PoEMemory.MemoryObjects;
using ExileCore.Shared.Enums;
using ExileCore.Shared.Helpers;
using SharpDX;

using System.Diagnostics;
using System.Runtime;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Windows.Forms;
using Willplug.Navigation;
using Action = TreeRoutine.TreeSharp.Action;
using ExileCore.PoEMemory.Elements;
using ExileCore.PoEMemory.Components;

namespace Willplug.BotBehavior
{
    public static class ChestBehavior
    {
        private static WillPlayer Me => WillBot.Me;
        private static GameController GameController = WillBot.gameController;
        private static IngameUIElements IngameUIElements = GameController.IngameState.IngameUi;
        private static readonly Stopwatch ControlTimer = new Stopwatch();
        private static Camera Camera => WillBot.gameController.IngameState.Camera;

        public static Composite DoOpenNearbyChest()
        {
            return new Decorator(delegate
            {
                LoadChestWhiteListIfNotLoaded();
                var chestToOpen = Me?.ClosestChest;
                if (chestToOpen == null) return false;
                var whitelisted = chestWhitelist != null && chestWhitelist.Contains(chestToOpen?.Path) == true;
                if (whitelisted) return true;
                return false;
                //var playerPos = GameController.Player.Pos;
                //var entityDistanceToPlayer =
                //    Math.Sqrt(Math.Pow(playerPos.X - entityPos.X, 2) + Math.Pow(playerPos.Y - entityPos.Y, 2));
                //var isTargetable = chestToOpen.GetComponent<Targetable>().isTargetable;
                //var isTargeted = chestToOpen.GetComponent<Targetable>().isTargeted;
                //return false;
            }, new PrioritySelector(
             CommonBehavior.MoveTo(x => Me.ClosestChest?.GridPos, xyzPositionDelegate: x => Me.ClosestChest?.Pos,
                  spec: CommonBehavior.OpenablesMovementSpec),
             new Action(delegate
             {
                 var chestToOpen = Me.ClosestChest;
                 var chestComponent = chestToOpen.GetComponent<Chest>();
                 ControlTimer.Restart();
                 while (ControlTimer.ElapsedMilliseconds < 3500 && chestComponent?.IsOpened == false)
                 {
                     var entityPos = chestToOpen.Pos;
                     var entityScreenPos = Camera.WorldToScreen(entityPos.Translate(0, 0, 0));
                     Mouse.SetCursorPosAndLeftOrRightClick(entityScreenPos, 80);
                     Thread.Sleep(100);
                 }
                 return RunStatus.Failure;
             })
            ));
        }



        private static List<string> chestWhitelist;
        private static bool hasLoadedChestWhitelist = false;
        private static void LoadChestWhiteListIfNotLoaded()
        {
            if (hasLoadedChestWhitelist == false)
            {
                hasLoadedChestWhitelist = true;
                try
                {
                    chestWhitelist = File.ReadAllLines(WillBot.Plugin.DirectoryFullName + "\\chestWhitelist.txt").ToList();
                }
                catch (Exception)
                {
                    WillBot.LogMessageCombo("Exception when loading chest white list. Recursive");
                    File.Create(WillBot.Plugin.DirectoryFullName + "\\chestWhitelist.txt");
                    hasLoadedChestWhitelist = false;
                    LoadChestWhiteListIfNotLoaded();
                }
            }
        }




    }
}
