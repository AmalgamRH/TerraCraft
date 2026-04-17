using System;
using System.Collections.Generic;
using TerraCraft.Content.Items.VanillaExt;
using Terraria;
using Terraria.ID;
using Terraria.IO;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace TerraCraft.Core.VanillaExt
{
    public class StartingItemsPlayer : ModPlayer
    {
        private bool hasInitialized = false;
        public override void OnEnterWorld()
        {
            if (hasInitialized)
                return;

            PlayerFileData data = Main.ActivePlayerFileData;
            TimeSpan playTime = data.GetPlayTime();

            if (playTime.TotalSeconds < 1)
            {
                for (int i = 0; i < 3; i++)
                {
                    Player.inventory[i].TurnToAir();
                }
                int pickType = Main.remixWorld ? ModContent.ItemType<AshWoodPickaxe>() : ModContent.ItemType<WoodPickaxe>();
                int axeType = Main.remixWorld ? ModContent.ItemType<AshWoodAxe>() : ModContent.ItemType<WoodAxe>();
                Player.inventory[0].SetDefaults(pickType);
                Player.inventory[0].Prefix(-1);
                Player.inventory[1].SetDefaults(axeType);
                Player.inventory[1].Prefix(-1);
            }

            hasInitialized = true;
        }
    }
}