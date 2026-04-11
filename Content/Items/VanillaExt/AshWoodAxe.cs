using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace TerraCraft.Content.Items.VanillaExt
{
    public class AshWoodAxe : ModItem
    {
        public override void SetDefaults()
        {
            Item.CloneDefaults(ItemID.CopperAxe);
            Item.crit = -2;
            Item.useTime = 25;
            Item.useAnimation = 30;
            Item.axe = 6;
            Item.scale = 1f;
            Item.damage = 3;
            Item.tileBoost = -2;
            Item.value = Item.sellPrice(0, 0, 20);
        }
    }
}