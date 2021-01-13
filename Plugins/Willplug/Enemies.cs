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
    public class Enemies
    {
        private GameController GameController;
        // killable monsters
        // Unique mobs ( map bosses most relevant
        // The entity list is updated every 100 ms
        // The only reason to cache certain entities is if there is processing of the entities which should be done seldom

        private static List<string> ignoredMonstersPaths = new List<string>()
        {
            @"Metadata/Monsters/Totems/BetrayalHealingTotem"
        };
        public HashSet<long> BlackListedMonsterAddresses = new HashSet<long>();

        private TimeCache<List<Entity>> _uniqueMonsters;
        public List<Entity> UniqueMonsters => _uniqueMonsters.Value;

        private TimeCache<List<Entity>> _killableMonsters;
        public List<Entity> KillableMonsters => _killableMonsters.Value;


        private TimeCache<List<Entity>> _nearbyMonsters;
        public List<Entity> NearbyMonsters => _nearbyMonsters.Value;

        private TimeCache<List<Entity>> _nearbyCorpses;
        public List<Entity> NearbyCorpses => _nearbyCorpses.Value;


        public Entity ClosestMonsterEntity => KillableMonsters?.FirstOrDefault() ?? null;


        public DateTime LastKilledMonsterTime { get; set; } = DateTime.Now;

        public Enemies(GameController GameController)
        {
            this.GameController = GameController;
            this._killableMonsters = new TimeCache<List<Entity>>(GetKillableMonsters, 200);
            this._uniqueMonsters = new TimeCache<List<Entity>>(GetUniqueMonsters, 280);
            this._nearbyMonsters = new TimeCache<List<Entity>>(GetNearbyMonsters, 180);
            this._nearbyCorpses = new TimeCache<List<Entity>>(GetNearbyCorpses, 300);

        }

        private List<Entity> GetNearbyCorpses()
        {
            try
            {
                var validMonsters = new List<Entity>(GameController.EntityListWrapper.ValidEntitiesByType[EntityType.Monster]);
                return validMonsters?.Where(x => x?.DistancePlayer < 45 && x?.IsAlive == false && x?.IsTargetable == true)?.OrderBy(x => x?.DistancePlayer)?.ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return null;
            }

        }
        // Unique monsters can be untargetable etc until they are activated. Mosten often activated by distance, sometimes levers etc.
        private List<Entity> GetUniqueMonsters()
        {
            try
            {
                var validMonsters = new List<Entity>(GameController.EntityListWrapper.ValidEntitiesByType[EntityType.Monster]);

                return validMonsters?.Where(x => x?.Rarity == MonsterRarity.Unique && x?.IsTargetable == true)?.OrderBy(x => x?.DistancePlayer)?.ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return null;
            }
        }

        private List<Entity> GetNearbyMonsters()
        {
            return _killableMonsters.Value?.Where(x => x.DistancePlayer < 45)?.ToList() ?? null;
        }


        private Entity bufferedNearestValidEntity = null;
        private List<Entity> GetKillableMonsters()
        {
            // Called from different threads.
            try
            {
                if (bufferedNearestValidEntity?.IsAlive == false)
                {
                    LastKilledMonsterTime = DateTime.Now;
                }
                var validMonsters = new List<Entity>(GameController.EntityListWrapper.ValidEntitiesByType[EntityType.Monster]);
                validMonsters = validMonsters?.Where(x => IsEntityAKillableMonster(x, maxDistance: 1000))?.OrderBy(x => x.DistancePlayer)?.ToList();

                if (validMonsters != null && validMonsters.Count > 0)
                {
                    bufferedNearestValidEntity = validMonsters?.First();
                    return validMonsters;
                }
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return null;
            }

        }

        public (int, Vector2) GetCorpsesForDesecrate()
        {
            try
            {
                var validCorpses = GameController.EntityListWrapper.ValidEntitiesByType[EntityType.Monster]?.Where(x => x.IsAlive == false && x.IsTargetable == true && x.DistancePlayer < 40)?.OrderBy(x => x.DistancePlayer)?.ToList();
                if (validCorpses?.Count == 0)
                {
                    return (0, new Vector2());
                }
                else
                {
                    return (validCorpses.Count, validCorpses.First().GridPos);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return (0, new Vector2());
            }
        }

        public bool IsEntityAKillableMonster(Entity entity, float maxDistance = 300)
        {
            //essence monolith: Metadata/MiscellaneousObjects/Monolith
            //Essence monster : Monster->stats->cannotbedamaged
            bool isDamageable = true;
            Stats monsterStats = null;
            try
            {
                monsterStats = entity.GetComponent<Stats>();
            }
            catch(Exception ex)
            {
                Console.WriteLine("Failed while getting component stats in checking for entity killable");
            }
            if (monsterStats != null)
            {
                int cannotBeDamagedNumber = -1;
                monsterStats.StatDictionary.TryGetValue(GameStat.CannotBeDamaged, out cannotBeDamagedNumber);
                if (cannotBeDamagedNumber == 1)
                {
                    isDamageable = false;
                }
            }
            if (BlackListedMonsterAddresses.Contains(entity.Address))
            {
                Console.WriteLine("Monster with address {0} is blacklisted", entity.Address);
                return false;
            }

            bool distanceOk = entity.DistancePlayer < maxDistance;

            if (entity.IsTargetable && entity.IsHostile && entity.IsValid
                && entity.IsAlive && isDamageable && !entity.IsHidden && distanceOk && !ignoredMonstersPaths.Contains(entity.Path))
            {
                return true;
            }
            return false;
        }



    }
}
//public List<long> BlackListedMonsterAddresses = new List<long>();

