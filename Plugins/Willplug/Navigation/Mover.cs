using ExileCore;
using ExileCore.PoEMemory;
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
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using TreeRoutine.TreeSharp;
using Willplug.Navigation;
using Input = ExileCore.Input;

namespace Willplug.Navigation
{

    public class Mover
    {
        private readonly GameController gameController;
        public Object gridDataLock { get; set; } = new Object();
        private List<Vector2> simplePath = new List<Vector2>();
        Dictionary<uint, ZoneMap> zoneMaps = new Dictionary<uint, ZoneMap>();

        public PathFindingWrapper pathFindingWrapper;

        public void RemoveNavigationDataForCurrentZone()
        {
            try
            {
                var areaHash = gameController.Area.CurrentArea.Hash;
                zoneMaps.Remove(areaHash);
            }
            catch (Exception ex)
            {

            }


        }
        public List<Vector2> CurrentSimplePathfindingPath
        {
            get
            {
                if (simplePath == null) return new List<Vector2>();
                lock (gridDataLock)
                {
                    return new List<Vector2>(simplePath);
                }
            }
        }

        public Mover(GameController gameController)
        {
            this.gameController = gameController;
            pathFindingWrapper = new PathFindingWrapper();
        }


        public ZoneMap GetZoneMap()
        {
            var areaHash = gameController.Area.CurrentArea.Hash;
            if (zoneMaps.ContainsKey(areaHash) == true)
            {
                return zoneMaps[areaHash];
            }
            return null;
        }

        public bool SetPath(Vector2 end)
        {

            ZoneMap zoneMap;        
            var areaHash = gameController.Area.CurrentArea.Hash;
            bool didGetZoneMap = zoneMaps.TryGetValue(areaHash, out zoneMap);
            if (didGetZoneMap == false)
            {
                WillBot.LogMessageCombo($"Unable to get zonemap with areaHash {areaHash}");
                return false;
            }
            bool didFindPath = pathFindingWrapper.TryToFindPathWithGraph(gameController.Player.GridPos, end, zoneMap);  
           // bool didFindPath = pathFindingWrapper.TryToFindPath(gameController.Player.GridPos, end, zoneMap);

            if (didFindPath == false)
            {
                WillBot.LogMessageCombo($"Unable to find a path to {end}");//. Trying to update terrain data");
                                                                             // UpdateTerrainData();
                                                                             //return RunStatus.Failure;
                return false;
            }
            else
            {
                WillBot.LogMessageCombo($"Found a path to {end}");
                //return RunStatus.Success;
                return true;
            }
        }

        public RunStatus FollowPath()
        {
            //Console.WriteLine("following path");
            var nextPath = pathFindingWrapper.GetNextPositionInCurrentPath(gameController.Player.GridPos);
            if (nextPath.Item1 == false)
            {
                // No more path points
                WillBot.LogMessageCombo($"Finished following path");
                //MoverHelper.ClickToStopCharacter();
                return RunStatus.Failure;
            }
            //Vector3 worldPositionCharacter = gameController.Player.Pos;
            //var worldPositionOfPoint = point.GridToWorld();
            var delta = nextPath.Item2 - gameController.Player.GridPos;
            double angleToPoint = 0;
            delta.GetPolarCoordinates(out angleToPoint);
            MoverHelper.MouseAsJoystickNonSmooth(gameController, (float)(angleToPoint * 180 / Math.PI) + 45);
            while (WillBot.Me.TownPortal?.ItemOnGround?.GetComponent<Targetable>().isTargeted == true)
            {
                Console.WriteLine("Town portal is targeted. Adjusting angle to avoid");
                angleToPoint += 0.1;
                MoverHelper.MouseAsJoystickNonSmooth(gameController, (float)(angleToPoint * 180 / Math.PI) + 45);
            }
            InputWrapper.LeftMouseButtonDown();
            return RunStatus.Running;
        }


        public bool GetIsWalkableValue(int x, int y)
        {
            var areaHash = gameController.Area.CurrentArea.Hash;
            if (zoneMaps.ContainsKey(areaHash) == true)
            {
                return zoneMaps[areaHash].GetIsWalkable(x, y);

            }
            return false;
        }

        private int updateExplorationInterval = 100;
        private DateTime previousUpdateExplorationTime = DateTime.Now;
        public void UpdateExplored()
        {
            if (DateTime.Now.Subtract(previousUpdateExplorationTime).TotalMilliseconds > updateExplorationInterval)
            {
                previousUpdateExplorationTime = DateTime.Now;

                lock (gridDataLock)
                {
                    var areaHash = gameController.Area.CurrentArea.Hash;
                    if (zoneMaps.ContainsKey(areaHash) == true)
                    {
                        zoneMaps[areaHash].UpdateExploration(gameController.Player.GridPos);
                    }
                }
            }
        }

