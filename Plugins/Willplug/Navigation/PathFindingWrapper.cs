using ExileCore;
using SharpDX;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using ExileCore.PoEMemory;
using ExileCore.PoEMemory.Components;
using ExileCore.PoEMemory.MemoryObjects;
using ExileCore.PoEMemory.Elements.InventoryElements;

using ExileCore.Shared;
using ExileCore.Shared.Enums;
using ExileCore.Shared.Helpers;
using ExileCore.Shared.Nodes;

using Newtonsoft.Json;
using Map = ExileCore.PoEMemory.Elements.Map;
using ExileCore.Shared.Cache;
using Roy_T.AStar.Paths;
using Roy_T.AStar.Grids;
using Roy_T.AStar.Primitives;
using System.Security.Policy;
using Roy_T.AStar.Graphs;

namespace Willplug.Navigation
{
    public class PathFindingWrapper
    {
        //private PathFinder royTPathFinder;

        private int pathFindingIndex = 0;
        public float RemainingPathDistanceSquaredToTarget { get; private set; } = 0;
        public List<Vector2> CurrentPathInPoEGridCoordinates => currentPathInPoEGridCoordinates;
        private List<Vector2> currentPathInPoEGridCoordinates = new List<Vector2>();

        private const float pathFollowingRadiusSquared = 15 * 15;


        public PathFindingWrapper()
        {
            //royTPathFinder = new PathFinder();
        }


        public (bool, Vector2) GetNextPositionInCurrentPath(Vector2 position)
        {
            int pathCount = currentPathInPoEGridCoordinates.Count;
            if (pathFindingIndex > pathCount - 1)
            {
                return (false, new Vector2());
            }
            int maxSearchIndex = MyExtensions.Clamp<int>((pathFindingIndex + 15), 0, pathCount);

            while (true)
            {
                var distanceSquaredToPlayerPos = position.DistanceSquared(currentPathInPoEGridCoordinates[pathFindingIndex]);
                if (distanceSquaredToPlayerPos < pathFollowingRadiusSquared)
                {
                    if (pathFindingIndex > 0)
                    {
                        RemainingPathDistanceSquaredToTarget -= currentPathInPoEGridCoordinates[pathFindingIndex].DistanceSquared(currentPathInPoEGridCoordinates[pathFindingIndex - 1]);
                        //Console.WriteLine($"remaining path distance: {RemainingPathDistanceSquaredToTarget} , pathfinding index {pathFindingIndex} , path count: {pathCount}");
                    }
                    if (pathFindingIndex == (pathCount - 1))
                    {
                        pathFindingIndex += 1;
                        return (true, currentPathInPoEGridCoordinates.Last());
                    }
                    pathFindingIndex += 1;
                }
                else
                {
                    break;
                }

            }
            return (true, currentPathInPoEGridCoordinates[pathFindingIndex]);
        }

