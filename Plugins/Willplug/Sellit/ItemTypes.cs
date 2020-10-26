using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Willplug.SellItems
{

    public enum ItemTypes
    {
        BodyArmour = 0,
        Helmet,
        Gloves,
        Belt,
        Boots,
        TwoHandedWeapon,
        OneHandedWeapon,
        Amulet,
        Ring,
        Other
    };

    public static class ItemStrings
    {
        // These are base item -> names 
        public static string ScrollOfWisdom = "scroll of wisdom";
        public static string OrbOfAlchemy = "orb of alchemy";
        public static string ChaosOrb = "chaos orb";
        public static string OrbOfScouring = "orb of scouring";
        public static string CartographersChisel = "cartographer's chisel";

    }
}
