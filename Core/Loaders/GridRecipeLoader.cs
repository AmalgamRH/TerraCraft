using System.Collections.Generic;
using System.Linq;
using Terraria;
using TerraCraft.Core.Utils;
using Terraria.ModLoader;
using Newtonsoft.Json;
using System.IO;
using System;
using TerraCraft.Core.DataStructures.GridCrafting;
using Terraria.ID;
using TerraCraft.Core.VanillaExt;

namespace TerraCraft.Core.Loaders
{
    public class GridRecipeLoader : ModSystem
    {
        public const string AssetPath = "Assets/Recipes/";
        public static string FilePath = Path.Combine(Path.GetDirectoryName(ModLoader.ModPath), "TerraCraft", "Recipes");
        private static readonly string BlacklistPath = Path.Combine(Path.GetDirectoryName(ModLoader.ModPath), "TerraCraft", "CraftingBlacklist.json");
        private const string BlacklistTemplateAsset = "Assets/Templates/CraftingBlacklist.json.template";
        private const string RecipeTemplateAsset = "Assets/Templates/CraftingRecipes.json.template";
        public static RecipeDatabase RecipeDB { get; private set; }

        private RecipeBlacklistDTO _blacklist;

        // ��PostAddRecipes()���أ��ȴ�����ģ����Ʒidȫ���������
        public override void PostAddRecipes()
        {
            LoadGridRecipes();
        }
        public void LoadGridRecipes()
        {
            // 加载黑名单
            _blacklist = RecipeBlacklistDTO.LoadFrom(BlacklistPath);
            if (_blacklist.HasAny)
                Mod.Logger.Info($"[GridRecipeLoader] Loaded blacklist: {_blacklist.DisabledRecipeIds.Count} recipe IDs, {_blacklist.DisabledGroupIds.Count} group IDs");

            // 若黑名单文件尚不存在，将内嵌模板输出到文件系统供参考
            EnsureBlacklistTemplate();

            var allRecipes = new List<GriddedRecipe>();

            // 加载嵌入资源
            foreach (string assetPath in Mod.GetFileNames()
                         .Where(p => p.StartsWith(AssetPath) && p.EndsWith(".json")))
            {
                try
                {
                    using Stream stream = Mod.GetFileStream(assetPath);
                    using StreamReader reader = new StreamReader(stream);
                    string jsonContent = reader.ReadToEnd();
                    ProcessJsonContent(jsonContent, assetPath, allRecipes);
                }
                catch (Exception e)
                {
                    Mod.Logger.Warn($"[GridRecipeLoader] Failed to load embedded recipe: {assetPath}\n{e.Message}");
                }
            }

            if (!Directory.Exists(FilePath))
            {
                Directory.CreateDirectory(FilePath);
            }

            // 记录外部目录中是否存在任何 .json 文件（用于后续判断是否生成模板）
            bool hasExternalJson = false;

            // 加载外部目录中的 JSON 配方文件
            foreach (string filePath in Directory.GetFiles(FilePath, "*.json", SearchOption.AllDirectories))
            {
                hasExternalJson = true;
                try
                {
                    string json = File.ReadAllText(filePath);
                    ProcessJsonContent(json, filePath, allRecipes);
                }
                catch (Exception e)
                {
                    Mod.Logger.Warn($"[GridRecipeLoader] Failed to load external recipe: {filePath}\n{e.Message}");
                }
            }

            // 如果外部目录没有任何 .json 文件，则生成示例模板（避免用户空目录）
            EnsureRecipeTemplateIfEmpty(hasExternalJson);

            RecipeDB = new RecipeDatabase { Recipes = allRecipes };
            RecipeDB.InitializeCache();
            CustomItemDataCache.LoadGridMaterialItem(allRecipes);
            Mod.Logger.Info($"[GridRecipeLoader] Successfully loaded {allRecipes.Count} grid recipes");
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
                Mod.Logger.Info($"[GridRecipeLoader] Blacklist template written to {templateOutput}");
            }
            catch (Exception e)
            {
                Mod.Logger.Warn($"[GridRecipeLoader] Failed to output blacklist template: {e.Message}");
            }
        }
        /// <summary>
        /// 如果外部目录没有任何 .json 文件，则将内嵌配方模板写出到该目录供用户参考。
        /// </summary>
        private void EnsureRecipeTemplateIfEmpty(bool hasExternalJson)
        {
            if (hasExternalJson)
                return;

            string templateOutput = Path.Combine(FilePath, "CraftingRecipes.json.template");
            if (File.Exists(templateOutput))
                return;

            try
            {
                string dir = Path.GetDirectoryName(templateOutput);
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                using Stream stream = Mod.GetFileStream(RecipeTemplateAsset);
                using StreamReader reader = new StreamReader(stream);
                string content = reader.ReadToEnd();
                File.WriteAllText(templateOutput, content);
                Mod.Logger.Info($"[GridRecipeLoader] Recipe template written to {templateOutput}");
            }
            catch (Exception e)
            {
                Mod.Logger.Warn($"[GridRecipeLoader] Failed to output recipe template: {e.Message}");
            }
        }

