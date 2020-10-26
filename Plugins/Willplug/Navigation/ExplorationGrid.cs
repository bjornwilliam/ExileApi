using ExileCore.Shared.Helpers;
using SharpDX;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.AccessControl;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Willplug.Navigation
{

    // https://stackoverflow.com/questions/26548756/looking-for-a-faster-way-to-sum-arrays-in-c-sharp
    public class ExplorationGrid
    {
        private byte[,] explorationGrid;
        private int xStart;
        private int yStart;
        private int width;
        private int height;
        private int xEnd;
        private int yEnd;


        private int xFindUnexploredStart;
        private int yFindUnexploredStart;

        public int InitialExploreSum { get; private set; }
        public int RemainingToExploreSum { get; private set; }

        public Vector2 CenterOfGrid => centerOfGrid;
        private Vector2 centerOfGrid;

        public ExplorationGrid(byte[,] explorationGrid, int xStart, int yStart, int width, int height)
        {
            this.explorationGrid = explorationGrid;
            this.xStart = xStart;
            this.yStart = yStart;
            this.width = width;
            this.height = height;


            this.xEnd = xStart + width;
            this.yEnd = yStart + height;

            this.xFindUnexploredStart = xStart;
            this.yFindUnexploredStart = yStart;

            // Try out parallel for loops
            //int localInitialExploreSum = 0;
            //var sw = new Stopwatch();
            //sw.Restart();
            //Parallel.For(xStart, xEnd, () => 0, (j, loop, subtotal) =>
            //  {
            //      for (int y = yStart; y < yEnd; y++)
            //      {
            //          subtotal += explorationGrid[j, y];
            //      }
            //      return subtotal;
            //  }, (z) => Interlocked.Add(ref localInitialExploreSum, z));

            //Console.WriteLine($"Parallel.for took: {sw.ElapsedMilliseconds} ms");
            //InitialExploreSum = localInitialExploreSum;

            for (int x = xStart; x < xEnd; x++)
            {
                for (int y = yStart; y < yEnd; y++)
                {
                    InitialExploreSum += explorationGrid[x, y];
                }
            }
            RemainingToExploreSum = InitialExploreSum;
            centerOfGrid = new Vector2(xStart + width / 2, yStart + height / 2);
        }

        public (bool, Vector2) FindUnexploredPosition()
        {

            // 2d arrays are row, column. So first index (x) is row number
            // This can be substantially improved by using data from UpdateExploration function
            for (int x = xFindUnexploredStart; x < xEnd; x++)
            {
                for (int y = yStart; y < yEnd; y++)
                {
                    if (explorationGrid[x, y] == 1)
                    {
                        xFindUnexploredStart = x;
                        //yFindUnexploredStart = y; // Cannot store the column value, but row value should be good
                        return (true, new Vector2(x, y));
                    }
                }
            }
            return (false, new Vector2());
        }


        public float DistanceToCenterFromPos(Vector2 pos)
        {
            return centerOfGrid.Distance(pos);
        }

        public bool IsPositionInside(Vector2 position)
        {
            int xPosition = (int)position.X;
            int yPosition = (int)position.Y;
            if (xPosition >= xStart && xPosition <= xEnd && yPosition >= yStart && yPosition <= yEnd)
            {
                return true;
            }
            return false;
        }

        public void UpdateExploration(int xExplore, int yExplore, int widthToExplore, int heightToExplore)
        {
            // Find overlapping region if any
            int xToExploreEnd = xExplore + widthToExplore;
            int yToExploreEnd = yExplore + heightToExplore;
            int xToExploreStart = MyExtensions.Clamp<int>(xExplore, this.xStart, this.xEnd);
            int yToExploreStart = MyExtensions.Clamp<int>(yExplore, this.yStart, this.yEnd);
            xToExploreEnd = MyExtensions.Clamp<int>(xToExploreEnd, this.xStart, this.xEnd);
            yToExploreEnd = MyExtensions.Clamp<int>(yToExploreEnd, this.yStart, this.yEnd);
            for (int x = xToExploreStart; x < xToExploreEnd; x++)
            {
                for (int y = yToExploreStart; y < yToExploreEnd; y++)
                {
                    RemainingToExploreSum -= explorationGrid[x, y];
                    explorationGrid[x, y] = 0;
                }
            }
        }
        public float GetExploredFactor()
        {
            float explored = 1 - (RemainingToExploreSum / (float)InitialExploreSum);
            return explored * 100;
        }



    }
}
