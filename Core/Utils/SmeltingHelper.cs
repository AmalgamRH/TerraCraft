using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;
using TerraCraft.Core.Systems.Smelting;
using Terraria.ObjectData;

namespace TerraCraft.Core.Utils
{
    public static class SmeltingHelper
    {
        public static bool TryGetTEFurnace(this Tile tile, int x, int y, out TEFurnace furnace)
        {
            furnace = null;
            Point16 topLeft = TileObjectData.TopLeft(x, y);
            if (TileEntity.ByPosition.TryGetValue(topLeft, out TileEntity te) && te is TEFurnace f)
            {
                furnace = f;
                return true;
            }
            return false;
        }

        public static bool TryGetTEFurnace(int x, int y, out TEFurnace furnace)
        {
            furnace = null;
            Point16 topLeft = TileObjectData.TopLeft(x, y);
            if (TileEntity.ByPosition.TryGetValue(topLeft, out TileEntity te) && te is TEFurnace f)
            {
                furnace = f;
                return true;
            }
            return false;
        }

        public static bool HasTEFurnace(int x, int y)
        {
            Point16 topLeft = TileObjectData.TopLeft(x, y);
            return TileEntity.ByPosition.TryGetValue(topLeft, out TileEntity te) && te is TEFurnace;
        }

        public static bool HasTEFurnace(this Tile tile, int x, int y)
        {
            Point16 topLeft = TileObjectData.TopLeft(x, y);
            return TileEntity.ByPosition.TryGetValue(topLeft, out TileEntity te) && te is TEFurnace;
        }
    }
}