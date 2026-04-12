using System;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace TerraCraft.Core.VanillaExt
{
    public class NormalOreMinPick : GlobalTile
    {
        /// <summary>
        /// 物块id -> 最低采集镐力
        /// </summary>
        public static Dictionary<int, int> OreMinPick = new Dictionary<int, int>()
        {
            { TileID.Copper, 25 },
            { TileID.Tin, 25 },
            { TileID.Amethyst, 25 },
            { TileID.Iron, 35 },
            { TileID.Lead, 35 },
            { TileID.Silver, 35 },
            { TileID.Tungsten, 35 },
            { TileID.Topaz, 35 },
            { TileID.Sapphire, 40 },
            { TileID.Emerald, 40 },
            { TileID.Gold, 45 },
            { TileID.Platinum, 45 },
            { TileID.Ruby, 45 },
            { TileID.AmberStoneBlock, 45 },
            { TileID.Diamond, 45 },
            { TileID.Obsidian, 50 },
        };
        public override bool CanExplode(int i, int j, int type)
        {
            if (OreMinPick.TryGetValue(type, out int requiredPickPower)) 
            { 
                if (Main.LocalPlayer.GetModPlayer<MaxPickPowerPlayer>().MaxPickPowerEver < requiredPickPower)
                {
                    return false;
                }
            }
            return base.CanExplode(i, j, type);
        }
        public override void KillTile(int i, int j, int type, ref bool fail, ref bool effectOnly, ref bool noItem)
        {
            // 检查当前物块是否需要特定镐力
            if (OreMinPick.TryGetValue(type, out int requiredPickPower))
            {
                Player player = Main.LocalPlayer;
                int currentPickPower = player.HeldItem.pick; // 获取当前镐子的镐力
                if (currentPickPower > 0)
                {
                    int diff = requiredPickPower - currentPickPower;

                    int failChance = Math.Min(80, 20 + diff * 6); // 差距1->26%，2->32%，... 10->80%
                    if (Main.rand.Next(100) < failChance)
                    {
                        fail = true;
                        return;
                    }

                    if (currentPickPower < requiredPickPower)
                    {
                        noItem = true;
                        return;
                    }
                }
            }
            base.KillTile(i, j, type, ref fail, ref effectOnly, ref noItem);
        }
    }
}