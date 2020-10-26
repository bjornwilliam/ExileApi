using ExileCore;
using ExileCore.PoEMemory.Components;
using ExileCore.PoEMemory.Elements;
using ExileCore.PoEMemory.Elements.InventoryElements;
using ExileCore.PoEMemory.MemoryObjects;
using ExileCore.PoEMemory.Models;
using ExileCore.RenderQ;
using ExileCore.Shared;
using ExileCore.Shared.Abstract;
using ExileCore.Shared.Enums;
using ExileCore.Shared.Helpers;
using FloodSpill;
using FloodSpill.Utilities;
using Roy_T.AStar.Grids;
using Roy_T.AStar.Primitives;
//using ExileCore.Shared.Helpers;
using SharpDX;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Windows.Forms;
using TreeRoutine.TreeSharp;
using Willplug.Navigation;
using Input = ExileCore.Input;
namespace Willplug.Navigation
{

    public class AreaTransitionInfo
    {
        // AreaTransition ->label -> text ( text above the area transition)
        public string areaTransitionLabelText;
        public Vector2 areaTransitionGridPosition;
        public int locatedInIslandWithId { get; private set; }
        public AreaTransitionType transitionType;

        public int leadsToIslandWithId;
        //public Dictionary<int, int> island2islandDict = new Dictionary<int, int>();
        // Need to check that gridPos of areatransition is within a radius of current island id
        public bool hasBeenEntered = false;
        // Zone transition = new areahash, else transition inside map data of same areahash
        public bool isZoneTransition = false;

        public uint leadsToZoneWithAreaHash;
        public uint locatedInZoneWithAreaHash;


        public AreaTransitionInfo(string text, Vector2 pos, int locatedInIslandWithId)
        {
            this.areaTransitionLabelText = text;
            this.areaTransitionGridPosition = pos;
            this.locatedInIslandWithId = locatedInIslandWithId;
        }
    }

    public class ZoneMap
    {

        //public Grid Grid => pathFindingGrid;
        //private Grid pathFindingGrid;
        private byte[,] processedFullSizeMapDataForPathFinding;
        private byte[,] processedFullSizeMapDataForExploration;


        // Islands. island = isolated walkable region in map data
        private int numberOfIslands;
        private int[,] islandProcessedMapDataForPathFinding;
        // Key is the island id
        public Dictionary<int, SubMap> SubMaps { get; private set; } = new Dictionary<int, SubMap>();

        // Key is grid pos converted to int of the itemonground 
        public Dictionary<Tuple<int, int>, AreaTransitionInfo> FoundAreaTransitions { get; private set; } = new Dictionary<Tuple<int, int>, AreaTransitionInfo>();
        public ZoneMap(byte[] rawMapData, int bytesPerRow, long cols, long rows)
        {
            InitializePathFindingVariables(rawMapData, bytesPerRow, cols, rows);
            //CreateImageFromPathfindingGrid();
            //PopulateMapBoundaryList(); // Used for "maphack"
        }

        private List<string> blackListedTransitions = new List<string>() { "sacred grove", "syndicate laboratory", "trial of" };
        public void TryAddAreaTransition(Vector2 playerPosition, LabelOnGround label)
        {
            try
            {
                int currentIslandId = GetSubMap(playerPosition).IslandId;
                var keyTuple = new Tuple<int, int>((int)label.ItemOnGround.GridPos.X, (int)label.ItemOnGround.GridPos.Y);
                string areaTransitionText = label?.Label?.Text?.ToLower() ?? "";
                foreach (var blackListedTransition in blackListedTransitions)
                {
                    if (areaTransitionText.Contains(blackListedTransition))
                    {
                        WillBot.LogMessageCombo($"Area transition is blacklisted {blackListedTransition}");
                        return;
                    }
                }

                WillBot.LogMessageCombo($"Area transition text: {areaTransitionText}");
                if (FoundAreaTransitions.ContainsKey(keyTuple) == false)
                {
                    var areaTransitionComponent = label.ItemOnGround.GetComponent<AreaTransition>();
                    var newAreaTransition = new AreaTransitionInfo(label.Label.Text, label.ItemOnGround.GridPos, currentIslandId);
                    newAreaTransition.transitionType = areaTransitionComponent.TransitionType;

                    WillBot.LogMessageCombo("added area transition to zonemap");
                    FoundAreaTransitions.Add(keyTuple, new AreaTransitionInfo(label.Label.Text, label.ItemOnGround.GridPos, currentIslandId));
                }
            }
            catch (Exception ex)
            {
                WillBot.LogMessageCombo("Failed adding area transition " + ex.ToString());

            }

        }

