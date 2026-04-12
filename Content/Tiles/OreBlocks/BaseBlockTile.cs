using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;
using Terraria.ObjectData;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TerraCraft.Core.VanillaExt;

namespace TerraCraft.Content.Tiles.OreBlocks
{
    public abstract class BaseBlockTile : ModTile
    {
        public sealed override void SetStaticDefaults()
        {
            Main.tileFrameImportant[Type] = true;
            Main.tileSolid[Type] = true;
            Main.tileObsidianKill[Type] = false;
            Main.tileNoAttach[Type] = false;
            Main.tileBlockLight[Type] = true;

            TileObjectData.newTile.CopyFrom(TileObjectData.Style2x2);
            TileObjectData.newTile.Width = 2;
            TileObjectData.newTile.Height = 2;
            TileObjectData.newTile.Origin = new Point16(0, 1);
            TileObjectData.newTile.DrawYOffset = 0;
            TileObjectData.newTile.LavaDeath = false;
            TileObjectData.newTile.AnchorBottom = AnchorData.Empty;
            TileObjectData.addTile(Type);

            SetDefaults();
        }
        public override bool CanExplode(int i, int j)
        {
            if (Main.LocalPlayer.GetModPlayer<MaxPickPowerPlayer>().MaxPickPowerEver >= MinPick)
            {
                return true;
            }
            return false;
        }
        public override bool CanPlace(int i, int j)
        {
            Rectangle tileRect = new Rectangle(i * 16, (j - 1) * 16, 32, 32);
            foreach (Player player in Main.player)
            {
                if (player.active && player.getRect().Intersects(tileRect))
                    return false;
            }

            bool CanAttach(int x, int y)
            {
                Tile tile = Main.tile[x, y];
                return tile.HasTile && !Main.tileNoAttach[tile.TileType];
            }

            // 检查底部
            if (CanAttach(i, j + 1) && CanAttach(i + 1, j + 1))
                return true;
            // 检查顶部
            if (CanAttach(i, j - 2) && CanAttach(i + 1, j - 2))
                return true;
            // 检查左侧
            if (CanAttach(i - 1, j - 1) && CanAttach(i - 1, j))
                return true;
            // 检查右侧
            if (CanAttach(i + 2, j - 1) && CanAttach(i + 2, j))
                return true;
            return false;
        }
        public virtual void SetDefaults() { }
        public override void NumDust(int i, int j, bool fail, ref int num)
        {
            num = fail ? 4 : 12;
        }
        public sealed override bool PreDraw(int i, int j, SpriteBatch spriteBatch)
        {
            var position = new Vector2(i, j) * 16;
            return true;
        }
    }
}
