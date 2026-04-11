using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace TerraCraft.Content.Items.VanillaExt
{
    public class WoodAxe : ModItem
    {
        public override void SetDefaults()
        {
            Item.CloneDefaults(ItemID.CopperAxe);
            Item.crit = -3;
            Item.useTime = 27;
            Item.useAnimation = 30;
            Item.axe = 5;
            Item.scale = 1f;
            Item.damage = 2;
            Item.tileBoost = -2;
            Item.value = Item.sellPrice(0, 0, 20);
        }
    }
}