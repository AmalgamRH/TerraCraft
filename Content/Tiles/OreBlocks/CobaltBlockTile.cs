using Microsoft.Xna.Framework;
using Terraria.ID;

namespace TerraCraft.Content.Tiles.OreBlocks
{
    public class CobaltBlockTile : BaseBlockTile
    {
        public override void SetDefaults()
        {
            DustType = DustID.Cobalt;
            AddMapEntry(new Color(106, 106, 101));
        }
    }
}