        public AreaTransitionInfo TryGetAreaTransition(LabelOnGround label)
        {
            try
            {
                var gridPosition = label.ItemOnGround.GridPos;
                var keyTuple = new Tuple<int, int>((int)gridPosition.X, (int)gridPosition.Y);
                return FoundAreaTransitions[keyTuple];
            }
            catch
            {
                return null;
            }
        }

        public Grid GetRoyTPathFindingGrid(Vector2 gridPosition)
        {
            var grid = GetSubMap(gridPosition)?.Grid;
            if (grid != null)
            {
                return grid;
            }
            else
            {
                int radius = 6;
                for (int i = 0; i < 100; i++)
                {
                    int xRandom = MathHepler.Randomizer.Next((int)gridPosition.X - radius, (int)gridPosition.X + radius);
                    int yRandom = MathHepler.Randomizer.Next((int)gridPosition.Y - radius, (int)gridPosition.Y + radius);
                    var randomGridPosition = new Vector2(xRandom, yRandom);
                    grid = GetSubMap(gridPosition)?.Grid;
                    if (grid != null)
                    {
                        return grid;
                    }
                }
            }
            return null;
        }

        public int TryGetIslandId(int xGridPos, int yGridPos)
        {
            try
            {
                int islandId = -1;
                if (xGridPos <= islandProcessedMapDataForPathFinding.GetUpperBound(0) &&
                    yGridPos <= islandProcessedMapDataForPathFinding.GetUpperBound(1))
                {

                    islandId = islandProcessedMapDataForPathFinding[xGridPos, yGridPos];
                    if (islandId != 0 && islandId != -1)
                    {
                        return islandId;
                    }
                }
                int startX = MyExtensions.Clamp<int>(xGridPos - 4, 0, islandProcessedMapDataForPathFinding.GetUpperBound(0));
                int endX = MyExtensions.Clamp<int>(xGridPos + 4, 0, islandProcessedMapDataForPathFinding.GetUpperBound(0));
                int startY = MyExtensions.Clamp<int>(yGridPos - 4, 0, islandProcessedMapDataForPathFinding.GetUpperBound(1));
                int endY = MyExtensions.Clamp<int>(yGridPos + 4, 0, islandProcessedMapDataForPathFinding.GetUpperBound(1));
                for (int x = startX; x <= endX; x++)
                {
                    for (int y = startY; y <= endY; y++)
                    {
                        islandId = islandProcessedMapDataForPathFinding[x, y];
                        if (islandId != 0 && islandId != -1)
                        {
                            return islandId;
                        }
                    }
                }
                return islandId;

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return -1;
            }
        }
        public SubMap GetSubMap(Vector2 gridPosition, bool initializeIfNotPresent = false)
        {
            int islandId = TryGetIslandId((int)gridPosition.X, (int)gridPosition.Y);
            if (islandId == 0 || islandId == -1)
            {
                Console.WriteLine($"Unable to find any valid island id near pos: {gridPosition}.");
                return null;
            }
            if (initializeIfNotPresent == true)
            {
                //InitializeSubMap(islandId);
            }
            SubMap subMap;
            bool gotSubMap = SubMaps.TryGetValue(islandId, out subMap);
            if (gotSubMap) return subMap;
            return null;
        }
        public SubMap GetSubMap(int islandId)
        {
            try
            {
                return SubMaps[islandId];
            }
            catch
            {
                return null;
            }
        }
        private void InitializeSubMap(int islandId)
        {
            if (islandId > 0 && islandId <= numberOfIslands)
            {
                if (SubMaps.ContainsKey(islandId) == false)
                {
                    Console.WriteLine($"Creating submap with island id: {islandId}");
                    SubMaps[islandId] = new SubMap(processedFullSizeMapDataForPathFinding, processedFullSizeMapDataForExploration, islandProcessedMapDataForPathFinding, islandId);
                }
            }
        }

