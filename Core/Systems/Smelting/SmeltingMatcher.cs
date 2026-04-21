using System.Collections.Generic;
using System.Linq;
using TerraCraft.Core.DataStructures.Smelting;
using TerraCraft.Core.Loaders;
using TerraCraft.Core.Systems;
using Terraria;
using Terraria.ID;

namespace TerraCraft.Core.Systems.Smelting
{
    public static class SmeltingMatcher
    {
        /// <summary>获取最佳配方，返回 null 表示无有效配方</summary>
        public static SmeltingRecipe? GetBestRecipe(int tileId, int mainItemType, IList<Item> materialSlots, Item fuelItem)
        {
            if (mainItemType == -1) return null;

            var database = SmeltingLoader.Database;
            if (database == null) return null;

            var allRecipesForTile = database.GetRecipesForTile(tileId);
            if (allRecipesForTile == null || allRecipesForTile.Count == 0) return null;

            var matchedRecipes = new List<SmeltingRecipe>();
            foreach (var recipe in allRecipesForTile)
            {
                if (recipe.Ingredients == null || recipe.Ingredients.Count == 0) continue;
                if (recipe.Ingredients[0].ItemType != mainItemType) continue;

                int requiredAmount = recipe.Ingredients[0].Amount;
                int currentAmount = GetMaterialAmount(materialSlots, mainItemType);
                if (currentAmount >= requiredAmount)
                {
                    matchedRecipes.Add(recipe);
                }
            }

            if (matchedRecipes.Count == 0) return null;

            matchedRecipes.Sort((a, b) => GetRecipePriority(tileId, b).CompareTo(GetRecipePriority(tileId, a)));
            return matchedRecipes.FirstOrDefault();
        }

        public static bool IsFuelValidForRecipe(SmeltingRecipe recipe, int fuelType, int fuelLevel)
        {
            if (recipe.Id == null) return false; // 无效配方
            if (fuelType == 0) return false;

            if (recipe.SpecificFuels != null && recipe.SpecificFuels.Count > 0)
                if (!recipe.SpecificFuels.Contains(fuelType)) return false;

            if (recipe.MinFuelLevel > 0 && fuelLevel < recipe.MinFuelLevel) return false;

            return true;
        }

        public static float GetTotalSpeed(int tileId, SmeltingRecipe recipe, float fuelSpeed)
        {
            if (recipe.Id == null) return 0f;
            float? furnaceSpeed = SmeltingTileDataBase.GetSpeedMultiplier(tileId, recipe.Label);
            if (!furnaceSpeed.HasValue || furnaceSpeed.Value <= 0f)
                return 0f;
            return furnaceSpeed.Value * fuelSpeed;
        }

        public static void GetFuelData(int itemType, out int burnTime, out float speed, out int level)
        {
            var db = SmeltingLoader.Database;
            if (db == null)
            {
                burnTime = 0; speed = 1f; level = 0;
                return;
            }
            burnTime = db.GetBurnTime(itemType);
            speed = db.GetFuelSpeed(itemType);
            level = db.GetFuelLevel(itemType);
        }

        public static bool CanAddOutput(SmeltingRecipe recipe, Item outputSlot)
        {
            if (recipe.Id == null || recipe.Outputs == null || recipe.Outputs.Count == 0) return false;
            var outputInfo = recipe.Outputs[0];
            if (outputSlot.IsAir) return true;
            return outputSlot.type == outputInfo.ItemType && outputSlot.stack + outputInfo.Amount <= outputSlot.maxStack;
        }

        private static int GetMaterialAmount(IList<Item> slots, int itemType)
        {
            int total = 0;
            foreach (var item in slots)
                if (item != null && !item.IsAir && item.type == itemType)
                    total += item.stack;
            return total;
        }

        private static int GetRecipePriority(int currentTileId, SmeltingRecipe recipe)
        {
            if (recipe.RequiredTileIds == null || recipe.RequiredTileIds.Count == 0)
                return -1;

            if (recipe.RequiredTileIds.Contains(currentTileId))
                return GetTilePriority(currentTileId);

            int maxPriority = -1;
            foreach (int requiredTile in recipe.RequiredTileIds)
            {
                if (TileCompatibilitySystem.IsTileCompatible(currentTileId, requiredTile))
                {
                    int priority = GetTilePriority(requiredTile);
                    if (priority > maxPriority) maxPriority = priority;
                }
            }
            return maxPriority;
        }

        private static int GetTilePriority(int tileType)
        {
            if (tileType == TileID.AdamantiteForge) return 100;
            if (tileType == TileID.Hellforge) return 50;
            if (tileType == TileID.Furnaces) return 10;
            if (tileType == TileID.GlassKiln) return 5;
            return 0;
        }
    }
}