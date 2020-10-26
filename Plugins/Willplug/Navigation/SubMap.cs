using ExileCore.PoEMemory.Elements;
using Roy_T.AStar.Graphs;
using Roy_T.AStar.Grids;
using Roy_T.AStar.Primitives;
using SharpDX;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Willplug.Navigation
{
    public class SubMap
    {
        public Exploration Exploration { get; private set; }
        public Grid Grid { get; private set; }
        // private byte[,] mapDataForPathFinding;
        private byte[,] mapDataForExploration;
        //private int[,] islandProcessedMapDataForPathFinding;
        public int IslandId { get; private set; }


        private int totalExploreSum = 0;
        public Dictionary<Position, Node> NodeDictionary { get; private set; } = new Dictionary<Position, Node>();


        private Velocity defaultVelocity = Velocity.FromKilometersPerHour(100);


        public SubMap(int islandId, int explorationGridColumns, int explorationGridRows)
        {
            this.mapDataForExploration = new byte[explorationGridColumns, explorationGridRows];
            this.IslandId = islandId;
        }

        public void InitializeExploration()
        {
            Exploration = new Exploration(mapDataForExploration, totalExploreSum);
        }
        public void SetExplorationGridValue(int x, int y, byte value)
        {
            if (this.mapDataForExploration[x, y] != value)
            {
                totalExploreSum += value;
                this.mapDataForExploration[x, y] = value;
            }    
        }
        public void AddNodeWithPosition(int x, int y)
        {
            int xPfPos = x / PathfindingConsts.myPathfindingGridSize;
            int yPfPos = y / PathfindingConsts.myPathfindingGridSize;
            var pos = new Position(xPfPos, yPfPos);
            if (!NodeDictionary.ContainsKey(pos))
            {
                var northPosToConnectTo = new Position(xPfPos, (int)(yPfPos - 1));
                var southPosToConnectTo = new Position(xPfPos, (int)(yPfPos + 1));
                var westPosToConnectTo = new Position((int)(xPfPos - 1), (int)(yPfPos));
                var eastPosToConnectTo = new Position((int)(xPfPos + 1), (int)(yPfPos));

                var northEastPosToConnectTo = new Position((int)(xPfPos + 1), (int)(yPfPos - 1));
                var southEastPosToConnectTo = new Position((int)(xPfPos + 1), (int)(yPfPos + 1));
                var northWestPosToConnectTo = new Position((int)(xPfPos - 1), (int)(yPfPos - 1));
                var southWestPosToConnectTo = new Position((int)(xPfPos - 1), (int)(yPfPos + 1));

                var newNode = new Node(pos);
                NodeDictionary.Add(pos, newNode);
                TryConnect(northPosToConnectTo, newNode);
                TryConnect(southPosToConnectTo, newNode);
                TryConnect(westPosToConnectTo, newNode);
                TryConnect(eastPosToConnectTo, newNode);

                TryConnect(northEastPosToConnectTo, newNode);
                TryConnect(southEastPosToConnectTo, newNode);
                TryConnect(northWestPosToConnectTo, newNode);
                TryConnect(southWestPosToConnectTo, newNode);
            }
            // need to add the node to the list and connect it to valid lateral and diagonal nodes
        }

        private void TryConnect(Position posExisting, Node newNode)
        {
            Node existingNode;
            bool nodeExists = NodeDictionary.TryGetValue(posExisting, out existingNode);
            if (nodeExists)
            {
                existingNode.Connect(newNode, defaultVelocity);
                newNode.Connect(existingNode, defaultVelocity);
            }
        }


        public SubMap(byte[,] processedFullSizeMapDataForPathFinding, byte[,] processedFullSizeMapDataForExploration, int[,] islandProcessedMapDataForPathFinding, int islandId)
        {
            var startTime = DateTime.Now;
            this.IslandId = islandId;
            this.mapDataForExploration = processedFullSizeMapDataForExploration.Clone() as byte[,];

            EraseNonIslandData(this.mapDataForExploration, islandProcessedMapDataForPathFinding, islandId);

            //var nodeA = new Node(new Position(1, 1));
            //var nodeB = new Node(new Position(2, 2));
            //nodeA.Connect(nodeB,2)

            int reducedWidth = processedFullSizeMapDataForPathFinding.GetLength(0) / PathfindingConsts.myPathfindingGridSize;
            int reducedHeight = processedFullSizeMapDataForPathFinding.GetLength(1) / PathfindingConsts.myPathfindingGridSize;
            var gridSize = new GridSize(columns: reducedWidth, rows: reducedHeight);
            var cellSize = new Size(Distance.FromMeters(PathfindingConsts.myPathfindingGridSize), Distance.FromMeters(PathfindingConsts.myPathfindingGridSize));
            var traversalVelocity = Velocity.FromKilometersPerHour(100);
            var startTimeGridCreate = DateTime.Now;
            Grid = Grid.CreateGridWithLateralAndDiagonalConnections(gridSize, cellSize, traversalVelocity);
            Console.WriteLine($"Creating grid took: {DateTime.Now.Subtract(startTimeGridCreate).TotalMilliseconds} milliseconds");

            //Parallel.For(0, reducedWidth, x =>
            // {

            var startTimeGridPrep = DateTime.Now;
            for (int x = 0; x < reducedWidth; x++)
            {
                for (int y = 0; y < reducedHeight; y++)
                {
                    int xAdjusted = x * PathfindingConsts.myPathfindingGridSize;
                    int yAdjusted = y * PathfindingConsts.myPathfindingGridSize;

                    if (processedFullSizeMapDataForPathFinding[xAdjusted, yAdjusted] == 0 || islandProcessedMapDataForPathFinding[xAdjusted, yAdjusted] != islandId)
                    {
                        // Grid.RemoveDiagonalConnectionsIntersectingWithNode(new GridPosition(x, y));
                        Grid.DisconnectNode(new GridPosition(x, y));
                    }
                    //if (processedFullSizeMapDataForPathFinding[xAdjusted, yAdjusted] != 0 && islandProcessedMapDataForPathFinding[xAdjusted, yAdjusted] != islandId)
                    //{
                    //    Grid.RemoveDiagonalConnectionsIntersectingWithNode(new GridPosition(x, y));
                    //    Grid.DisconnectNode(new GridPosition(x, y));
                    //}
                }
            }
            Console.WriteLine($"Prepping grid took: {DateTime.Now.Subtract(startTimeGridPrep).TotalMilliseconds} milliseconds");

            //});

            //for (int x = 0; x < reducedWidth; x++)
            //{
            //    for (int y = 0; y < reducedHeight; y++)
            //    {
            //        int xAdjusted = x * PathfindingConsts.myPathfindingGridSize;
            //        int yAdjusted = y * PathfindingConsts.myPathfindingGridSize;

            //        if (processedFullSizeMapDataForPathFinding[xAdjusted, yAdjusted] == 0)
            //        {
            //            // Grid.RemoveDiagonalConnectionsIntersectingWithNode(new GridPosition(x, y));
            //            Grid.DisconnectNode(new GridPosition(x, y));
            //        }
            //        if (processedFullSizeMapDataForPathFinding[xAdjusted, yAdjusted] != 0 && islandProcessedMapDataForPathFinding[xAdjusted, yAdjusted] != islandId)
            //        {
            //            //Grid.RemoveDiagonalConnectionsIntersectingWithNode(new GridPosition(x, y));
            //            Grid.DisconnectNode(new GridPosition(x, y));
            //        }
            //    }
            //}
            var startTimeExplorationPrep = DateTime.Now;
            Exploration = new Exploration(mapDataForExploration);
            Console.WriteLine($"Prepping exploration took: {DateTime.Now.Subtract(startTimeExplorationPrep).TotalMilliseconds} milliseconds");


            Console.WriteLine($"Creating submap took: {DateTime.Now.Subtract(startTime).TotalMilliseconds} milliseconds");
        }



        //public bool ContainsGridPosition(Vector2 gridPosition)
        //{
        //    int length = 7;
        //    int xStart = MyExtensions.Clamp<int>((int)(gridPosition.X - length), 0, mapDataForPathFinding.GetLength(0));
        //    int yStart = MyExtensions.Clamp<int>((int)(gridPosition.Y - length), 0, mapDataForPathFinding.GetLength(1));
        //    int xStop = MyExtensions.Clamp<int>((int)(gridPosition.X + length), 0, mapDataForPathFinding.GetLength(0));
        //    int yStop = MyExtensions.Clamp<int>((int)(gridPosition.Y + length), 0, mapDataForPathFinding.GetLength(1));
        //    for (int x = xStart; x < xStop; x++)
        //    {
        //        for (int y = yStart; y < yStop; y++)
        //        {
        //            if (mapDataForPathFinding[x, y] == 1)
        //            {
        //                return true;
        //            }
        //        }
        //    }
        //    return false;
        //}


        //public void AddFoundAreaTransition(string areaTransitionLabelText, Vector2 areaTransitionGridPos)
        //{
        //    if (foundAreaTransitions.ContainsKey(areaTransitionLabelText) == false)
        //    {
        //        foundAreaTransitions.Add(areaTransitionLabelText, areaTransitionGridPos);
        //    }
        //}

        private void EraseNonIslandData(byte[,] array, int[,] refArray, int islandId)
        {
            var startTime = DateTime.Now;
            for (int x = 0; x < array.GetLength(0); x++)
            {
                for (int y = 0; y < array.GetLength(1); y++)
                {
                    if (refArray[x, y] != islandId)
                    {
                        array[x, y] = 0;
                    }
                }
            }
            Console.WriteLine($"Erasing non island data took: {DateTime.Now.Subtract(startTime).TotalMilliseconds} milliseconds");

        }

        public GridPosition PoEGridPositionToRoyTPathFindingGridPosition(Vector2 position)
        {
            return new GridPosition((int)(position.X / PathfindingConsts.myPathfindingGridSize), (int)(position.Y / PathfindingConsts.myPathfindingGridSize));
        }
    }
}