        public void InitializePathFindingVariables(byte[] rawMapData, int bytesPerRow, long cols, long rows)
        {
            processedFullSizeMapDataForPathFinding = new byte[cols * Terrain.MapCellSizeI, rows * Terrain.MapCellSizeI];
            Terrain.PopulatePathfindingGrid(rawMapData, bytesPerRow, cols, rows, processedFullSizeMapDataForPathFinding);
            processedFullSizeMapDataForExploration = processedFullSizeMapDataForPathFinding.Clone() as byte[,];
            IdIslands(rawMapData, bytesPerRow, cols, rows);
        }


        private void IdIslands(byte[] rawMapData, int bytesPerRow, long cols, long rows)
        {
            var startTime = DateTime.Now;
            const int OutsideIslandsUnwalkable = -1;

            var baseMapArray = new byte[cols * Terrain.MapCellSizeI, rows * Terrain.MapCellSizeI];
            Terrain.PopulatePathfindingGrid(rawMapData, bytesPerRow, cols, rows, baseMapArray);

            islandProcessedMapDataForPathFinding = new int[cols * Terrain.MapCellSizeI, rows * Terrain.MapCellSizeI];
            var markArray = new int[cols * Terrain.MapCellSizeI, rows * Terrain.MapCellSizeI];

            var floodSpiller = new FloodSpiller();
            Predicate<int, int> positionQualifier = (x, y) => baseMapArray[x, y] == 0;
            var floodParameters = new FloodParameters(startX: 0, startY: 0)
            {
                ProcessStartAsFirstNeighbour = true,
                NeighbourhoodType = NeighbourhoodType.Eight,
                Qualifier = positionQualifier,
                NeighbourProcessor = (x, y, mark) =>
                {
                    islandProcessedMapDataForPathFinding[x, y] = OutsideIslandsUnwalkable;
                }
            };
            floodSpiller.SpillFlood(floodParameters, markArray);
            int currentIslandId = 1;
            int currentStartX = 0;
            int currentStartY = 0;
            int sumOfCellsInCurrentIsland = 0;
            while (true)
            {
                var ret = GetPositionOfFirstMatchingValue(islandProcessedMapDataForPathFinding, 0);
                if (ret.Item1 == -1)
                {
                    // done. No more islands
                    break;
                }
                // add new submap

                SubMaps[currentIslandId] = new SubMap(currentIslandId, (int)cols * Terrain.MapCellSizeI, (int)rows * Terrain.MapCellSizeI);

                currentStartX = ret.Item1;
                currentStartY = ret.Item2;
                Predicate<int, int> positionQualifierIslands = (x, y) => islandProcessedMapDataForPathFinding[x, y] == 0;
                floodParameters = new FloodParameters(startX: currentStartX, startY: currentStartY)
                {
                    ProcessStartAsFirstNeighbour = true,
                    NeighbourhoodType = NeighbourhoodType.Eight,
                    Qualifier = positionQualifierIslands,
                    NeighbourProcessor = (x, y, mark) =>
                    {
                        islandProcessedMapDataForPathFinding[x, y] = currentIslandId;
                        if (processedFullSizeMapDataForPathFinding[x, y] == 1)
                        {
                            SubMaps[currentIslandId].SetExplorationGridValue(x, y, 1);
                            SubMaps[currentIslandId].AddNodeWithPosition(x, y);
                        }
                        sumOfCellsInCurrentIsland += 1;
                    }
                };
                floodSpiller.SpillFlood(floodParameters, markArray);
                Console.WriteLine("Sum of current cells: {0}", sumOfCellsInCurrentIsland);
                if (sumOfCellsInCurrentIsland < 50)
                {
                    SubMaps.Remove(currentIslandId);
                    // erase the area. F.ex sarn ramparts has a few rogue walkable pixels in the middle of nowhere
                    Predicate<int, int> eraseQualifier = (x, y) => islandProcessedMapDataForPathFinding[x, y] == currentIslandId;
                    floodParameters = new FloodParameters(startX: currentStartX, startY: currentStartY)
                    {
                        ProcessStartAsFirstNeighbour = true,
                        NeighbourhoodType = NeighbourhoodType.Eight,
                        Qualifier = eraseQualifier,
                        NeighbourProcessor = (x, y, mark) =>
                        {
                            islandProcessedMapDataForPathFinding[x, y] = OutsideIslandsUnwalkable;
                        }
                    };
                }
                else
                {
                    SubMaps[currentIslandId].InitializeExploration();
                    currentIslandId += 1;
                }
                sumOfCellsInCurrentIsland = 0;
            }
            numberOfIslands = currentIslandId - 1;
            //SaveArrayAsImage(islandProcessedMapDataForPathFinding);
            Console.WriteLine($"Id islands took: {DateTime.Now.Subtract(startTime).TotalMilliseconds} milliseconds");
        }