        public override void Unload()
        {
            CustomItemDataCache.UnloadGridMaterialItem();
            RecipeDB = null;
        }
        // ����JSON���ݣ��Զ�����ʽ
        private void ProcessJsonContent(string jsonContent, string sourcePath, List<GriddedRecipe> allRecipes)
        {
            try // ���Խ���Ϊ�¸�ʽ
            {
                var dbDTO = JsonConvert.DeserializeObject<RecipeDatabaseDTO>(jsonContent);
                if (dbDTO != null)
                {
                    ProcessRecipeDatabase(dbDTO, allRecipes);
                    return;
                }
            }
            catch { } // �¸�ʽ����ʧ�ܣ����Ծɸ�ʽ

            try // ���Խ���Ϊ�ɸ�ʽ
            {
                var legacyDbDTO = JsonConvert.DeserializeObject<LegacyRecipeDatabaseDTO>(jsonContent);
                if (legacyDbDTO != null)
                {
                    ProcessLegacyRecipeDatabase(legacyDbDTO, allRecipes);
                    return;
                }
            }
            catch (Exception e)
            {
                Mod.Logger.Warn($"[GridRecipeLoader] Failed to parse JSON: {sourcePath}\n{e.Message}");
            }
        }

        private void ProcessRecipeDatabase(RecipeDatabaseDTO dbDTO, List<GriddedRecipe> allRecipes)
        {
            if (dbDTO == null) return;

            // 处理普通配方
            if (dbDTO.Recipes != null)
            {
                foreach (var recipeDTO in dbDTO.Recipes)
                {
                    // 检查配方黑名单
                    if (_blacklist.DisabledRecipeIds?.Contains(recipeDTO.Id) == true)
                        continue;

                    var converted = ConvertToStruct(recipeDTO);
                    if (converted.HasValue)
                        allRecipes.Add(converted.Value);
                }
            }

            // 处理模板配方
            if (dbDTO.MaterialDefinitions != null && dbDTO.RecipeGroups != null)
            {
                // 构建材料映射
                var materialDefs = dbDTO.MaterialDefinitions.ToDictionary(md => md.Id, md => md);
                
                foreach (var group in dbDTO.RecipeGroups)
                {
                    // 检查模板组黑名单
                    if (_blacklist.DisabledGroupIds?.Contains(group.Id) == true)
                    {
                        Mod.Logger.Info($"[GridRecipeLoader] Skipping blacklisted template group: {group.Id}");
                        continue;
                    }

                    if (string.IsNullOrEmpty(group.MaterialSource) || !materialDefs.ContainsKey(group.MaterialSource))
                    {
                        Mod.Logger.Warn($"[GridRecipeLoader] Recipe template group {group.Id} references a non-existent material source: {group.MaterialSource}");
                        continue;
                    }

                    var materialDef = materialDefs[group.MaterialSource];
                    var generated = GenerateRecipesFromMaterialGroup(group, materialDef);
                    allRecipes.AddRange(generated);
                }
            }
        }

