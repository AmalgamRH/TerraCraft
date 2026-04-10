using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.GameInput;
using Terraria.UI;

namespace TerraCraft.Core.UI
{
    /// <summary>
    /// 仅负责绘制物品槽、悬停提示和预览分配效果，所有交互由外部处理。
    /// </summary>
    internal class VanillaItemSlotWrapper : UIElement
    {
        internal Item Item;
        private readonly int _context;
        private readonly float _scale;

        // 预览分配相关（由外部设置）
        public bool IsPreviewSlot = false;
        public Item PreviewItem = null;      // 若不为 null，绘制预览物品而非实际 Item

        public VanillaItemSlotWrapper(int context = ItemSlot.Context.BankItem, float scale = 1f)
        {
            _context = context;
            _scale = scale;
            Item = new Item();
            Item.SetDefaults(0);
            Width.Set(TextureAssets.InventoryBack9.Value.Width * scale, 0f);
            Height.Set(TextureAssets.InventoryBack9.Value.Height * scale, 0f);
        }

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            base.DrawSelf(spriteBatch);
            float oldScale = Main.inventoryScale;
            Main.inventoryScale = _scale;
            Rectangle rect = GetInnerDimensions().ToRectangle();

            // 决定绘制哪个物品
            Item drawItem = PreviewItem ?? Item;

            // 绘制槽位背景和物品
            ItemSlot.Draw(spriteBatch, ref drawItem, _context, rect.TopLeft());

            // 处理悬停提示（始终基于实际物品，除非预览物品非空也可提示）
            if (rect.Contains(Main.MouseScreen.ToPoint()) && !PlayerInput.IgnoreMouseInterface)
            {
                Main.LocalPlayer.mouseInterface = true;
                Item hoverItem = PreviewItem ?? Item;
                ItemSlot.MouseHover(ref hoverItem, _context);
            }

            Main.inventoryScale = oldScale;
        }

        // 清除预览状态
        public void ClearPreview()
        {
            IsPreviewSlot = false;
            PreviewItem = null;
        }
    }
}