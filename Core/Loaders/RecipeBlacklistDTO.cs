using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace TerraCraft.Core.Loaders
{
    /// <summary>
    /// 配方黑名单 DTO，支持禁用指定配方 ID、模板组 ID 和标签。
    /// 从外部 JSON 文件加载，用于合成/熔炼加载器跳过特定配方。
    /// </summary>
    internal class RecipeBlacklistDTO
    {
        /// <summary>禁用指定配方 ID</summary>
        [JsonProperty("DisabledRecipeIds")]
        public List<string> DisabledRecipeIds { get; set; } = new();

        /// <summary>禁用指定模板组 ID（该组生成的所有配方都会被跳过）</summary>
        [JsonProperty("DisabledTemplateIds")]
        public List<string> DisabledGroupIds { get; set; } = new();

        /// <summary>禁用指定标签（smelting 用 Label 字段过滤）</summary>
        [JsonProperty("DisabledLabels")]
        public List<string> DisabledLabels { get; set; } = new();

        /// <summary>是否有任何禁用项</summary>
        public bool HasAny =>
            (DisabledRecipeIds != null && DisabledRecipeIds.Count > 0) ||
            (DisabledGroupIds != null && DisabledGroupIds.Count > 0) ||
            (DisabledLabels != null && DisabledLabels.Count > 0);

        /// <summary>从 JSON 文件加载黑名单，文件不存在则返回空黑名单</summary>
        public static RecipeBlacklistDTO LoadFrom(string filePath)
        {
            try
            {
                if (File.Exists(filePath))
                {
                    string json = File.ReadAllText(filePath);
                    var result = JsonConvert.DeserializeObject<RecipeBlacklistDTO>(json);
                    return result ?? new RecipeBlacklistDTO();
                }
            }
            catch (Exception ex)
            {
                // 调用方负责处理日志
                System.Diagnostics.Debug.WriteLine($"[RecipeBlacklistDTO] Failed to load blacklist: {filePath}\n{ex.Message}");
            }
            return new RecipeBlacklistDTO();
        }
    }
}
