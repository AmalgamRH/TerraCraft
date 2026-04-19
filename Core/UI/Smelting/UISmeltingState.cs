using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TerraCraft.Core.Systems.Smelting;
using TerraCraft.Core.UI.GridCrafting;
using TerraCraft.Core.Utils;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI;

namespace TerraCraft.Core.UI.Smelting
{
    internal class UISmeltingState : UIState
    {
        private UISmeltingPanel panel;
        public Point16 TilePos { get; private set; }
        public int TileId { get; private set; }
        public int ItemIconId { get; private set; }

        public UISmeltingState(int i, int j, int tileId, int itemiconid)
        {
            TilePos = new Point16(i, j);
            TileId = tileId;
            ItemIconId = itemiconid;
        }
        public UISmeltingState(Vector2 pos, int tileId, int itemiconid)
            : this((int)pos.X, (int)pos.Y, tileId, itemiconid) { }
        public UISmeltingState(Point16 pos, int tileId, int itemiconid)
            : this(pos.X, pos.Y, tileId, itemiconid) { }

        public override void OnInitialize()
        {
            initialTimer = 10;
            soundPlayed = false;

            if (!SmeltingHelper.TryGetTEFurnace(TilePos.X, TilePos.Y, out TEFurnace furnace))
            {
                TerraCraft.Instance.Logger.Error("[UISmeltingState] 未能获取有效TE，UI关闭");
                ModContent.GetInstance<WorkstationUIRegister>().CloseWorkstationUI();
                return;
            }

            panel = new UISmeltingPanel();
            panel.SetPos(150f, 270f);
            panel.InitializeGrid(TilePos, TileId, ItemIconId, furnace);
            Append(panel);
        }

        private int initialTimer = 10;
        private bool soundPlayed = false;

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            if (!SmeltingHelper.HasTEFurnace(TilePos.X, TilePos.Y))
            {
                ModContent.GetInstance<WorkstationUIRegister>().CloseWorkstationUI();
                Main.playerInventory = false;
                return;
            }

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