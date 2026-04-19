using System.Collections.Generic;
using System.Linq;
using TerraCraft.Core.Utils;  // 假设 TileCompatibilitySystem 在这个命名空间下（如果熔炉也支持兼容链条）
using TerraCraft.Core.DataStructures.Smelting;
using TerraCraft.Core.Systems;

namespace TerraCraft.Core.DataStructures.Smelting
{
    public class SmeltingDatabase
    {
        public List<SmeltingRecipe> Recipes { get; set; } = new List<SmeltingRecipe>();
        public List<SmeltingFuel> Fuels { get; set; } = new List<SmeltingFuel>();

        // 缓存结构 - 按熔炉TileId分组的配方
        private Dictionary<int, List<SmeltingRecipe>> _recipesByTileId;

        // 缓存结构 - 通用熔炼配方
        private List<SmeltingRecipe> _universalRecipes;
        private bool _cacheInitialized = false;

        // 燃料缓存：ItemType -> struct
        private Dictionary<int, SmeltingFuel> _fuelData;

        public void InitializeCache()
        {
            if (_cacheInitialized) return;

            // ---------- 配方缓存 ----------
            // 通用配方（不限熔炉）
            _universalRecipes = Recipes
                .Where(r => r.RequiredTileIds == null || r.RequiredTileIds.Count == 0)
                .ToList();

            // 按具体熔炉ID分组（仅当 RequiredTileIds 非空且包含元素）
            _recipesByTileId = Recipes
                .Where(r => r.RequiredTileIds != null && r.RequiredTileIds.Count > 0)
                .SelectMany(recipe => recipe.RequiredTileIds.Select(tileId => (tileId, recipe)))
                .GroupBy(x => x.tileId)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(x => x.recipe).ToList());

            // 燃料缓存
            _fuelData = new Dictionary<int, SmeltingFuel>();
            foreach (var fuel in Fuels)
            {
                if (fuel.BurnTime > 0 && !_fuelData.ContainsKey(fuel.ItemType))
                {
                    _fuelData[fuel.ItemType] = fuel;
                }
            }

            _cacheInitialized = true;
        }

        /// <summary>获取指定熔炉（TileId）可用的所有配方（包含通用配方 + 直接/间接兼容的熔炉配方）</summary>
        public List<SmeltingRecipe> GetRecipesForTile(int tileId)
        {
            if (!_cacheInitialized) InitializeCache();

            var recipeSet = new HashSet<SmeltingRecipe>();

            // 添加所有通用配方
            if (_universalRecipes != null)
            {
                foreach (var recipe in _universalRecipes)
                    recipeSet.Add(recipe);
            }

            // 获取当前熔炉所有兼容的tile
            var compatibleTiles = TileCompatibilitySystem.GetCompatibleTiles(tileId);
            foreach (var compatibleTileId in compatibleTiles)
            {
                if (_recipesByTileId.TryGetValue(compatibleTileId, out var tileRecipes))
                {
                    foreach (var recipe in tileRecipes)
                        recipeSet.Add(recipe);
                }
            }

            return recipeSet.ToList();
        }

        /// <summary>根据输入物品类型获取所有配方（原料中任意匹配）</summary>
        public List<SmeltingRecipe> GetRecipesByInput(int itemType)
        {
            if (!_cacheInitialized) InitializeCache();

            return Recipes
                .Where(r => r.Ingredients != null && r.Ingredients.Any(i => i.ItemType == itemType))
                .ToList();
        }

        /// <summary>根据输出物品类型获取所有配方</summary>
        public List<SmeltingRecipe> GetRecipesByOutput(int itemType)
        {
            if (!_cacheInitialized) InitializeCache();

            return Recipes
                .Where(r => r.Outputs != null && r.Outputs.Any(o => o.ItemType == itemType))
                .ToList();
        }

        /// <summary>获取某物品作为燃料的燃烧时间（ticks），返回0表示不可燃</summary>
        public int GetBurnTime(int itemType)
        {
            if (!_cacheInitialized) InitializeCache();
            return _fuelData.TryGetValue(itemType, out var data) ? data.BurnTime : 0;
        }
        public int GetFuelLevel(int itemType)
        {
            if (!_cacheInitialized) InitializeCache();
            return _fuelData.TryGetValue(itemType, out var data) ? data.Level : 0;
        }
        public float GetFuelSpeed(int itemType)
        {
            if (!_cacheInitialized) InitializeCache();
            return _fuelData.TryGetValue(itemType, out var data) ? data.Speed : 1f;
        }
        /// <summary>检查某物品是否可作为燃料</summary>
        public bool IsFuel(int itemType) => GetBurnTime(itemType) > 0;
        public (int? ReplaceWithType, int ReplaceAmount) GetFuelReplacement(int itemType)
        {
            if (!_cacheInitialized) InitializeCache();
            if (_fuelData.TryGetValue(itemType, out var fuel))
                return (fuel.ReplaceWithType, fuel.ReplaceAmount);
            return (null, 1);
        }
        /// <summary>清空所有缓存（配方列表变化时调用）</summary>
        public void ClearCache()
        {
            _recipesByTileId = null;
            _universalRecipes = null;
            _fuelData = null;
            _cacheInitialized = false;
        }
    }
}