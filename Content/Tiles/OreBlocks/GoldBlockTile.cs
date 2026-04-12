using Microsoft.Xna.Framework;
using Terraria.ID;

namespace TerraCraft.Content.Tiles.OreBlocks
{
    public class GoldBlockTile : BaseBlockTile
    {
        public override void SetDefaults()
        {
            DustType = DustID.Gold;
            MinPick = 50;
            AddMapEntry(new Color(106, 106, 101));
        }
    }
}
