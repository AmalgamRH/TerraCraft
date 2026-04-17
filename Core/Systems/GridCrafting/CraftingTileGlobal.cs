using System.Collections.Generic;
using TerraCraft.Core.UI.GridCrafting;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ObjectData;

namespace TerraCraft.Core.Systems.GridCrafting
{
    public class CraftingTileGlobal : GlobalTile
    {
        private GridCraftingUIRegister UIRegister => ModContent.GetInstance<GridCraftingUIRegister>();

        /// <summary>
        /// 判断该 tileType 是否在我们支持的合成台列表里
        /// </summary>
        private static bool IsSupportedCraftingStation(int type)
            => CraftingStationSize.IsSupported(type);

        public override void MouseOver(int i, int j, int type)
        {
            if (!IsSupportedCraftingStation(type))
                return;

            int style = TileObjectData.GetTileStyle(Main.tile[i, j]);
            int itemID = TileLoader.GetItemDropFromTypeAndStyle(type, style);

            Player player = Main.LocalPlayer;
            player.cursorItemIconEnabled = true;
            player.cursorItemIconID = itemID;
            player.mouseInterface = true;
        }

        public override void RightClick(int i, int j, int type)
        {
            if (!IsSupportedCraftingStation(type))
                return;

            int style = TileObjectData.GetTileStyle(Main.tile[i, j]);
            int itemID = TileLoader.GetItemDropFromTypeAndStyle(type, style);

            Main.playerInventory = true;
            UIRegister.OpenGridCraftingUI(type, itemID);
        }
    }
}