        private Node TryGetValidNodeAroundGridPos(ZoneMap zoneMap, Dictionary<Position, Node> nodes, Vector2 gridPos)
        {
            Node node;
            var royTGraphPos = zoneMap.PoEGridPositionToRoyTPathFindingGraphPosition(gridPos);
            bool gotNode = nodes.TryGetValue(royTGraphPos, out node);
            if (gotNode) return node;

            int distance = 10;
            int startX = (int)gridPos.X - distance;
            int endX = (int)gridPos.X + distance;
            int startY = (int)gridPos.Y - distance;
            int endY = (int)gridPos.Y + distance;
            for (int x = startX; x <= endX; x++)
            {
                for (int y = startY; y <= endY; y++)
                {
                    var graphPos = zoneMap.PoEGridPositionToRoyTPathFindingGraphPosition(new Vector2(x,y));
                    gotNode = nodes.TryGetValue(graphPos, out node);
                    if (gotNode) return node;
                }
            }
            return null;
        }
        public bool TryToFindPathWithGraph(Vector2 start, Vector2 end, ZoneMap zoneMap)
        {
            try
            {
                PathFinder pathFinder = new PathFinder();
                pathFindingIndex = 0;
                currentPathInPoEGridCoordinates.Clear();
                const int radius = 7;
                /* If the player is too close to obstacles a path might not be found. 
                 * Shift the player "coordinate" away from obstacle
                 */
                //int maxAttemptsToFindAPath = 4
                var royTGraphPosStart = zoneMap.PoEGridPositionToRoyTPathFindingGraphPosition(start);
                var royTGraphPosEnd = zoneMap.PoEGridPositionToRoyTPathFindingGraphPosition(end);

                var nodeDictionary = zoneMap.GetSubMap(start).NodeDictionary;
                Node startNode = TryGetValidNodeAroundGridPos(zoneMap, nodeDictionary, start);
                Node endNode = TryGetValidNodeAroundGridPos(zoneMap, nodeDictionary, end);

                //bool gotStartNode = zoneMap.GetSubMap(start).NodeDictionary.TryGetValue(royTGraphPosStart, out startNode);
                //bool gotEndNode = zoneMap.GetSubMap(start).NodeDictionary.TryGetValue(royTGraphPosEnd, out endNode);

                if (startNode == null || endNode == null)
                {
                    Console.WriteLine("Unable to find correct navigation grid");
                    return false;
                }
                if (startNode == endNode)
                {
                    Console.WriteLine("Nodes are equal. abort");
                    return false;
                }

                while (true)
                {
                    var path = pathFinder.FindPath(startNode, endNode, Velocity.FromKilometersPerHour(100000));
                    if (path == null || path.Edges.Count == 0) // || path.Type == PathType.ClosestApproach)
                    {
                        Console.WriteLine("Unable to find a path with given start/end, randomizing start/end");
                        int xRandom = MathHepler.Randomizer.Next((int)start.X - radius, (int)start.X + radius);
                        int yRandom = MathHepler.Randomizer.Next((int)start.Y - radius, (int)start.Y + radius);
                        startNode = TryGetValidNodeAroundGridPos(zoneMap, nodeDictionary, new Vector2(xRandom, yRandom));

                        xRandom = MathHepler.Randomizer.Next((int)end.X - radius, (int)end.X + radius);
                        yRandom = MathHepler.Randomizer.Next((int)end.Y - radius, (int)end.Y + radius);
                        endNode = TryGetValidNodeAroundGridPos(zoneMap, nodeDictionary, new Vector2(xRandom, yRandom));
                    }
                    else
                    {
                        RemainingPathDistanceSquaredToTarget = 0;
                        int counter = 0;
                        var previousEdgeVector = new Vector2();
                        foreach (var edge in path.Edges)
                        {
                            var compensatedEdgeVector = new Vector2(edge.Start.Position.X * PathfindingConsts.myPathfindingGridSize, edge.Start.Position.Y * PathfindingConsts.myPathfindingGridSize);
                            if (counter != 0)
                            {
                                var distance = previousEdgeVector.Distance(compensatedEdgeVector);
                                RemainingPathDistanceSquaredToTarget += distance;
                            }
                            currentPathInPoEGridCoordinates.Add(compensatedEdgeVector);
                            previousEdgeVector = compensatedEdgeVector;
                            counter += 1;
                        }
                        RemainingPathDistanceSquaredToTarget = RemainingPathDistanceSquaredToTarget * RemainingPathDistanceSquaredToTarget;

                        return true;
                    }
                }
            }
            catch
            {
                return false;
            }

        }
        //public bool TryToFindPath(Vector2 start, Vector2 end, ZoneMap zoneMap)
        //{
        //    try
        //    {
        //        PathFinder pathFinder = new PathFinder();
        //        pathFindingIndex = 0;
        //        currentPathInPoEGridCoordinates.Clear();
        //        const int radius = 7;
        //        /* If the player is too close to obstacles a path might not be found. 
        //         * Shift the player "coordinate" away from obstacle
        //         */
        //        //int maxAttemptsToFindAPath = 4
        //        var royTGridPosStart = zoneMap.PoEGridPositionToRoyTPathFindingGridPosition(start);
        //        var royTGridPosEnd = zoneMap.PoEGridPositionToRoyTPathFindingGridPosition(end);

