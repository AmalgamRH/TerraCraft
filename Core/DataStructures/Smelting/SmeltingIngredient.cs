namespace TerraCraft.Core.DataStructures.Smelting
{
    /// <summary>烧炼原料</summary>
    public struct SmeltingIngredient
    {
        /// <summary>原料物品ID</summary>
        public int ItemType { get; set; }

        /// <summary>开始烧炼所需最小数量</summary>
        public int Amount { get; set; } = 1;

        public SmeltingIngredient()
        {
            Amount = 1;
        }
    }
}