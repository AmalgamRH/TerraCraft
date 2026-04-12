using TerraCraft.Content.Tiles.OreBlocks;
using Terraria;
using Terraria.ModLoader;

namespace TerraCraft.Content.Items.Placeables.OreBlocks
{
    public class MythrilBlock : BaseOreBlockItem
    {
        public override void SetDefaults()
        {
            Item.DefaultToPlaceableTile(ModContent.TileType<MythrilBlockTile>());
            Item.value = Item.sellPrice(0, 0, 0, 30);
        }
    }
}