        private void ProcessLegacyRecipeDatabase(LegacyRecipeDatabaseDTO dbDTO, List<GriddedRecipe> allRecipes)
        {
            if (dbDTO == null) return;

            // 处理普通配方
            if (dbDTO.Recipes != null)
            {
                foreach (var recipeDTO in dbDTO.Recipes)
                {
                    // 检查配方黑名单
                    if (_blacklist.DisabledRecipeIds?.Contains(recipeDTO.Id) == true)
                        continue;

                    var converted = ConvertToStruct(recipeDTO);
                    if (converted.HasValue)
                        allRecipes.Add(converted.Value);
                }
            }

            // 处理模板配方（旧格式）
            if (dbDTO.RecipeGroups != null)
            {
                foreach (var group in dbDTO.RecipeGroups)
                {
                    // 检查模板组黑名单
                    if (_blacklist.DisabledGroupIds?.Contains(group.Id) == true)
                    {
                        Mod.Logger.Info($"[GridRecipeLoader] Skipping blacklisted legacy template group: {group.Id}");
                        continue;
                    }

                    var generated = GenerateRecipesFromLegacyTemplate(group);
                    allRecipes.AddRange(generated);
                }
            }
        }

        #region �¸�ʽģ�������߼�
        private List<GriddedRecipe> GenerateRecipesFromMaterialGroup(TemplateGroupDTO group, MaterialDefinitionDTO materialDef)
        {
            var results = new List<GriddedRecipe>();
            if (group.Template == null || materialDef.Materials == null) return results;

            foreach (var material in materialDef.Materials)
            {
                var replacements = BuildReplacementsFromMaterial(material, group.PlaceholderMappings);
                var recipeDTO = CloneTemplateWithReplacements(group.Template, replacements);

                // ����Ƿ��пյĲ�����ƷID
                if (recipeDTO.Outputs != null && recipeDTO.Outputs.Any(o => string.IsNullOrWhiteSpace(o.ItemId)))
                {
                    // ������գ���Ĭ����
                    continue;
                }

                var converted = ConvertToStruct(recipeDTO);
                if (converted.HasValue)
                {
                    // 检查生成后的配方 ID 是否在黑名单中
                    if (_blacklist.DisabledRecipeIds?.Contains(converted.Value.Id) == true)
                        continue;
                    results.Add(converted.Value);
                }
            }
            return results;
        }

        private Dictionary<string, string> BuildReplacementsFromMaterial(Dictionary<string, string> material, Dictionary<string, string> placeholderMappings)
        {
            var replacements = new Dictionary<string, string>();
            
            if (placeholderMappings != null)
            {
                foreach (var mapping in placeholderMappings)
                {
                    string placeholder = mapping.Key;
                    string materialProperty = mapping.Value;
                    
                    if (material.ContainsKey(materialProperty))
                    {
                        replacements[placeholder] = material[materialProperty];
                    }
                    else
                    {
                        // ���Material���Բ����ڣ���¼����
                        Mod.Logger.Warn($"[GridRecipeLoader] Missing replacement material property: {materialProperty}");
                    }
                }
            }
            
            return replacements;
        }
        #endregion

        #region �ɸ�ʽģ�������߼��������ݣ�
        private List<GriddedRecipe> GenerateRecipesFromLegacyTemplate(LegacyTemplateGroupDTO group)
        {
            var results = new List<GriddedRecipe>();
            if (group.Template == null || group.Variants == null) return results;

            foreach (var variant in group.Variants)
            {
                var recipeDTO = CloneTemplateWithReplacements(group.Template, variant);

                // ����Ƿ��пյĲ�����ƷID
                if (recipeDTO.Outputs != null && recipeDTO.Outputs.Any(o => string.IsNullOrWhiteSpace(o.ItemId)))
                {
                    continue;
                }

                var converted = ConvertToStruct(recipeDTO);
                if (converted.HasValue)
                {
                    // 检查生成后的配方 ID 是否在黑名单中
                    if (_blacklist.DisabledRecipeIds?.Contains(converted.Value.Id) == true)
                        continue;
                    results.Add(converted.Value);
                }
            }
            return results;
        }
        #endregion

