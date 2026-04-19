namespace TerraCraft.Core.DataStructures.Smelting
{
    /// <summary>烧炼替换规则，用于处理岩浆桶->空桶这种</summary>
    public struct SmeltingReplacement
    {
        /// <summary>原始物品类型</summary>
        public int OriginalItemType { get; set; }

        /// <summary>替换成的物品类型，null表示直接移除(不替换)</summary>
        public int? ReplaceWithType { get; set; }

        /// <summary>替换后的数量</summary>
        public int ReplaceAmount { get; set; } = 1;

        public SmeltingReplacement()
        {
            ReplaceAmount = 1;
        }
    }
}