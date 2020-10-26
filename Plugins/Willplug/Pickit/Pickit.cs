using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using ExileCore;
using ExileCore.PoEMemory;
using ExileCore.PoEMemory.Components;
using ExileCore.PoEMemory.Elements.InventoryElements;
using ExileCore.PoEMemory.MemoryObjects;
using ExileCore.Shared;
using ExileCore.Shared.Enums;
using ExileCore.Shared.Helpers;
using ExileCore.Shared.Nodes;

using Newtonsoft.Json;
using SharpDX;
using TreeRoutine.TreeSharp;

namespace Willplug
{
    public class Pickit //:  BaseSettingsPlugin<WillplugSettings>
    {
        #region PickitRelated
        private const string PickitRuleDirectory = "Pickit Rules";
        private readonly List<Entity> _entities = new List<Entity>();
        private readonly Stopwatch _pickUpTimer = Stopwatch.StartNew();

        private readonly WaitTime toPick = new WaitTime(1);
        private readonly WaitTime wait3ms = new WaitTime(1);
        private readonly WaitTime waitForNextTry = new WaitTime(1);
        private Vector2 _clickWindowOffset;
        private HashSet<string> _magicRules;
        private HashSet<string> _normalRules;
        private HashSet<string> _rareRules;
        private HashSet<string> _uniqueRules;
        private Dictionary<string, int> _weightsRules = new Dictionary<string, int>();
        private WaitTime pickitCoroutineWaitTime;
        public DateTime buildDate;
        private uint coroutineCounter;
        private Vector2 cursorBeforePickIt;
        private bool CustomRulesExists = true;
        private bool FullWork = true;
        private Element LastLabelClick;
        public string MagicRuleFile;
        private WaitTime mainWorkCoroutine = new WaitTime(5);
        public string NormalRuleFile;
        private Coroutine pickItCoroutine;
        public string RareRuleFile;
        private WaitTime tryToPick = new WaitTime(7);
        public string UniqueRuleFile;
        private WaitTime waitPlayerMove = new WaitTime(10);
        private List<string> PickitFiles { get; set; }
        #endregion

        public List<CustomItem> itemsToPickup = new List<CustomItem>();

        private BaseSettingsPlugin<WillplugSettings> basePlugin;

        private WillplugSettings Settings;
        public Pickit(WillplugSettings settings, BaseSettingsPlugin<WillplugSettings> basePlugin)
        {
            this.Settings = settings;
            this.basePlugin = basePlugin;
        }

        #region PickitRelated

        public bool InCustomList(HashSet<string> checkList, CustomItem itemEntity, ItemRarity rarity)
        {
            if (checkList.Contains(itemEntity.BaseName) && itemEntity.Rarity == rarity) return true;
            return false;
        }