        #region 核心模板逻辑
        private GriddedRecipeDTO CloneTemplateWithReplacements(TemplateDTO template, Dictionary<string, string> replacements)
        {
            var dto = new GriddedRecipeDTO
            {
                Id = ReplacePlaceholders(template.Id, replacements),
                Shaped = template.Shaped,
                RequiredTiles = template.RequiredTiles == null ? null : new List<string>(),
                Ingredients = new List<IngredientDTO>(),
                Outputs = new List<OutputDTO>(),
                Replacements = new List<ReplacementDTO>()
            };

            // �滻ԭ��
            if (template.Ingredients != null)
            {
                foreach (var ing in template.Ingredients)
                {
                    dto.Ingredients.Add(new IngredientDTO
                    {
                        X = ing.X,
                        Y = ing.Y,
                        ItemId = ReplacePlaceholders(ing.ItemId, replacements),
                        RecipeGroup = ing.RecipeGroup,
                        Amount = ing.Amount
                    });
                }
            }

            // �滻����
            foreach (var outDTO in template.Outputs)
            {
                dto.Outputs.Add(new OutputDTO
                {
                    ItemId = ReplacePlaceholders(outDTO.ItemId, replacements),
                    Amount = outDTO.Amount,
                    UseDurability = outDTO.UseDurability,
                    MaxDurability = outDTO.MaxDurability,
                    InitialDurability = outDTO.InitialDurability
                });
            }

            // �滻�滻����
            foreach (var rep in template.Replacements)
            {
                dto.Replacements.Add(new ReplacementDTO
                {
                    X = rep.X,
                    Y = rep.Y,
                    OriginalItemId = ReplacePlaceholders(rep.OriginalItemId, replacements),
                    ReplaceWith = ReplacePlaceholders(rep.ReplaceWith, replacements),
                    ReplaceAmount = rep.ReplaceAmount
                });
            }

            // �滻���
            if (template.RequiredTiles != null)
            {
                dto.RequiredTiles = new List<string>();
                foreach (var tile in template.RequiredTiles)
                {
                    string replaced = ReplacePlaceholders(tile, replacements);
                    if (!string.IsNullOrWhiteSpace(replaced))
                        dto.RequiredTiles.Add(replaced);
                }
            }

            // �滻Condition
            if (template.Conditions != null)
            {
                dto.Conditions = new List<string>();
                foreach (var cond in template.Conditions)
                {
                    dto.Conditions.Add(ReplacePlaceholders(cond, replacements));
                }
            }

            // ����Pattern
            if (template.Pattern != null && template.Pattern.Any())
            {
                dto.Pattern = new List<List<PatternCellDTO>>();
                foreach (var row in template.Pattern)
                {
                    var newRow = new List<PatternCellDTO>();
                    foreach (var cell in row)
                    {
                        if (cell == null)
                        {
                            newRow.Add(null);
                            continue;
                        }

                        // �Ƚ���ռλ���滻
                        var newCell = new PatternCellDTO
                        {
                            ItemId = ReplacePlaceholders(cell.ItemId, replacements),
                            RecipeGroup = ReplacePlaceholders(cell.RecipeGroup, replacements),
                            Amount = cell.Amount
                        };

                        // ���ܽ��������ǰ�����RecipeGroupǰ׺�����Ϊ�䷽��
                        if (string.IsNullOrEmpty(newCell.RecipeGroup) && !string.IsNullOrEmpty(newCell.ItemId))
                        {
                            string raw = newCell.ItemId;
                            if (raw.StartsWith("RecipeGroup:", StringComparison.OrdinalIgnoreCase))
                            {
                                newCell.RecipeGroup = raw.Substring("RecipeGroup:".Length);
                                newCell.ItemId = null;
                            }
                            else
                            {
                                //���򣬳��Խ���Ϊ��ƷID
                                int id = ItemIDResolver.ParseItemType(raw);
                                if (id == 0)
                                {
                                    // ����ʧ�ܣ�����RecipeGroup
                                    newCell.RecipeGroup = raw;
                                    newCell.ItemId = null;
                                }
                                // �����ɹ�������ItemId
                            }
                        }
                        newRow.Add(newCell);
                    }
                    dto.Pattern.Add(newRow);
                }
            }

            return dto;
        }

