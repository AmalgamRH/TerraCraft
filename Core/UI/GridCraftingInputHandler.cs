using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.GameInput;
using Terraria.ID;

namespace TerraCraft.Core.UI.GridCrafting
{
    internal class GridCraftingInputHandler
    {
        private enum DragMode { None, PickUp, DistributePreview }
        private DragMode _currentMode = DragMode.None;

        private readonly List<VanillaItemSlotWrapper> _inputSlots;
        private readonly Action _onSlotsChanged;

        // 预览分配状态
        private Item _previewTemplate;           // 分配物品的模板（类型、前缀等）
        private int _previewTotalAmount;         // 总分配数量（鼠标初始堆叠数）
        private List<int> _previewTargetSlots = new List<int>(); // 当前预览涉及的槽索引
        private int _lastPreviewHoveredSlot = -1;

        // 双击检测
        private int _lastClickSlot = -1;
        private uint _lastClickFrame = 0;
        private const int DoubleClickFrameThreshold = 10;
        private Item _lastClickItemSnapshot;

        // 自追踪鼠标状态
        private bool _mouseLeftWasDown = false;
        private bool _mouseRightWasDown = false;

        public GridCraftingInputHandler(List<VanillaItemSlotWrapper> slots, System.Action onChanged)
        {
            _inputSlots = slots;
            _onSlotsChanged = onChanged;
        }

