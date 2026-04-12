using Microsoft.Xna.Framework;
using Terraria.ID;

namespace TerraCraft.Content.Tiles.OreBlocks
{
    public class TitaniumBlockTile : BaseBlockTile
    {
        public override void SetDefaults()
        {
            DustType = DustID.Titanium;
            AddMapEntry(new Color(106, 106, 101));
        }
    }
}