        private string ReplacePlaceholders(string input, Dictionary<string, string> replacements)
        {
            if (string.IsNullOrEmpty(input)) return input;
            foreach (var kv in replacements)
            {
                input = input.Replace("{" + kv.Key + "}", kv.Value);
            }
            return input;
        }
        #endregion

        #region DTOת���߼�
        private GriddedRecipe? ConvertToStruct(GriddedRecipeDTO dto)
        {
            try
            {
                // ת��RequiredTiles������Ϊ��
                List<int> tileIds = null;
                if (dto.RequiredTiles != null)
                {
                    tileIds = new List<int>();
                    foreach (string tileStr in dto.RequiredTiles)
                    {
                        if (string.IsNullOrWhiteSpace(tileStr))
                            continue; // ���Կ��ַ���
                        int id = TileIDResolver.ParseTileType(tileStr);
                        if (id != 0)
                            tileIds.Add(id);
                        }
                }

                // �����������Ϊ�գ�����Ϊͨ�ã���ֵΪ null��
                if (tileIds.Count == 0)
                    tileIds = null;

                // ת��Ingredients����Pattern��Ingredients��
                List<RecipeIngredient> ingredients = new List<RecipeIngredient>();
                int gridWidth = 1;   // Ĭ�ϳߴ磬����Shaped = true����Patternʱ���ܱ�AutoComputeDimensions����
                int gridHeight = 1;

                // ����ʹ��Pattern��������Shaped�䷽��
                if (dto.Shaped && dto.Pattern != null && dto.Pattern.Count > 0)
                {
                    ingredients = ParsePattern(dto.Pattern, out gridWidth, out gridHeight);
                }
                else if (dto.Ingredients != null)
                {
                    // ���ݾɵ�����ʽ��Shaped���������䷽��Shaped = false��
                    foreach (var ingDTO in dto.Ingredients)
                    {
                        int itemType = 0;
                        if (!string.IsNullOrEmpty(ingDTO.ItemId))
                            itemType = ItemIDResolver.ParseItemType(ingDTO.ItemId);

                        ingredients.Add(new RecipeIngredient
                        {
                            X = ingDTO.X,
                            Y = ingDTO.Y,
                            ItemType = itemType,
                            RecipeGroup = ingDTO.RecipeGroup,
                            Amount = ingDTO.Amount
                        });
                    }
                }

                // ת��Outputs
                List<RecipeOutput> outputs = new List<RecipeOutput>();
                if (dto.Outputs != null)
                {
                    foreach (var outDTO in dto.Outputs)
                    {
                        int itemType = ItemIDResolver.ParseItemType(outDTO.ItemId);
                        outputs.Add(new RecipeOutput
                        {
                            ItemType = itemType,
                            Amount = outDTO.Amount,
                            UseDurability = outDTO.UseDurability,
                            MaxDurability = outDTO.MaxDurability,
                            InitialDurability = outDTO.InitialDurability
                        });
                    }
                }

                // ת��Replacements
                List<RecipeReplacement> replacements = new List<RecipeReplacement>();
                if (dto.Replacements != null)
                {
                    foreach (var repDTO in dto.Replacements)
                    {
                        int originalType = 0;
                        if (!string.IsNullOrEmpty(repDTO.OriginalItemId))
                            originalType = ItemIDResolver.ParseItemType(repDTO.OriginalItemId);

                        int? replaceWithType = null;
                        if (!string.IsNullOrEmpty(repDTO.ReplaceWith))
                            replaceWithType = ItemIDResolver.ParseItemType(repDTO.ReplaceWith);

                        replacements.Add(new RecipeReplacement
                        {
                            X = repDTO.X,
                            Y = repDTO.Y,
                            OriginalItemType = originalType,
                            ReplaceWithType = replaceWithType,
                            ReplaceAmount = repDTO.ReplaceAmount
                        });
                    }
                }

                // ���������״�䷽��δʹ��Pattern����Ingredients�������꣬�Զ�����ߴ�
                if (dto.Shaped && (dto.Pattern == null || dto.Pattern.Count == 0))
                {
                    var tempRecipe = new GriddedRecipe
                    {
                        Id = dto.Id,
                        GridWidth = 0,
                        GridHeight = 0,
                        Shaped = true,
                        Ingredients = ingredients,
                    };
                    AutoComputeDimensions(ref tempRecipe);
                    gridWidth = tempRecipe.GridWidth;
                    gridHeight = tempRecipe.GridHeight;
                }

                List<string> conditionStrings = null;
                if (dto.Conditions != null && dto.Conditions.Count > 0)
                {
                    conditionStrings = new List<string>(dto.Conditions);
                }

                var recipe = new GriddedRecipe
                {
                    Id = dto.Id,
                    GridWidth = gridWidth,
                    GridHeight = gridHeight,
                    Shaped = dto.Shaped,
                    RequiredTileIds = tileIds,
                    Ingredients = ingredients,
                    Outputs = outputs,
                    Replacements = replacements,
                    Conditions = conditionStrings
                };

                string tileInfo = tileIds == null ? "None" : string.Join(", ", tileIds.Select(id => $"{id}"));
                string ingredientsInfo = ingredients.Count == 0 ? "None" : string.Join(", ", ingredients.Select(ing => {
                    string itemInfo = ing.ItemType != 0 ? $"ItemID:{ing.ItemType}" : ing.RecipeGroup;
                    return $"({ing.X},{ing.Y}):{itemInfo}��{ing.Amount}";
                }));
                string outputsInfo = outputs.Count == 0 ? "None" : string.Join(", ", outputs.Select(output => $"ItemID:{output.ItemType}��{output.Amount}"));
                // Mod.Logger.Debug($"[Recipe] ID: {dto.Id} | Type: {(dto.Shaped ? "Shaped" : "Shapeless")} | Size: {gridWidth}x{gridHeight} | Tiles: {tileInfo} | Ingredients: {ingredientsInfo} | Outputs: {outputsInfo}");
                return recipe;
            }
            catch (Exception e)
            {
                Mod.Logger.Warn($"[GridRecipeLoader] Failed to convert recipe to struct: {dto.Id}\n{e.Message}");
                return null;
            }
        }