        private (int, int) GetPositionOfFirstMatchingValue(int[,] array, int valueToMatch)
        {
            for (int x = 0; x < array.GetLength(0); x++)
            {
                for (int y = 0; y < array.GetLength(1); y++)
                {
                    if (array[x, y] == valueToMatch)
                    {
                        return (x, y);
                    }
                }
            }
            return (-1, -1);
        }




        public GridPosition PoEGridPositionToRoyTPathFindingGridPosition(Vector2 position)
        {
            return new GridPosition((int)(position.X / PathfindingConsts.myPathfindingGridSize), (int)(position.Y / PathfindingConsts.myPathfindingGridSize));
        }

        public Roy_T.AStar.Primitives.Position PoEGridPositionToRoyTPathFindingGraphPosition(Vector2 position)
        {
            return new Roy_T.AStar.Primitives.Position((int)(position.X / PathfindingConsts.myPathfindingGridSize), (int)(position.Y / PathfindingConsts.myPathfindingGridSize));
        }


        public void UpdateExploration(Vector2 playerGridPos)
        {
            if (processedFullSizeMapDataForExploration == null) return;
            // If I flip all 1s to 0s it can make calculating unexplored regions easier..
            int radius = 80;
            int centerX = (int)playerGridPos.X;
            int centerY = (int)playerGridPos.Y;
            //var cameraToGridAngle = 0.25 * Math.PI;
            int startX = MyExtensions.Clamp<int>(centerX - radius, 0, int.MaxValue);
            int startY = MyExtensions.Clamp<int>(centerY - radius, 0, int.MaxValue);
            int stopX = MyExtensions.Clamp<int>(centerX + radius, 0, processedFullSizeMapDataForExploration.GetLength(0));
            int stopY = MyExtensions.Clamp<int>(centerY + radius, 0, processedFullSizeMapDataForExploration.GetLength(1));
            try
            {
                GetSubMap(playerGridPos, initializeIfNotPresent: false)?.Exploration?.UpdateExploration(startX, startY, stopX - startX, stopY - startY);
            }
            catch
            {
                Console.WriteLine("Update exploration exception: start X: {0}, stopX: {1}, startY: {2}, stopY: {3}", startX, stopX, startY, stopY);
            }
        }

        public bool GetIsWalkable(int x, int y)
        {
            return processedFullSizeMapDataForPathFinding[x, y] == 1;
        }

