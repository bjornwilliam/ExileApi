using ExileCore;
using ExileCore.PoEMemory.Components;
using ExileCore.PoEMemory.Elements;
using ExileCore.PoEMemory.Elements.InventoryElements;
using ExileCore.PoEMemory.MemoryObjects;
using ExileCore.PoEMemory.Models;
using ExileCore.Shared;
using ExileCore.Shared.Abstract;
using ExileCore.Shared.Enums;
using ExileCore.Shared.Helpers;
//using ExileCore.Shared.Helpers;
using SharpDX;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using Input = ExileCore.Input;

namespace Willplug.Navigation
{
    static public class MoverHelper
    {


        static public void ClickToStopCharacter()
        {
            var gameWindowRect = WillBot.gameController.Window.GetWindowRectangle();
            var centerOfScreen = WillBot.gameController.Game.IngameState.Camera.WorldToScreen(WillBot.gameController.Player.Pos);
            centerOfScreen.X -= 100;

            int latency = (int)WillBot.gameController.IngameState.CurLatency;
            Mouse.SetCursorPosAndLeftOrRightClick(centerOfScreen,  latency);
            //Input.Click(MouseButtons.Left);
            //Thread.Sleep(50);
            //Input.MouseMove();

            //Input.SetCursorPos(gameWindowRect.TopLeft + centerOfScreen);
            //Thread.Sleep(50);
            //Input.Click(MouseButtons.Left);
            //Input.Click(MouseButtons.Left);
            //Input.LeftDown();
            //Thread.Sleep(5);
            //Input.LeftUp();

        }

        static public IEnumerator MouseAsJoystick(GameController gameController, float desiredAngle)
        {
            var gameWindowRect = gameController.Window.GetWindowRectangle();
            var centerOfScreen = gameController.Game.IngameState.Camera.WorldToScreen(gameController.Player.Pos);

            float mouseDistance = 0.2f * gameWindowRect.Height;
            float xPos = mouseDistance * (float)Math.Cos(desiredAngle * Math.PI / 180);
            float yPos = -mouseDistance * (float)Math.Sin(desiredAngle * Math.PI / 180);

            Vector2 mousePositionClient = new Vector2(centerOfScreen.X + xPos,
               centerOfScreen.Y + yPos);
            Vector2 mousePositionToClick = gameWindowRect.TopLeft + mousePositionClient;
            yield return Input.SetCursorPositionSmooth(mousePositionToClick);
        }
        static public void MouseAsJoystickNonSmooth(GameController gameController, float desiredAngle)
        {
            var gameWindowRect = gameController.Window.GetWindowRectangle();
            var centerOfScreen = gameController.Game.IngameState.Camera.WorldToScreen(gameController.Player.Pos);

            float mouseDistance = 0.25f * gameWindowRect.Height; // prev 0.15
            float xPos = mouseDistance * (float)Math.Cos(desiredAngle * Math.PI / 180);
            float yPos = -mouseDistance * (float)Math.Sin(desiredAngle * Math.PI / 180);

            Vector2 mousePositionClient = new Vector2(centerOfScreen.X + xPos,
               centerOfScreen.Y + yPos);
            Vector2 mousePositionToClick = gameWindowRect.TopLeft + mousePositionClient;
            Input.SetCursorPos(mousePositionToClick);
            Thread.Sleep(50);

            //yield return Input.SetCursorPositionSmooth(mousePositionToClick);
        }
    }
}
