using ExileCore;
using ExileCore.PoEMemory.Elements.InventoryElements;
using ExileCore.Shared.Enums;
using SharpDX;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Security.Policy;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using TreeRoutine.TreeSharp;
using Willplug.SellItems;
using Action = TreeRoutine.TreeSharp.Action;


namespace Willplug.BotBehavior
{
    public static class LevelGemsBehavior
    {

        static private readonly Stopwatch ControlTimer = new Stopwatch();
        private static GameController GameController = WillBot.gameController;
        private static WillPlayer Me => WillBot.Me;
        private static int Latency => (int)GameController.Game.IngameState.CurLatency;




        private static List<RectangleF> gemRectsToLevelUp = new List<RectangleF>();
        private static DateTime lastCheckForGemsToLevelUp = DateTime.Now;
        private static bool GetGemsToLevelUp()
        {
            gemRectsToLevelUp.Clear();
            if (DateTime.Now.Subtract(lastCheckForGemsToLevelUp).TotalMilliseconds > 2000)
            {
                lastCheckForGemsToLevelUp = DateTime.Now;
                var gemsToLevelRectList = new List<RectangleF>();

                var gemElements = GameController.Game.IngameState.IngameUi.GemLvlUpPanel?.GetChildAtIndex(0);

                if (gemElements?.ChildCount > 0)
                {
                    foreach (var element in gemElements.Children)
                    {
                        var levelUpSymbol = element?.GetChildAtIndex(1);
                        if (levelUpSymbol != null)
                        {
                            gemRectsToLevelUp.Add(levelUpSymbol.GetClientRectCache);
                        }
                    }
                }
                if (gemRectsToLevelUp.Count > 0)
                {
                    return true;
                }
            }
            return false;
        }


        public static Composite LevelGems()
        {
            return new Decorator(x => GetGemsToLevelUp(),
                new Action(delegate
                {
                    Mouse.blockInput(true);
                    InputWrapper.ResetMouseButtons();
                    Thread.Sleep(80);
                    foreach (var rect in gemRectsToLevelUp)
                    {
                        Mouse.SetCursorPosAndLeftOrRightClick(rect, Latency);
                        Thread.Sleep(50);
                       
                    }
                    Mouse.blockInput(false);
                })
                );


        }

    }
}