        public bool OverrideChecks(CustomItem item)
        {
            try
            {
                #region Currency

                if (Settings.AllCurrency && item.ClassName.EndsWith("Currency"))
                {
                    return !item.Path.Equals("Metadata/Items/Currency/CurrencyIdentification", StringComparison.Ordinal) ||
                           !Settings.IgnoreScrollOfWisdom;
                }

                #endregion

                #region Shaper & Elder

                if (Settings.ElderItems)
                {
                    if (item.IsElder)
                        return true;
                }

                if (Settings.ShaperItems)
                {
                    if (item.IsShaper)
                        return true;
                }

                if (Settings.FracturedItems)
                {
                    if (item.IsFractured)
                        return true;
                }

                #endregion

                #region Rare Overrides
                if (Settings.HarvestSeeds && item.ClassName == "HarvestSeed") return true;

                if (Settings.Rares && item.Rarity == ItemRarity.Rare)
                {
                    if (Settings.RareJewels && (item.ClassName == "Jewel" || item.ClassName == "AbyssJewel")) return true;
                    if (Settings.RareRings && item.ClassName == "Ring" && item.ItemLevel <= 74 && item.ItemLevel >= Settings.RareRingsilvl) return true;
                    if (Settings.RareAmulets && item.ClassName == "Amulet" && item.ItemLevel <= 74 && item.ItemLevel >= Settings.RareAmuletsilvl) return true;
                    if (Settings.RareBelts && item.ClassName == "Belt" && item.ItemLevel <= 74 && item.ItemLevel >= Settings.RareBeltsilvl) return true;
                    if (Settings.RareGloves && item.ClassName == "Gloves" && item.ItemLevel <= 74 && item.ItemLevel >= Settings.RareGlovesilvl) return true;
                    if (Settings.RareBoots && item.ClassName == "Boots" && item.ItemLevel <= 74 && item.ItemLevel >= Settings.RareBootsilvl) return true;
                    if (Settings.RareHelmets && item.ClassName == "Helmet" && item.ItemLevel <= 74 && item.ItemLevel >= Settings.RareHelmetsilvl) return true;
                    if (Settings.RareArmour && item.ClassName == "Body Armour" && item.ItemLevel <= 74 && item.ItemLevel >= Settings.RareArmourilvl) return true;

                    if (Settings.RareTwoHandedWeapon && item.IsTwoHandedWeapon && item.ItemLevel <= 74 && item.ItemLevel >= Settings.RareTwoHandedWeaponilvl) return true;


                    if (Settings.RareWeapon && item.IsWeapon && item.ItemLevel >= Settings.RareWeaponilvl &&
                      item.Width * item.Height <= Settings.ItemCells) return true;

                    if (Settings.RareWeapon && item.IsWeapon && item.ItemLevel >= Settings.RareWeaponilvl &&
                        item.Width <= Settings.RareWeaponWidth && item.Height <= Settings.RareWeaponHeight) return true;

                    if (Settings.RareShield && item.ClassName == "Shield" && item.ItemLevel >= Settings.RareShieldilvl &&
                        item.Width * item.Height <= Settings.ItemCells) return true;
                }

                #endregion

                #region Sockets/Links/RGB

                if (Settings.Sockets && item.Sockets >= Settings.TotalSockets.Value) return true;
                if (Settings.Links && item.LargestLink >= Settings.LargestLink) return true;
                if (Settings.RGB && item.IsRGB) return true;

                #endregion

                #region Divination Cards

                if (Settings.AllDivs && item.ClassName == "DivinationCard") return true;

                #endregion

                #region Maps

                if (Settings.Maps && item.MapTier >= Settings.MapTier.Value) return true;
                if (Settings.Maps && Settings.UniqueMap && item.MapTier >= 1 && item.Rarity == ItemRarity.Unique) return true;
                if (Settings.Maps && Settings.MapFragments && item.ClassName == "MapFragment") return true;

                #endregion

                #region Quest Items

                if (Settings.QuestItems && item.ClassName == "QuestItem") return true;

                #endregion

                #region Skill Gems

                if (Settings.Gems && item.Quality >= Settings.GemQuality.Value && item.ClassName.Contains("Skill Gem")) return true;

                #endregion

                #region Uniques

                if (Settings.AllUniques && item.Rarity == ItemRarity.Unique) return true;

                #endregion
            }
            catch (Exception e)
            {
                //LogError($"{nameof(OverrideChecks)} error: {e}");
            }

            return false;
        }

        public bool DoWePickThis(CustomItem itemEntity)
        {
            if (!itemEntity.IsValid)
                return false;

            var pickItemUp = false;


            #region Force Pickup All

            if (Settings.PickUpEverything)
            {
                return true;
            }

            #endregion

            #region Rarity Rule Switch

            if (CustomRulesExists)
            {
                switch (itemEntity.Rarity)
                {
                    case ItemRarity.Normal:
                        if (_normalRules != null)
                        {
                            if (InCustomList(_normalRules, itemEntity, itemEntity.Rarity))
                                pickItemUp = true;
                        }

                        break;
                    case ItemRarity.Magic:
                        if (_magicRules != null)
                        {
                            if (InCustomList(_magicRules, itemEntity, itemEntity.Rarity))
                                pickItemUp = true;
                        }

                        break;
                    case ItemRarity.Rare:
                        if (_rareRules != null)
                        {
                            if (InCustomList(_rareRules, itemEntity, itemEntity.Rarity))
                                pickItemUp = true;
                        }

                        break;
                    case ItemRarity.Unique:
                        if (_uniqueRules != null)
                        {
                            if (InCustomList(_uniqueRules, itemEntity, itemEntity.Rarity))
                                pickItemUp = true;
                        }

                        break;
                }
            }

            #endregion

            #region Override Rules

            if (OverrideChecks(itemEntity)) pickItemUp = true;

            #endregion

            #region Metamorph edit

            if (itemEntity.IsMetaItem)
            {
                pickItemUp = true;
            }

            #endregion 

            return pickItemUp;
        }