        public void Update()
        {
            if (PlayerInput.IgnoreMouseInterface)
            {
                _mouseLeftWasDown = false;
                _mouseRightWasDown = false;
                Reset();
                return;
            }

            int hoveredSlot = GetHoveredSlotIndex();
            bool leftDown = Main.mouseLeft;
            bool rightDown = Main.mouseRight;
            bool leftJustPressed = leftDown && !_mouseLeftWasDown;
            bool leftJustReleased = !leftDown && _mouseLeftWasDown;
            bool rightJustPressed = rightDown && !_mouseRightWasDown;

            if (ShiftInUse && hoveredSlot != -1 && Main.mouseItem.netID == 0 && 
                _inputSlots[hoveredSlot].Item.netID != 0)
            {
                if (Main.LocalPlayer.ItemSpace(_inputSlots[hoveredSlot].Item).
                    CanTakeItemToPersonalInventory)
                    Main.cursorOverride = 8;
            }

            // ========== 右键按下瞬间：拿取槽内一半 ==========
            if (rightJustPressed && hoveredSlot != -1)
            {
                Item slotItem = _inputSlots[hoveredSlot].Item;
                if (!slotItem.IsAir && Main.mouseItem.IsAir)
                {
                    int takeAmount = slotItem.stack > 1 ? slotItem.stack / 2 : 1;
                    Item newMouseItem = new Item();
                    newMouseItem.SetDefaults(slotItem.type);
                    newMouseItem.stack = takeAmount;
                    newMouseItem.prefix = slotItem.prefix;
                    newMouseItem.favorited = slotItem.favorited;
                    Main.mouseItem = newMouseItem;

                    slotItem.stack -= takeAmount;
                    if (slotItem.stack <= 0)
                        slotItem.TurnToAir();

                    SoundEngine.PlaySound(SoundID.Grab);
                    Main.mouseRightRelease = false;
                    _onSlotsChanged?.Invoke();

                    _mouseLeftWasDown = leftDown;
                    _mouseRightWasDown = rightDown;
                    return;
                }
            }

            // 没有悬停且不在预览模式，不干预原版
            if (hoveredSlot == -1 && _currentMode == DragMode.None)
            {
                _mouseLeftWasDown = leftDown;
                _mouseRightWasDown = rightDown;
                return;
            }

            // ========== 左键按下瞬间 ==========
            if (leftJustPressed && hoveredSlot != -1)
            {
                Main.mouseLeftRelease = false;
                uint currentFrame = Main.GameUpdateCount;

                if (ShiftInUse && Main.mouseItem.netID == 0 && _inputSlots[hoveredSlot].Item.netID != 0 && Main.cursorOverride == 8)
                {
                    Item slotItem = _inputSlots[hoveredSlot].Item;
                    bool moved = MoveItemToPlayerInventory(hoveredSlot);
                    if (moved)
                    {
                        SoundEngine.PlaySound(SoundID.Grab);
                        _onSlotsChanged?.Invoke();
                        _mouseLeftWasDown = leftDown;
                        _mouseRightWasDown = rightDown;
                    }
                    return;
                }

                // 双击收集检测
                if (_lastClickSlot == hoveredSlot &&
                    currentFrame - _lastClickFrame <= DoubleClickFrameThreshold && !ShiftInUse)
                {
                    if (_lastClickItemSnapshot != null && !_lastClickItemSnapshot.IsAir)
                    {
                        if (Main.mouseItem.IsAir || IsSameItem(Main.mouseItem, _lastClickItemSnapshot))
                        {
                            CollectAllSameItems(_lastClickItemSnapshot);
                        }
                    }

                    _lastClickSlot = -1;
                    _lastClickFrame = 0;
                    _lastClickItemSnapshot = null;
                    _onSlotsChanged?.Invoke();
                    _mouseLeftWasDown = leftDown;
                    _mouseRightWasDown = rightDown;
                    return;
                }

                _lastClickSlot = hoveredSlot;
                _lastClickFrame = currentFrame;
                _lastClickItemSnapshot = _inputSlots[hoveredSlot].Item.Clone();

                if (Main.mouseItem.IsAir)
                {
                    // 空手点击非空槽 → 拿起物品
                    if (!_inputSlots[hoveredSlot].Item.IsAir)
                    {
                        TakeItemFromSlot(hoveredSlot);
                        _currentMode = DragMode.PickUp;
                    }
                }
                else
                {
                    // 鼠标有物品：根据槽内物品类型决定行为
                    Item slotItem = _inputSlots[hoveredSlot].Item;
                    bool isSame = IsSameItem(Main.mouseItem, slotItem);
                    bool slotEmpty = slotItem.IsAir;

                    // 情况1：槽为空 → 进入预览分配模式
                    if (slotEmpty)
                    {
                        if (Main.mouseItem.stack == 1)
                        {
                            // 直接放入
                            _inputSlots[hoveredSlot].Item = Main.mouseItem.Clone();
                            Main.mouseItem.TurnToAir();
                            SoundEngine.PlaySound(SoundID.Grab);
                            _onSlotsChanged?.Invoke();
                            _currentMode = DragMode.None;
                        }
                        else
                        {
                            StartPreviewDistribution(hoveredSlot);
                            SoundEngine.PlaySound(SoundID.Grab);
                            _currentMode = DragMode.DistributePreview;
                        }
                    }
                    // 情况2：槽有同物品 → 尝试堆叠，然后决定是否进入预览分配
                    else if (isSame)
                    {
                        // 保存鼠标原始数量用于后续预览
                        int originalMouseStack = Main.mouseItem.stack;

                        // 执行堆叠合并（尽可能多地放入）
                        bool anyMoved = TryStackItems(Main.mouseItem, slotItem);

                        if (anyMoved)
                        {
                            SoundEngine.PlaySound(SoundID.Grab);
                            _onSlotsChanged?.Invoke();

                            // 如果合并后鼠标仍有剩余且类型相同 → 进入预览分配模式
                            if (!Main.mouseItem.IsAir && Main.mouseItem.type == slotItem.type)
                            {
                                StartPreviewDistribution(hoveredSlot);
                                _currentMode = DragMode.DistributePreview;
                            }
                            else
                            {
                                // 全部放入或类型改变，不进入预览
                                _currentMode = DragMode.None;
                            }
                        }
                        else
                        {
                            // 无法堆叠（如槽已满），什么也不做，不播放音效
                            _currentMode = DragMode.None;
                        }
                    }
                    // 情况3：槽有不同物品 → 交换
                    else
                    {
                        SwapItems(hoveredSlot);
                        SoundEngine.PlaySound(SoundID.Grab);
                        _onSlotsChanged?.Invoke();
                        _currentMode = DragMode.None;
                    }
                }

                _mouseLeftWasDown = leftDown;
                _mouseRightWasDown = rightDown;
                return;
            }

            // ========== 预览拖拽中 ==========
            if (_currentMode == DragMode.DistributePreview && leftDown)
            {
                if (hoveredSlot != -1 && hoveredSlot != _lastPreviewHoveredSlot)
                {
                    _lastPreviewHoveredSlot = hoveredSlot;
                    UpdatePreviewForHoveredSlot(hoveredSlot);
                }

                Main.LocalPlayer.mouseInterface = true;
                Main.mouseLeftRelease = false;
            }

            // ========== 左键松开：执行实际分配 ==========
            if (leftJustReleased)
            {
                if (_currentMode == DragMode.DistributePreview)
                {
                    ApplyRealDistribution();
                    _onSlotsChanged?.Invoke();
                }
                Reset();
            }

            // 安全重置
            if (!leftDown && _currentMode != DragMode.None)
                Reset();

            _mouseLeftWasDown = leftDown;
            _mouseRightWasDown = rightDown;
        }

