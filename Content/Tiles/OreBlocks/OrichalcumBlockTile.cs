using Microsoft.Xna.Framework;
using Terraria.ID;

namespace TerraCraft.Content.Tiles.OreBlocks
{
    public class OrichalcumBlockTile : BaseBlockTile
    {
        public override void SetDefaults()
        {
            DustType = DustID.Orichalcum;
            AddMapEntry(new Color(106, 106, 101));
        }
    }
}
