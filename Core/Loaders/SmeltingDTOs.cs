using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;

namespace TerraCraft.Core.Loaders
{
    #region 燃料 DTO
    internal class SmeltingFuelDTO
    {
        public List<string> ItemTypes { get; set; }
        public int Level { get; set; } = 0;
        public float Speed { get; set; } = 1f;
        public int BurnTime { get; set; }

        /// <summary>替换规则</summary>
        public SmeltingFuelReplacementDTO Replacement { get; set; }
    }
    internal class SmeltingFuelReplacementDTO
    {
        public string ReplaceWith { get; set; }
        public int ReplaceAmount { get; set; } = 1;
    }
    #endregion

    #region 基础 DTO
    internal class SmeltingIngredientDTO
    {
        public string ItemId { get; set; }
        public int Amount { get; set; } = 1;
    }

    internal class SmeltingOutputDTO
    {
        public string ItemId { get; set; }
        public int Amount { get; set; } = 1;
    }

    internal class SmeltingReplacementDTO
    {
        public string OriginalItemId { get; set; }
        public string ReplaceWith { get; set; }
        public int ReplaceAmount { get; set; } = 1;
    }
    #endregion

    #region 配方 DTO（支持简化写法）
    [JsonConverter(typeof(SmeltingRecipeConverter))]
    internal class SmeltingRecipeDTO
    {
        public string Id { get; set; }
        public List<string> RequiredTiles { get; set; }
        public string Label { get; set; }
        public List<SmeltingIngredientDTO> Ingredients { get; set; }
        public List<SmeltingOutputDTO> Outputs { get; set; }
        public List<SmeltingReplacementDTO> Replacements { get; set; }
        public int BaseSmeltTime { get; set; } = 600;
        public List<string> SpecificFuels { get; set; }
        public int MinFuelLevel { get; set; } = 0;
    }

    // 支持简化写法：Ingredients 可以是 string 数组或对象数组
    internal class SmeltingRecipeConverter : JsonConverter<SmeltingRecipeDTO>
    {
        public override bool CanWrite => false;

        public override SmeltingRecipeDTO ReadJson(JsonReader reader, Type objectType, SmeltingRecipeDTO existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            JObject obj = JObject.Load(reader);
            var dto = new SmeltingRecipeDTO();

            dto.Id = obj.Value<string>("Id");
            dto.RequiredTiles = obj["RequiredTiles"]?.ToObject<List<string>>();
            dto.Label = obj.Value<string>("Label");
            dto.BaseSmeltTime = obj.Value<int>("BaseSmeltTime");
            dto.SpecificFuels = obj["SpecificFuels"]?.ToObject<List<string>>();
            dto.MinFuelLevel = obj.Value<int>("MinFuelLevel");

            // 解析 Ingredients（支持字符串数组或对象数组）
            var ingredientsToken = obj["Ingredients"];
            if (ingredientsToken != null)
            {
                dto.Ingredients = new List<SmeltingIngredientDTO>();
                if (ingredientsToken.Type == JTokenType.Array)
                {
                    foreach (var item in ingredientsToken)
                    {
                        if (item.Type == JTokenType.String)
                        {
                            dto.Ingredients.Add(new SmeltingIngredientDTO { ItemId = item.Value<string>() });
                        }
                        else if (item.Type == JTokenType.Object)
                        {
                            dto.Ingredients.Add(item.ToObject<SmeltingIngredientDTO>());
                        }
                    }
                }
            }

            // 解析 Outputs（同样支持字符串或对象）
            var outputsToken = obj["Outputs"];
            if (outputsToken != null)
            {
                dto.Outputs = new List<SmeltingOutputDTO>();
                if (outputsToken.Type == JTokenType.Array)
                {
                    foreach (var item in outputsToken)
                    {
                        if (item.Type == JTokenType.String)
                        {
                            dto.Outputs.Add(new SmeltingOutputDTO { ItemId = item.Value<string>() });
                        }
                        else if (item.Type == JTokenType.Object)
                        {
                            dto.Outputs.Add(item.ToObject<SmeltingOutputDTO>());
                        }
                    }
                }
            }

            dto.Replacements = obj["Replacements"]?.ToObject<List<SmeltingReplacementDTO>>();
            return dto;
        }

        public override void WriteJson(JsonWriter writer, SmeltingRecipeDTO value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
    #endregion

    #region 模板与材料定义 DTO
    internal class SmeltingTemplateDTO
    {
        public string Id { get; set; }
        public List<string> RequiredTiles { get; set; }
        public string Label { get; set; }
        public List<SmeltingIngredientDTO> Ingredients { get; set; }
        public List<SmeltingOutputDTO> Outputs { get; set; }
        public List<SmeltingReplacementDTO> Replacements { get; set; }
        public int BaseSmeltTime { get; set; } = 600;
        public List<string> SpecificFuels { get; set; }
        public int MinFuelLevel { get; set; } = 0;
    }

    internal class SmeltingMaterialDefinitionDTO
    {
        public string Id { get; set; }
        public string Description { get; set; }
        public List<Dictionary<string, string>> Materials { get; set; }
    }

    internal class SmeltingTemplateGroupDTO
    {
        public string Id { get; set; }
        public string MaterialSource { get; set; }
        public SmeltingTemplateDTO Template { get; set; }
        public Dictionary<string, string> PlaceholderMappings { get; set; }
    }

    internal class SmeltingDatabaseDTO
    {
        public List<SmeltingRecipeDTO> Smelting { get; set; }
        public List<SmeltingFuelDTO> Fuels { get; set; }
        public List<SmeltingMaterialDefinitionDTO> MaterialDefinitions { get; set; }
        public List<SmeltingTemplateGroupDTO> SmeltingGroups { get; set; }
    }
    #endregion
}