        private IEnumerator FindItemToPick()
        {
            //if (!Input.GetKeyState(Settings.PickUpKey.Value) || !basePlugin.GameController.Window.IsForeground()) yield break;
            var window = basePlugin.GameController.Window.GetWindowRectangleTimeCache;
            var rect = new RectangleF(window.X, window.X, window.X + window.Width, window.Y + window.Height);
            var playerPos = basePlugin.GameController.Player.GridPos;

            List<CustomItem> currentLabels;
            var morphPath = "Metadata/MiscellaneousObjects/Metamorphosis/MetamorphosisMonsterMarker";

            if (Settings.UseWeight)
            {
                currentLabels = basePlugin.GameController.Game.IngameState.IngameUi.ItemsOnGroundLabels
                    .Where(x => x.Address != 0 &&
                                x.ItemOnGround?.Path != null &&
                                x.IsVisible && x.Label.GetClientRectCache.Center.PointInRectangle(rect) &&
                                (x.CanPickUp || x.MaxTimeForPickUp.TotalSeconds <= 0) || x.ItemOnGround?.Path == morphPath)
                    .Select(x => new CustomItem(x, basePlugin.GameController.Files,
                        x.ItemOnGround.DistancePlayer, _weightsRules, x.ItemOnGround?.Path == morphPath))
                    .OrderByDescending(x => x.Weight).ThenBy(x => x.Distance).ToList();
            }
            else
            {
                currentLabels = basePlugin.GameController.Game.IngameState.IngameUi.ItemsOnGroundLabels
                    .Where(x => x.Address != 0 &&
                                x.ItemOnGround?.Path != null &&
                                x.IsVisible && x.Label.GetClientRectCache.Center.PointInRectangle(rect) &&
                                (x.CanPickUp || x.MaxTimeForPickUp.TotalSeconds <= 0) || x.ItemOnGround?.Path == morphPath)
                    .Select(x => new CustomItem(x, basePlugin.GameController.Files,
                        x.ItemOnGround.DistancePlayer, _weightsRules, x.ItemOnGround?.Path == morphPath))
                    .OrderBy(x => x.Distance).ToList();
            }

            basePlugin.GameController.Debug["PickIt"] = currentLabels;
            var pickUpThisItem = currentLabels.FirstOrDefault(x => DoWePickThis(x) && x.Distance < Settings.PickupRange);
            if (pickUpThisItem.IsMetaItem ? pickUpThisItem?.WorldIcon != null : pickUpThisItem?.GroundItem != null) yield return TryToPickV2(pickUpThisItem);
            FullWork = true;
        }
        public Object itemsToPickupLock { get; set; } = new Object();
        public IEnumerator PickUpItems()
        {
            //if (itemsToPickup == null) yield break;
            //    List<CustomItem> localItemsToPickUp = null;
            //    lock (itemsToPickupLock)
            //    {
            //        localLoadedMonsters = new List<Entity>(LoadedMonsters);
            //    }
            foreach (var item in itemsToPickup)
            {
                if (DoWePickThis(item) && (item.Distance < Settings.PickupRange))
                {
                    if (item.IsMetaItem ? item.WorldIcon != null : item.GroundItem != null)
                    {
                        yield return TryToPickV2(item);
                    }
                }
            }
        }


