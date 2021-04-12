using System;
using System.Collections.Generic;
using ExileCore.Shared.Cache;
using ExileCore.Shared.Helpers;
using GameOffsets;
using JM.LinqFaster;
using ProcessMemoryUtilities.Memory;

namespace ExileCore.PoEMemory.MemoryObjects
{
    public class TerrainDataObject : RemoteMemoryObject
    {

        private readonly CachedValue<TerrainData> _terrain;

        public TerrainDataObject()
        {
            _terrain = new AreaCache<TerrainData>(() => Address == 0 ? default : M.Read<TerrainData>(Address));
        }
        public long Columns => _terrain.Value.NumCols; // -1
        public long Rows => _terrain.Value.NumRows; // -1
        public int BytesPerRow => _terrain.Value.BytesPerRow;
        public byte[] MeleeData => M.ReadMem(_terrain.Value.LayerMelee.First, (int)_terrain.Value.LayerMelee.Size);
        public byte[] RangedData => M.ReadMem(_terrain.Value.LayerRanged.First, (int)_terrain.Value.LayerRanged.Size);


        //public long Columns => M.Read<long>(Address + 0x18) -1;
        //public long Rows => M.Read<long>(Address + 0x20) -1;
        //public int BytesPerRow => M.Read<int>(Address + 0x108);

        //public long P1Start => M.Read<long>(Address + 0xD8);
        //public long P1End => M.Read<long>(Address + 0xD8 + 8);
        //public byte[] MeleeLayerPathfindinData => M.ReadMem(P1Start, (int)(P1End - P1Start));

        //public long P2Start => M.Read<long>(Address + 0xF0);
        //public long P2End => M.Read<long>(Address + 0xF0 + 8);
        //public byte[] RangedLayerPathfindinData => M.ReadMem(P2Start, (int)(P2End - P2Start));
    }
}
