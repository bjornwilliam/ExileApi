using System;
using System.Collections;
using System.Runtime.InteropServices;
using System.Threading;
using C5;
using ExileCore;
using ExileCore.Shared;
using SharpDX;
using ExileCore.PoEMemory.MemoryObjects;
using ExileCore.PoEMemory.Elements;
using System.Reflection.Emit;
using ExileCore.Shared.Enums;

namespace Willplug
{
    public class Mouse
    {

        private static GameController GameController = WillBot.gameController;
        public const int MOUSEEVENTF_MOVE = 0x0001;
        public const int MouseeventfLeftdown = 0x02;
        public const int MouseeventfLeftup = 0x04;
        public const int MouseeventfMiddown = 0x0020;
        public const int MouseeventfMidup = 0x0040;
        public const int MouseeventfRightdown = 0x0008;
        public const int MouseeventfRightup = 0x0010;
        public const int MouseEventWheel = 0x800;

        // 
        private const int MovementDelay = 10;
        private const int ClickDelay = 1;

        [DllImport("user32.dll")]
        public static extern bool SetCursorPos(int x, int y);

        [DllImport("user32.dll")]
        private static extern void mouse_event(int dwFlags, int dx, int dy, int cButtons, int dwExtraInfo);

        [DllImport("user32.dll")]
        public static extern bool BlockInput(bool fBlockIt);

        /// <summary>
        /// Sets the cursor position relative to the game window.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="gameWindow"></param>
        /// <returns></returns>
        public static bool SetCursorPos(int x, int y, RectangleF gameWindow)
        {
            return SetCursorPos(x + (int)gameWindow.X, y + (int)gameWindow.Y);
        }

        /// <summary>
        /// Sets the cursor position to the center of a given rectangle relative to the game window
        /// </summary>
        /// <param name="position"></param>
        /// <param name="gameWindow"></param>
        /// <returns></returns>
        public static bool SetCurosPosToCenterOfRec(RectangleF position, RectangleF gameWindow)
        {
            return SetCursorPos((int)(gameWindow.X + position.Center.X), (int)(gameWindow.Y + position.Center.Y));
        }

        /// <summary>
        /// Retrieves the cursor's position, in screen coordinates.
        /// </summary>
        /// <see>See MSDN documentation for further information.</see>
        [DllImport("user32.dll")]
        public static extern bool GetCursorPos(out Point lpPoint);

        public static SharpDX.Point GetCursorPosition()
        {
            GetCursorPos(out var lpPoint);
            return lpPoint;
        }

        public static void LeftMouseDown()
        {
            mouse_event(MouseeventfLeftdown, 0, 0, 0, 0);
        }

        public static void LeftMouseUp()
        {
            mouse_event(MouseeventfLeftup, 0, 0, 0, 0);
        }

        public static void RightMouseDown()
        {
            mouse_event(MouseeventfRightdown, 0, 0, 0, 0);
        }

        public static void RightMouseUp()
        {
            mouse_event(MouseeventfRightup, 0, 0, 0, 0);
        }


        public enum MyMouseClicks
        {
            NoClick,
            LeftClick,
            RightClick,
            MiddleClick
        }

        public static bool SetCursorPosAndLeftOrRightClick(Vector2 screenPos, int extraDelay, MyMouseClicks clickType = MyMouseClicks.LeftClick)
        {
            try
            {
                RectangleF clientRect = new RectangleF(screenPos.X, screenPos.Y, 1, 1);
                return SetCursorPosAndLeftOrRightClick(clientRect, extraDelay, clickType: clickType, randomClick: false);
            }
            catch
            {
                return false;
            }
        }


        public static bool SetCursorPosAndLeftOrRightClick(LabelOnGround labelOnGround, int extraDelay, MyMouseClicks clickType = MyMouseClicks.LeftClick, bool useCache = true, bool randomClick = false)
        {
            try
            {
                var label = labelOnGround?.Label;
                if (label == null) return false;

                RectangleF clientRect;
                if (useCache)
                {
                    clientRect = label.GetClientRectCache;
                }
                else
                {
                    clientRect = label.GetClientRect();
                }
                return SetCursorPosAndLeftOrRightClick(clientRect, extraDelay, clickType: clickType, randomClick: randomClick);
            }
            catch
            {
                return false;
            }
        }

