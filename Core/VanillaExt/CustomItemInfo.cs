using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace TerraCraft.Core.VanillaExt
{
    public class CustomItemInfo : GlobalItem
    {
        private bool isFuel = false;
        public override bool InstancePerEntity => true;
        public override void SetDefaults(Item item)
        {
            if (CustomItemDataCache.MaterialItemIds.Contains(item.type))
            {
                item.material = true;
            }
            if (CustomItemDataCache.FuelItemIds.Contains(item.type))
            {
                isFuel = true;
            }
        }
        public override void ModifyTooltips(Item item, List<TooltipLine> tooltips)
        {
            if (isFuel)
            {
                int materialIndex = tooltips.FindIndex(t => t.Mod == "Terraria" && t.Name == "Material");
                int firstPrefixIndex = tooltips.FindIndex(t => t.Mod == "Terraria" && t.Name.StartsWith("Prefix"));

                int insertIndex;
                if (materialIndex != -1)
                    insertIndex = materialIndex + 1;
                else if (firstPrefixIndex != -1)
                    insertIndex = firstPrefixIndex;
                else
                    insertIndex = tooltips.Count;

                var newLine = new TooltipLine(Mod, "Fuel", TerraCraft.GetLocalizedText("Tooltips.Fuel"));
                tooltips.Insert(insertIndex, newLine);
            }
        }
    }
}