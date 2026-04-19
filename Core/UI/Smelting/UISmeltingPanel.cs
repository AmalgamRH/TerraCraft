using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using TerraCraft.Core.DataStructures.GridCrafting;
using TerraCraft.Core.Systems.Durability;
using TerraCraft.Core.Systems.GridCrafting;
using TerraCraft.Core.Systems.Smelting;
using TerraCraft.Core.Utils;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.GameInput;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI;

namespace TerraCraft.Core.UI.GridCrafting
{
    internal class UISmeltingPanel : UIPanel
    {
        public int ItemIcon { get; set; } = ItemID.None;
        public int TileId { get; set; }
        public Point16 TilePos { get; private set; }
        private Player Player => Main.LocalPlayer;

        private TEFurnace _furnace;

        private List<UICustomItemSlot> materialSlots = new List<UICustomItemSlot>();
        private UICustomItemSlot fuelSlot;
        private UICustomItemSlot outputSlot;

        private SmeltingInputHandler _inputHandler;

        public void InitializeGrid(Point16 tilePos, int tileId, int itemiconid, TEFurnace furnace)
        {
            _furnace = furnace;
            TilePos = tilePos;
            BackgroundColor = new Color(63, 82, 151) * 0.8f;
            TileId = tileId;
            ItemIcon = itemiconid;
            RecreateSlots();
            LoadFromFurnace();
            _inputHandler = new SmeltingInputHandler(
                materialSlots.Concat([fuelSlot]).ToList(),
                materialSlots,
                fuelSlot,
                outputSlot,
                _furnace);
        }

