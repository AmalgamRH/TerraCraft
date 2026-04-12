using Microsoft.Xna.Framework;
using Terraria.ID;

namespace TerraCraft.Content.Tiles.OreBlocks
{
    public class IronBlockTile : BaseBlockTile
    {
        public override void SetDefaults()
        {
            DustType = DustID.Iron;
            MinPick = 40;
            AddMapEntry(new Color(106, 106, 101));
        }
    }
}
