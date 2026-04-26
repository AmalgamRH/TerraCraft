using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Terraria.ModLoader;
using TerraCraft.Core.DataStructures.Smelting;
using TerraCraft.Core.Utils;
using Terraria.ID;
using TerraCraft.Core.VanillaExt;

namespace TerraCraft.Core.Loaders
{
    public class SmeltingLoader : ModSystem
    {
        public const string AssetPath = "Assets/SmeltingData";
        public static string ExternalPath = Path.Combine(Path.GetDirectoryName(ModLoader.ModPath), "TerraCraft", "SmeltingData");
        private static readonly string BlacklistPath = Path.Combine(Path.GetDirectoryName(ModLoader.ModPath), "TerraCraft", "SmeltingBlacklist.json");
        private const string BlacklistTemplateAsset = "Assets/Templates/SmeltingBlacklist.json.template";
        private const string SmeltingTemplateAsset = "Assets/Templates/SmeltingRecipes.json.template";
        public static SmeltingDatabase Database { get; private set; }

        private RecipeBlacklistDTO _blacklist;

        public override void PostAddRecipes()
        {
            LoadSmeltingRecipes();
        }

        private void LoadSmeltingRecipes()
        {
            // 加载黑名单
            _blacklist = RecipeBlacklistDTO.LoadFrom(BlacklistPath);
            if (_blacklist.HasAny)
                Mod.Logger.Info($"[SmeltingLoader] Loaded blacklist: {_blacklist.DisabledRecipeIds.Count} recipe IDs, {_blacklist.DisabledGroupIds.Count} group IDs, {_blacklist.DisabledLabels.Count} labels");

            // 若黑名单文件尚不存在，将内嵌模板输出到文件系统供参考
            EnsureBlacklistTemplate();

            var allSmeltingRecipes = new List<SmeltingRecipe>();
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
                    ProcessJson(json, assetPath, allSmeltingRecipes, allFuels);
                }
                catch (Exception e)
                {
                    Mod.Logger.Warn($"[SmeltingLoader] Failed to load embedded resource: {assetPath}\n{e.Message}");
                }
            }

            // 确保外部目录存在，若不存在则创建
            if (!Directory.Exists(ExternalPath))
            {
                Directory.CreateDirectory(ExternalPath);
            }

            bool hasExternalJson = false;

            // 加载外部目录中的 JSON 文件
            foreach (string filePath in Directory.GetFiles(ExternalPath, "*.json", SearchOption.AllDirectories))
            {
                hasExternalJson = true;
                try
                {
                    string json = File.ReadAllText(filePath);
                    ProcessJson(json, filePath, allSmeltingRecipes, allFuels);
                }
                catch (Exception e)
                {
                    Mod.Logger.Warn($"[SmeltingLoader] Failed to load external file: {filePath}\n{e.Message}");
                }
            }

            // 如果外部没有找到任何 .json 文件，则写出内嵌模板供用户参考
            EnsureSmeltingTemplateIfEmpty(hasExternalJson);

            // 应用唯一性约束：同一主材料 + 同一熔炉类型只能保留一个配方
            allSmeltingRecipes = ApplyUniquenessConstraint(allSmeltingRecipes);

            Database = new SmeltingDatabase
            {
                Recipes = allSmeltingRecipes,
                Fuels = allFuels
            };
            Database.InitializeCache();

            CustomItemDataCache.LoadFuelItem(allFuels);

