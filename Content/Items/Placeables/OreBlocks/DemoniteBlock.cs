using TerraCraft.Content.Tiles.OreBlocks;
using Terraria;
using Terraria.ModLoader;

namespace TerraCraft.Content.Items.Placeables.OreBlocks
{
    public class DemoniteBlock : BaseOreBlockItem
    {
        public override void SetDefaults()
        {
            Item.DefaultToPlaceableTile(ModContent.TileType<DemoniteBlockTile>());
            Item.value = Item.sellPrice(0, 0, 0, 30);
        }
    }
}