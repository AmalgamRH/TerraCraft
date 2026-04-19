namespace TerraCraft.Core.DataStructures.Smelting
{
    /// <summary>ÉÕÁ¶Êä³ö</summary>
    public struct SmeltingOutput
    {
        public int ItemType { get; set; }
        public int Amount { get; set; } = 1;
        public SmeltingOutput()
        {
            Amount = 1;
        }
    }
}