//private static long previousClosestMonsterAddress = 0;
//private static DateTime previousClosestMonsterCheck = DateTime.Now;
//private static double sameMonsterForThisTimeMs = 0;

//public Entity ClosestMonsterEntity
//{
//    get
//    {
//        if (CurrentlyValidMonsterEntities != null && CurrentlyValidMonsterEntities.Count > 0)
//        {
//            var nearest = CurrentlyValidMonsterEntities.First();
//            if (nearest.Address == previousClosestMonsterAddress)
//            {
//                var duration = DateTime.Now.Subtract(previousClosestMonsterCheck).TotalMilliseconds;
//                sameMonsterForThisTimeMs += duration;
//                if (sameMonsterForThisTimeMs > 15000)
//                {
//                    BlackListedMonsterAddresses.Add(nearest.Address);
//                }
//            }
//            else
//            {
//                sameMonsterForThisTimeMs = 0;
//            }
//            previousClosestMonsterCheck = DateTime.Now;
//            previousClosestMonsterAddress = nearest.Address;

//            return nearest;
//        }
//        return null;
//    }
//}

//if (DateTime.Now.Subtract(previousUpdateBotStateTime).TotalMilliseconds > updateBotStateInterval)
//{
//    previousUpdateBotStateTime = DateTime.Now;
//    lock (MyLocks.CurrentlyValidMonstersLock)
//    {
//        try
//        {
//            if (currentValidMonsterEntities != null && currentValidMonsterEntities.Count > 0)
//            {
//                if (currentValidMonsterEntities.First().IsAlive == false)
//                {
//                    LastKilledMonsterTime = DateTime.Now;
//                    Console.WriteLine("Updated previously killed monster time");
//                }
//            }
//            currentValidMonsterEntities = GameController.EntityListWrapper.ValidEntitiesByType[EntityType.Monster].Where(x => WillBot.IsEntityAKillableMonster(x, maxDistance: 500)).OrderBy(x => x.DistancePlayer).ToList();
//        }
//        catch (Exception ex)
//        {
//            currentValidMonsterEntities = null;
//        }
//    }

//    // Console.WriteLine($"Updating items to pick took: {DateTime.Now.Subtract(pickitUpdateItemsStartTime).TotalMilliseconds} milliseconds");
//}