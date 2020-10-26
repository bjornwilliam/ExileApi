using ExileCore;
using ExileCore.Shared.Cache;
using ExileCore.Shared.Enums;
using ExileCore.Shared.Helpers;
using SharpDX;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using TreeRoutine.DefaultBehaviors.Actions;
using TreeRoutine.DefaultBehaviors.Helpers;
using TreeRoutine.TreeSharp;
using Willplug.Navigation;
using Action = TreeRoutine.TreeSharp.Action;

namespace Willplug.BotBehavior
{
    public static class LoginBehavior
    {



        static private readonly Stopwatch ControlTimer = new Stopwatch();
        static private GameController GameController = WillBot.gameController;

        static private RectangleF loginButtonRect2560x1600 = new RectangleF(1624, 1030, 90, 35);
        static private RectangleF playButtonRect2560x1600 = new RectangleF(2257, 960, 90, 30);
        public static Composite Login()
        {
            return new DecoratorContinue(x => GameController.Game.IsLoginState == true,
                new Action(delegate
                {
                    Mouse.SetCursorPosAndLeftOrRightClick(loginButtonRect2560x1600, 250);
                    ControlTimer.Restart();
                    while (ControlTimer.ElapsedMilliseconds < 6000 && GameController.Game.IsLoginState)
                    {
                        Thread.Sleep(50);
                    }
                    if (GameController.Game.IsLoginState == true)
                    {
                        return RunStatus.Failure;
                    }
                    return RunStatus.Success;

                }
                ));
        }

        public delegate int CharacterPositionDelegate(object context);
        public static Composite TryEnterGameFromLoginScreen(CharacterPositionDelegate characterPositionDelegate)
        {
            return new Sequence(
                Login(),
                ChooseCharacterAndPressPlay(characterPositionDelegate)
                );
        }

        public static Composite ChooseCharacterAndPressPlay(CharacterPositionDelegate characterPositionDelegate)
        {
            return new DecoratorContinue(x => GameController.Game.IsSelectCharacterState == true,

                new Action(delegate (object context)
                {
                    for (int i = 0; i < 6; i++)
                    {
                        InputWrapper.KeyPress(Keys.Up);
                        Thread.Sleep(40);
                    }
                    for (int i = 1; i < characterPositionDelegate(context); i++)
                    {
                        InputWrapper.KeyPress(Keys.Down);
                        Thread.Sleep(40);
                    }

                    Mouse.SetCursorPosAndLeftOrRightClick(playButtonRect2560x1600, 250);
                    ControlTimer.Restart();
                    while (ControlTimer.ElapsedMilliseconds < 6000 && GameController.Game.IsSelectCharacterState)
                    {
                        Thread.Sleep(50);
                    }
                    if (GameController.Game.IsSelectCharacterState == false)
                    {
                        return RunStatus.Success;
                    }
                    return RunStatus.Failure;

                })
                );
        }



    }
}
