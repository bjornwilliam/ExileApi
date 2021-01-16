using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using ExileCore;
using ExileCore.PoEMemory;
using ExileCore.PoEMemory.Components;
using ExileCore.PoEMemory.MemoryObjects;
using ExileCore.PoEMemory.Elements.InventoryElements;

using ExileCore.Shared;
using ExileCore.Shared.Enums;
using ExileCore.Shared.Helpers;
using ExileCore.Shared.Nodes;

using SharpDX;

using Map = ExileCore.PoEMemory.Elements.Map;
using ExileCore.Shared.Cache;
using System.Security.Policy;

using TreeRoutine;
using TreeRoutine.TreeSharp;
using Willplug.Combat;
using Willplug.BotBehavior;
using Willplug.Navigation;
using Willplug.BotBehavior.Town;
using System.Threading;
using Action = TreeRoutine.TreeSharp.Action;

namespace Willplug
{
    public class WillplugCore : BaseTreeRoutinePlugin<WillplugSettings, BaseTreeCache>
    {




        public List<Entity> LoadedMonsters { get; set; } = new List<Entity>();


        private Pickit pickit;
        private Mover mover;

        private Coroutine pickItCoroutine;
        private readonly Stopwatch DebugTimer = Stopwatch.StartNew();
        private WaitTime pickitCoroutineWaitTime;


        public Composite Tree { get; set; }
        private Coroutine TreeCoroutine { get; set; }
        private const string treeroutineName = "helllo";

        public Composite BuffTree { get; set; }
        private Coroutine BuffTreeCoroutine { get; set; }
        private const string buffTreeroutineName = "Buffstree";

        private Coroutine AutoLootCoroutine { get; set; }
        private const string autoLootCoroutineName = "autoloot";
        public Composite AutoLootTree { get; set; }



        public WillplugCore() : base()
        {
        }
        public override void ReceiveEvent(string eventId, object args)
        {
            WillBot.LogMessageCombo($"Recived event {eventId}");
            switch (eventId)
            {
                case "stashie_done":
                    WillBot.Me.StashieHasCompletedStashing = true;
                    WillBot.Me.IsPlayerInventoryFull = false;
                    break;
                    //case "stashie_stop_drop_items":
                    //    WillBot.Me.StashieHasCompletedStashing = true;
                    //    break;
            }
        }




        public override bool Initialise()
        {
            base.Initialise();
            Name = "Willplug";

            pickit = new Pickit(this.Settings, this);
            mover = new Mover(GameController);
            WillBot.Me = new WillPlayer(this, GameController, pickit, LoadedMonsters);
            WillBot.Mover = mover;
            WillBot.gameController = GameController;
            WillBot.basePlugin = this;
            WillBot.KeyboardHelper = new TreeRoutine.DefaultBehaviors.Helpers.KeyboardHelper(GameController);

            Input.RegisterKey(Settings.TestKey1);
            Input.RegisterKey(Settings.TestKey2);
            Input.RegisterKey(Settings.TryLootNearbykey);
            //BuffTree = BuffBehavior.CreateBerserkerBuffTree();
            //BuffTree = NecroBuffs.CreateNecroBuffTree();
            BuffTree = CharacterAbilityTrees.CreateVortexCharAbilityTree();
            Tree = CreateTree();
            Settings.Enable.OnValueChanged += (sender, b) =>
            {
                if (b)
                {
                    if (Core.ParallelRunner.FindByName(treeroutineName) == null) InitCoroutine();
                    TreeCoroutine?.Resume();
                }
                else
                    TreeCoroutine?.Pause();
            };
            InitCoroutine();
            InitBuffRoutine();
            InitAutoLootRoutine();
            #region PickitRelated
            DebugTimer.Reset();
            Settings.MouseSpeed.OnValueChanged += (sender, f) => { Mouse.speedMouse = Settings.MouseSpeed.Value; };
            pickitCoroutineWaitTime = new WaitTime(Settings.ExtraDelay);
            Settings.ExtraDelay.OnValueChanged += (sender, i) => pickitCoroutineWaitTime = new WaitTime(i);
            pickit.LoadRuleFiles();
            #endregion

            return true;
        }
        private void InitCoroutine()
        {
            TreeCoroutine = new Coroutine(() => TickTree(Tree), new WaitTime(1000 / 10), this, "Willplug tree");
            TreeCoroutine.AutoResume = false;
            Core.ParallelRunner.Run(TreeCoroutine);
            TreeCoroutine.Pause();
        }

