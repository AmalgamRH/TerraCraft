using Microsoft.Xna.Framework;
using Terraria.ID;

namespace TerraCraft.Content.Tiles.OreBlocks
{
    public class PalladiumBlockTile : BaseBlockTile
    {
        public override void SetDefaults()
        {
            DustType = DustID.Palladium;
            AddMapEntry(new Color(106, 106, 101));
        }
    }
}
