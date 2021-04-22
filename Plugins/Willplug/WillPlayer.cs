using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using ExileCore;
using ExileCore.PoEMemory;
using ExileCore.PoEMemory.Components;
using ExileCore.PoEMemory.Elements;
using ExileCore.PoEMemory.Elements.InventoryElements;
using ExileCore.PoEMemory.MemoryObjects;
using ExileCore.Shared;
using ExileCore.Shared.Cache;
using ExileCore.Shared.Enums;
using ExileCore.Shared.Helpers;
using ExileCore.Shared.Nodes;

using Newtonsoft.Json;
using SharpDX;
using TreeRoutine;
using Willplug.Navigation;
using Willplug.SellItems;

namespace Willplug
{
    public enum AreaToTownOrHideoutTransitionReasons
    {
        AreaCompleted,
        InventoryFull,
        DiedAndReleasedToTown
    }

    public class WillPlayer
    {
        private GameController GameController;
        public Pickit pickit;
        public Sellit sellit;
        public Enemies enemies;

        private BaseTreeRoutinePlugin<WillplugSettings, BaseTreeCache> basePlugin;
        private readonly List<Entity> loadedMonsters;

        public bool IsPlayerInventoryFull { get; set; } = false;
        public AreaToTownOrHideoutTransitionReasons CurrentAreaToTownTransitionReason { get; set; } = AreaToTownOrHideoutTransitionReasons.AreaCompleted;
        public bool HasStashedItemsThisTownCycle { get; set; } = false;
        public bool HasSoldItemsThisTownCycle { get; set; } = false;
        public bool HasPerformedChaosRecipeThisTownCycle { get; set; } = false;

        // Various entities/items to scan for
        private TimeCache<Entity> _deliriumMirrorCache;
        public Entity DeliriumMirror => _deliriumMirrorCache.Value;

        private TimeCache<LabelOnGround> _timeLessMonolithCache;
        public LabelOnGround TimelessMonolith => _timeLessMonolithCache.Value;

        private TimeCache<LabelOnGround> _essenceMonsterMonolithCache;
        public LabelOnGround EssenceMonsterMonolith => _essenceMonsterMonolithCache.Value;

        private TimeCache<LabelOnGround> _harvestChestCache;
        public LabelOnGround HarvestChest => _harvestChestCache.Value;
        private TimeCache<LabelOnGround> _strongboxCache;
        public LabelOnGround StrongBox => _strongboxCache.Value;


        private TimeCache<List<LabelOnGround>> _areaTransitionsCache;
        public LabelOnGround ClosestAreaTransitionLabel => _areaTransitionsCache?.Value?.OrderBy(x => x?.ItemOnGround?.DistancePlayer)?.FirstOrDefault() ?? null;


        private TimeCache<IEnumerable<Entity>> _chestCache;
        public Entity ClosestChest => _chestCache?.Value?.FirstOrDefault();


        private TimeCache<LabelOnGround> _blightPumpMapCache;
        public LabelOnGround BlightPumpInMap => _blightPumpMapCache.Value;



        public WillPlayer(BaseTreeRoutinePlugin<WillplugSettings, BaseTreeCache> basePlugin, GameController GameController, Pickit pickit,
    List<Entity> loadedMonsters)
        {
            this.basePlugin = basePlugin;
            this.GameController = GameController;
            this.pickit = pickit;
            this.loadedMonsters = loadedMonsters;
            this.sellit = new Sellit(GameController);
            this.enemies = new Enemies(GameController);


            int updateIntervalForMiscObjects = 500;
            this._deliriumMirrorCache = new TimeCache<Entity>(FindDeliriumMirror, updateIntervalForMiscObjects);
            this._timeLessMonolithCache = new TimeCache<LabelOnGround>(FindTimelessMonolith, updateIntervalForMiscObjects);
            this._essenceMonsterMonolithCache = new TimeCache<LabelOnGround>(FindEssenceMonsterMonolith, updateIntervalForMiscObjects);
            this._harvestChestCache = new TimeCache<LabelOnGround>(FindHarvestChest, updateIntervalForMiscObjects);
            this._strongboxCache = new TimeCache<LabelOnGround>(FindStrongBox, updateIntervalForMiscObjects);

            this._areaTransitionsCache = new TimeCache<List<LabelOnGround>>(FindAreaTransitionLabels, updateIntervalForMiscObjects);

            this._chestCache = new TimeCache<IEnumerable<Entity>>(FindChests, updateIntervalForMiscObjects);

            this._blightPumpMapCache = new TimeCache<LabelOnGround>(FindBlightMapStartPump, updateIntervalForMiscObjects);

        }
        public WillplugSettings Settings => basePlugin.Settings;