        private void InitBuffRoutine()
        {
            BuffTreeCoroutine = new Coroutine(() => TickTree(BuffTree), new WaitTime(1000 / 40), this, "buff tree");
            BuffTreeCoroutine.AutoResume = true;
            //Core.ParallelRunner.Run(BuffTreeCoroutine);
            //BuffTreeCoroutine.Pause();
        }

        private void InitAutoLootRoutine()
        {
            AutoLootTree = CreateAutoLootTree();
            AutoLootCoroutine = new Coroutine(() => TickTree(AutoLootTree), new WaitTime(1000 / 40), this, autoLootCoroutineName);
            AutoLootCoroutine.AutoResume = false;
            Core.ParallelRunner.Run(AutoLootCoroutine);
            AutoLootCoroutine.Pause();
        }
        protected override void UpdateCache()
        {
            base.UpdateCache();
        }

        static bool autoLootingDone = false;
        public static Composite CreateAutoLootTree()
        {
            return new Decorator(delegate
            {
                if (WillBot.Plugin.TreeHelper.CanTickMap() && CommonBehavior.DoLooting(ignoreStrongBox:true))
                {
                    return true;
                }
                else
                {
                    autoLootingDone = true;
                    return false;
                }
            },
             new Sequence(
                  new Inverter(CommonBehavior.MoveTo(x => WillBot.Me.ClosestItemToLoot?.GridPos,
                  y => WillBot.Me.ClosestItemToLoot?.LabelOnGround?.ItemOnGround?.Pos, CommonBehavior.LootingMovementSpec)),
                 LootBehavior.TryToPickLootCloseToPlayer()
                ));
        }



