using System.Collections.Generic;
using TerraCraft.Core.UI.GridCrafting;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace TerraCraft.Core.Systems.GridCrafting
{
    public class CraftingTileGlobal : GlobalTile
    {
        private GridCraftingUIRegister UIRegister => ModContent.GetInstance<GridCraftingUIRegister>();

        // 使用元组列表，Tile 与 Item 成对出现，易于维护
        private static readonly List<(int TileType, int ItemType)> GridCraftingPairs = new()
        {
            (TileID.WorkBenches, ItemID.WorkBench),
            (TileID.HeavyWorkBench, ItemID.HeavyWorkBench),
            (TileID.Sawmill, ItemID.Sawmill)
        };

        public override void MouseOver(int i, int j, int type)
        {
            var pair = GridCraftingPairs.Find(p => p.TileType == type);
            if (pair != default)
            {
                Player player = Main.LocalPlayer;
                player.cursorItemIconEnabled = true;
                player.cursorItemIconID = pair.ItemType;
                player.mouseInterface = true;
            }
        }

        public override void RightClick(int i, int j, int type)
        {
            var pair = GridCraftingPairs.Find(p => p.TileType == type);
            if (pair != default)
            {
                Main.playerInventory = true;
                // 传入 Tile ID 和对应的物品 ID（用于 UI 图标显示等）
                UIRegister.OpenGridCraftingUI(pair.TileType, pair.ItemType);
            }
        }
    }
}
