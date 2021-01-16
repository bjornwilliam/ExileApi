using GameOffsets;
using System.Collections.Generic;

namespace ExileCore.PoEMemory.MemoryObjects
{
    public class TerrainDataObject : RemoteMemoryObject
    {

        public long Columns => M.Read<long>(Address + 0x18) -1;
        public long Rows => M.Read<long>(Address + 0x20) -1;
        public int BytesPerRow => M.Read<int>(Address + 0xE0);

        public long P1Start => M.Read<long>(Address + 0xB0);
        public long P1End => M.Read<long>(Address + 0xB0 + 8);
        public byte[] MeleeLayerPathfindinData => M.ReadMem(P1Start, (int)(P1End - P1Start));

        public long P2Start => M.Read<long>(Address + 0xC8);
        public long P2End => M.Read<long>(Address + 0xC8 + 8);
        public byte[] RangedLayerPathfindinData => M.ReadMem(P2Start, (int)(P2End - P2Start));
    }
}
