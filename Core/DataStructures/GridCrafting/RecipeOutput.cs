namespace TerraCraft.Core.DataStructures.GridCrafting
{
    public struct RecipeOutput
    {
        public int ItemType { get; set; }
        public int Amount { get; set; } = 1;

        // 耐久度配置
        public bool UseDurability { get; set; } = false;
        public int? MaxDurability { get; set; }   // null 表示使用默认映射表
        public int? InitialDurability { get; set; } // null 表示等于 MaxDurability

        public RecipeOutput()
        {
            Amount = 1;
            UseDurability = false;
        }
    }
}