            Mod.Logger.Info($"[SmeltingLoader] Successfully loaded {allSmeltingRecipes.Count} smelting recipes, {allFuels.Count} fuels");
        }

        /// <summary>
        /// 如果外部目录没有任何 .json 文件，则将内嵌熔炼配方模板写出到该目录供用户参考。
        /// </summary>
        private void EnsureSmeltingTemplateIfEmpty(bool hasExternalJson)
        {
            if (hasExternalJson)
                return;

            string templateOutput = Path.Combine(ExternalPath, "SmeltingRecipes.json.template");
            if (File.Exists(templateOutput))
                return;

            try
            {
                string dir = Path.GetDirectoryName(templateOutput);
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                using Stream stream = Mod.GetFileStream(SmeltingTemplateAsset);
                using StreamReader reader = new StreamReader(stream);
                string content = reader.ReadToEnd();
                File.WriteAllText(templateOutput, content);
                Mod.Logger.Info($"[SmeltingLoader] Recipe template written to {templateOutput}");
            }
            catch (Exception e)
            {
                Mod.Logger.Warn($"[SmeltingLoader] Failed to output recipe template: {e.Message}");
            }
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
                            // 检查配方黑名单
                            if (_blacklist.DisabledRecipeIds?.Contains(recipeDto.Id) == true)
                                continue;
                            if (!string.IsNullOrEmpty(recipeDto.Label) && _blacklist.DisabledLabels?.Contains(recipeDto.Label) == true)
                                continue;

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
                                    Mod.Logger.Warn($"[SmeltingLoader] Fuel replacement item invalid: {fuelDto.Replacement.ReplaceWith}");
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
                                    Mod.Logger.Warn($"[SmeltingLoader] Unknown item ID: {itemId}, fuel skipped");
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
                            // 检查模板组黑名单
                            if (_blacklist.DisabledGroupIds?.Contains(group.Id) == true)
                            {
                                Mod.Logger.Info($"[SmeltingLoader] Skipping blacklisted template group: {group.Id}");
                                continue;
                            }

                            if (!materialDefs.TryGetValue(group.MaterialSource, out var materialDef))
                            {
                                Mod.Logger.Warn($"[SmeltingLoader] Template group {group.Id} referenced non-existent material source: {group.MaterialSource}");
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
                Mod.Logger.Warn($"[SmeltingLoader] Failed to parse JSON: {sourcePath}\n{e.Message}");
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
                {
                    // 检查生成后的配方 ID 是否在黑名单中
                    if (_blacklist.DisabledRecipeIds?.Contains(recipe.Value.Id) == true)
                        continue;
                    results.Add(recipe.Value);
                }
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
                    Mod.Logger.Warn($"[SmeltingLoader] Material missing property '{property}', placeholder '{placeholder}' will not be replaced");
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
                    Mod.Logger.Warn($"[SmeltingLoader] Recipe {dto.Id} has no valid ingredients, skipping");
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
                    Mod.Logger.Warn($"[SmeltingLoader] Recipe {dto.Id} has no valid outputs, skipping");
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
                Mod.Logger.Warn($"[SmeltingLoader] Failed to convert recipe: {dto.Id}\n{e.Message}");
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
                        Mod.Logger.Warn($"[SmeltingLoader] Recipe conflict for main material {mainMaterial} with furnace {tileId}! Keeping later-loaded recipe: {recipe.Id} overwrites {existing.Id}");
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

        /// <summary>
        /// 若外部黑名单文件尚不存在，将内嵌模板写出到同目录供用户参考。
        /// 用户可参考模板创建真正的黑名单 JSON（需删除注释）。
        /// </summary>
        private void EnsureBlacklistTemplate()
        {
            try
            {
                string templateOutput = BlacklistPath + ".template";
                if (File.Exists(BlacklistPath) || File.Exists(templateOutput))
                    return;

                string dir = Path.GetDirectoryName(BlacklistPath);
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                using Stream stream = Mod.GetFileStream(BlacklistTemplateAsset);
                using StreamReader reader = new StreamReader(stream);
                string content = reader.ReadToEnd();
                File.WriteAllText(templateOutput, content);
                Mod.Logger.Info($"[SmeltingLoader] Blacklist template written to {templateOutput}");
            }
            catch (Exception e)
            {
                Mod.Logger.Warn($"[SmeltingLoader] Failed to output blacklist template: {e.Message}");
            }
        }

        public override void Unload()
        {
            Database = null;
            CustomItemDataCache.UnloadFuelItem();
        }
    }
}