using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Willplug
{
    static public class MyLocks
    {
       static public Object LoadedMonstersLock { get; set; } = new object();
        static public Object CurrentlyValidMonstersLock { get; set; } = new object();

        static public Object UpdateKillableMonstersLock { get; set; } = new object();
        static public Object ClosestMonsterLock { get; set; } = new object();

        static public Object UpdateTerrainDataLock { get; set; } = new object();
    }
}
