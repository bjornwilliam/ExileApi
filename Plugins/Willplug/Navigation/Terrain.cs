using SharpDX;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Willplug.Navigation
{
    static public class Terrain
    {
        static public int MapCellSizeI = 23; // map coords tiles are 23 x 23
        static public float WorldCellSizeF = 250.0f; // world coords tiles are 250 x 250

        static public float MapToWorldScalar = WorldCellSizeF / MapCellSizeI; // 10.869565f
        static public float WorldToMapScalar = MapCellSizeI / WorldCellSizeF; // 0.092f

        //public static bool WalkableValue

        public static byte WalkableValue(byte[] data, int bytesPerRow, long c, long r)
        {
            var offset = r * bytesPerRow + c / 2;
            if (offset < 0 || offset >= data.Length)
            {
                throw new Exception(string.Format($"WalkableValue failed: ({c}, {r}) [{bytesPerRow}] => {offset}"));
            }
            byte b;
            // 0 0 1 1 2 2 3 3   <- c/2  above when calculating offset
            // F T F T F T F T   <- (c&1 ) test
            // 0 1 2 3 4 5 6 7   <- column numbers
            // Every byte gets split up -> walkable tile expressd with 4 bits
            if ((c & 1) == 0)
            {
                b = (byte)(data[offset] & 0xF);
            }
            else
            {
                b = (byte)(data[offset] >> 4);
            }
            return b;
        }

        static public List<Vector2> GetAllGridPositionsForOnes(byte[] data, int bytesPerRow, long cols, long rows)
        {
            byte prevByte = 0;
            List<Vector2> allones = new List<Vector2>();
            for (long r = rows * MapCellSizeI - 1; r >= 0; --r)
            {
                for (long c = 0; c < cols * MapCellSizeI; c++)
                {
                    var b = WalkableValue(data, bytesPerRow, c, r);
                    if (b == 1 || b == 0)
                    {

                        allones.Add(new Vector2(c, r));
                    }
                    else if (b != 0 && prevByte == 0)
                    {
                        allones.Add(new Vector2(c, r));
                    }
                    prevByte = b;
                }
            }
            return allones;
        }

        public static void PopulatePathfindingGrid(byte[] data, int bytesPerRow, long cols, long rows, byte[,] pfGrid)
        {
            for (long r = rows * MapCellSizeI - 1; r >= 0; --r)
            {
                for (long c = 0; c < cols * MapCellSizeI; c++)
                {
                    var b = WalkableValue(data, bytesPerRow, c, r);
                    if (b < 2)
                    {
                        pfGrid[c, r] = 0;
                    }
                    else
                    {
                        pfGrid[c, r] = 1;
                    }
                }
            }
        }

        public static void BuildMap(string saveName, byte[] data, int bytesPerRow, long cols, long rows)
        {
            StringBuilder sb = new StringBuilder();

            for (long r = rows * MapCellSizeI - 1; r >= 0; --r)
            {
                for (long c = 0; c < cols * MapCellSizeI; c++)
                {
                    var b = WalkableValue(data, bytesPerRow, c, r);
                    var ch = b.ToString()[0];
                    if (b == 0)
                        ch = ' ';
                    sb.AppendFormat("{0}", ch);
                }
                sb.AppendLine();
            }
            File.WriteAllText(saveName, sb.ToString());
        }
    }
}
