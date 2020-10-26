using GameOffsets;
using System.Collections.Generic;

namespace ExileCore.PoEMemory.MemoryObjects
{
    public class TerrainDataObject : RemoteMemoryObject
    {

        public long Columns => M.Read<long>(Address + 0x18);
        public long Rows => M.Read<long>(Address + 0x20);
        public int BytesPerRow => M.Read<int>(Address + 0x108);

        public long P1Start => M.Read<long>(Address + 0xD8);
        public long P1End => M.Read<long>(Address + 0xD8 + 8);
        public byte[] MeleeLayerPathfindinData => M.ReadMem(P1Start, (int)(P1End - P1Start));

        public long P2Start => M.Read<long>(Address + 0xF0);
        public long P2End => M.Read<long>(Address + 0xF0 + 8);
        public byte[] RangedLayerPathfindinData => M.ReadMem(P2Start, (int)(P2End - P2Start));
    }
}
