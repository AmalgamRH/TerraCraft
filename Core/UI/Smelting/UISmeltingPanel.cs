using Microsoft.Xna.Framework;
using ReLogic.Content;
using System;
using System.Collections.Generic;
using System.Linq;
using TerraCraft.Core.Systems.Smelting;
using TerraCraft.Core.Utils;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
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

        private UISmeltingProgressBar _progressBar;  // 箭头，左→右，熔炼进度
        private UISmeltingProgressBar _fireBar;       // 火焰，下→上，燃料剩余

        private SmeltingInputHandler _inputHandler;

        private const string ArrowTexPath = "TerraCraft/Assets/UI/Smelting/SmeltingArrow";
        private const string FireTexPath = "TerraCraft/Assets/UI/Smelting/SmeltingFire";
        private const string Fire2TexPath = "TerraCraft/Assets/UI/Smelting/SmeltingFire2";

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
                materialSlots[i].Item = _furnace.material[i];
            fuelSlot.Item = _furnace.fuel;
            outputSlot.Item = _furnace.output;
        }

        private void RecreateSlots(int inputSlotNum = 1)
        {
            SetPadding(0);

            foreach (var slot in materialSlots) RemoveChild(slot);
            if (outputSlot != null) RemoveChild(outputSlot);
            if (fuelSlot != null) RemoveChild(fuelSlot);
            if (_progressBar != null) RemoveChild(_progressBar);
            if (_fireBar != null) RemoveChild(_fireBar);
            materialSlots.Clear();

            const float texSize = 52f;
            const float inputScale = 0.85f;
            const float outputScale = 1.2f;
            const float arrowScale = 1f;
            const float padding = 16f;
            const float slotGap = 8f;
            const float outputSpacing = 80f;
            const float iconSpacing = -10f;

            float slotW = texSize * inputScale;
            float slotH = texSize * inputScale;
            float outputW = texSize * outputScale;
            float outputH = texSize * outputScale;

            var arrowTex = TerraCraft.GetAsset2D(ArrowTexPath, AssetRequestMode.ImmediateLoad);
            float arrowW = arrowTex.Width() * arrowScale;
            float arrowH = arrowTex.Height() * arrowScale;

            var fireTex = TerraCraft.GetAsset2D(FireTexPath, AssetRequestMode.ImmediateLoad);
            float fireW = fireTex.Width() * arrowScale;
            float fireH = fireTex.Height() * arrowScale;

            // 垂直布局
            float inputSlotsTop = padding;
            float fuelSlotTop = padding + (slotH + slotGap) * 1.75f;
            float inputTotalW = inputSlotNum * slotW + (inputSlotNum - 1) * slotGap;
            float fuelSlotLeft = (inputTotalW - slotW) / 2f;

            float leftAreaTop = inputSlotsTop;
            float leftAreaBottom = fuelSlotTop + slotH;
            float leftAreaCenterY = (leftAreaTop + leftAreaBottom) / 2f;
            float leftAreaRight = Math.Max(inputTotalW, fuelSlotLeft + slotW);

            // 火焰条：水平居中于左侧区域，垂直居中于材料槽底~燃料槽顶之间
            float fireMidY = (inputSlotsTop + slotH + fuelSlotTop) / 2f;
            float fireLeft = (inputTotalW - fireW) / 2f;
            float fireTop = fireMidY - fireH / 2f;

            // 输出槽
            float outputSlotLeft = leftAreaRight + outputSpacing;
            float outputSlotTop = leftAreaCenterY - outputH / 2f;

            // 箭头进度条
            float arrowLeft = leftAreaRight + (outputSpacing - arrowW) / 2f;
            float arrowTop = leftAreaCenterY - arrowH / 2f;

            // 图标
            bool hasIcon = ItemIcon > ItemID.None && TextureAssets.Item[ItemIcon] != null;
            float iconLeft = 0f, iconTop = 0f, iconW = 0f, iconH = 0f;
            if (hasIcon)
            {
                var iconTex = TextureAssets.Item[ItemIcon];
                iconW = iconTex.Width();
                iconH = iconTex.Height();
                iconLeft = arrowLeft + (arrowW - iconW) / 2f;
                iconTop = outputSlotTop + outputH + iconSpacing;
            }

            // 面板宽度 & X 偏移
            float minLeft = Math.Min(0f, fuelSlotLeft);
            float maxRight = outputSlotLeft + outputW;
            if (hasIcon) maxRight = Math.Max(maxRight, iconLeft + iconW);
            float layoutW = maxRight - minLeft;
            float newPanelWidth = layoutW + 100f;
            float offsetX = (newPanelWidth - layoutW) / 2f - minLeft;

            // 材料槽
            for (int i = 0; i < inputSlotNum; i++)
            {
                var slot = new UICustomItemSlot(ItemSlot.Context.BankItem, inputScale);
                slot.Left.Set(i * (slotW + slotGap) + offsetX, 0f);
                slot.Top.Set(inputSlotsTop, 0f);
                materialSlots.Add(slot);
                Append(slot);
            }

            // 火焰进度条（材料槽与燃料槽之间，下→上）
            _fireBar = new UISmeltingProgressBar(
                texPath: FireTexPath,
                texPath2: Fire2TexPath,
                scale: arrowScale,
                direction: UISmeltingProgressBar.FillDirection.BottomToTop,
                bgColor: new Color(47, 56, 106),
                fgColor: Color.White);
            _fireBar.SetFuelRatio(_furnace);
            _fireBar.Left.Set(fireLeft + offsetX, 0f);
            _fireBar.Top.Set(fireTop, 0f);
            Append(_fireBar);

            // 燃料槽
            fuelSlot = new UICustomItemSlot(ItemSlot.Context.BankItem, inputScale);
            fuelSlot.Left.Set(fuelSlotLeft + offsetX, 0f);
            fuelSlot.Top.Set(fuelSlotTop, 0f);
            Append(fuelSlot);

            // 输出槽
            outputSlot = new UICustomItemSlot(ItemSlot.Context.BankItem, outputScale);
            outputSlot.Left.Set(outputSlotLeft + offsetX, 0f);
            outputSlot.Top.Set(outputSlotTop, 0f);
            Append(outputSlot);

            // 箭头进度条（左→右，熔炼进度）
            _progressBar = new UISmeltingProgressBar(
                texPath: ArrowTexPath,
                scale: arrowScale,
                direction: UISmeltingProgressBar.FillDirection.LeftToRight,
                bgColor: new Color(47, 56, 106),
                fgColor: Color.White);
            _progressBar.SetFurnace(_furnace);
            _progressBar.Left.Set(arrowLeft + offsetX, 0f);
            _progressBar.Top.Set(arrowTop, 0f);
            Append(_progressBar);

            // 图标
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

            // 面板尺寸
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