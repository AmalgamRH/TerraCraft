using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace TerraCraft.Core.VanillaExt
{
    public class MaterialItemDefines : GlobalItem
    {
        public override void SetDefaults(Item item)
        {
            if (RecipeMaterialCache.MaterialItemIds.Contains(item.type))
            {
                item.material = true;
            }
        }
    }
    public static class RecipeMaterialCache
    {
        public static HashSet<int> MaterialItemIds { get; private set; } = new();

        public static void Load(List<DataStructures.GridCrafting.GriddedRecipe> recipes)
        {
            MaterialItemIds.Clear();

            foreach (var recipe in recipes)
            {
                if (recipe.Ingredients == null) continue;

                foreach (var ing in recipe.Ingredients)
                {
                    // 直接物品ID
                    if (ing.ItemType > 0)
                    {
                        MaterialItemIds.Add(ing.ItemType);
                    }
                    // 配方组
                    else if (!string.IsNullOrEmpty(ing.RecipeGroup))
                    {
                        try
                        {
                            var items = Utils.RecipeGroupResolver.GetRecipeGroupItems(ing.RecipeGroup);
                            foreach (int id in items)
                                MaterialItemIds.Add(id);
                        }
                        catch { }
                    }
                }
            }

            ModContent.GetInstance<TerraCraft>().Logger.Info($"[TerraCraft] Cached {MaterialItemIds.Count} material items.");
        }

        public static void Unload()
        {
            MaterialItemIds.Clear();
        }
    }
}