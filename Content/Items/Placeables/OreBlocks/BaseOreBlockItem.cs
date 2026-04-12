using Terraria;
using Terraria.ModLoader;

namespace TerraCraft.Content.Items.Placeables.OreBlocks
{
    public abstract class BaseOreBlockItem : ModItem
    {
        public sealed override void SetStaticDefaults()
        {
            Item.ResearchUnlockCount = 100;
        }
    }
}