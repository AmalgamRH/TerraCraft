using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace TerraCraft.Content.Items.VanillaExt
{
    public class AshWoodPickaxe : ModItem
    {
        public override void SetDefaults()
        {
            Item.CloneDefaults(ItemID.CopperPickaxe);
            Item.crit = -3;
            Item.useTime = 18;
            Item.useAnimation = 23;
            Item.pick = 20;
            Item.damage = 2;
            Item.scale = 0.9f;
            Item.tileBoost = -2;
            Item.value = Item.sellPrice(0, 0, 20);
        }
    }
}