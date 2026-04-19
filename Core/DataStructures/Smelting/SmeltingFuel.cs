using System.Collections.Generic;

namespace TerraCraft.Core.DataStructures.Smelting
{
    /// <summary>燃料</summary>
    public struct SmeltingFuel
    {
        /// <summary>燃料物品ID</summary>
        public int ItemType { get; set; }

        /// <summary>燃烧时间</summary>
        public int BurnTime { get; set; }
        public int Level { get; set; } = 0;
        public float Speed { get; set; } = 1f;

        /// <summary>替换规则：燃烧后替换成的物品类型（null 表示不替换）</summary>
        public int? ReplaceWithType { get; set; }

        /// <summary>替换后的数量</summary>
        public int ReplaceAmount { get; set; } = 1;
        public SmeltingFuel() 
        {
            Level = 0;
            Speed = 1f;
            ReplaceAmount = 1;
        }
    }
}