        // Stashie sends an event done which is registered in this variable
        public bool StashieHasCompletedStashing = false;


        public Element DestroyItemOnCursosWindow => GameController.Game.IngameState.IngameUi.GetChildAtIndex(125) ?? null;



        //public Element MapStashOverviewElement => GameController.Game.IngameState.IngameUi.StashElement.GetChildAtIndex(2).GetChildAtIndex(0).GetChildAtIndex(1).GetChildAtIndex(3).GetChildAtIndex(0);
        //  private const string areaTransitionPath = @"Metadata/MiscellaneousObjects/AreaTransition";
        private IEnumerable<Entity> FindChests()
        {
            try
            {
                var validChests = new List<Entity>(GameController.EntityListWrapper.ValidEntitiesByType[EntityType.Chest]);
                return validChests?.OrderBy(entity => entity?.DistancePlayer);
                //             var validChests = validChests
                //.Where(entity => entity.HasComponent<Render>() &&
                //                 entity.Address != GameController.Player.Address &&
                //                 entity.IsValid &&
                //                 entity.IsTargetable &&
                //                 (
                //                  entity.HasComponent<Chest>()
                //                 ))?.OrderBy(entity => entity?.DistancePlayer);
                //             return entities;
            }
            catch (Exception ex)
            {
                WillBot.LogMessageCombo(ex.ToString());
                return null;
            }
        }
        //private IEnumerable<Entity> FindDaemons()
        //{
        //    var validDaemons= new List<Entity>(GameController.EntityListWrapper.ValidEntitiesByType[EntityType.Daemon]);
        //}


        private List<LabelOnGround> FindAreaTransitionLabels()
        {
            var list = new List<LabelOnGround>();
            foreach (var itemOnGround in GameController.Game.IngameState.IngameUi.ItemsOnGroundLabels)
            {
                var areaTransitionComponent = itemOnGround.ItemOnGround.GetComponent<AreaTransition>();
                if (areaTransitionComponent == null) continue;
                list.Add(itemOnGround);
            }
            return list;
        }

        private Entity FindDeliriumMirror()
        {
            try
            {
                return GameController.EntityListWrapper.ValidEntitiesByType[EntityType.IngameIcon].FirstOrDefault(x => x.Path?.Contains("AfflictionInitiator") == true);
            }
            catch
            {
                return null;
            }
        }

        private LabelOnGround FindBlightMapStartPump()
        {
            try
            {
                return GameController.Game.IngameState.IngameUi.ItemsOnGroundLabels?.FirstOrDefault(x => x?.ItemOnGround?.Path?.Contains(@"Blight/Objects/BlightPump") == true);
            }
            catch
            {
                return null;
            }
        }

        private LabelOnGround FindTimelessMonolith()
        {

            try
            {
                return GameController.Game.IngameState.IngameUi.ItemsOnGroundLabels?.FirstOrDefault(x => x?.ItemOnGround?.Path?.Contains(@"Objects/LegionInitiator") == true);
            }

            catch
            {
                return null;
            }

        }
        private LabelOnGround FindEssenceMonsterMonolith()
        {

            try
            {
                return GameController.Game.IngameState.IngameUi.ItemsOnGroundLabels?.FirstOrDefault(x => x?.ItemOnGround?.Path?.Contains(@"Metadata/MiscellaneousObjects/Monolith") == true);
            }

            catch
            {
                return null;
            }

        }
        private LabelOnGround FindHarvestChest()
        {

            try
            {
                return GameController.Game.IngameState.IngameUi.ItemsOnGroundLabels?.FirstOrDefault(x => x?.ItemOnGround?.Path?.Contains(@"Metadata/Terrain/Leagues/Harvest/Objects/HarvestFeatureChest") == true);
            }
            catch
            {
                return null;
            }

        }
        private LabelOnGround FindStrongBox()
        {

            try
            {
                return GameController.Game.IngameState.IngameUi.ItemsOnGroundLabels?.FirstOrDefault(x => x?.ItemOnGround?.Path?.Contains(@"Metadata/Chests/StrongBoxes/") == true);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return null;

            }

        }


