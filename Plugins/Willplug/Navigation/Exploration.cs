using Roy_T.AStar.Grids;
using SharpDX;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms.VisualStyles;

namespace Willplug.Navigation
{
    public class Exploration
    {

        public int InitialExploreSum { get; private set; }
        public int CurrentExploreSum { get; private set; }

        private const int remainingExploreSumThreshold = 9000;
        public float ExploredFactor
        {
            get
            {
                float explored = 1 - (CurrentExploreSum / (float)InitialExploreSum);
                return explored*100;
            }
        }


        private List<ExplorationGrid> grids = new List<ExplorationGrid>();

        private const float maxExploredFactor = 70f;

        public Exploration(byte[,] explorationGrid, int initialExploreSum)
        {
            InitialExploreSum = initialExploreSum;
            CurrentExploreSum = InitialExploreSum;

            int width = explorationGrid.GetLength(0);
            int height = explorationGrid.GetLength(1);
            int numberOfGrids = 3;
            int widthOfEachGrid = width / numberOfGrids;
            int heightOfEachGrid = height / numberOfGrids;

            // Create x grids
            for (int x = 0; x < numberOfGrids; x++)
            {
                for (int y = 0; y < numberOfGrids; y++)
                {
                    int xStart = widthOfEachGrid * x;
                    int yStart = heightOfEachGrid * y;
                    int clampedWidth = MyExtensions.Clamp<int>(widthOfEachGrid, 0, width - xStart);
                    int clampedHeight = MyExtensions.Clamp<int>(heightOfEachGrid, 0, height - yStart);
                    grids.Add(new ExplorationGrid(explorationGrid, xStart, yStart, clampedWidth, clampedHeight));
                }
            }
        }
        public Exploration(byte[,] explorationGrid)
        {
            var f = explorationGrid.Cast<byte>().ToArray();
            InitialExploreSum = f.Sum<byte>(x => (int)x);
            CurrentExploreSum = InitialExploreSum;

            int width = explorationGrid.GetLength(0);
            int height = explorationGrid.GetLength(1);
            int numberOfGrids = 3;
            int widthOfEachGrid = width / numberOfGrids;
            int heightOfEachGrid = height / numberOfGrids;

            // Create x grids
            for (int x = 0; x < numberOfGrids; x++)
            {
                for (int y= 0; y < numberOfGrids; y++)
                {
                    int xStart = widthOfEachGrid * x;
                    int yStart = heightOfEachGrid * y;
                    int clampedWidth = MyExtensions.Clamp<int>(widthOfEachGrid, 0, width - xStart);
                    int clampedHeight = MyExtensions.Clamp<int>(heightOfEachGrid, 0, height - yStart);
                    grids.Add(new ExplorationGrid(explorationGrid, xStart, yStart, clampedWidth, clampedHeight));
                }
            }
        }

        public (bool, Vector2) FindPositionToExplore(Vector2 currentPosition)
        {
            // Check current grid first to see if it can be explored further
            var gridIAmInside = grids.Find(x => x.IsPositionInside(currentPosition));
            if (gridIAmInside.GetExploredFactor() < maxExploredFactor && gridIAmInside.RemainingToExploreSum > remainingExploreSumThreshold)
            {
                WillBot.LogMessageCombo($"Exploring inside current grid. Remaining explore sum: {gridIAmInside.RemainingToExploreSum}");            
                // Find unexplored position inside this grid.
                return gridIAmInside.FindUnexploredPosition();
            }
            // FindClosesGrid with explored factor below a certain x
            var testGrids = grids.OrderBy(x => x.DistanceToCenterFromPos(currentPosition)).ToList().Where(
                x => x.GetExploredFactor() < maxExploredFactor && x.RemainingToExploreSum  > remainingExploreSumThreshold)?.ToList();
            if (testGrids == null || testGrids.Count == 0)
            {
                WillBot.LogMessageCombo($"Unable to find a grid to explore that satisfies exploration factor { maxExploredFactor}, and remaining explore sum: {remainingExploreSumThreshold}");
                return (false, new Vector2());
            }
            else
            {
                var unexploredPosition = testGrids.First().FindUnexploredPosition();
                WillBot.LogMessageCombo($"Found new grid to explore. Remaining explore sum: {testGrids.First().RemainingToExploreSum}");

                return unexploredPosition;
            }
        }

        public void UpdateExploration(int xStart, int yStart, int width, int height )
        {
            int exploredSumInAllGrids = 0;
            foreach (var grid in grids)
            {

                grid.UpdateExploration(xStart, yStart, width, height);
                exploredSumInAllGrids += grid.RemainingToExploreSum;

               // CurrentExploreSum = CurrentExploreSum - (grid.InitialExploreSum - grid.CurrentExploreSum);
            }
            CurrentExploreSum = exploredSumInAllGrids;
        }


    }
}
