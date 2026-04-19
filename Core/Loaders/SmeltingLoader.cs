using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Terraria.ModLoader;
using TerraCraft.Core.DataStructures.Smelting;
using TerraCraft.Core.Utils;
using Terraria.ID;

namespace TerraCraft.Core.Loaders
{
    public class SmeltingLoader : ModSystem
    {
        public const string AssetPath = "Assets/SmeltingData";
        public static string ExternalPath = Path.Combine(Path.GetDirectoryName(ModLoader.ModPath), "TerraCraft", "SmeltingData");

        public static SmeltingDatabase Database { get; private set; }

        public override void PostAddRecipes()
        {
            LoadSmeltingRecipes();
        }

        private void LoadSmeltingRecipes()
        {
            var allRecipes = new List<SmeltingRecipe>();
            var allFuels = new List<SmeltingFuel>();

            // 加载嵌入资源
            foreach (string assetPath in Mod.GetFileNames()
                         .Where(p => p.StartsWith(AssetPath) && p.EndsWith(".json")))
            {
                try
                {
                    using Stream stream = Mod.GetFileStream(assetPath);
                    using StreamReader reader = new StreamReader(stream);
                    string json = reader.ReadToEnd();
                    ProcessJson(json, assetPath, allRecipes, allFuels);
                }
                catch (Exception e)
                {
                    Mod.Logger.Warn($"[SmeltingLoader] 加载嵌入资源失败: {assetPath}\n{e.Message}");
                }
            }

            // 加载外部目录
            if (Directory.Exists(ExternalPath))
            {
                foreach (string filePath in Directory.GetFiles(ExternalPath, "*.json", SearchOption.AllDirectories))
                {
                    try
                    {
                        string json = File.ReadAllText(filePath);
                        ProcessJson(json, filePath, allRecipes, allFuels);
                    }
                    catch (Exception e)
                    {
                        Mod.Logger.Warn($"[SmeltingLoader] 加载外部文件失败: {filePath}\n{e.Message}");
                    }
                }
            }

            // 应用唯一性约束：同一主材料 + 同一熔炉类型只能保留一个配方
            allRecipes = ApplyUniquenessConstraint(allRecipes);

            Database = new SmeltingDatabase
            {
                Recipes = allRecipes,
                Fuels = allFuels
            };
            Database.InitializeCache();

            Mod.Logger.Info($"[SmeltingLoader] Successfully loaded {allRecipes.Count} smelting recipes, {allFuels.Count} fuels");
        }

