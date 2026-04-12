using Microsoft.Xna.Framework;
using Terraria.ID;

namespace TerraCraft.Content.Tiles.OreBlocks
{
    public class CopperBlockTile : BaseBlockTile
    {
        public override void SetDefaults()
        {
            DustType = DustID.Copper;
            MinPick = 35;
            AddMapEntry(new Color(106, 106, 101));
        }
    }
}
