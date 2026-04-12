using Microsoft.Xna.Framework;
using Terraria.ID;

namespace TerraCraft.Content.Tiles.OreBlocks
{
    public class DemoniteBlockTile : BaseBlockTile
    {
        public override void SetDefaults()
        {
            DustType = DustID.Demonite;
            MinPick = 55;
            AddMapEntry(new Color(106, 106, 101));
        }
    }
}