        private DateTime previousBreakTime = DateTime.Now;
        private TimeSpan breakInterval = new TimeSpan(3, 59, 0);
        private Composite CreateTree()
        {


            return new PrioritySelector(

            new Decorator(x => GameController.Game.IsPreGame == true, LoginBehavior.TryEnterGameFromLoginScreen(x => 3)),
            new Decorator(x => TreeHelper.CanTickTown(), CommonBehavior.TownToHideout()),

            new Decorator(x => TreeHelper.CanTickHideout(), new PrioritySelector(
                                new Decorator(x => (DateTime.Now.Subtract(previousBreakTime) > breakInterval), new Action(delegate
                                {
                                    previousBreakTime = DateTime.Now;
                                    var breakLength = new TimeSpan(hours: 0, minutes: MathHepler.Randomizer.Next(8, 15), seconds: 0);
                                    Thread.Sleep(breakLength);
                                    return RunStatus.Failure;
                                }
                 )),
               //new Inverter(CommonBehavior.CloseOpenPanels()),
               CommonBehavior.HandleDestroyItemPopup(),
                CharacterAbilityTrees.ActivateNecroAuras(),
                new Action(delegate
                {
                    CommonBehavior.CouldNotFindValidPositionToExplore = false;
                    WillBot.Me.enemies.LastKilledMonsterTime = DateTime.Now;
                    return RunStatus.Failure;
                }),
                SellBehavior.SellItems(sellUnidCrItems: true),
                TownBehavior.IdentifyUniquesInPlayerInventory(),
                TownBehavior.Stashie(),
            //ChaosRecipeBehavior.ChaosRecipeHandler(),



            // TownBehavior.EnterBloodAquaducts())),
            MapPrepBehavior.GetOpenAndEnterMap())),
            new Decorator(x => TreeHelper.CanTickDead(),
                AreaBehavior.DiedBehavior()),
            new Decorator(x => TreeHelper.CanTickMap(),// !PlayerHelper.isPlayerDead(),
                new PrioritySelector(

                   //new Inverter(CommonBehavior.CloseOpenPanels()),
                   new Inverter(CommonBehavior.CloseOpenPanelsIfOpenPanels()),
                    new Decorator(delegate
                    {
                        // if not killed a monster in the last x seconds -> exit map
                        if (DateTime.Now.Subtract(WillBot.Me.enemies.LastKilledMonsterTime).TotalSeconds > 60)
                        {

                            return true;
                        }
                        return false;
                    },
                        CommonBehavior.OpenAndEnterTownPortal()
                    ),
                      CharacterAbilityTrees.CreateNecroBuffTree(),
                    new Decorator(ret => CommonBehavior.DoCombat(), NecroCombat.NecroCombatComposite()),
                    LevelGemsBehavior.LevelGems(),
                    LootBehavior.BuildLootComposite(),
                    ChestBehavior.DoOpenNearbyChest(),
                    AreaBehavior.InteractWithLabelOnGroundOpenable(() => WillBot.Me.TimelessMonolith),
                    AreaBehavior.InteractWithLabelOnGroundOpenable(() => WillBot.Me.EssenceMonsterMonolith),
                    AreaBehavior.InteractWithLabelOnGroundOpenable(() => WillBot.Me.HarvestChest),
                    AreaBehavior.InteractWithLabelOnGroundOpenable(() => WillBot.Me.StrongBox),
                    AreaBehavior.InteractWithLabelOnGroundOpenable(() => WillBot.Me.BlightPumpInMap),
                    //AreaBehavior.DefendBlightPumpMap(),

                    AreaBehavior.OpenDeliriumMirror(),
                    CommonBehavior.MoveToUnkillableUniqueMonster(),
                    //CommonBehavior.MoveToDeliriumPause(),
                    new Decorator(x => CommonBehavior.DoMoveToMonster(),
                    CommonBehavior.MoveTo(ret => WillBot.Me.enemies.ClosestMonsterEntity?.GridPos, x => WillBot.Me.enemies.ClosestMonsterEntity?.Pos,
                    CommonBehavior.DefaultMovementSpec)),
                    new Action(delegate
                    {
                        AreaBehavior.UpdateAreaTransitions();
                        return RunStatus.Failure;
                    }),

                    new Decorator(delegate
                    {
                        if (CommonBehavior.DoMoveToMonster() == false && CommonBehavior.DoCombat() == false && CommonBehavior.DoMoveToNonKillableUniqueMonster() == false)
                        {
                            WillBot.LogMessageCombo($"Checking for exit/area transitions");
                            return true;
                        }
                        return false;
                    },
                    new PrioritySelector(
                        CommonBehavior.HandleDestroyItemPopup(),
                        AreaBehavior.TryDoAreaTransition(),
                        new Decorator(delegate
                        {
                            if (CommonBehavior.MapComplete())
                            {
                                WillBot.LogMessageCombo($"Exiting map due to map complete criterias fulfilled, explored more than {WillBot.Settings.MapAreaExploration}");
                                return true;
                            }
                            return false;
                        },
                            CommonBehavior.OpenAndEnterTownPortal()
                        ),
                        new Decorator(delegate
                        {
                            if (CommonBehavior.CouldNotFindValidPositionToExplore == true)
                            {
                                WillBot.LogMessageCombo("Exiting map due to not finding a valid position to explore");
                                return true;
                            }
                            return false;
                        },
                            CommonBehavior.OpenAndEnterTownPortal()
                        ),
                        new Decorator(
                            delegate
                            {
                                if (WillBot.Me.IsPlayerInventoryFull == true)
                                {
                                    WillBot.LogMessageCombo("Exiting map due to inventory full.");
                                    return true;
                                }
                                return false;
                            },
                            CommonBehavior.OpenAndEnterTownPortal())
                     )),
                    CommonBehavior.DoExploringComposite()
                )));

        }


        public override void EntityAdded(Entity entityWrapper)
        {
            //if (WillBot.IsEntityAKillableMonster(entityWrapper))
            ////if (entityWrapper.HasComponent<Monster>())
            //{
            //    lock (MyLocks.LoadedMonstersLock)
            //    {
            //        LoadedMonsters.Add(entityWrapper);
            //    }
            //}
        }
        public override void EntityRemoved(Entity entityWrapper)
        {
            //lock (MyLocks.LoadedMonstersLock)
            //{
            //    LoadedMonsters.Remove(entityWrapper);
            //}
        }