        public RunStatus TryToPickAction()
        {
            CustomItem pickItItem = itemsToPickup.First();
            if (!pickItItem.IsValid)
            {
                return RunStatus.Failure;
            }
            var centerOfItemLabel = pickItItem.LabelOnGround.Label.GetClientRectCache.Center;
            var rectangleOfGameWindow = basePlugin.GameController.Window.GetWindowRectangleTimeCache;
            var oldMousePosition = Mouse.GetCursorPositionVector();
            _clickWindowOffset = rectangleOfGameWindow.TopLeft;
            rectangleOfGameWindow.Inflate(-55, -55);
            centerOfItemLabel.X += rectangleOfGameWindow.Left;
            centerOfItemLabel.Y += rectangleOfGameWindow.Top;

            if (!rectangleOfGameWindow.Intersects(new RectangleF(centerOfItemLabel.X, centerOfItemLabel.Y, 3, 3)))
            {
                //LogMessage($"Label outside game window. Label: {centerOfItemLabel} Window: {rectangleOfGameWindow}", 5, Color.Red);
                return RunStatus.Failure;
            }

            var tryCount = 0;

            while (!pickItItem.IsTargeted() && tryCount < 5)
            {
                var completeItemLabel = pickItItem.LabelOnGround?.Label;

                if (completeItemLabel == null)
                {
                    if (tryCount > 0)
                    {
                        //  LogMessage("Probably item already picked.", 3);
                        return RunStatus.Failure;
                    }

                    //LogError("Label for item not found.", 5);
                    return RunStatus.Failure;
                }

                /*while (GameController.Player.GetComponent<Actor>().isMoving)
                {
                    yield return waitPlayerMove;
                }*/
                var clientRect = completeItemLabel.GetClientRect();

                var clientRectCenter = clientRect.Center;

                var vector2 = clientRectCenter + _clickWindowOffset;

                Mouse.MoveCursorToPosition(vector2);
                Thread.Sleep(100);
                Input.Click(MouseButtons.Left);
                tryCount++;
            }

            if (pickItItem.IsTargeted())
                Input.Click(MouseButtons.Left);

            tryCount = 0;
            while (basePlugin.GameController.Game.IngameState.IngameUi.ItemsOnGroundLabels.FirstOrDefault(
                       x => x.Address == pickItItem.LabelOnGround.Address) != null && tryCount < 6)
            {
                Thread.Sleep(50);
                tryCount++;
            }

            return RunStatus.Success;
            //yield return waitForNextTry;

            //   Mouse.MoveCursorToPosition(oldMousePosition);
        }
        private int updateItemsToPickupInterval = 140;
        private DateTime previousUpdateItemsToPickup = DateTime.Now;
        public void UpdateItemsToPickUp()
        {
            if (DateTime.Now.Subtract(previousUpdateItemsToPickup).TotalMilliseconds > updateItemsToPickupInterval)
            {
                previousUpdateItemsToPickup = DateTime.Now;
                var window = basePlugin.GameController.Window.GetWindowRectangleTimeCache;
                var rect = new RectangleF(window.X, window.X, window.X + window.Width, window.Y + window.Height);
                var playerPos = basePlugin.GameController.Player.GridPos;

                List<CustomItem> currentLabels;
                var morphPath = "Metadata/MiscellaneousObjects/Metamorphosis/MetamorphosisMonsterMarker";

                var harvestMiscItems = "Metadata/MiscellaneousObjects/Harvest/";
                //if (Settings.UseWeight)
                //{
                //    currentLabels = basePlugin.GameController.Game.IngameState.IngameUi.ItemsOnGroundLabels
                //        .Where(x => x.Address != 0 &&
                //                    x.ItemOnGround?.Path != null &&
                //                    x.IsVisible && x.Label.GetClientRectCache.Center.PointInRectangle(rect) &&
                //                    (x.CanPickUp || x.MaxTimeForPickUp.TotalSeconds <= 0) || x.ItemOnGround?.Path == morphPath)
                //        .Select(x => new CustomItem(x, basePlugin.GameController.Files,
                //            x.ItemOnGround.DistancePlayer, _weightsRules, x.ItemOnGround?.Path == morphPath))
                //        .OrderByDescending(x => x.Weight).ThenBy(x => x.Distance).ToList();
                //}
                //else
                //{
                var tmpList = basePlugin.GameController.Game.IngameState.IngameUi?.ItemsOnGroundLabels?.Where(x => x.ItemOnGround?.Path?.Contains(harvestMiscItems) == false)?.ToList();
                if (tmpList == null || tmpList.Count == 0)
                {
                    itemsToPickup.Clear();
                    return;
                }
                currentLabels = tmpList
                    .Where(x => x.Address != 0 &&
                                x.ItemOnGround?.Path != null &&
                                x.IsVisible && // x.Label.GetClientRectCache.Center.PointInRectangle(rect) &&
                                (x.CanPickUp || x.MaxTimeForPickUp.TotalSeconds <= 0) || x.ItemOnGround?.Path == morphPath)

                    .Select(x => new CustomItem(x, basePlugin.GameController.Files,
                        x.ItemOnGround.DistancePlayer, _weightsRules, x.ItemOnGround?.Path == morphPath))
                    .Where(x => DoWePickThis(x))
                    .OrderBy(x => x.Distance).ToList();
                //}

                itemsToPickup = currentLabels;
            }
        }