        public static bool SetCursorPosAndLeftOrRightClick(RectangleF rect, int extraDelay, MyMouseClicks clickType = MyMouseClicks.LeftClick, bool randomClick = false)
        {
            try
            {
                var gameWindowRect = GameController.Window.GetWindowRectangle();
                int posX = 0;
                int posY = 0;
                if (randomClick)
                {
                    // Constrain to center +- x percent of width and height
                    int xRandomStart = (int)(rect.Center.X - 0.1f * rect.Width);
                    int xRandomStop = (int)(rect.Center.X + 0.1f * rect.Width);
                    int yRandomStart = (int)(rect.Center.Y - 0.1f * rect.Height);
                    int yRandomStop = (int)(rect.Center.Y + 0.1f * rect.Height);

                    posX = (int)gameWindowRect.TopLeft.X + ExileCore.Shared.Helpers.MathHepler.Randomizer.Next(xRandomStart, xRandomStop);
                    posY = (int)gameWindowRect.TopLeft.Y + ExileCore.Shared.Helpers.MathHepler.Randomizer.Next(yRandomStart, yRandomStop);
                }
                else
                {
                    posX = (int)gameWindowRect.TopLeft.X + (int)rect.Center.X;
                    posY = (int)gameWindowRect.TopLeft.Y + (int)rect.Center.Y;
                }
                var inventory = GameController.Game.IngameState.IngameUi.InventoryPanel[InventoryIndex.PlayerInventory];
                var inventoryRect = inventory.GetClientRectCache;
                RectangleF blacklistedRectangleIfPlayerInventoryVisible = new RectangleF(inventoryRect.TopRight.X, 0, inventoryRect.Width, inventoryRect.TopLeft.Y);
                var inputRect = new Rectangle((int)rect.X, (int)rect.Y, (int)rect.Width, (int)rect.Height);
                if (inventory.IsVisible == true && blacklistedRectangleIfPlayerInventoryVisible.Contains(inputRect) == true)
                {
                    WillBot.LogMessageCombo("Player inventory is visible and you tried to click inside the active items rectangle.");
                    return false;
                }

                // Clamp away x percent of screen height and width
                float clampFactor = 0.1f;
                posX = (int)MyExtensions.Clamp<float>(posX, gameWindowRect.TopLeft.X + 5, gameWindowRect.TopRight.X - 5);
                posY = (int)MyExtensions.Clamp<float>(posY, gameWindowRect.TopLeft.Y + 0.05f * gameWindowRect.Height, gameWindowRect.BottomLeft.Y - clampFactor * gameWindowRect.Height);
                SetCursorPos(posX, posY);
                Thread.Sleep(MovementDelay + extraDelay);
                switch (clickType)
                {
                    case MyMouseClicks.LeftClick:
                        LeftClick(ClickDelay);
                        break;
                    case MyMouseClicks.RightClick:
                        RightClick(ClickDelay);
                        break;
                    case MyMouseClicks.NoClick:
                        break;
                    case MyMouseClicks.MiddleClick:
                        break;
                }
                return true;
            }
            catch
            {
                return false;
            }

        }

        //public static void SetCursorPosAndLeftOrRightClick(Vector2 coords, int extraDelay, bool leftClick = true)
        //{
        //    var posX = (int)coords.X;
        //    var posY = (int)coords.Y;
        //    SetCursorPos(posX, posY);
        //    Thread.Sleep(MovementDelay + extraDelay);

        //    if (leftClick)
        //        LeftClick(ClickDelay);
        //    else
        //        RightClick(ClickDelay);
        //}

        public static void LeftClick(int extraDelay)
        {
            LeftMouseDown();
            if (extraDelay > 0) Thread.Sleep(ClickDelay);
            LeftMouseUp();
        }

        public static void RightClick(int extraDelay)
        {
            RightMouseDown();
            Thread.Sleep(ClickDelay);
            RightMouseUp();
        }

        public static void VerticalScroll(bool forward, int clicks)
        {
            if (forward)
                mouse_event(MouseEventWheel, 0, 0, clicks * 120, 0);
            else
                mouse_event(MouseEventWheel, 0, 0, -(clicks * 120), 0);
        }
        ////////////////////////////////////////////////////////////

        [StructLayout(LayoutKind.Sequential)]
        public struct Point
        {
            public int X;
            public int Y;

            public static implicit operator SharpDX.Point(Point point)
            {
                return new SharpDX.Point(point.X, point.Y);
            }
        }
        public static void blockInput(bool block)
        {
            BlockInput(block);
        }
        #region MyFix

        private static void SetCursorPosition(float x, float y)
        {
            SetCursorPos((int)x, (int)y);
        }

        public static Vector2 GetCursorPositionVector()
        {
            var currentMousePoint = GetCursorPosition();
            return new Vector2(currentMousePoint.X, currentMousePoint.Y);
        }

        public static void SetCursorPosition(Vector2 end)
        {
            var cursor = GetCursorPositionVector();
            var stepVector2 = new Vector2();
            var step = (float)Math.Sqrt(Vector2.Distance(cursor, end)) * 1.618f;
            if (step > 275) step = 240;
            stepVector2.X = (end.X - cursor.X) / step;
            stepVector2.Y = (end.Y - cursor.Y) / step;
            var fX = cursor.X;
            var fY = cursor.Y;

            for (var j = 0; j < step; j++)
            {
                fX += +stepVector2.X;
                fY += stepVector2.Y;
                SetCursorPosition(fX, fY);
                Thread.Sleep(2);
            }
        }

        public static void SetCursorPosAndLeftClickHuman(Vector2 coords, int extraDelay)
        {
            SetCursorPosition(coords);
            Thread.Sleep(MovementDelay + extraDelay);
            LeftMouseDown();
            Thread.Sleep(MovementDelay + extraDelay);
            LeftMouseUp();
        }

        public static void SetCursorPos(Vector2 vec)
        {
            SetCursorPos((int)vec.X, (int)vec.Y);
        }

        public static void MoveCursorToPosition(Vector2 vec)
        {

            SetCursorPos((int)vec.X, (int)vec.Y);
            MouseMove();
        }

        public static float speedMouse;

        public static IEnumerator SetCursorPosHuman(Vector2 vec)
        {
            var step = (float)Math.Sqrt(Vector2.Distance(GetCursorPositionVector(), vec)) * speedMouse / 20;

            if (step > 6)
            {
                for (var i = 0; i < step; i++)
                {
                    var vector2 = Vector2.SmoothStep(GetCursorPositionVector(), vec, i / step);
                    SetCursorPos((int)vector2.X, (int)vector2.Y);
                    yield return new WaitTime(1);
                }
            }
            else
                SetCursorPos(vec);
        }

        public static IEnumerator LeftClick()
        {
            LeftMouseDown();
            yield return new WaitTime(2);
            LeftMouseUp();
        }

        public static void MouseMove()
        {
            mouse_event(MOUSEEVENTF_MOVE, 0, 0, 0, 0);
        }

        #endregion
    }
}
