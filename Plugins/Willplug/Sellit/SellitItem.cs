using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using ExileCore;
using ExileCore.PoEMemory;
using ExileCore.PoEMemory.Components;
using ExileCore.PoEMemory.Elements;
using ExileCore.PoEMemory.Elements.InventoryElements;
using ExileCore.PoEMemory.MemoryObjects;
using ExileCore.Shared.Enums;
using SharpDX;
using Map = ExileCore.PoEMemory.Components.Map;

namespace Willplug.SellItems
{


    public class SellitItem
    {

        public SellitItem(NormalInventoryItem item, FilesContainer fs)
        {
            NormalInventoryItem = item;
            Rect = item.GetClientRectCache;
            Center = item.GetClientRectCache.Center;
            ItemEntity = item.Item;

            Path = ItemEntity.Path;
            var baseItemType = fs.BaseItemTypes.Translate(Path);
            if (baseItemType != null)
            {
                ClassName = baseItemType.ClassName;
                BaseName = baseItemType.BaseName;
                Width = baseItemType.Width;
                Height = baseItemType.Height;
            }


            if (ItemEntity.HasComponent<Quality>())
            {
                var quality = ItemEntity.GetComponent<Quality>();
                Quality = quality.ItemQuality;
            }

            if (ItemEntity.HasComponent<Base>())
            {
                var abase = ItemEntity.GetComponent<Base>();
                IsElder = abase.isElder;
                IsShaper = abase.isShaper;
            }

            if (ItemEntity.HasComponent<Mods>())
            {
                var mods = ItemEntity.GetComponent<Mods>();
                Rarity = mods.ItemRarity;
                IsIdentified = mods.Identified;
                ItemLevel = mods.ItemLevel;
                IsFractured = mods.HaveFractured;
                if (ItemLevel >=60 && ItemLevel <= 74)
                {
                    IsCrItemLevel = true;
                }

            }

            if (ItemEntity.HasComponent<Sockets>())
            {
                var sockets = ItemEntity.GetComponent<Sockets>();
                IsRGB = sockets.IsRGB;
                Sockets = sockets.NumberOfSockets;
                LargestLink = sockets.LargestLinkSize;
            }

            var ArmourClass = new List<string>
            {
                "Belt",
                "Helmet",
                "Body Armour",
                "Boots",
                "Gloves"
            };
            if (ArmourClass.Any(ClassName.Equals)) IsArmour = true;

            switch (ClassName)
            {
                case "Amulet":
                    ItemType = ItemTypes.Amulet;
                    IsAmulet = true;
                    break;
                case "Ring":
                    ItemType = ItemTypes.Ring;
                    IsRing = true;
                    break;
                case "Belt":
                    ItemType = ItemTypes.Belt;
                    IsBelt = true;
                    break;
                case "Helmet":
                    ItemType = ItemTypes.Helmet;
                    IsHelmet = true;
                    break;
                case "Body Armour":
                    ItemType = ItemTypes.BodyArmour;
                    IsBodyArmor = true;
                    break;
                case "Boots":
                    ItemType = ItemTypes.Boots;
                    IsBoots = true;
                    break;
                case "Gloves":
                    ItemType = ItemTypes.Gloves;
                    IsGloves = true;
                    break;

                case "Two Hand Mace":
                case "Two Hand Axe":
                case "Two Hand Sword":
                case "Bow":
                case "Staff":
                    IsWeapon = true;
                    ItemType = ItemTypes.TwoHandedWeapon;
                    IsTwoHandedWeapon = true;
                    break;
                case "Shield":
                case "Quivers":               
                case "One Hand Mace":
                case "One Hand Axe":
                case "One Hand Sword":
                case "Thrusting One Hand Sword":
                case "Claw":
                case "Dagger":
                case "Sceptre":
                case "Wand":
                    IsWeapon = true;
                    ItemType = ItemTypes.OneHandedWeapon;
                    IsOneHandedWeapon = true;
                    break;
            }

            var WeaponClass = new List<string>
                {
                    "Two Hand Axe",
                    "Two Hand Mace",
                    "Two Hand Sword",
                    "One Hand Axe",
                    "Bow",
                    "Staff",
                    "One Hand Sword",
                    "One Hand Mace",
                    "Thrusting One Hand Sword",
                    "Claw",
                    "Dagger",
                    "Sceptre",
                    "Wand"
                };
            if (ItemEntity?.Path?.Contains(@"/Weapons/") == true) IsWeapon = true;
            if (WeaponClass.Any(ClassName.Equals)) IsWeapon = true;

            MapTier = ItemEntity.HasComponent<Map>() ? ItemEntity.GetComponent<Map>().Tier : 0;
        }
        public NormalInventoryItem NormalInventoryItem { get; }
        public Entity ItemEntity { get; }
        public Vector2 Center { get; }
        public RectangleF Rect { get; }
        public string BaseName { get; } = "";
        public string ClassName { get; } = "";

        public MinimapIcon WorldIcon { get; }
        public int Height { get; }
        public bool IsElder { get; }
        public bool IsIdentified { get; }
        public bool IsRGB { get; }
        public bool IsShaper { get; }
        public bool IsWeapon { get; }
        public int ItemLevel { get; }
        public int LargestLink { get; }
        public int MapTier { get; }
        public string Path { get; }
        public int Quality { get; }
        public ItemRarity Rarity { get; }
        public int Sockets { get; }
        public int Width { get; }
        public bool IsFractured { get; }
        public int Weight { get; set; }
        public bool IsArmour { get; }

        public ItemTypes ItemType { get; } = ItemTypes.Other;
        public bool IsBelt { get; }
        public bool IsHelmet { get; }
        public bool IsGloves { get; }
        public bool IsBoots { get; }
        public bool IsBodyArmor { get; }

        public bool IsOneHandedWeapon { get; }
        public bool IsTwoHandedWeapon { get; }
        public bool IsAmulet { get; }
        public bool IsRing { get; }
        public bool IsCrItemLevel { get; }

        public override string ToString()
        {
            return $"{BaseName} ({ClassName}) W: {Weight}";
        }
    }


}