        #region 核心交互方法

        /// <summary>
        /// 将鼠标物品尽可能多地堆叠到目标槽中。返回是否移动了任何物品。
        /// </summary>
        private bool TryStackItems(Item source, Item destination)
        {
            if (source.IsAir || destination.IsAir) return false;
            if (source.type != destination.type) return false;

            int canAdd = Math.Min(source.stack, destination.maxStack - destination.stack);
            if (canAdd <= 0) return false;

            destination.stack += canAdd;
            source.stack -= canAdd;
            if (source.stack <= 0)
                source.TurnToAir();
            return true;
        }

        /// <summary>
        /// 交换鼠标物品和指定槽物品。
        /// </summary>
        private void SwapItems(int slotIndex)
        {
            Item temp = Main.mouseItem.Clone();
            Main.mouseItem = _inputSlots[slotIndex].Item.Clone();
            _inputSlots[slotIndex].Item = temp;
        }

        #endregion

        #region 预览分配核心方法

        private void StartPreviewDistribution(int initialSlot)
        {
            _previewTemplate = Main.mouseItem.Clone();
            _previewTotalAmount = Main.mouseItem.stack;
            _previewTargetSlots.Clear();
            _previewTargetSlots.Add(initialSlot);
            _lastPreviewHoveredSlot = initialSlot;

            UpdatePreviewVisuals();
        }

        private void UpdatePreviewForHoveredSlot(int slot)
        {
            if (!_previewTargetSlots.Contains(slot))
            {
                Item slotItem = _inputSlots[slot].Item;
                if (slotItem.IsAir)
                {
                    _previewTargetSlots.Add(slot);
                    UpdatePreviewVisuals();
                }
            }
        }

        private void UpdatePreviewVisuals()
        {
            // 清除所有槽的预览状态
            foreach (var slot in _inputSlots)
                slot.ClearPreview();

            if (_previewTargetSlots.Count == 0) return;

            int baseAmount = _previewTotalAmount / _previewTargetSlots.Count;
            int remainder = _previewTotalAmount % _previewTargetSlots.Count;

            for (int i = 0; i < _previewTargetSlots.Count; i++)
            {
                int idx = _previewTargetSlots[i];
                int amount = baseAmount + (i < remainder ? 1 : 0);
                if (amount <= 0) continue;

                Item previewItem = _previewTemplate.Clone();
                previewItem.stack = Math.Min(amount, previewItem.maxStack);
                _inputSlots[idx].PreviewItem = previewItem;
                _inputSlots[idx].IsPreviewSlot = true;
            }
        }

        private void ApplyRealDistribution()
        {
            if (_previewTemplate == null || _previewTemplate.IsAir) return;

            // 清除预览
            foreach (var slot in _inputSlots)
                slot.ClearPreview();

            // 收集有效目标槽
            List<int> validSlots = new List<int>();
            foreach (int idx in _previewTargetSlots)
            {
                Item slotItem = _inputSlots[idx].Item;
                if (slotItem.IsAir)
                    validSlots.Add(idx);
            }

            if (validSlots.Count == 0) return;

            // 清空目标槽
            foreach (int idx in validSlots)
                _inputSlots[idx].Item.TurnToAir();

            int baseAmount = _previewTotalAmount / validSlots.Count;
            int remainder = _previewTotalAmount % validSlots.Count;

            for (int i = 0; i < validSlots.Count; i++)
            {
                int idx = validSlots[i];
                int amount = baseAmount + (i < remainder ? 1 : 0);
                if (amount <= 0) continue;

                Item newItem = _previewTemplate.Clone();
                newItem.stack = Math.Min(amount, newItem.maxStack);
                _inputSlots[idx].Item = newItem;
            }

            // 计算已分配总量，更新鼠标
            int allocated = 0;
            foreach (int idx in validSlots)
                allocated += _inputSlots[idx].Item.stack;

            int remaining = _previewTotalAmount - allocated;
            if (remaining > 0)
            {
                if (Main.mouseItem.IsAir)
                {
                    Main.mouseItem = _previewTemplate.Clone();
                    Main.mouseItem.stack = remaining;
                }
                else
                    Main.mouseItem.stack = remaining;
            }
            else
                Main.mouseItem.TurnToAir();
        }

        #endregion

        #region 辅助方法

