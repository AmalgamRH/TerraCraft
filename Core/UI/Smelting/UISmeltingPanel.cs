using Microsoft.Xna.Framework;
using ReLogic.Content;
using System;
using System.Collections.Generic;
using System.Linq;
using TerraCraft.Core.DataStructures.Smelting;
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
        private UISmeltingProgressBar _progressBar;
        private UISmeltingProgressBar _fireBar;
        private SmeltingInputHandler _inputHandler;

        private const string ArrowTexPath = "TerraCraft/Assets/UI/Smelting/SmeltingArrow";
        private const string FireTexPath = "TerraCraft/Assets/UI/Smelting/SmeltingFire";
        private const string Fire2TexPath = "TerraCraft/Assets/UI/Smelting/SmeltingFire2";

        private int _lastMainItemType = -1;

        public void InitializeGrid(Point16 tilePos, int tileId, int itemiconid, TEFurnace furnace)
        {
            _furnace = furnace;
            TilePos = tilePos;
            BackgroundColor = new Color(63, 82, 151) * 0.8f;
            TileId = tileId;
            ItemIcon = itemiconid;
            _lastMainItemType = -1;
            RebuildUIForRecipe(null);
        }

        private void RebuildUIForRecipe(SmeltingRecipe? forcedRecipe = null)
        {
            RemoveAllChildren();

            // 获取当前主材料（从熔炉数据中读取，避免 UI 同步延迟）
            int mainType = -1;
            for (int i = 0; i < _furnace.material.Length; i++)
                if (_furnace.material[i] != null && !_furnace.material[i].IsAir)
                { mainType = _furnace.material[i].type; break; }

            SmeltingRecipe? recipe = forcedRecipe ?? (mainType != -1 ? SmeltingMatcher.GetBestRecipe(TileId, mainType, _furnace.material, _furnace.fuel) : null);

            int targetSlotCount = (recipe.HasValue && recipe.Value.Ingredients != null) ? recipe.Value.Ingredients.Count : 1;
            targetSlotCount = Math.Clamp(targetSlotCount, 1, TEFurnace.MAX_MATERIALS);

            // 保存旧材料（深拷贝）
            var oldMaterials = new List<Item>();
            for (int i = 0; i < _furnace.material.Length; i++)
                oldMaterials.Add(_furnace.material[i]?.Clone() ?? new Item());

            // 清理旧控件
            foreach (var slot in materialSlots) RemoveChild(slot);
            if (fuelSlot != null) RemoveChild(fuelSlot);
            if (outputSlot != null) RemoveChild(outputSlot);
            if (_progressBar != null) RemoveChild(_progressBar);
            if (_fireBar != null) RemoveChild(_fireBar);
            materialSlots.Clear();

            // 创建新槽位
            CreateSlots(targetSlotCount, recipe);

            // 准备新物品数组，长度固定为 MAX_MATERIALS
            var newMaterials = new Item[TEFurnace.MAX_MATERIALS];
            for (int i = 0; i < TEFurnace.MAX_MATERIALS; i++)
                newMaterials[i] = new Item();

            if (recipe.HasValue)
            {
                // 按配方顺序填充材料
                for (int i = 0; i < recipe.Value.Ingredients.Count; i++)
                {
                    int requiredType = recipe.Value.Ingredients[i].ItemType;
                    // 从旧材料中查找第一个匹配的且未被使用的物品
                    for (int j = 0; j < oldMaterials.Count; j++)
                    {
                        if (!oldMaterials[j].IsAir && oldMaterials[j].type == requiredType)
                        {
                            newMaterials[i] = oldMaterials[j].Clone();
                            oldMaterials[j].TurnToAir(); // 标记已使用
                            break;
                        }
                    }
                }
                // 剩余未使用的旧材料掉落
                foreach (var old in oldMaterials)
                    if (!old.IsAir) DropItemAtFurnace(old);
            }
            else
            {
                // 无配方：尝试合并所有旧材料到第一个槽
                Item combined = new Item();
                foreach (var old in oldMaterials)
                {
                    if (old.IsAir) continue;
                    if (combined.IsAir) combined = old.Clone();
                    else if (combined.type == old.type) combined.stack += old.stack;
                    else DropItemAtFurnace(old.Clone());
                }
                if (!combined.IsAir) newMaterials[0] = combined;
            }

            // 写入新槽位并同步到熔炉（注意长度保持一致）
            for (int i = 0; i < TEFurnace.MAX_MATERIALS; i++)
            {
                if (i < materialSlots.Count)
                    materialSlots[i].Item = newMaterials[i];
                _furnace.material[i] = newMaterials[i];
            }

            // 燃料/输出槽保留原数据
            fuelSlot.Item = _furnace.fuel?.Clone() ?? new Item();
            outputSlot.Item = _furnace.output?.Clone() ?? new Item();

            // 重新创建输入处理器
            _inputHandler = new SmeltingInputHandler(
                materialSlots.Concat(new[] { fuelSlot }).ToList(),
                materialSlots,
                fuelSlot,
                outputSlot,
                _furnace);
        }

        private void CreateSlots(int slotCount, SmeltingRecipe? recipe)
        {
            SetPadding(0);
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

            float inputSlotsTop = padding;
            float fuelSlotTop = padding + (slotH + slotGap) * 1.75f;
            float inputTotalW = slotCount * slotW + (slotCount - 1) * slotGap;
            float fuelSlotLeft = (inputTotalW - slotW) / 2f;

            float leftAreaTop = inputSlotsTop;
            float leftAreaBottom = fuelSlotTop + slotH;
            float leftAreaCenterY = (leftAreaTop + leftAreaBottom) / 2f;
            float leftAreaRight = Math.Max(inputTotalW, fuelSlotLeft + slotW);

            float fireMidY = (inputSlotsTop + slotH + fuelSlotTop) / 2f;
            float fireLeft = (inputTotalW - fireW) / 2f;
            float fireTop = fireMidY - fireH / 2f;

            float outputSlotLeft = leftAreaRight + outputSpacing;
            float outputSlotTop = leftAreaCenterY - outputH / 2f;

            float arrowLeft = leftAreaRight + (outputSpacing - arrowW) / 2f;
            float arrowTop = leftAreaCenterY - arrowH / 2f;

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

            float minLeft = Math.Min(0f, fuelSlotLeft);
            float maxRight = outputSlotLeft + outputW;
            if (hasIcon) maxRight = Math.Max(maxRight, iconLeft + iconW);
            float layoutW = maxRight - minLeft;
            float newPanelWidth = layoutW + 100f;
            float offsetX = (newPanelWidth - layoutW) / 2f - minLeft;

            // 材料槽
            for (int i = 0; i < slotCount; i++)
            {
                var slot = new UICustomItemSlot(ItemSlot.Context.BankItem, inputScale);
                slot.Left.Set(i * (slotW + slotGap) + offsetX, 0f);
                slot.Top.Set(inputSlotsTop, 0f);
                materialSlots.Add(slot);
                Append(slot);
            }

            // 火焰进度条
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

            // 箭头进度条
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

            float contentBottom = leftAreaBottom;
            contentBottom = Math.Max(contentBottom, outputSlotTop + outputH);
            if (hasIcon) contentBottom = Math.Max(contentBottom, iconTop + iconH);
            this.SetSize(newPanelWidth, contentBottom + padding);
        }

        private void DropItemAtFurnace(Item item)
        {
            if (item.IsAir) return;
            Item.NewItem(new EntitySource_TileBreak(TilePos.X, TilePos.Y),
                TilePos.X * 16, TilePos.Y * 16, 16, 16, item.type, item.stack, false, item.prefix);
        }

        public override void Update(GameTime gameTime)
        {
            // 仅在鼠标没有持有物品且没有正在进行的拖拽操作时重建UI
            bool canRebuild = Main.mouseItem.IsAir && !Main.mouseLeft && !Main.mouseRight;

            int currentMainType = -1;
            for (int i = 0; i < _furnace.material.Length; i++)
                if (_furnace.material[i] != null && !_furnace.material[i].IsAir)
                { currentMainType = _furnace.material[i].type; break; }

            if (canRebuild && currentMainType != _lastMainItemType)
            {
                _lastMainItemType = currentMainType;
                RebuildUIForRecipe(null);
            }

            _inputHandler?.Update();
            base.Update(gameTime);
            _inputHandler?.SyncFromFurnace(); // 可能需要调整同步时机
        }

        public override void OnDeactivate()
        {
            SaveCurrentItemsToFurnace();
            base.OnDeactivate();
        }

        private void SaveCurrentItemsToFurnace()
        {
            if (_furnace == null) return;
            for (int i = 0; i < materialSlots.Count && i < _furnace.material.Length; i++)
                _furnace.material[i] = materialSlots[i].Item.Clone();
            if (fuelSlot != null) _furnace.fuel = fuelSlot.Item.Clone();
            if (outputSlot != null) _furnace.output = outputSlot.Item.Clone();
        }
    }
}