        public LabelOnGround MapDeviceLabelOnGround
        {
            get
            {
                var stringToMatch = "MappingDevice";
                var found = GameController.Game.IngameState.IngameUi.ItemsOnGroundLabels.
                    Where(x => x.ItemOnGround?.Path?.Contains(stringToMatch) ?? false).ToList();
                if (found.Count > 0)
                {
                    return found.First();
                }
                return null;
            }
        }

        public LabelOnGround ZanaHideoutVendor
        {
            get
            {
                var zanaString = "Zana, Master Cartographer";
                var found = GameController.Game.IngameState.IngameUi.ItemsOnGroundLabels.Where(x => x.Label?.Text?.Equals(zanaString) ?? false).ToList();
                if (found.Count > 0)
                {
                    return found.First();
                }
                return null;
            }
        }

        public LabelOnGround NavaliHideoutVendor
        {
            get
            {
                var navaliString = "Navali";
                var found = GameController.Game.IngameState.IngameUi.ItemsOnGroundLabels.Where(x => x.Label?.Text?.Equals(navaliString) ?? false).ToList();
                if (found.Count > 0)
                {
                    return found.First();
                }
                return null;
            }
        }

        public Element AreaInstanceButtons
        {
            get
            {
                var instanceManager = GameController.Game.IngameState.IngameUi.AreaInstanceUi?.GetChildAtIndex(4);
                if (instanceManager != null && instanceManager.ChildCount > 0)
                {
                    var child2 = instanceManager.GetChildAtIndex(2);
                    var child3 = instanceManager.GetChildAtIndex(3);

                    if (child2.Width > 0 && child2.Height > 0 && child2.Width > child2.Height)
                    {
                        return child2;
                    }
                    else if (child3.Width > 0 && child3.Height > 0 && child3.Width > child3.Height)
                    {
                        return child3;
                    }
                    return null;
                    //When the slider appears due too many instances  -> the instance button is no longer in index 3 , but in 2
                }
                return null;
            }
        }

        public Element AreaInstanceNewButton => AreaInstanceButtons?.GetChildAtIndex(0)?.GetChildAtIndex(0)?.GetChildAtIndex(0);

        public LabelOnGround StashLabel
        {
            get
            {
                var foundStash = GameController.Game.IngameState.IngameUi.ItemsOnGroundLabels.Where(x => x.Label?.Text?.Equals("Stash") ?? false).ToList();
                if (foundStash.Count > 0)
                {
                    return foundStash.First();
                }
                return null;
            }
        }


        public LabelOnGround TownPortal
        {
            get
            {
                foreach (var itemOnGround in GameController.Game.IngameState.IngameUi.ItemsOnGroundLabels)
                {
                    if (itemOnGround?.ItemOnGround != null && itemOnGround?.ItemOnGround.Type == EntityType.TownPortal)
                    {
                        return itemOnGround;
                    }
                }
                return null;
            }
        }

        public NormalInventoryItem TownPortalInInventory
        {
            get
            {
                try
                {
                    var inventory = GameController.Game.IngameState.IngameUi.InventoryPanel[InventoryIndex.PlayerInventory];
                    var stashItems = inventory?.VisibleInventoryItems;
                    var foundIfany = stashItems?.Where(x => x.Item?.Path.Contains("Metadata/Items/Currency/CurrencyPortal") ?? false).ToList();
                    return (foundIfany.Count > 0) ? foundIfany.First() : null;
                }
                catch
                {
                    return null;
                }
            }
        }



        public Vector2 CurrentPositionToExplore { get; set; }
        public bool IsCurrentPositionToExploreValid { get; set; }





        public CustomItem ClosestItemToLoot
        {
            get
            {
                if (pickit.itemsToPickup != null && pickit.itemsToPickup.Count > 0)
                {
                    return pickit.itemsToPickup.First();
                }
                return null;
            }
        }


        public Boolean isHealthBelowPercentage(int healthPercentage)
        {
            var playerLife = GameController.Game.IngameState.Data.LocalPlayer.GetComponent<Life>();
            return playerLife.HPPercentage * 100 < healthPercentage;
        }

        public Boolean isHealthBelowValue(int healthValue)
        {
            var playerLife = GameController.Game.IngameState.Data.LocalPlayer.GetComponent<Life>();
            return playerLife.CurHP < healthValue;
        }

        public Boolean isManaBelowPercentage(int manaPercentage)
        {
            var playerLife = GameController.Game.IngameState.Data.LocalPlayer.GetComponent<Life>();
            return playerLife.MPPercentage * 100 < manaPercentage;
        }

        public Boolean isManaBelowValue(int manaValue)
        {
            var playerLife = GameController.Game.IngameState.Data.LocalPlayer.GetComponent<Life>();
            return playerLife.CurMana < manaValue;
        }

