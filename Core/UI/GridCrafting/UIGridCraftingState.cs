using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TerraCraft.Core.Utils;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.Map;
using Terraria.ModLoader;
using Terraria.UI;

namespace TerraCraft.Core.UI.GridCrafting
{
    internal class UIGridCraftingState : UIState
    {
        private UIGridCraftingPanel panel;
        public UIGridCraftingState(int tileId, int itemiconid)
        {
            TileId = tileId;
            ItemIconId = itemiconid;
        }
        public int TileId { get; private set; }
        public int ItemIconId { get; private set; }
        public override void OnInitialize()
        {
            initialTimer = 10;
            soundPlayed = false;
            panel = new UIGridCraftingPanel();
            panel.SetPos(150f, 270f);
            panel.InitializeGrid(TileId, ItemIconId);
            Append(panel);
        }

        private int initialTimer = 10;
        private bool soundPlayed = false;
        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
            if (initialTimer > 0) initialTimer--;
            if (!Main.playerInventory && initialTimer == 0)
            {
                ModContent.GetInstance<WorkstationUIRegister>().CloseWorkstationUI();
                if (!soundPlayed)
                {
                    SoundEngine.PlaySound(SoundID.MenuClose);
                    soundPlayed = true;
                }
            }
        }
        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            base.DrawSelf(spriteBatch);
            Main.hidePlayerCraftingMenu = true;
        }
    }
}