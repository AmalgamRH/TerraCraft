using Iced.Intel;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using TerraCraft.Core.UI.GridCrafting.Preview;
using TerraCraft.Core.UI.Smelting;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI;

namespace TerraCraft.Core.UI.GridCrafting
{
    public class WorkstationUIRegister : ModSystem
    {
        internal UserInterface WorkstationUI;

        /// <summary>打开合成界面</summary>
        public void OpenGridCraftingUI(int tileId, int itemiconid = 0)
        {
            if (WorkstationUI == null) return;
            if (WorkstationUI.CurrentState != null)
            {
                CloseWorkstationUI();
            }
            WorkstationUI.SetState(new UIGridCraftingState(tileId, itemiconid));
            SoundEngine.PlaySound(SoundID.MenuOpen);
        }
        /// <summary>打开合成预览界面</summary>
        /// <param name="isFromGuide">是否从向导处开启</param>
        public void OpenGridCraftingPreviewUI(bool isFromGuide = false)
        {
            if (WorkstationUI == null) return;
            WorkstationUI.SetState(null);
            if (WorkstationUI.CurrentState == null)
            {
                WorkstationUI.SetState(new UICraftPreviewerState(isFromGuide));
                SoundEngine.PlaySound(SoundID.MenuOpen);
            }
        }
        /// <summary>打开熔炉界面</summary>
        public void OpenSmeltingUI(int i, int j, int tileId, int itemiconid = 0)
        {
            if (WorkstationUI == null) return;
            if (WorkstationUI.CurrentState != null)
            {
                CloseWorkstationUI();
            }
            WorkstationUI.SetState(new UISmeltingState(i, j, tileId, itemiconid));
            SoundEngine.PlaySound(SoundID.MenuOpen);
        }

        /// <summary>关闭全部制作站UI</summary>
        public void CloseWorkstationUI()
        {
            if (WorkstationUI == null) return;
            WorkstationUI.SetState(null);
        }

        public override void Load()
        {
            WorkstationUI = new UserInterface();
        }
        public override void Unload()
        {
            WorkstationUI = null;
        }
        public override void UpdateUI(GameTime gameTime)
        {
            WorkstationUI?.Update(gameTime);
        }

        public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
        {
            int inventoryIndex = layers.FindIndex((layer) => layer.Name.Equals("Vanilla: Inventory"));
            if (inventoryIndex != -1)
            {
                layers.Insert(inventoryIndex, new LegacyGameInterfaceLayer("TerraCraft: Workstation UI", delegate ()
                {
                    WorkstationUI.Draw(Main.spriteBatch, new GameTime());
                    return true;
                },
                InterfaceScaleType.UI));
            }
        }
    }
}
