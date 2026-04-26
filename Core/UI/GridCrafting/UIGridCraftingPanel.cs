using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using ReLogic.Content;
using System;
using System.Collections.Generic;
using System.Linq;
using TerraCraft.Core.DataStructures.GridCrafting;
using TerraCraft.Core.Systems.Durability;
using TerraCraft.Core.Systems.GridCrafting;
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
    internal class UIGridCraftingPanel : UIPanel
    {
        public int ItemIcon { get; set; } = ItemID.None;
        public int TileId { get; set; }
        public int GridWidth { get; private set; }
        public int GridHeight { get; private set; }

        private UICustomItemSlot outputSlot;
        private List<UICustomItemSlot> inputSlots = new List<UICustomItemSlot>();
        private Player Player => Main.LocalPlayer;

        private GridCraftingMatcher _currentMatcher;
        private List<(GriddedRecipe Recipe, Dictionary<int, int> Consumptions, List<GridCraftingMatcher.ReplacementAction> Replacements)> _allMatches;
        private int _currentMatchIndex;
        private string _previousRecipeId;

        private GriddedRecipe? _currentRecipe => MatchAt(_currentMatchIndex)?.Recipe;
        private Dictionary<int, int> _currentConsumptions => MatchAt(_currentMatchIndex)?.Consumptions;
        private List<GridCraftingMatcher.ReplacementAction> _currentReplacements => MatchAt(_currentMatchIndex)?.Replacements;

        private (GriddedRecipe Recipe, Dictionary<int, int> Consumptions, List<GridCraftingMatcher.ReplacementAction> Replacements)?
            MatchAt(int i) => (_allMatches != null && _allMatches.Count > 0 && i < _allMatches.Count)
                ? _allMatches[i]
                : null;

        // 导航按钮
        private UIButtonNeo _leftArrow;
        private UIButtonNeo _rightArrow;

        // 面板尺寸
        private float _originalPanelWidth;
        private float _originalPanelHeight;
        private float _expandedPanelWidth;
        private float _expandedPanelHeight;

        // 鼠标交互
        private bool _wasMouseLeftPressed;
        private bool _wasMouseOverOutputLastFrame;

        private int _craftStackDelay = 7;      // 当前延迟帧数 (7 → 6 → 5 → 4 → 3 → 2)
        private int _craftStackCounter = 0;    // 当前已执行次数（用于判断是否触发加速）
        private int _craftTimesTried = 0;      // 已触发加速的次数（原版用于限制 superFastStack，此处仅保留结构）
        private int _craftWaitFrames = 0;      // 剩余等待帧数，0 表示可以立即执行下一次
        private const int FIRST_DELAY_FRAMES = 30;   // 第一次操作后的额外等待帧数

        private CustomItemSlotInputHandler _inputHandler;
        private Item[] _lastGridItems;

        // ══════════════════════════════════════════════════
        //  初始化入口
        // ══════════════════════════════════════════════════
        public void InitializeGrid(int tileId, int itemiconid)
        {
            BackgroundColor = new Color(63, 82, 151) * 0.8f;
            TileId = tileId;
            ItemIcon = itemiconid;
            (GridWidth, GridHeight) = CraftingStationSize.GetGridSize(tileId);

            BuildInputSlots();
            BuildOutputSlot();
            BuildDecoArrow();
            BuildNavArrows();
            BuildIcon();
            ComputePanelSizes();

            this.SetSize(_originalPanelWidth, _originalPanelHeight);
            UpdateArrowVisibility();

            _inputHandler = new CustomItemSlotInputHandler(inputSlots, RefreshMatching);
        }

        // ══════════════════════════════════════════════════
        //  分段构建
        // ══════════════════════════════════════════════════

        // ── 布局常量（所有 Build* 方法共享） ──
        private const float Spacing = 8f;
        private const float Padding = 16f;
        private const float OutputSpacing = 48f;
        private const float IconSpacing = 16f;
        private const float NavSpacing = 4f;

        // 由 BuildInputSlots 填充，后续方法都用这两个值
        private Vector2 _slotSize;
        private float _actualSpacing;
        private float _outputLeft;
        private float _outputTop;
        private float _outputSlotW;
        private float _outputSlotH;
        private float _iconTop;
        private float _navArrowW;
        private float _navArrowH;

        private void ClearChildren()
        {
            foreach (var slot in inputSlots) RemoveChild(slot);
            if (outputSlot != null) RemoveChild(outputSlot);
            if (_leftArrow != null) RemoveChild(_leftArrow);
            if (_rightArrow != null) RemoveChild(_rightArrow);
            inputSlots.Clear();

            _previousRecipeId = null;
            _allMatches = null;
            _currentMatchIndex = 0;
        }

        private void BuildInputSlots()
        {
            SetPadding(0);
            ClearChildren();

            _slotSize = new Vector2(44.2f);
            _actualSpacing = Spacing;

            for (int y = 0; y < GridHeight; y++)
            {
                for (int x = 0; x < GridWidth; x++)
                {
                    var slot = new UICustomItemSlot(ItemSlot.Context.BankItem, 0.85f);
                    if (x == 0 && y == 0)
                    {
                        _slotSize = slot.GetSize(true);   // GetSize 内部调 Recalculate
                        _actualSpacing = _slotSize.X + Spacing;
                    }
                    slot.Left.Set(Padding + x * _actualSpacing, 0f);
                    slot.Top.Set(Padding + y * _actualSpacing, 0f);
                    inputSlots.Add(slot);
                    Append(slot);
                }
            }
        }

        private void BuildOutputSlot()
        {

            outputSlot = new UICustomItemSlot(4, 0.85f);

            // GetSize(true) 强制 Recalculate，拿到真实像素尺寸
            Vector2 outSize = outputSlot.GetSize(true);
            _outputSlotW = outSize.X;
            _outputSlotH = outSize.Y;

            float gridActualHeight = (GridHeight - 1) * _actualSpacing + _slotSize.Y;
            _outputLeft = Padding + GridWidth * _actualSpacing + OutputSpacing;
            _outputTop = Padding + (gridActualHeight - _outputSlotH) / 2f;

            outputSlot.Left.Set(_outputLeft, 0f);
            outputSlot.Top.Set(_outputTop, 0f);
            Append(outputSlot);
        }

        private void BuildDecoArrow()
        {
            var arrowTex = TextureAssets.GolfBallArrow;
            var arrowRect = new Rectangle(0, 0, arrowTex.Width() / 2 - 2, arrowTex.Height());
            var arrow = new UIImageNeo(arrowTex)
            {
                NormalizedOrigin = new Vector2(0.5f),
                IgnoresMouseInteraction = true,
                Color = new Color(47, 56, 106) * 0.7f,
                Rotation = -MathHelper.PiOver2,
                Rectangle = arrowRect,
                ImageScale = 1f
            };
            float left = _outputLeft - OutputSpacing;
            float top = _outputTop + (arrowRect.Height - _outputSlotH) / 2f;
            arrow.SetSize(_slotSize);
            arrow.Left.Set(left, 0f);
            arrow.Top.Set(top, 0f);
            Append(arrow);
        }

        private void BuildNavArrows()
        {
            var normalTex = TerraCraft.GetAsset2D("TerraCraft/Assets/UI/GridCrafting/ArrowSmall", AssetRequestMode.ImmediateLoad);
            var glowTex = TerraCraft.GetAsset2D("TerraCraft/Assets/UI/GridCrafting/ArrowSmall_Glow", AssetRequestMode.ImmediateLoad);

            float offset = TileId == -1 ? 8 : 0;
            // 纹理直接给出像素尺寸，不依赖任何 Recalculate
            _navArrowW = normalTex.Value.Width;
            _navArrowH = normalTex.Value.Height;
            _iconTop = _outputTop + _outputSlotH + (TileId == -1 ? offset : IconSpacing);

            _leftArrow = new UIButtonNeo(normalTex, glowTex, spriteEffects: SpriteEffects.FlipHorizontally);
            _leftArrow.Left.Set(_outputLeft - _navArrowW - NavSpacing + offset, 0f);
            _leftArrow.Top.Set(_iconTop, 0f);
            _leftArrow.OnLeftClick += (_, __) => NavigateMatch(-1);
            Append(_leftArrow);

            _rightArrow = new UIButtonNeo(normalTex, glowTex, spriteEffects: SpriteEffects.None);
            _rightArrow.Left.Set(_outputLeft + _outputSlotW + NavSpacing - offset, 0f);
            _rightArrow.Top.Set(_iconTop, 0f);
            _rightArrow.OnLeftClick += (_, __) => NavigateMatch(1);
            Append(_rightArrow);
        }

        private void BuildIcon()
        {
            if (ItemIcon <= ItemID.None || TextureAssets.Item[ItemIcon] == null) return;

            var iconTex = TextureAssets.Item[ItemIcon];
            var icon = new UIImageNeo(iconTex)
            {
                IgnoresMouseInteraction = true,
                Color = Color.White * 0.8f
            };
            float left = _outputLeft + Math.Abs(iconTex.Width() - _outputSlotW) / 2f;
            icon.SetSize(_slotSize);
            icon.Left.Set(left, 0f);
            icon.Top.Set(_iconTop, 0f);
            Append(icon);
        }

        private void ComputePanelSizes()
        {
            float gridActualHeight = (GridHeight - 1) * _actualSpacing + _slotSize.Y;

            _originalPanelWidth = _outputLeft + _outputSlotW + Padding + 4f;
            _originalPanelHeight = Padding + gridActualHeight + Padding + 4f;
            float offset = TileId == -1 ? 8 : 0;
            float navRightEdge = _outputLeft + _outputSlotW + NavSpacing + _navArrowW + Padding + 4f - offset;
            float iconHeight = ItemIcon > ItemID.None ? _slotSize.Y : 0f;
            float iconArrowBottom = _iconTop + iconHeight + Padding + 4f;
            float contentBottom = Math.Max(_originalPanelHeight, iconArrowBottom);

            _expandedPanelWidth = Math.Max(_originalPanelWidth, navRightEdge);
            _expandedPanelHeight = contentBottom;
        }

        // ══════════════════════════════════════════════════
        //  导航 & 可见性
        // ══════════════════════════════════════════════════
        private void NavigateMatch(int direction)
        {
            if (_allMatches == null || _allMatches.Count <= 1) return;
            _currentMatchIndex = (_currentMatchIndex + direction + _allMatches.Count) % _allMatches.Count;
            UpdateOutputFromMatch();
            _previousRecipeId = _allMatches[_currentMatchIndex].Recipe.Id;
            SoundEngine.PlaySound(SoundID.MenuTick);
        }

        private void UpdateOutputFromMatch()
        {
            var match = MatchAt(_currentMatchIndex);
            if (match.HasValue)
            {
                outputSlot.Item.SetDefaults(match.Value.Recipe.Outputs[0].ItemType);
                outputSlot.Item.stack = match.Value.Recipe.Outputs[0].Amount;
            }
            else
            {
                outputSlot.Item.TurnToAir();
            }
        }

        private void UpdateArrowVisibility()
        {
            bool show = _allMatches != null && _allMatches.Count > 1;
            if (_leftArrow != null) _leftArrow.IgnoresMouseInteraction = !show;
            if (_rightArrow != null) _rightArrow.IgnoresMouseInteraction = !show;

            // Active = false 让元素完全跳过 Draw 和 Update
            if (_leftArrow != null) _leftArrow.Active = show;
            if (_rightArrow != null) _rightArrow.Active = show;

            this.SetSize(show ? _expandedPanelWidth : _originalPanelWidth, _originalPanelHeight);
        }

        // ══════════════════════════════════════════════════
        //  Update & Matching
        // ══════════════════════════════════════════════════
        public override void Update(GameTime gameTime)
        {
            _inputHandler?.Update();
            HandleOutputSlotInteraction();
            base.Update(gameTime);
            RefreshMatching();
        }

        private bool HasGridChanged(Item[] currentGrid)
        {
            if (_lastGridItems == null || _lastGridItems.Length != currentGrid.Length)
            {
                _lastGridItems = currentGrid.Select(i => i?.Clone()).ToArray();
                return true;
            }
            for (int i = 0; i < currentGrid.Length; i++)
            {
                var cur = currentGrid[i];
                var last = _lastGridItems[i];
                if ((cur == null) != (last == null)) { Snapshot(currentGrid); return true; }
                if (cur != null && (cur.type != last.type || cur.stack != last.stack)) { Snapshot(currentGrid); return true; }
            }
            return false;

            void Snapshot(Item[] items) => _lastGridItems = items.Select(i => i?.Clone()).ToArray();
        }

        private void RefreshMatching()
        {
            if (inputSlots.Count == 0) { UpdateArrowVisibility(); return; }

            Item[] gridItems = inputSlots.Select(s => s.Item).ToArray();

            bool hasDynamic = _allMatches != null && _allMatches.Count > 0 &&
                              _allMatches.Any(m => m.Recipe.Conditions?.Count > 0);

            if (!hasDynamic && !HasGridChanged(gridItems)) { UpdateArrowVisibility(); return; }

            string prevId = _previousRecipeId;

            _currentMatcher = new GridCraftingMatcher(TileId, GridWidth, GridHeight, gridItems);
            _allMatches = _currentMatcher.MatchAll()
                                             .Where(m => AreConditionsMet(m.Recipe))
                                             .ToList();

            if (prevId != null)
            {
                int idx = _allMatches.FindIndex(m => m.Recipe.Id == prevId);
                _currentMatchIndex = idx >= 0 ? idx : 0;
            }
            else _currentMatchIndex = 0;

            if (_allMatches.Count > 0)
            {
                var output = _allMatches[_currentMatchIndex].Recipe.Outputs[0];
                outputSlot.Item.SetDefaults(output.ItemType);
                outputSlot.Item.stack = output.Amount;
                _previousRecipeId = _allMatches[_currentMatchIndex].Recipe.Id;
            }
            else
            {
                outputSlot.Item.TurnToAir();
                _previousRecipeId = null;
            }

            UpdateArrowVisibility();
            _lastGridItems = gridItems.Select(i => i?.Clone()).ToArray();
        }

        private bool AreConditionsMet(GriddedRecipe recipe)
        {
            if (recipe.Conditions == null || recipe.Conditions.Count == 0) return true;
            foreach (string c in recipe.Conditions)
            {
                Condition cond = ConditionResolver.Parse(c);
                if (cond == null || !cond.Predicate()) return false;
            }
            return true;
        }

        // ══════════════════════════════════════════════════
        //  输出槽交互
        // ══════════════════════════════════════════════════
        private void HandleOutputSlotInteraction()
        {
            bool leftDown = Main.mouseLeft;
            Rectangle outputRect = outputSlot.GetInnerDimensions().ToRectangle();
            bool mouseOverOutput = outputRect.Contains(Main.MouseScreen.ToPoint());

            if (!mouseOverOutput || PlayerInput.IgnoreMouseInterface)
            {
                _wasMouseOverOutputLastFrame = false;
                _wasMouseLeftPressed = leftDown;
                return;
            }

            bool leftJustPressed = leftDown && !_wasMouseLeftPressed;
            bool rightHeld = Main.mouseRight;
            bool shiftHeld = Main.keyState.IsKeyDown(Keys.LeftShift) || Main.keyState.IsKeyDown(Keys.RightShift);

            if (leftJustPressed && shiftHeld && _currentRecipe.HasValue)
            {
                CraftAll();
                Main.mouseLeftRelease = false;
            }
            else if (leftJustPressed && !shiftHeld && _currentRecipe.HasValue && _currentRecipe.Value.Outputs?.Count > 0)
            {
                if (TryCraftAndGiveToMouse(_currentRecipe.Value.Outputs[0].Amount))
                {
                    RefreshMatching();
                    Main.mouseLeftRelease = false;
                }
            }

            if (rightHeld && _currentRecipe.HasValue && _currentRecipe.Value.Outputs?.Count > 0)
            {
                _craftStackCounter++;

                if (_craftWaitFrames <= 1)
                {
                    int outputAmount = _currentRecipe.Value.Outputs[0].Amount;
                    if (TryCraftAndGiveToMouse(outputAmount))
                    {
                        RefreshMatching();

                        if (_craftWaitFrames == 0)
                            _craftWaitFrames = FIRST_DELAY_FRAMES;
                        else
                            _craftWaitFrames = _craftStackDelay;

                        int num;
                        switch (_craftStackDelay)
                        {
                            case 7: num = 30; break;
                            case 6: num = 25; break;
                            case 5: num = 20; break;
                            case 4: num = 15; break;
                            case 3: num = 10; break;
                            default: num = 4; break;
                        }
                        if (_craftStackCounter >= num)
                        {
                            _craftStackDelay--;
                            if (_craftStackDelay < 2)
                                _craftStackDelay = 2;

                            _craftTimesTried++;
                            _craftStackCounter = 0;
                        }
                    }
                    else
                    {
                        ResetCraftAcceleration();
                    }
                }
                else
                {
                    _craftWaitFrames--;
                }

                Main.mouseRightRelease = false;
            }
            else
            {
                // 松开右键时完全重置加速状态
                ResetCraftAcceleration();
            }

            _wasMouseLeftPressed = leftDown;
            _wasMouseOverOutputLastFrame = true;
        }
        private void ResetCraftAcceleration()
        {
            _craftStackDelay = 7;
            _craftStackCounter = 0;
            _craftTimesTried = 0;
            _craftWaitFrames = 0;
        }

        private void CraftAll()
        {
            if (!_currentRecipe.HasValue || _currentRecipe.Value.Outputs?.Count == 0) return;
            int outputAmount = _currentRecipe.Value.Outputs[0].Amount;
            while (_currentRecipe.HasValue && CanConsumeInputs())
                if (!TryCraftAndGiveToMouse(outputAmount, noSound: true)) break;
            SoundEngine.PlaySound(SoundID.Grab);
            RefreshMatching();
        }

        // ══════════════════════════════════════════════════
        //  合成核心
        // ══════════════════════════════════════════════════
        private bool TryCraftAndGiveToMouse(int takeAmount, bool noSound = false)
        {
            if (!_currentRecipe.HasValue || _currentConsumptions == null) return false;
            if (!CanConsumeInputs()) return false;

            var output = _currentRecipe.Value.Outputs[0];
            int itemType = output.ItemType;
            if (itemType == 0) return false;

            Item craftedItem = new Item(itemType, takeAmount, prefix: -1);
            if (output.UseDurability)
            {
                int max = output.MaxDurability ?? 100;
                int initial = output.InitialDurability ?? max;
                if (max > 0) craftedItem.durability().EnableDurability(initial, max);
            }

            Item mouseItem = Main.mouseItem;
            if (mouseItem.IsAir)
                Main.mouseItem = craftedItem.Clone();
            else if (mouseItem.type == craftedItem.type && mouseItem.stack < mouseItem.maxStack)
            {
                // 必须能装下完整一份产出，否则不合成、不消耗材料
                if (mouseItem.maxStack - mouseItem.stack < craftedItem.stack) return false;
                mouseItem.stack += craftedItem.stack;
            }
            else return false;
            PerformConsumption();
            if (!noSound) SoundEngine.PlaySound(SoundID.Grab);
            return true;
        }

        private bool CanConsumeInputs()
        {
            foreach (var kv in _currentConsumptions)
            {
                if (kv.Key >= inputSlots.Count) return false;
                if (inputSlots[kv.Key].Item.stack < kv.Value) return false;
            }
            return true;
        }

        private void PerformConsumption()
        {
            foreach (var kv in _currentConsumptions)
            {
                Item slotItem = inputSlots[kv.Key].Item;
                slotItem.stack -= kv.Value;
                if (slotItem.stack <= 0) slotItem.TurnToAir();
            }

            if (_currentReplacements == null) return;
            foreach (var rep in _currentReplacements)
            {
                Item cur = inputSlots[rep.SlotIndex].Item;
                bool empty = cur.IsAir || cur.stack <= 0;
                if (empty)
                {
                    if (rep.ReplaceWithItem.HasValue)
                    {
                        inputSlots[rep.SlotIndex].Item.SetDefaults(rep.ReplaceWithItem.Value);
                        inputSlots[rep.SlotIndex].Item.stack = rep.ReplaceAmount;
                    }
                    else inputSlots[rep.SlotIndex].Item.TurnToAir();
                }
                else
                {
                    if (rep.ReplaceWithItem.HasValue)
                        Player.QuickSpawnItem(Player.GetSource_FromThis(), rep.ReplaceWithItem.Value, rep.ReplaceAmount);
                }
            }
        }

        // ══════════════════════════════════════════════════
        //  关闭时退还物品
        // ══════════════════════════════════════════════════
        public override void OnDeactivate()
        {
            base.OnDeactivate();
            foreach (var slot in inputSlots)
            {
                if (!slot.Item.IsAir)
                {
                    Player.QuickSpawnItem(new EntitySource_OverfullInventory(Player, "GridCrafting"), slot.Item, slot.Item.stack);
                    slot.Item.TurnToAir();
                }
            }
        }
    }
}