        private IEnumerator TryToPickV2(CustomItem pickItItem)
        {
            if (!pickItItem.IsValid)
            {
                FullWork = true;
                //LogMessage("PickItem is not valid.", 5, Color.Red);
                yield break;
            }

            var centerOfItemLabel = pickItItem.LabelOnGround.Label.GetClientRectCache.Center;
            var rectangleOfGameWindow = basePlugin.GameController.Window.GetWindowRectangleTimeCache;
            var oldMousePosition = Mouse.GetCursorPositionVector();
            _clickWindowOffset = rectangleOfGameWindow.TopLeft;
            rectangleOfGameWindow.Inflate(-55, -55);
            centerOfItemLabel.X += rectangleOfGameWindow.Left;
            centerOfItemLabel.Y += rectangleOfGameWindow.Top;

            if (!rectangleOfGameWindow.Intersects(new RectangleF(centerOfItemLabel.X, centerOfItemLabel.Y, 3, 3)))
            {
                FullWork = true;
                //LogMessage($"Label outside game window. Label: {centerOfItemLabel} Window: {rectangleOfGameWindow}", 5, Color.Red);
                yield break;
            }

            var tryCount = 0;

            while (!pickItItem.IsTargeted() && tryCount < 5)
            {
                var completeItemLabel = pickItItem.LabelOnGround?.Label;

                if (completeItemLabel == null)
                {
                    if (tryCount > 0)
                    {
                        //  LogMessage("Probably item already picked.", 3);
                        yield break;
                    }

                    //LogError("Label for item not found.", 5);
                    yield break;
                }

                /*while (GameController.Player.GetComponent<Actor>().isMoving)
                {
                    yield return waitPlayerMove;
                }*/
                var clientRect = completeItemLabel.GetClientRect();

                var clientRectCenter = clientRect.Center;

                var vector2 = clientRectCenter + _clickWindowOffset;

                Mouse.MoveCursorToPosition(vector2);
                yield return wait3ms;
                Mouse.MoveCursorToPosition(vector2);
                yield return wait3ms;
                yield return Mouse.LeftClick();
                yield return toPick;
                tryCount++;
            }

            if (pickItItem.IsTargeted())
                Input.Click(MouseButtons.Left);

            tryCount = 0;

            while (basePlugin.GameController.Game.IngameState.IngameUi.ItemsOnGroundLabels.FirstOrDefault(
                       x => x.Address == pickItItem.LabelOnGround.Address) != null && tryCount < 6)
            {
                tryCount++;
                yield return new WaitTime(200);
            }

            //yield return waitForNextTry;

            //   Mouse.MoveCursorToPosition(oldMousePosition);
        }

        #region (Re)Loading Rules

        public void LoadRuleFiles()
        {
            var PickitConfigFileDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Plugins", "Compiled", nameof(Willplug),
                PickitRuleDirectory);

            if (!Directory.Exists(PickitConfigFileDirectory))
            {
                Directory.CreateDirectory(PickitConfigFileDirectory);
                CustomRulesExists = false;
                return;
            }

            var dirInfo = new DirectoryInfo(PickitConfigFileDirectory);

            PickitFiles = dirInfo.GetFiles("*.txt").Select(x => Path.GetFileNameWithoutExtension(x.Name)).ToList();
            _normalRules = LoadPickit(Settings.NormalRuleFile);
            _magicRules = LoadPickit(Settings.MagicRuleFile);
            _rareRules = LoadPickit(Settings.RareRuleFile);
            _uniqueRules = LoadPickit(Settings.UniqueRuleFile);
            _weightsRules = LoadWeights("Weights");
        }

        public HashSet<string> LoadPickit(string fileName)
        {
            var hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            if (fileName == string.Empty)
            {
                CustomRulesExists = false;
                return hashSet;
            }

            var pickitFile = $@"{basePlugin.DirectoryFullName}\{PickitRuleDirectory}\{fileName}.txt";

            if (!File.Exists(pickitFile))
            {
                CustomRulesExists = false;
                return hashSet;
            }

            var lines = File.ReadAllLines(pickitFile);

            foreach (var x in lines.Where(x => !string.IsNullOrWhiteSpace(x) && !x.StartsWith("#")))
            {
                hashSet.Add(x.Trim());
            }

            //LogMessage($"PICKIT :: (Re)Loaded {fileName}", 5, Color.GreenYellow);
            return hashSet;
        }

        public Dictionary<string, int> LoadWeights(string fileName)
        {
            var result = new Dictionary<string, int>();
            var filePath = $@"{basePlugin.DirectoryFullName}\{PickitRuleDirectory}\{fileName}.txt";
            if (!File.Exists(filePath)) return result;

            var lines = File.ReadAllLines(filePath);

            foreach (var x in lines.Where(x => !string.IsNullOrEmpty(x) && !x.StartsWith("#") && x.IndexOf('=') > 0))
            {
                try
                {
                    var s = x.Split('=');
                    if (s.Length == 2) result[s[0].Trim()] = int.Parse(s[1]);
                }
                catch (Exception e)
                {
                    DebugWindow.LogError($"{nameof(Willplug)} => Error when parse weight.");
                }
            }

            return result;
        }

        //public override void OnPluginDestroyForHotReload()
        //{
        //    pickItCoroutine.Done(true);
        //}

        #endregion


        #endregion

    }
}