        public override void AreaChange(AreaInstance area)
        {
            if (GameController.Player != null)
            {
                if (!WillBot.isBotPaused)
                {
                    lock (MyLocks.UpdateTerrainDataLock)
                    {
                        WillBot.LogMessageCombo("AreaChange: Updating terrain data STARTED");
                        mover.UpdateTerrainData(doNotUpdateIfAlreadyExists: true);
                        WillBot.LogMessageCombo("AreaChange: Updating terrain data FINISHED");
                    }

                }
                if (Settings.Enable.Value && area != null)
                {
                    Cache.InHideout = area.IsHideout;
                    Cache.InTown = area.IsTown;

                    if (Cache.InHideout == true)
                    {
                        WillBot.Me.HasStashedItemsThisTownCycle = false;
                        WillBot.Me.HasSoldItemsThisTownCycle = false;
                        WillBot.Me.enemies.BlackListedMonsterAddresses.Clear();
                        ChaosRecipeBehavior.ResetData();
                    }
                }
            }

        }

        public override void DrawSettings()
        {
            base.DrawSettings();
        }

        public static Vector2 DeltaInWorldToMinimapDelta(Vector2 delta, double diag, float scale, float deltaZ = 0)
        {
            const float CAMERA_ANGLE = 38 * MathUtil.Pi / 180;

            // Values according to 40 degree rotation of cartesian coordiantes, still doesn't seem right but closer
            var cos = (float)(diag * Math.Cos(CAMERA_ANGLE) / scale);
            var sin = (float)(diag * Math.Sin(CAMERA_ANGLE) / scale); // possible to use cos so angle = nearly 45 degrees

            // 2D rotation formulas not correct, but it's what appears to work?
            return new Vector2((delta.X - delta.Y) * cos, deltaZ - (delta.X + delta.Y) * sin);
        }

        private Camera camera => GameController.Game.IngameState.Camera;
        private IngameUIElements ingameStateIngameUi;
        private CachedValue<float> _diag;
        private float diag =>
        _diag?.Value ?? (_diag = new TimeCache<float>(() =>
        {


            return (float)Math.Sqrt(camera.Width * camera.Width + camera.Height * camera.Height);
        }, 100)).Value;
        private float k;
        private bool largeMap;
        private float scale;
        private CachedValue<RectangleF> _mapRect;
        private Map mapWindow => GameController.Game.IngameState.IngameUi.Map;
        private Vector2 screentCenterCache;
        private RectangleF MapRect => _mapRect?.Value ?? (_mapRect = new TimeCache<RectangleF>(() => mapWindow.GetClientRect(), 100)).Value;
        private Vector2 screenCenter =>
        new Vector2(MapRect.Width / 2, MapRect.Height / 2 - 20) + new Vector2(MapRect.X, MapRect.Y) +
        new Vector2(mapWindow.LargeMapShiftX, mapWindow.LargeMapShiftY);
        public override void Render()
        {
            if (WillBot.isBotPaused == false)
            {
                //DrawLinesOnMinimap();
                //DrawOutlineOfMapOnMiniMap();

            }

        }

        public void DrawOutlineOfMapOnMiniMap()
        {
            var outlineList = WillBot.Mover.GetZoneMap().mapBoundaryList;
            var playerPos = GameController.Player.GridPos;
            foreach (var g in outlineList)
            {
                Vector2 position = screentCenterCache + DeltaInWorldToMinimapDelta(g - playerPos, diag, scale, 0);
                Graphics.DrawLine(position, new Vector2(position.X + 2, position.Y + 2), 6, Color.DimGray);
            }
        }


        public void DrawLinesOnMinimap()
        {
            var playerPos = GameController.Player.GetComponent<Positioned>().GridPos;
            var posZ = GameController.Player.GetComponent<Render>().Pos.Z;
            var mapWindowLargeMapZoom = mapWindow.LargeMapZoom;
            var simplePath = mover.pathFindingWrapper.CurrentPathInPoEGridCoordinates;
            if (simplePath != null)
            {
                foreach (var point in simplePath)
                {
                    //var worldPoint = mover.PathGridToWorld(point);
                    Vector2 position = screentCenterCache + DeltaInWorldToMinimapDelta(point - playerPos, diag, scale, 0);// (0 - posZ) / (9f / mapWindowLargeMapZoom));
                    Graphics.DrawLine(position, new Vector2(position.X + 2, position.Y + 2), 6, Color.Green);
                }
            }
            if (WillBot.Me.IsCurrentPositionToExploreValid)
            {
                Vector2 position = screentCenterCache + DeltaInWorldToMinimapDelta(WillBot.Me.CurrentPositionToExplore - playerPos, diag, scale, 0);// (0 - posZ) / (9f / mapWindowLargeMapZoom));
                Graphics.DrawLine(position, new Vector2(position.X + 2, position.Y + 2), 9, Color.Red);
            }
            Graphics.DrawLine(screentCenterCache, new Vector2(screentCenterCache.X + 4, screentCenterCache.Y + 4), 7, Color.Red);
        }

