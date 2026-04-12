using Microsoft.Xna.Framework;
using Terraria.ID;

namespace TerraCraft.Content.Tiles.OreBlocks
{
    public class CrimtaneBlockTile : BaseBlockTile
    {
        public override void SetDefaults()
        {
            DustType = DustID.CrimtaneWeapons;
            MinPick = 55;
            AddMapEntry(new Color(106, 106, 101));
        }
    }
}
