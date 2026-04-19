using System.Collections.Generic;

namespace TerraCraft.Core.DataStructures.Smelting
{
    public struct SmeltingRecipe
    {
        /// <summary>配方ID</summary>
        public string Id { get; set; }

        /// <summary>所需烧炼环境ID（留空表示通用）</summary>
        public List<int> RequiredTileIds { get; set; }

        /// <summary>原料列表（支持多原料）</summary>
        public List<SmeltingIngredient> Ingredients { get; set; }

        /// <summary>产物列表（支持多输出）</summary>
        public List<SmeltingOutput> Outputs { get; set; }

        /// <summary>替换规则（例如返还空桶）</summary>
        public List<SmeltingReplacement> Replacements { get; set; }

        /// <summary>标签，表示一类配方，比如“Ore”表示矿石配方</summary>
        public string Label {  get; set; }

        /// <summary>需要的烧制时间（ticks），实际受熔炉烧炼速率乘算影响</summary>
        public int BaseSmeltTime { get; set; } = 600;

        /// <summary>是否需要某种特定燃料才能冶炼，留空表示任意燃料</summary>
        public List<int> SpecificFuels { get; set; }

        /// <summary>所需最低燃料等级</summary>
        public int MinFuelLevel { get; set; } = 0;

        public SmeltingRecipe()  // 这里不要初始化RequiredTileIds和指定燃料，保持为null方便检测
        {
            Ingredients = new List<SmeltingIngredient>();
            Outputs = new List<SmeltingOutput>();
            Replacements = new List<SmeltingReplacement>();
            BaseSmeltTime = 600;
            MinFuelLevel = 0;
        }
    }
}