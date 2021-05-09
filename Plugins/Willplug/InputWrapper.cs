using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Input = ExileCore.Input;

namespace Willplug
{
    static public class InputWrapper
    {



        static public void LeftMouseButtonDown()
        {
            if (Input.GetKeyState(Keys.LButton) == false) Input.LeftDown();
        }
        static public void LeftMouseButtonUp()
        {
            if (Input.GetKeyState(Keys.LButton) == true) Input.LeftUp();
        }

        static public void RightMouseButtonDown()
        {
            if (Input.GetKeyState(Keys.RButton) == false) Input.RightDown();
        }
        static public void RightMouseButtonUp()
        {
            if (Input.GetKeyState(Keys.RButton) == true) Input.RightUp();
        }

        static public void ResetMouseButtons()
        {
            RightMouseButtonUp();
            LeftMouseButtonUp();
        }

        public static void KeyPress(Keys key)
        {
            Input.KeyDown(key);
            Thread.Sleep(1);
            Input.KeyUp(key);
        }
        public static void KeyDown(Keys key)
        {
            if (Input.IsKeyDown(key) == false)
            {
                Input.KeyDown(key);
            }
        }
        public static void KeyUp(Keys key)
        {
            if (Input.IsKeyDown(key) == true)
            {
                Input.KeyUp(key);
            }
        }
    }
}