        private int GetHoveredSlotIndex()
        {
            for (int i = 0; i < _inputSlots.Count; i++)
                if (_inputSlots[i].GetInnerDimensions().ToRectangle().Contains(Main.MouseScreen.ToPoint()))
                    return i;
            return -1;
        }

        private void Reset()
        {
            _currentMode = DragMode.None;
            foreach (var slot in _inputSlots)
                slot.ClearPreview();
            _previewTemplate = null;
            _previewTargetSlots.Clear();
            _lastPreviewHoveredSlot = -1;
        }

        private void TakeItemFromSlot(int slotIndex)
        {
            Item slotItem = _inputSlots[slotIndex].Item;
            if (slotItem.IsAir) return;
            Main.mouseItem = slotItem.Clone();
            slotItem.TurnToAir();
            SoundEngine.PlaySound(SoundID.Grab);
        }

        private bool IsSameItem(Item currentItem, Item targetItem)
        {
            return targetItem.type == currentItem.type &&
                   targetItem.prefix == currentItem.prefix &&
                   SameGlobals(currentItem, targetItem);
        }
        private bool SameGlobals(Item a, Item b)
        {
            List<Type> typesA = new List<Type>();
            List<Type> typesB = new List<Type>();

            foreach (var g in a.Globals)
            {
                if (g != null)
                    typesA.Add(g.GetType());
            }

            foreach (var g in b.Globals)
            {
                if (g != null)
                    typesB.Add(g.GetType());
            }

            if (typesA.Count != typesB.Count)
                return false;

            typesA.Sort((x, y) => string.CompareOrdinal(x.FullName, y.FullName));
            typesB.Sort((x, y) => string.CompareOrdinal(x.FullName, y.FullName));

            for (int i = 0; i < typesA.Count; i++)
            {
                if (typesA[i] != typesB[i])
                    return false;
            }

            return true;
        }
        public static bool ShiftInUse
        {
            get
            {
                if (!Main.keyState.PressingShift())
                    return false;
                return true;
            }
        }

        /// <summary>将指定槽位的物品移动到玩家背包（类似原版 cursorOverride == 8）</summary>
        private bool MoveItemToPlayerInventory(int slotIndex)
        {
            Item item = _inputSlots[slotIndex].Item;
            if (item.IsAir) return false;

            Player player = Main.LocalPlayer;
            // 尝试将物品放入玩家背包（自动堆叠、处理剩余）
            Item result = player.GetItem(player.whoAmI, item, GetItemSettings.InventoryEntityToPlayerInventorySettings);

            // 如果 result 为空，说明物品全部转移成功
            if (result.IsAir)
            {
                _inputSlots[slotIndex].Item = result;
                return true;
            }
            else
            {
                // 部分转移（例如背包空间不足），更新槽位中的剩余数量
                _inputSlots[slotIndex].Item = result;
                return result.stack < item.stack; // 只要有部分移动就算成功
            }
        }

        #endregion

        #region 收集所有同类型物品（修复堆叠上限）

        /// <summary>
        /// 收集所有完全相同的物品（type + prefix + ModItem）
        /// </summary>
        private void CollectAllSameItems(Item targetTemplate)
        {
            if (targetTemplate == null || targetTemplate.IsAir) return;

            Item mouseItem = Main.mouseItem;

            // 鼠标已有物品但不一致 → 不允许收集
            if (!mouseItem.IsAir && !IsSameItem(mouseItem, targetTemplate))
                return;

            // 最大堆叠
            int maxStack = targetTemplate.maxStack;

            int originalStack = mouseItem.IsAir ? 0 : mouseItem.stack;
            int spaceLeft = maxStack - originalStack;
            if (spaceLeft <= 0) return;

            int collected = 0;

            foreach (var slot in _inputSlots)
            {
                if (IsSameItem(slot.Item, targetTemplate))
                {
                    int canTake = Math.Min(slot.Item.stack, spaceLeft - collected);
                    if (canTake <= 0) break;

                    collected += canTake;
                    slot.Item.stack -= canTake;

                    if (slot.Item.stack <= 0)
                        slot.Item.TurnToAir();

                    if (collected >= spaceLeft)
                        break;
                }
            }

            if (collected == 0) return;

            // 创建最终鼠标物品（完全复制模板）
            Item newMouseItem = targetTemplate.Clone();
            newMouseItem.stack = originalStack + collected;

            Main.mouseItem = newMouseItem;

            SoundEngine.PlaySound(SoundID.Grab);
        }

        #endregion
    }
}