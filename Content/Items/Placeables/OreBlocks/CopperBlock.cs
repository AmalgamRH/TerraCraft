using TerraCraft.Content.Tiles.OreBlocks;
using Terraria;
using Terraria.ModLoader;

namespace TerraCraft.Content.Items.Placeables.OreBlocks
{
    public class CopperBlock : BaseOreBlockItem
    {
        public override void SetDefaults()
        {
            Item.DefaultToPlaceableTile(ModContent.TileType<CopperBlockTile>());
            Item.value = Item.sellPrice(0, 0, 0, 30);
        }
    }
}