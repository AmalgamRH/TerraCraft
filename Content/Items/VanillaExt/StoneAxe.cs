using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace TerraCraft.Content.Items.VanillaExt
{
    public class StoneAxe : ModItem
    {
        public override void SetDefaults()
        {
            Item.CloneDefaults(ItemID.CopperAxe);
            Item.crit = -2;
            Item.useTime = 24;
            Item.useAnimation = 30;
            Item.axe = 6;
            Item.scale = 1f;
            Item.damage = 3;
            Item.tileBoost = -1;
            Item.value = Item.sellPrice(0, 0, 25);
        }
    }
}