        private List<RecipeIngredient> ParsePattern(List<List<PatternCellDTO>> pattern, out int width, out int height)
        {
            var ingredients = new List<RecipeIngredient>();
            height = pattern.Count;
            width = 0;

            // ȷ�������г���һ�£�ȡ�����ȣ�
            foreach (var row in pattern)
            {
                if (row.Count > width) width = row.Count;
            }

            for (int y = 0; y < height; y++)
            {
                var row = pattern[y];
                for (int x = 0; x < width; x++)
                {
                    if (x < row.Count && row[x] != null)
                    {
                        var cell = row[x];
                        int itemType = 0;
                        if (!string.IsNullOrEmpty(cell.ItemId))
                            itemType = ItemIDResolver.ParseItemType(cell.ItemId);

                        ingredients.Add(new RecipeIngredient
                        {
                            X = x,
                            Y = y,
                            ItemType = itemType,
                            RecipeGroup = cell.RecipeGroup,
                            Amount = cell.Amount ?? 1
                        });
                    }
                }
            }

            return ingredients;
        }

        private void AutoComputeDimensions(ref GriddedRecipe recipe)
        {
            if (!recipe.Shaped) return;
            if (recipe.Ingredients == null || recipe.Ingredients.Count == 0)
            {
                recipe.GridWidth = 0;
                recipe.GridHeight = 0;
                return;
            }

            int maxX = -1, maxY = -1;
            foreach (var ing in recipe.Ingredients)
            {
                if (ing.X.HasValue && ing.X.Value > maxX) maxX = ing.X.Value;
                if (ing.Y.HasValue && ing.Y.Value > maxY) maxY = ing.Y.Value;
            }

            if (maxX == -1 || maxY == -1)
            {
                recipe.GridWidth = 0;
                recipe.GridHeight = 0;
                return;
            }

            recipe.GridWidth = maxX + 1;
            recipe.GridHeight = maxY + 1;
        }
        #endregion
    }
}