        public void UpdateTerrainData(bool doNotUpdateIfAlreadyExists = false)
        {
            var startTime = DateTime.Now;
            var areaHash = gameController.Area.CurrentArea.Hash;
            var rows = gameController.IngameState.Data.CurrentTerrainData.Rows;
            var cols = gameController.IngameState.Data.CurrentTerrainData.Columns;
            var bytesPerRow = gameController.IngameState.Data.CurrentTerrainData.BytesPerRow;
            var meleeData = gameController.IngameState.Data.CurrentTerrainData.MeleeData;

            lock (gridDataLock)
            {
                if (zoneMaps.ContainsKey(areaHash) == false)
                {
                    zoneMaps.Add(areaHash, new ZoneMap(meleeData, bytesPerRow, cols, rows));
                }
                else
                {
                    if (doNotUpdateIfAlreadyExists == false)
                    {
                        zoneMaps[areaHash].InitializePathFindingVariables(meleeData, bytesPerRow, cols, rows);
                    }               
                }
            }
            Console.WriteLine($"Updating terrain data took: {DateTime.Now.Subtract(startTime).TotalMilliseconds} milliseconds");
        }

        public (bool, Vector2) GetUnexploredPosition()
        {
            var areaHash = gameController.Area.CurrentArea.Hash;
            if (zoneMaps.ContainsKey(areaHash))
            {
                return zoneMaps[areaHash].GetSubMap(gameController.Player.GridPos).Exploration.FindPositionToExplore(gameController.Player.GridPos);
            }
            return (false, new Vector2());
        }

        public float GetPercentOfZoneExplored()
        {
            int initialExploreSum = 0;
            int currentExploreSum = 0;
            var areaHash = gameController.Area.CurrentArea.Hash;
            if (zoneMaps.ContainsKey(areaHash))
            {
                foreach (var keyValue in zoneMaps[areaHash].SubMaps)
                {
                    if (keyValue.Key > 0)
                    {
                        initialExploreSum += keyValue.Value.Exploration.InitialExploreSum;
                        currentExploreSum += keyValue.Value.Exploration.CurrentExploreSum;
                    }
                }
                float explored = 1 - (currentExploreSum / (float)initialExploreSum);
                return explored * 100;
            }
            return 0;
        }

        public float GetPercentOfCurrentSubMapExplored()
        {
            var areaHash = gameController.Area.CurrentArea.Hash;
            if (zoneMaps.ContainsKey(areaHash))
            {
                return zoneMaps[areaHash]?.GetSubMap(gameController.Player.GridPos)?.Exploration?.ExploredFactor ?? 0;
            }
            return 0;
        }

    }
}


//public IEnumerator MovePlayerToGridPos(Vector2 end)
//{
//    FindPath(gameController.Player.GridPos, end);
//    pathfindingIndex = 0;
//    while (true)
//    {
//        if (cancelMovement == true)
//        {
//            cancelMovement = false;
//            yield break;
//        }
//        var currentPoint = simplePath[pathfindingIndex];
//        if (gameController.Player.GridPos.Distance(currentPoint) < 7)
//        {
//            pathfindingIndex += 1;
//            if (pathfindingIndex >= simplePath.Count)
//            {
//                pathfindingIndex = simplePath.Count - 1;
//                InputWrapper.ResetMouseButtons();
//                yield break;
//            }
//            currentPoint = simplePath[pathfindingIndex];
//        }
//        //Vector3 worldPositionCharacter = gameController.Player.Pos;
//        //var worldPositionOfPoint = point.GridToWorld();
//        var delta = currentPoint - gameController.Player.GridPos;
//        double angleToPoint = 0;
//        delta.GetPolarCoordinates(out angleToPoint);
//        yield return MoverHelper.MouseAsJoystick(gameController, (float)(angleToPoint * 180 / Math.PI) + 45);
//        InputWrapper.LeftMouseButtonDown();
//        yield return new WaitTime(10);
//    }
//}

//public Vector2 PathGridToWorld(Vector2 pathGridPos)
//{
//    return new Vector2(pathGridPos.X * pathGridSize, pathGridPos.Y * pathGridSize);
//}
//public Vector2 WorldToPathGrid(Vector2 worldPos)
//{
//    return new Vector2(worldPos.X / pathGridSize, worldPos.Y / pathGridSize);
//}