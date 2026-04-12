using Microsoft.Xna.Framework;
using Terraria.ID;

namespace TerraCraft.Content.Tiles.OreBlocks
{
    public class MythrilBlockTile : BaseBlockTile
    {
        public override void SetDefaults()
        {
            DustType = DustID.Mythril;
            AddMapEntry(new Color(106, 106, 101));
        }
    }
}
