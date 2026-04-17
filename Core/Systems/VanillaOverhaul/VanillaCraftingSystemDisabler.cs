using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using System;
using System.Reflection;
using System.Threading;
using TerraCraft.Core.Configs;
using TerraCraft.Core.UI.GridCrafting;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI;

namespace TerraCraft.Core.Systems.VanillaOverhaul
{
    public class VanillaCraftingSystemDisabler : ModSystem
    {
        public TCGameplayConfig config => ModContent.GetInstance<TCGameplayConfig>();

        public override void Load()
        {
            On_Main.DrawInventory += BanVanillaGuideCrafting;
        }
        public override void Unload()
        {
            On_Main.DrawInventory -= BanVanillaGuideCrafting;
        }
        public override void PreUpdateWorld()
        {
            if (config.UseOverhaulGameplay)
            {
                Main.InGuideCraftMenu = false;
            }
        }

        public override void PostUpdateEverything()
        {
            if (config.UseOverhaulGameplay)
            {
                var ui = ModContent.GetInstance<GridCraftingUIRegister>().GridCraftingUI;
                if (Main.playerInventory && ui.CurrentState == null)
                {
                    ui.SetState(new UIGridCraftingState(-1, 0));
                    SoundEngine.PlaySound(SoundID.MenuOpen);
                }
                Main.hidePlayerCraftingMenu = true;
                Main.InGuideCraftMenu = false;
            }
        }

        private void BanVanillaGuideCrafting(On_Main.orig_DrawInventory orig, Main self)
        {
            if (config.UseOverhaulGameplay && Main.InGuideCraftMenu)
            {
                Main.InGuideCraftMenu = false;
                ModContent.GetInstance<GridCraftingUIRegister>().OpenGridCraftingPreviewUI(isFromGuide: true);
            }
            orig.Invoke(self);
        }
    }
}