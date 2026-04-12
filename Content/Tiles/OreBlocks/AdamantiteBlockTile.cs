using Microsoft.Xna.Framework;
using Terraria.ID;

namespace TerraCraft.Content.Tiles.OreBlocks
{
    public class AdamantiteBlockTile : BaseBlockTile
    {
        public override void SetDefaults()
        {
            DustType = DustID.Adamantite;
            AddMapEntry(new Color(106, 106, 101));
        }
    }
}