        public (bool, Vector2) FindNearestUnexploredPositionInRadius(Vector2 playerGridPos)
        {
            return GetSubMap(playerGridPos).Exploration.FindPositionToExplore(playerGridPos);
        }
        public int CheckForUnpassableTerrain(Vector2 gridPosStart, Vector2 gridPosEnd, int radius)
        {
            Vector2 centerPoint = new Vector2(0.5f * (gridPosStart.X + gridPosEnd.X), 0.5f * (gridPosStart.Y + gridPosEnd.Y));
            int sumOfUnwalkableTerrain = 0;
            float alpha = 0;
            float alphaSteps = (float)(2 * Math.PI / 360.0);
            HashSet<(int, int)> addedCoords = new HashSet<(int, int)>();
            for (int r = 0; r < radius; r++)
            {
                alpha = 0;
                while (alpha <= 2 * Math.PI)
                {
                    int x = (int)(centerPoint.X + r * Math.Cos(alpha));
                    int y = (int)(centerPoint.Y + r * Math.Sin(alpha));

                    if (x >= 0 && x < processedFullSizeMapDataForPathFinding.GetLength(0) && y >= 0 && y < processedFullSizeMapDataForPathFinding.GetLength(1))
                    {
                        if (addedCoords.Contains((x, y)) == false)
                        {
                            if (processedFullSizeMapDataForPathFinding[x, y] == 0)
                            {
                                sumOfUnwalkableTerrain += 1;
                            }
                            addedCoords.Add((x, y));
                        }
                    }
                    alpha += alphaSteps;
                }
            }
            Console.WriteLine($"Sum of unwalkable terrain between points is: {sumOfUnwalkableTerrain}");


            //float a = (gridPosEnd.Y - gridPosStart.Y) / (gridPosEnd.X - gridPosStart.X);
            //y = a * (x - gridPosStart.X) + gridPosStart.Y;
            //Perpendicual line
            // y = -1/a * (x - x0) + y0
            return sumOfUnwalkableTerrain;
        }

        public void CreateImageFromPathfindingGrid()
        {
            var bitmap = new Bitmap(processedFullSizeMapDataForPathFinding.GetLength(0), processedFullSizeMapDataForPathFinding.GetLength(1));
            for (var x = 0; x < bitmap.Width; x++)
            {
                for (var y = 0; y < bitmap.Height; y++)
                {
                    if (processedFullSizeMapDataForPathFinding[x, y] == 1)
                    {
                        bitmap.SetPixel(x, y, System.Drawing.Color.White);
                    }
                    else
                    {
                        bitmap.SetPixel(x, y, System.Drawing.Color.Black);
                    }
                }
            }

            var rotatedBitmap = MyExtensions.RotateBitmap(bitmap, 45);
            rotatedBitmap.RotateFlip(RotateFlipType.RotateNoneFlipY);
            rotatedBitmap.Save("pathfindingImage.png");
        }