        public Boolean isEnergyShieldBelowPercentage(int energyShieldPercentage)
        {
            var playerLife = GameController.Game.IngameState.Data.LocalPlayer.GetComponent<Life>();
            return playerLife.MaxES > 0 && playerLife.ESPercentage * 100 < energyShieldPercentage;
        }

        public Boolean isEnergyShieldBelowValue(int energyShieldValue)
        {
            var playerLife = GameController.Game.IngameState.Data.LocalPlayer.GetComponent<Life>();
            return playerLife.MaxES > 0 && playerLife.CurMana < energyShieldValue;
        }

        public Boolean playerHasBuffs(List<String> buffs)
        {
            if (buffs == null || buffs.Count == 0)
                return false;

            var playerBuffs = GameController.Game.IngameState.Data.LocalPlayer.GetComponent<ExileCore.PoEMemory.Components.Buffs>();
            if (playerBuffs == null)
                return false;

            //   bool hasAnybuff = false;
            foreach (var buff in buffs)
            {

                if (playerBuffs.HasBuff(buff ?? "") == false)
                {
                    return false;
                    //hasAnybuff = true;
                }
                //if (!String.IsNullOrEmpty(buff) && !playerBuffs.Any(x => !String.IsNullOrWhiteSpace(x.Name) && buff.StartsWith(x.Name)))
                //{
                //    return false;
                //}
            }
            return true;
        }

        public int? getPlayerStat(string playerStat)
        {
            int statValue = 0;

            if (!GameController.EntityListWrapper.Player.Stats.TryGetValue((GameStat)GameController.Files.Stats.records[playerStat].ID, out statValue))
                return null;

            return statValue;
        }

        public Boolean playerDoesNotHaveAnyOfBuffs(List<String> buffs)
        {
            if (buffs == null || buffs.Count == 0)
                return true;

            var playerBuffs = GameController.Game.IngameState.Data.LocalPlayer.GetComponent<ExileCore.PoEMemory.Components.Buffs>();
            if (playerBuffs == null)
                return true;


            foreach (var buff in buffs)
            {
                if (playerBuffs.HasBuff(buff ?? "") == true)
                {
                    return false;
                }
                //if (!String.IsNullOrEmpty(buff) && playerBuffs.Any(x => !String.IsNullOrWhiteSpace(x.Name) && buff.StartsWith(x.Name)))
                //{
                //    return false;
                //}
            }
            return true;
        }

        public int GetChargesForBuff(string nameOfBuff)
        {
            //var playerLife = GameController.Game.IngameState.Data.LocalPlayer.GetComponent<Life>();
           // var playerBuffs = playerLife.Buffs;
            var playerBuffs = GameController.Game.IngameState.Data.LocalPlayer.GetComponent<ExileCore.PoEMemory.Components.Buffs>();

            if (playerBuffs == null || String.IsNullOrEmpty(nameOfBuff))
                return 0;

       
            var buff = playerBuffs.BuffsList.Find(x => !String.IsNullOrWhiteSpace(x.Name) && nameOfBuff.StartsWith(x.Name));
            return buff?.Charges ?? 0;
        }

        public int CountPlayerDeployedObjectsWithName(string name)
        {
            var actor = GameController.Game.IngameState.Data.LocalPlayer.GetComponent<Actor>();
            int numberOfObjects = actor?.DeployedObjects?.Where(x => x?.Entity?.Path?.Contains(name) ?? false)?.Count() ?? 0;
            return numberOfObjects;
        }

        public Boolean entityDoesNotHaveAnyOfBuffs(Entity entity, List<String> buffs)
        {
            if (entity == null) return true;
            if (buffs == null || buffs.Count == 0)
                return true;
            var entityLife = entity.GetComponent<Life>();
            var entityBuffs = entity.GetComponent<Buffs>();
            if (entityBuffs == null || entityBuffs.BuffsList.Count == 0) return true;

            foreach (var buff in buffs)
            {
                if (!String.IsNullOrEmpty(buff) && entityBuffs.BuffsList.Any(x => !String.IsNullOrWhiteSpace(x.Name) && buff.StartsWith(x.Name)))
                {
                    return false;
                }
            }
            return true;
        }


        public Boolean isPlayerDead()
        {
            var playerLife = GameController.Game.IngameState.Data.LocalPlayer.GetComponent<Life>();
            return playerLife.CurHP <= 0;
        }


    }
}