        private void ProcessJson(string json, string sourcePath, List<SmeltingRecipe> allRecipes, List<SmeltingFuel> allFuels)
        {
            try
            {
                var dbDto = JsonConvert.DeserializeObject<SmeltingDatabaseDTO>(json);
                if (dbDto != null)
                {
                    // 处理普通配方
                    if (dbDto.Smelting != null)
                    {
                        foreach (var recipeDto in dbDto.Smelting)
                        {
                            var recipe = ConvertToRecipe(recipeDto);
                            if (recipe.HasValue)
                                allRecipes.Add(recipe.Value);
                        }
                    }

                    // 处理燃料
                    if (dbDto.Fuels != null)
                    {
                        foreach (var fuelDto in dbDto.Fuels)
                        {
                            if (fuelDto.ItemTypes == null) continue;

                            // 解析替换规则
                            int? replaceWithType = null;
                            int replaceAmount = 1;
                            if (fuelDto.Replacement != null && !string.IsNullOrEmpty(fuelDto.Replacement.ReplaceWith))
                            {
                                replaceWithType = ItemIDResolver.ParseItemType(fuelDto.Replacement.ReplaceWith);
                                replaceAmount = fuelDto.Replacement.ReplaceAmount;
                                if (replaceWithType == 0)
                                    Mod.Logger.Warn($"[SmeltingLoader] 燃料替换物品无效: {fuelDto.Replacement.ReplaceWith}");
                            }

                            foreach (string itemId in fuelDto.ItemTypes)
                            {
                                int itemType = ItemIDResolver.ParseItemType(itemId);
                                if (itemType != 0)
                                {
                                    allFuels.Add(new SmeltingFuel
                                    {
                                        ItemType = itemType,
                                        BurnTime = fuelDto.BurnTime,
                                        Level = fuelDto.Level,
                                        Speed = fuelDto.Speed,
                                        ReplaceWithType = replaceWithType,
                                        ReplaceAmount = replaceAmount
                                    });
                                }
                                else
                                {
                                    Mod.Logger.Warn($"[SmeltingLoader] 未知物品ID: {itemId}，燃料跳过");
                                }
                            }
                        }
                    }

                    // 处理模板组
                    if (dbDto.MaterialDefinitions != null && dbDto.SmeltingGroups != null)
                    {
                        var materialDefs = dbDto.MaterialDefinitions.ToDictionary(md => md.Id, md => md);
                        foreach (var group in dbDto.SmeltingGroups)
                        {
                            if (!materialDefs.TryGetValue(group.MaterialSource, out var materialDef))
                            {
                                Mod.Logger.Warn($"[SmeltingLoader] 模板组 {group.Id} 引用了不存在的材料源: {group.MaterialSource}");
                                continue;
                            }

                            var generated = GenerateRecipesFromGroup(group, materialDef);
                            allRecipes.AddRange(generated);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Mod.Logger.Warn($"[SmeltingLoader] 解析 JSON 失败: {sourcePath}\n{e.Message}");
            }
        }

        private List<SmeltingRecipe> GenerateRecipesFromGroup(SmeltingTemplateGroupDTO group, SmeltingMaterialDefinitionDTO materialDef)
        {
            var results = new List<SmeltingRecipe>();
            if (group.Template == null || materialDef.Materials == null)
                return results;

            foreach (var material in materialDef.Materials)
            {
                var replacements = BuildReplacements(material, group.PlaceholderMappings);
                var recipeDto = CloneTemplateWithReplacements(group.Template, replacements);

                // 跳过产出为空的配方
                if (recipeDto.Outputs == null || recipeDto.Outputs.Count == 0)
                    continue;

                var recipe = ConvertToRecipe(recipeDto);
                if (recipe.HasValue)
                    results.Add(recipe.Value);
            }
            return results;
        }

        private Dictionary<string, string> BuildReplacements(Dictionary<string, string> material, Dictionary<string, string> mappings)
        {
            var result = new Dictionary<string, string>();
            if (mappings == null) return result;

            foreach (var kv in mappings)
            {
                string placeholder = kv.Key;
                string property = kv.Value;
                if (material.TryGetValue(property, out string value))
                    result[placeholder] = value;
                else
                    Mod.Logger.Warn($"[SmeltingLoader] 材料缺少属性 '{property}'，占位符 '{placeholder}' 将不会被替换");
            }
            return result;
        }

        private SmeltingRecipeDTO CloneTemplateWithReplacements(SmeltingTemplateDTO template, Dictionary<string, string> replacements)
        {
            var dto = new SmeltingRecipeDTO
            {
                Id = ReplacePlaceholders(template.Id, replacements),
                RequiredTiles = template.RequiredTiles?.Select(t => ReplacePlaceholders(t, replacements)).ToList(),
                Label = ReplacePlaceholders(template.Label, replacements),
                BaseSmeltTime = template.BaseSmeltTime,
                SpecificFuels = template.SpecificFuels?.Select(f => ReplacePlaceholders(f, replacements)).ToList(),
                Ingredients = new List<SmeltingIngredientDTO>(),
                Outputs = new List<SmeltingOutputDTO>(),
                Replacements = new List<SmeltingReplacementDTO>(),
                MinFuelLevel = template.MinFuelLevel
            };

            if (template.Ingredients != null)
            {
                foreach (var ing in template.Ingredients)
                {
                    dto.Ingredients.Add(new SmeltingIngredientDTO
                    {
                        ItemId = ReplacePlaceholders(ing.ItemId, replacements),
                        Amount = ing.Amount
                    });
                }
            }

            if (template.Outputs != null)
            {
                foreach (var outDto in template.Outputs)
                {
                    dto.Outputs.Add(new SmeltingOutputDTO
                    {
                        ItemId = ReplacePlaceholders(outDto.ItemId, replacements),
                        Amount = outDto.Amount
                    });
                }
            }

            if (template.Replacements != null)
            {
                foreach (var rep in template.Replacements)
                {
                    dto.Replacements.Add(new SmeltingReplacementDTO
                    {
                        OriginalItemId = ReplacePlaceholders(rep.OriginalItemId, replacements),
                        ReplaceWith = ReplacePlaceholders(rep.ReplaceWith, replacements),
                        ReplaceAmount = rep.ReplaceAmount
                    });
                }
            }

            return dto;
        }

        private string ReplacePlaceholders(string input, Dictionary<string, string> replacements)
        {
            if (string.IsNullOrEmpty(input)) return input;
            foreach (var kv in replacements)
                input = input.Replace("{" + kv.Key + "}", kv.Value);
            return input;
        }

        private SmeltingRecipe? ConvertToRecipe(SmeltingRecipeDTO dto)
        {
            try
            {
                // 转换 RequiredTiles
                List<int> tileIds = null;
                if (dto.RequiredTiles != null && dto.RequiredTiles.Count > 0)
                {
                    tileIds = new List<int>();
                    foreach (string tileStr in dto.RequiredTiles)
                    {
                        if (string.IsNullOrWhiteSpace(tileStr)) continue;
                        int id = TileIDResolver.ParseTileType(tileStr);
                        if (id != 0) tileIds.Add(id);
                    }
                    if (tileIds.Count == 0) tileIds = null;
                }

                // 转换 Ingredients
                var ingredients = new List<SmeltingIngredient>();
                if (dto.Ingredients != null)
                {
                    foreach (var ingDto in dto.Ingredients)
                    {
                        int itemType = ItemIDResolver.ParseItemType(ingDto.ItemId);
                        if (itemType == 0) continue;
                        ingredients.Add(new SmeltingIngredient
                        {
                            ItemType = itemType,
                            Amount = ingDto.Amount
                        });
                    }
                }

                if (ingredients.Count == 0)
                {
                    Mod.Logger.Warn($"[SmeltingLoader] 配方 {dto.Id} 没有有效原料，跳过");
                    return null;
                }

                // 转换 Outputs
                var outputs = new List<SmeltingOutput>();
                if (dto.Outputs != null)
                {
                    foreach (var outDto in dto.Outputs)
                    {
                        int itemType = ItemIDResolver.ParseItemType(outDto.ItemId);
                        if (itemType == 0) continue;
                        outputs.Add(new SmeltingOutput
                        {
                            ItemType = itemType,
                            Amount = outDto.Amount
                        });
                    }
                }

                if (outputs.Count == 0)
                {
                    Mod.Logger.Warn($"[SmeltingLoader] 配方 {dto.Id} 没有有效产出，跳过");
                    return null;
                }

                // 转换 Replacements
                var replacements = new List<SmeltingReplacement>();
                if (dto.Replacements != null)
                {
                    foreach (var repDto in dto.Replacements)
                    {
                        int original = ItemIDResolver.ParseItemType(repDto.OriginalItemId);
                        if (original == 0) continue;
                        int? replaceWith = string.IsNullOrEmpty(repDto.ReplaceWith) ? null : ItemIDResolver.ParseItemType(repDto.ReplaceWith);
                        replacements.Add(new SmeltingReplacement
                        {
                            OriginalItemType = original,
                            ReplaceWithType = replaceWith,
                            ReplaceAmount = repDto.ReplaceAmount
                        });
                    }
                }

                // 转换 SpecificFuels
                List<int> specificFuels = null;
                if (dto.SpecificFuels != null && dto.SpecificFuels.Count > 0)
                {
                    specificFuels = new List<int>();
                    foreach (var fuelStr in dto.SpecificFuels)
                    {
                        int fuelType = ItemIDResolver.ParseItemType(fuelStr);
                        if (fuelType != 0) specificFuels.Add(fuelType);
                    }
                    if (specificFuels.Count == 0) specificFuels = null;
                }

                return new SmeltingRecipe
                {
                    Id = dto.Id,
                    RequiredTileIds = tileIds,
                    Label = dto.Label,
                    Ingredients = ingredients,
                    Outputs = outputs,
                    Replacements = replacements,
                    BaseSmeltTime = dto.BaseSmeltTime,
                    SpecificFuels = specificFuels,
                    MinFuelLevel = dto.MinFuelLevel
                };
            }
            catch (Exception e)
            {
                Mod.Logger.Warn($"[SmeltingLoader] 转换配方失败: {dto.Id}\n{e.Message}");
                return null;
            }
        }

        /// <summary>
        /// 应用唯一性约束：对于同一主材料（第一个原料）和同一熔炉类型（RequiredTileIds 集合），只保留最后一个配方。
        /// 如果出现冲突，保留后加载的配方，并发出警告。
        /// </summary>
        private List<SmeltingRecipe> ApplyUniquenessConstraint(List<SmeltingRecipe> recipes)
        {
            var uniqueMap = new Dictionary<(int mainMaterial, int? tileId), SmeltingRecipe>();
            // 注意：一个配方可能支持多个熔炉 ID，需要为每个熔炉 ID 单独检查
            foreach (var recipe in recipes)
            {
                if (recipe.Ingredients == null || recipe.Ingredients.Count == 0)
                    continue;

                int mainMaterial = recipe.Ingredients[0].ItemType;
                var tileIds = (recipe.RequiredTileIds == null || recipe.RequiredTileIds.Count == 0)
                    ? new List<int> { 0 }   // 0 代表通用配方
                    : recipe.RequiredTileIds;

                foreach (int tileId in tileIds)
                {
                    var key = (mainMaterial, tileId == 0 ? (int?)null : tileId);
                    if (uniqueMap.TryGetValue(key, out var existing))
                    {
                        Mod.Logger.Warn($"[SmeltingLoader] 主材料 {mainMaterial} 与熔炉 {tileId} 的配方冲突！保留后加载的配方：{recipe.Id} 覆盖 {existing.Id}");
                    }
                    uniqueMap[key] = recipe;
                }
            }

            // 去重后返回
            var result = new List<SmeltingRecipe>();
            var seenRecipes = new HashSet<SmeltingRecipe>();
            foreach (var recipe in uniqueMap.Values)
            {
                if (!seenRecipes.Contains(recipe))
                {
                    seenRecipes.Add(recipe);
                    result.Add(recipe);
                }
            }
            return result;
        }

        public override void Unload()
        {
            Database = null;
        }
    }
}