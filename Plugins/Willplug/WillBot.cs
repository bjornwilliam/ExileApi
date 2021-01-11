using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using ExileCore;
using ExileCore.PoEMemory.Components;
using ExileCore.PoEMemory.Elements.InventoryElements;
using ExileCore.PoEMemory.MemoryObjects;
using ExileCore.Shared;
using ExileCore.Shared.Enums;
using ExileCore.Shared.Helpers;
using ExileCore.Shared.Nodes;

using Newtonsoft.Json;
using SharpDX;
using TreeRoutine;
using TreeRoutine.DefaultBehaviors.Helpers;
using Willplug.Navigation;

namespace Willplug
{
    public static class WillBot
    {

        static public GameController gameController;
        static public BaseTreeRoutinePlugin<WillplugSettings, BaseTreeCache> basePlugin;
        public static WillPlayer Me { get; set; }
        public static Mover Mover { get; set; }
        static public WillplugSettings Settings => basePlugin.Settings;

        static public BaseTreeRoutinePlugin<WillplugSettings, BaseTreeCache> Plugin => basePlugin;
        static public KeyboardHelper KeyboardHelper { get; set; } = null;

        static private int updateBotStateInterval = 1000;
        static private DateTime previousUpdateBotStateTime = DateTime.Now;


        static public bool isBotPaused = false;

        public static void LogMessageCombo(string msg)
        {
            Plugin.LogMessage(msg);
            Console.WriteLine(msg);
        }

        static public void SetCursorPositionOnTarget(Vector3 target)
        {
            var mousePositionClient = gameController.Game.IngameState.Camera.WorldToScreen(target);
            var gameWindowRect = gameController.Window.GetWindowRectangle();
            Vector2 mousePositionScreen = gameWindowRect.TopLeft + mousePositionClient;
            if (mousePositionScreen.PointInRectangle(gameWindowRect) == false)
            {

                float clampedX = MyExtensions.Clamp<float>(mousePositionScreen.X, gameWindowRect.TopLeft.X + 10, gameWindowRect.TopRight.X - 10);
                float clampedY = MyExtensions.Clamp<float>(mousePositionScreen.Y, gameWindowRect.TopLeft.Y + 10, gameWindowRect.BottomLeft.Y - 10);
                mousePositionScreen = new Vector2(clampedX, clampedY);
                Console.WriteLine("clamped mouse position in SetCursorPositionOnTarget");
                Input.SetCursorPos(mousePositionScreen);
            }
            else
            {
                Input.SetCursorPos(mousePositionScreen);
            }

        }


    }
}