        //        var navigationGrid = zoneMap.GetRoyTPathFindingGrid(start);
        //        if (navigationGrid == null)
        //        {
        //            Console.WriteLine("Unable to find correct navigation grid");
        //            return false;
        //        }

        //        while (true)
        //        {
        //            var path = pathFinder.FindPath(royTGridPosStart, royTGridPosEnd, navigationGrid);
        //            if (path == null || path.Edges.Count == 0 || path.Type == PathType.ClosestApproach)
        //            {
        //                Console.WriteLine("Unable to find a path with given start/end, randomizing start/end");
        //                int xRandom = MathHepler.Randomizer.Next((int)start.X - radius, (int)start.X + radius);
        //                int yRandom = MathHepler.Randomizer.Next((int)start.Y - radius, (int)start.Y + radius);
        //                royTGridPosStart = zoneMap.PoEGridPositionToRoyTPathFindingGridPosition(new Vector2(xRandom, yRandom));

        //                xRandom = MathHepler.Randomizer.Next((int)end.X - radius, (int)end.X + radius);
        //                yRandom = MathHepler.Randomizer.Next((int)end.Y - radius, (int)end.Y + radius);
        //                royTGridPosEnd = zoneMap.PoEGridPositionToRoyTPathFindingGridPosition(new Vector2(xRandom, yRandom));
        //            }
        //            else
        //            {
        //                int counter = 0;
        //                var previousEdgeVector = new Vector2();
        //                foreach (var edge in path.Edges)
        //                {
        //                    var edgeVector = new Vector2(edge.Start.Position.X, edge.Start.Position.Y);
        //                    if (counter != 0)
        //                    {
        //                        RemainingPathDistanceSquaredToTarget += previousEdgeVector.DistanceSquared(edgeVector);
        //                    }
        //                    currentPathInPoEGridCoordinates.Add(new Vector2(edge.Start.Position.X, edge.Start.Position.Y));
        //                    previousEdgeVector = edgeVector;
        //                    counter += 1;
        //                }
        //                return true;
        //            }
        //        }
        //    }
        //    catch
        //    {
        //        return false;
        //    }

        //}

    }
}


//private INode GetValidRoyTNode(Grid navigationGrid, ZoneMap zoneMap, Vector2 position)
//{
//    const int radius = 7;
//    INode royTNode = null;
//    GridPosition royTGridPosStart;
//    try
//    {
//        royTGridPosStart = zoneMap.PoEGridPositionToRoyTPathFindingGridPosition(position);
//        royTNode = navigationGrid.GetNode(royTGridPosStart);
//        return royTNode;
//    }
//    catch
//    {

//        for (int i = 0; i < 15; i++)
//        {
//            try
//            {
//                int xRandom = MathHepler.Randomizer.Next((int)position.X - radius, (int)position.X + radius);
//                int yRandom = MathHepler.Randomizer.Next((int)position.Y - radius, (int)position.Y + radius);
//                var randomPosition = new Vector2(xRandom, yRandom);
//                royTGridPosStart = zoneMap.PoEGridPositionToRoyTPathFindingGridPosition(randomPosition);
//                royTNode = navigationGrid.GetNode(royTGridPosStart);
//                return royTNode;
//            }
//            catch
//            {
//                Console.WriteLine("Error when getting royT node in TryToFindPath");
//                royTNode = null;
//            }
//        }
//        Console.WriteLine("Unable to get royT node");
//        return null;
//    }
//}