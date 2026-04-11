using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace TerraCraft.Core.VanillaExt
{
    public class CopperToolModify : GlobalItem
    {
        public override void SetDefaults(Item item)
        {
            base.SetDefaults(item);

            switch (item.type)
            {
                case ItemID.CopperAxe: 
                case ItemID.CopperAxeOld:
                    item.tileBoost = 0;
                    item.scale = 1;
                    break;

                case ItemID.CopperPickaxe:
                case ItemID.CopperPickaxeOld:
                    item.tileBoost = 0;
                    item.scale = 1;
                    break;
                case ItemID.CactusPickaxe:
                    item.pick = 20;
                    break;
            }
        }
    }
}