        public void SaveArrayAsImage(int[,] array)
        {

            var bitmap = new Bitmap(array.GetLength(0), array.GetLength(1));
            for (var x = 0; x < bitmap.Width; x++)
            {
                for (var y = 0; y < bitmap.Height; y++)
                {
                    if (array[x, y] == -1)
                    {
                        bitmap.SetPixel(x, y, System.Drawing.Color.Black);
                    }
                    else if (array[x, y] == 1)
                    {
                        bitmap.SetPixel(x, y, System.Drawing.Color.White);
                    }
                    else if (array[x, y] == 2)
                    {
                        bitmap.SetPixel(x, y, System.Drawing.Color.Red);
                    }
                    else if (array[x, y] == 3)
                    {
                        bitmap.SetPixel(x, y, System.Drawing.Color.Purple);
                    }
                    else if (array[x, y] == 4)
                    {
                        bitmap.SetPixel(x, y, System.Drawing.Color.Cyan);
                    }
                    else if (array[x, y] == 5)
                    {
                        bitmap.SetPixel(x, y, System.Drawing.Color.DarkSlateGray);
                    }
                    else if (array[x, y] == 6)
                    {
                        bitmap.SetPixel(x, y, System.Drawing.Color.Magenta);
                    }
                    else if (array[x, y] == 7)
                    {
                        bitmap.SetPixel(x, y, System.Drawing.Color.GreenYellow);
                    }
                    else
                    {
                        var value = array[x, y];
                        bitmap.SetPixel(x, y, System.Drawing.Color.Blue);
                    }
                }
            }
            // Flip then rotate

            var rotatedBitmap = MyExtensions.RotateBitmap(bitmap, 45);
            rotatedBitmap.RotateFlip(RotateFlipType.RotateNoneFlipY);
            rotatedBitmap.Save("islandTest.png");
        }
        //  public HashSet<Vector2> mapBoundaryList = new HashSet<Vector2>();
        public List<Vector2> mapBoundaryList = new List<Vector2>();
        public void PopulateMapBoundaryList()
        {
            // Add all boundaries to x,y grid
            // Then add those to the list somehow
            mapBoundaryList.Clear();
            int width = processedFullSizeMapDataForPathFinding.GetLength(0);
            int height = processedFullSizeMapDataForPathFinding.GetLength(1);
            var pathFindingGrid = processedFullSizeMapDataForPathFinding;
            var markArray = new int[width, height];

            var outlineArray = new int[width, height];
            var floodSpiller = new FloodSpiller();
            var startTime = DateTime.Now;
            Predicate<int, int> positionQualifier = (x, y) =>
            {
                if (pathFindingGrid[x, y] == 1)
                {
                    outlineArray[x, y] = 1;
                    if (mapBoundaryList.Count > 0)
                    {

                        var current = new Vector2(x, y);
                        var count = mapBoundaryList.Where(vec => vec.DistanceSquared(current) < 10).Count();
                        if (count == 0)
                        {
                            mapBoundaryList.Add(new Vector2(x, y));
                        }
                        //var lastAdded = mapBoundaryList.Last();
                        //if (lastAdded.DistanceSquared(current) > 100)
                        //{
                        //    mapBoundaryList.Add(new Vector2(x, y));
                        //}
                    }
                    else
                    {
                        mapBoundaryList.Add(new Vector2(x, y));
                    }
                    return false;
                }
                else
                {
                    return true;
                }
            };
            var floodParameters = new FloodParameters(startX: 0, startY: 0)
            {
                ProcessStartAsFirstNeighbour = true,
                NeighbourhoodType = NeighbourhoodType.Eight,
                Qualifier = positionQualifier,
                NeighbourProcessor = (x, y, mark) =>
                {
                },
            };
            floodSpiller.SpillFlood(floodParameters, markArray);
            // Remove 
            var endTime = DateTime.Now;
            Console.WriteLine("elapsed boundary time: {0}", endTime.Subtract(startTime).TotalMilliseconds);

        }
    }

}


//int xStart = (int)playerGridPos.X;
//int yStart = (int)playerGridPos.Y;
//int startRadius = 200;
//int maxRadius = 400;
//double playerAngleRad = WillBot.gameController.Player.GetComponent<Positioned>().Rotation;

//double currentMapAngleRad = playerAngleRad - Math.PI / 2;

//            if (currentMapAngleRad< 0) currentMapAngleRad = 2 * Math.PI - currentMapAngleRad;


//            float angleRadIncrements = 5 * (float)Math.PI / 180f;
//            for (int radius = startRadius; radius<maxRadius; radius += 4)
//            {

//                while (true)
//                {
//                    int x = xStart + (int)(radius * Math.Cos(currentMapAngleRad));
//int y = yStart + (int)(radius * Math.Sin(currentMapAngleRad));

//x = MyExtensions.Clamp<int>(x, 0, processedFullSizeMapDataForExploration.GetLength(0) - 1);
//                    y = MyExtensions.Clamp<int>(y, 0, processedFullSizeMapDataForExploration.GetLength(1) - 1);

//                    if (processedFullSizeMapDataForExploration[x, y] == 1)
//                    {
//                        unexploredPosition = new Vector2(x, y);
//                        return true;
//                    }

//                    currentMapAngleRad += angleRadIncrements;

//                    if (currentMapAngleRad > (2 * Math.PI))
//                    {
//                        currentMapAngleRad = 0;
//                        break;
//                    }
//                }
//            }
//            unexploredPosition = new Vector2(0, 0);
//            return false;