        private void LoadFromFurnace()
        {
            if (_furnace == null) return;

            for (int i = 0; i < materialSlots.Count && i < _furnace.material.Length; i++)
            {
                materialSlots[i].Item = _furnace.material[i];
            }
            fuelSlot.Item = _furnace.fuel;
            outputSlot.Item = _furnace.output;
        }
        private void RecreateSlots(int inputSlotNum = 1)
        {
            SetPadding(0);

            foreach (var slot in materialSlots)
                RemoveChild(slot);
            if (outputSlot != null)
                RemoveChild(outputSlot);
            if (fuelSlot != null)
                RemoveChild(fuelSlot);
            materialSlots.Clear();

            // ========== 常量 ==========
            const float texSize = 52f;
            const float inputScale = 0.85f;
            const float outputSlotScale = 1.2f;
            const float arrowScale = 1.5f;

            const float padding = 16f;
            const float slotGap = 8f;
            const float outputSpacing = 80f;   // leftAreaRight 到 outputSlotLeft
            const float iconSpacing = -10f;

            // ========== 尺寸 ==========
            float slotW = texSize * inputScale;        // 44.2
            float slotH = texSize * inputScale;        // 44.2
            float outputW = texSize * outputSlotScale;   // 62.4
            float outputH = texSize * outputSlotScale;   // 62.4

            var arrowTex = TextureAssets.GolfBallArrow;
            var arrowTexRect = new Rectangle(0, 0, arrowTex.Width() / 2 - 2, arrowTex.Height());
            float arrowW = arrowTexRect.Width * arrowScale;
            float arrowH = arrowTexRect.Height * arrowScale;

            // ========== 左侧布局 ==========
            // 行0: 输入槽
            // 行1: 空一个槽的距离（slotH + slotGap）
            // 行2: 燃料槽
            float inputSlotsTop = padding;
            float fuelSlotTop = padding + (slotH + slotGap) * 1.75f;   // 空一行

            float inputTotalW = inputSlotNum * slotW + (inputSlotNum - 1) * slotGap;
            float fuelSlotLeft = (inputTotalW - slotW) / 2f;

            float leftAreaTop = inputSlotsTop;
            float leftAreaBottom = fuelSlotTop + slotH;
            float leftAreaCenterY = (leftAreaTop + leftAreaBottom) / 2f;
            float leftAreaRight = Math.Max(inputTotalW, fuelSlotLeft + slotW);

            // ========== 输出槽 ==========
            float outputSlotLeft = leftAreaRight + outputSpacing;
            float outputSlotTop = leftAreaCenterY - outputH / 2f;

            // ========== 箭头（居中于间距区域，垂直居中） ==========
            float arrowLeft = leftAreaRight + (outputSpacing - arrowW) / 2f;
            float arrowTop = leftAreaCenterY - arrowH / 2f;

            // ========== 图标（对齐箭头正下方水平居中） ==========
            bool hasIcon = ItemIcon > ItemID.None && TextureAssets.Item[ItemIcon] != null;
            float iconLeft = 0f, iconTop = 0f, iconW = 0f, iconH = 0f;
            if (hasIcon)
            {
                var iconTex = TextureAssets.Item[ItemIcon];
                iconW = iconTex.Width();
                iconH = iconTex.Height();
                iconLeft = arrowLeft + (arrowW - iconW) / 2f;   // 对齐箭头中心
                iconTop = outputSlotTop + outputH + iconSpacing;
            }

            // ========== 包围盒 & 居中偏移 ==========
            float minLeft = Math.Min(0f, fuelSlotLeft);
            float maxRight = outputSlotLeft + outputW;
            if (hasIcon) maxRight = Math.Max(maxRight, iconLeft + iconW);

            float layoutW = maxRight - minLeft;
            float newPanelWidth = layoutW + 100f;
            float offsetX = (newPanelWidth - layoutW) / 2f - minLeft;

            // ========== 创建控件 ==========
            for (int i = 0; i < inputSlotNum; i++)
            {
                var slot = new UICustomItemSlot(ItemSlot.Context.BankItem, inputScale);
                slot.Left.Set(i * (slotW + slotGap) + offsetX, 0f);
                slot.Top.Set(inputSlotsTop, 0f);
                materialSlots.Add(slot);
                Append(slot);
            }

            fuelSlot = new UICustomItemSlot(ItemSlot.Context.BankItem, inputScale);
            fuelSlot.Left.Set(fuelSlotLeft + offsetX, 0f);
            fuelSlot.Top.Set(fuelSlotTop, 0f);
            Append(fuelSlot);

            outputSlot = new UICustomItemSlot(ItemSlot.Context.BankItem, outputSlotScale);
            outputSlot.Left.Set(outputSlotLeft + offsetX, 0f);
            outputSlot.Top.Set(outputSlotTop, 0f);
            Append(outputSlot);

            var arrow = new UIImageNeo(arrowTex)
            {
                NormalizedOrigin = new Vector2(0.5f),
                IgnoresMouseInteraction = true,
                Color = new Color(47, 56, 106) * 0.7f,
                Rotation = -MathHelper.PiOver2,
                Rectangle = arrowTexRect,
                ImageScale = arrowScale
            };

            arrow.Width.Set(arrowW, 0f);
            arrow.Height.Set(arrowH, 0f);
            arrow.Left.Set(arrowLeft + offsetX, 0f);
            arrow.Top.Set(arrowTop, 0f);
            Append(arrow);

            if (hasIcon)
            {
                var iconTex = TextureAssets.Item[ItemIcon];
                var icon = new UIImageNeo(iconTex)
                {
                    IgnoresMouseInteraction = true,
                    Color = Color.White * 0.8f
                };
                icon.Width.Set(iconW, 0f);
                icon.Height.Set(iconH, 0f);
                icon.Left.Set(iconLeft + offsetX, 0f);
                icon.Top.Set(iconTop, 0f);
                Append(icon);
            }

            // ========== 面板尺寸 ==========
            float contentBottom = leftAreaBottom;
            contentBottom = Math.Max(contentBottom, outputSlotTop + outputH);
            if (hasIcon) contentBottom = Math.Max(contentBottom, iconTop + iconH);
            this.SetSize(newPanelWidth, contentBottom + padding);
        }

        public override void Update(GameTime gameTime)
        {
            _inputHandler?.Update();
            base.Update(gameTime);
            _inputHandler?.SyncFromFurnace();
        }

        public override void OnDeactivate()
        {
            base.OnDeactivate();
        }
    }
}