        public override Job Tick()
        {
            //ingameStateIngameUi = GameController.Game.IngameState.IngameUi;

            screentCenterCache = screenCenter;
            k = camera.Width < 1024f ? 1120f : 1024f;
            scale = k / camera.Height * camera.Width * 3f / 4f / mapWindow.LargeMapZoom;

            if (Settings.TestKey1.PressedOnce())
            {
                if (TreeCoroutine.Running)
                {
                    WillBot.isBotPaused = true;
                    Console.WriteLine("Bot paused");
                    InputWrapper.ResetMouseButtons();
                    TreeCoroutine.AutoResume = false;
                    TreeCoroutine.Pause();
                }
                else
                {
                    WillBot.Mover.UpdateTerrainData(doNotUpdateIfAlreadyExists: true);
                    WillBot.isBotPaused = false;
                    Console.WriteLine("Bot resumed");
                    TreeCoroutine.AutoResume = true;
                    TreeCoroutine.Resume();
                }
            }
            if (Settings.TestKey2.PressedOnce())
            {
                //ChaosRecipeBehavior.MapChaosRecipeItems();
                //ChaosRecipeBehavior.GetNumberOfCrRecipeSetsInStash();
                var gameControllerEntities = GameController.EntityListWrapper;

                var labelOnGrounds = GameController.IngameState.IngameUi.ItemsOnGroundLabels;

                var pause = 5;


            }
            if (Settings.TryLootNearbykey.PressedOnce())
            {
                AutoLootCoroutine.AutoResume = true;
                AutoLootCoroutine.Resume();
                Console.WriteLine("Looting nearby");
            }
            if (autoLootingDone == true)
            {
                AutoLootCoroutine.AutoResume = false;
                AutoLootCoroutine.Pause();
                Console.WriteLine("Looting done");
                autoLootingDone = false;
            }

            //Debug check for seed cache
            //var nearestEntities = GameController.Entities.Where(x => x.DistancePlayer < 15).ToList();
            //foreach (var entity in nearestEntities)
            //{
            //    WillBot.WillBot.LogMessageComboCombo($"Distance player: {entity.DistancePlayer}, name: {entity.ToString()}");

            //}

            if (WillBot.isBotPaused == false)
            {
                WillBot.Mover.UpdateExplored();
                StuckTracker.Update();
                pickit.UpdateItemsToPickUp();

            }
            return null;
        }

        public Vector2 testMovePosition = new Vector2();
        public bool runTestMove = false;



        public IEnumerator MainWorkCoroutine()
        {

            while (true)
            {
                if (runTestMove)
                {
                    InputWrapper.ResetMouseButtons();
                    WillBot.LogMessageCombo($"Running test move");
                    runTestMove = false;
                    // yield return mover.MovePlayerToGridPos(testMovePosition);
                }

                yield return new WaitTime(1);
            }
        }





    }
}



//public bool HasEnoughNearbyMonsters(int minimumMonsterCount, int maxDistance)
//{
//    var mobCount = 0;
//    var maxDistanceSquare = maxDistance * maxDistance;

//    var playerPosition = GameController.Player.GridPos;


//    if (LoadedMonsters != null)
//    {
//        List<Entity> localLoadedMonsters = null;
//        lock (MyLocks.LoadedMonstersLock)
//        {
//            localLoadedMonsters = new List<Entity>(LoadedMonsters);
//        }
//        // Make sure we create our own list to iterate as we may be adding/removing from the list
//        foreach (var monster in localLoadedMonsters)
//        {
//            if (WillBot.IsEntityAKillableMonster(monster) == false) continue;
//            var monsterPosition = monster.GridPos;

//            float monsterDistanceSquare = monsterPosition.DistanceSquared(playerPosition);

//            if (monsterDistanceSquare <= maxDistanceSquare)
//            {
//                mobCount++;
//            }

//            if (mobCount >= minimumMonsterCount)
//            {

//                WillBot.LogMessageCombo($"Found enough monsters {monster.ToString()}");
//                return true;
//            }
//        }
//    }
//    WillBot.LogMessageCombo("Not enough monsters");
//    return false;
//}