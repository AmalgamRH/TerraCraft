using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace TerraCraft.Content.Items.VanillaExt
{
    public class StonePickaxe : ModItem
    {
        public override void SetDefaults()
        {
            Item.CloneDefaults(ItemID.CopperPickaxe);
            Item.crit = -2;
            Item.useTime = 18;
            Item.useAnimation = 23;
            Item.pick = 25;
            Item.damage = 3;
            Item.scale = 0.9f;
            Item.tileBoost = -1;
            Item.value = Item.sellPrice(0, 0, 25);
        }
    }
}