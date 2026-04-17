using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.GameInput;
using Terraria.UI;

namespace TerraCraft.Core.UI.GridCrafting.Preview
{
    /// <summary>
    /// 纯展示物品槽，不响应任何鼠标交互，不设置 mouseInterface，仅用于预览显示。
    /// </summary>
    internal class UICraftPreviewSlot : UIElement
    {
        private Item _item;
        private readonly float _scale;
        public string NameOverride;
        public int Context = ItemSlot.Context.GuideItem;
        public Item Item
        {
            get => _item;
            set
            {
                _item = value?.Clone() ?? new Item();
                if (_item.IsAir) _item.SetDefaults(0);
            }
        }

        public UICraftPreviewSlot(float scale = 0.85f)
        {
            _scale = scale;
            _item = new Item();
            _item.SetDefaults(0);

            Width.Set(TextureAssets.InventoryBack4.Value.Width * scale, 0f);
            Height.Set(TextureAssets.InventoryBack4.Value.Height * scale, 0f);
        }

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            float oldScale = Main.inventoryScale;
            Main.inventoryScale = _scale;

            Rectangle rect = GetInnerDimensions().ToRectangle();

            // 绘制背景
            if (Context == ItemSlot.Context.GuideItem)
                spriteBatch.Draw(TextureAssets.InventoryBack4.Value, rect, new Color(200, 200, 200) * 0.3f);

            // 绘制物品（包含数量、耐久等）
            if (!_item.IsAir)
            {
                ItemSlot.Draw(spriteBatch, ref _item, Context, rect.TopLeft());

                if (rect.Contains(Main.MouseScreen.ToPoint()) && !PlayerInput.IgnoreMouseInterface)
                {
                    Main.LocalPlayer.mouseInterface = true;
                    Item hoverItem = _item;
                    if (NameOverride != null)
                    {
                        hoverItem.SetNameOverride(NameOverride);
                    }
                    ItemSlot.MouseHover(ref hoverItem, ItemSlot.Context.InventoryItem);
                }
            }
            
            Main.inventoryScale = oldScale;
        }
    }
}