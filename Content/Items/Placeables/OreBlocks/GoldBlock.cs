using TerraCraft.Content.Tiles.OreBlocks;
using Terraria;
using Terraria.ModLoader;

namespace TerraCraft.Content.Items.Placeables.OreBlocks
{
    public class GoldBlock : BaseOreBlockItem
    {
        public override void SetDefaults()
        {
            Item.DefaultToPlaceableTile(ModContent.TileType<GoldBlockTile>());
            Item.value = Item.sellPrice(0, 0, 0, 30);
        }
    }
}