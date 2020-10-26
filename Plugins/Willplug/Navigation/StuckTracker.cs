using ExileCore;
using ExileCore.Shared.Helpers;
using SharpDX;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Willplug.Navigation
{
    public static class StuckTracker
    {

        private static GameController GameController = WillBot.gameController;

        private static DateTime previousCheckForStuckTime = DateTime.Now;
        private static float accumulatedDistance = 0;
        private static Vector2 previousGridPosition = new Vector2(0, 0);
        private static int cumulativeDistanceSumsTimeframe = 1500;
        private static FixedSizedQueue cumuluativeDistanceMeasurementsQueue = new FixedSizedQueue(30);


        public static float GetDistanceMoved()
        {
            return cumuluativeDistanceMeasurementsQueue.GetSum();
        }

        public static void Update()
        {
            if (DateTime.Now.Subtract(previousCheckForStuckTime).TotalMilliseconds > (cumulativeDistanceSumsTimeframe / cumuluativeDistanceMeasurementsQueue.Size))
            {
                previousCheckForStuckTime = DateTime.Now;
                if (previousGridPosition.X == 0 && previousGridPosition.Y == 0)
                {
                    previousGridPosition = GameController.Player.GridPos;
                }
                cumuluativeDistanceMeasurementsQueue.Enqueue(GameController.Player.GridPos.Distance(previousGridPosition));
                accumulatedDistance = cumuluativeDistanceMeasurementsQueue.GetSum();
                if (accumulatedDistance < 15)
                {

                }
                previousGridPosition = GameController.Player.GridPos;
            }
        }

    }
}
