using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
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

        private VanillaItemSlotWrapper outputSlot;
        private List<VanillaItemSlotWrapper> inputSlots = new List<VanillaItemSlotWrapper>();
        private Player Player => Main.LocalPlayer;

        private GridCraftingMatcher _currentMatcher;
        private GriddedRecipe? _currentRecipe;
        private Dictionary<int, int> _currentConsumptions;
        private List<GridCraftingMatcher.ReplacementAction> _currentReplacements;

        // 输出槽交互
        private bool _wasMouseLeftPressed;
        private bool _wasMouseRightPressed;
        private bool _wasMouseOverOutputLastFrame;
        private int _craftRepeatTimer;
        private const int CraftRepeatDelay = 30;

        // 输入槽交互处理器（已分离）
        private GridCraftingInputHandler _inputHandler;

        private Item[] _lastGridItems;

        public void InitializeGrid(int tileId, int itemiconid)
        {
            BackgroundColor = new Color(63, 82, 151) * 0.8f;
            TileId = tileId;
            ItemIcon = itemiconid;
            (GridWidth, GridHeight) = CraftingStationSize.GetGridSize(tileId);
            RecreateSlots();

            // 初始化交互处理器，传入回调 RefreshMatching
            _inputHandler = new GridCraftingInputHandler(inputSlots, RefreshMatching);
        }

        private void RecreateSlots()
        {
            SetPadding(0);

            foreach (var slot in inputSlots)
                RemoveChild(slot);
            if (outputSlot != null)
                RemoveChild(outputSlot);
            inputSlots.Clear();

            const float spacing = 8f;
            const float padding = 16;
            const float outputSpacing = 48f;
            const float iconSpacing = 16f;

            Vector2 slotSize = new Vector2(44.2f);
            float actualSpacing = spacing;

            for (int y = 0; y < GridHeight; y++)
            {
                for (int x = 0; x < GridWidth; x++)
                {
                    var slot = new VanillaItemSlotWrapper(ItemSlot.Context.BankItem, 0.85f);
                    if (x == 0 && y == 0)
                    {
                        slotSize = slot.GetSize(true);
                        actualSpacing = slotSize.X + spacing;
                    }
                    slot.Left.Set(padding + x * actualSpacing, 0f);
                    slot.Top.Set(padding + y * actualSpacing, 0f);
                    inputSlots.Add(slot);
                    Append(slot);
                }
            }

            outputSlot = new VanillaItemSlotWrapper(4, 0.85f);
            float outputSlotHeight = outputSlot.Height.Pixels;
            float gridActualHeight = (GridHeight - 1) * actualSpacing + slotSize.Y;
            float outputLeft = padding + GridWidth * actualSpacing + outputSpacing;
            float outputTop = padding + (gridActualHeight - outputSlotHeight) / 2f;
            outputSlot.Left.Set(outputLeft, 0f);
            outputSlot.Top.Set(outputTop, 0f);
            Append(outputSlot);

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
            float arrowLeft = outputLeft - outputSpacing;
            float arrowTop = outputTop + (arrowRect.Height - outputSlot.Height.Pixels) / 2;
            arrow.SetSize(slotSize);
            arrow.Left.Set(arrowLeft, 0f);
            arrow.Top.Set(arrowTop, 0f);
            Append(arrow);

            if (ItemIcon > ItemID.None && TextureAssets.Item[ItemIcon] != null)
            {
                var iconTexture = TextureAssets.Item[ItemIcon];
                var craftstationIcon = new UIImageNeo(iconTexture)
                {
                    IgnoresMouseInteraction = true,
                    Color = Color.White * 0.8f
                };
                float iconLeft = outputLeft + Math.Abs(iconTexture.Width() - outputSlot.Width.Pixels) / 2;
                float iconTop = outputTop + outputSlot.Height.Pixels + iconSpacing;
                craftstationIcon.SetSize(slotSize);
                craftstationIcon.Left.Set(iconLeft, 0f);
                craftstationIcon.Top.Set(iconTop, 0f);
                Append(craftstationIcon);
            }

            float totalHeight = padding + gridActualHeight + padding;
            float totalWidth = outputLeft + outputSlot.Width.Pixels + padding;
            this.SetSize(totalWidth, totalHeight);
        }

        public override void Update(GameTime gameTime)
        {
            // 先让 InputHandler 处理输入槽交互并阻止原版干预
            _inputHandler?.Update();
            // 再处理输出槽
            HandleOutputSlotInteraction();
            // 最后调 base（原版 UI 系统此时 mouseLeftRelease 已被我们处理过）
            base.Update(gameTime);

            if (TileId == 0 || GridWidth == 0) return;
            RefreshMatching();
        }

        private bool HasGridChanged(Item[] currentGrid)
        {
            if (_lastGridItems == null || _lastGridItems.Length != currentGrid.Length)
            {
                _lastGridItems = currentGrid.Select(item => item?.Clone()).ToArray();
                return true;
            }

            for (int i = 0; i < currentGrid.Length; i++)
            {
                var cur = currentGrid[i];
                var last = _lastGridItems[i];
                if (cur == null && last != null) return SetAndReturnTrue(currentGrid);
                if (cur != null && last == null) return SetAndReturnTrue(currentGrid);
                if (cur != null && last != null && (cur.type != last.type || cur.stack != last.stack))
                    return SetAndReturnTrue(currentGrid);
            }
            return false;

            bool SetAndReturnTrue(Item[] items)
            {
                _lastGridItems = items.Select(item => item?.Clone()).ToArray();
                return true;
            }
        }

        private void RefreshMatching()
        {
            Item[] gridItems = inputSlots.Select(s => s.Item).ToArray();
            if (!HasGridChanged(gridItems)) return;

            _currentMatcher = new GridCraftingMatcher(TileId, GridWidth, GridHeight, gridItems);
            var match = _currentMatcher.Match();
            _currentRecipe = match.Recipe;
            _currentConsumptions = match.Consumptions;
            _currentReplacements = match.Replacements;

            if (_currentRecipe.HasValue && _currentRecipe.Value.Outputs?.Count > 0)
            {
                var output = _currentRecipe.Value.Outputs[0];
                outputSlot.Item.SetDefaults(output.ItemType);
                outputSlot.Item.stack = output.Amount;
            }
            else
            {
                outputSlot.Item.TurnToAir();
            }

            _lastGridItems = gridItems.Select(item => item?.Clone()).ToArray();
        }

        // ================= 输出槽交互 =================
        private void HandleOutputSlotInteraction()
        {
            bool leftDown = Main.mouseLeft;

            Rectangle outputRect = outputSlot.GetInnerDimensions().ToRectangle();
            bool mouseOverOutput = outputRect.Contains(Main.MouseScreen.ToPoint());

            if (!mouseOverOutput || PlayerInput.IgnoreMouseInterface)
            {
                _craftRepeatTimer = 0;
                _wasMouseOverOutputLastFrame = false;
                _wasMouseLeftPressed = leftDown; // 关键：离开时也要更新
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
                int amount = _currentRecipe.Value.Outputs[0].Amount;
                if (TryCraftAndGiveToMouse(amount))
                {
                    RefreshMatching();
                    Main.mouseLeftRelease = false;
                }
            }

            if (rightHeld && _currentRecipe.HasValue)
            {
                if (_craftRepeatTimer <= 0)
                {
                    if (TryCraftAndGiveToMouse(1))
                    {
                        RefreshMatching();
                        _craftRepeatTimer = CraftRepeatDelay;
                    }
                }
                else
                    _craftRepeatTimer--;
                Main.mouseRightRelease = false;
            }
            else
            {
                _craftRepeatTimer = 0;
            }

            _wasMouseLeftPressed = leftDown; // 统一在这里更新
            _wasMouseOverOutputLastFrame = true;
        }

        private void CraftAll()
        {
            while (_currentRecipe.HasValue && CanConsumeInputs())
            {
                if (!TryCraftAndGiveToMouse(_currentRecipe.Value.Outputs[0].Amount, noSound: true))
                    break;
            }
            SoundEngine.PlaySound(SoundID.Grab);
            RefreshMatching();
        }

        // ================= 合成核心逻辑 =================
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
                if (max > 0)
                    craftedItem.durability().EnableDurability(initial, max);
            }

            Item mouseItem = Main.mouseItem;
            if (mouseItem.IsAir)
            {
                Main.mouseItem = craftedItem.Clone();
            }
            else if (mouseItem.type == craftedItem.type && mouseItem.stack < mouseItem.maxStack)
            {
                int canAdd = Math.Min(craftedItem.stack, mouseItem.maxStack - mouseItem.stack);
                mouseItem.stack += canAdd;
                craftedItem.stack -= canAdd;
                if (craftedItem.stack > 0) return false;
            }
            else return false;

            PerformConsumption();
            if (!noSound)
            {
                SoundEngine.PlaySound(SoundID.Grab);
            }
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
            if (_currentReplacements != null)
            {
                foreach (var rep in _currentReplacements)
                {
                    if (rep.ReplaceWithItem.HasValue)
                    {
                        inputSlots[rep.SlotIndex].Item.SetDefaults(rep.ReplaceWithItem.Value);
                        inputSlots[rep.SlotIndex].Item.stack = rep.ReplaceAmount;
                    }
                    else inputSlots[rep.SlotIndex].Item.TurnToAir();
                }
            }
        }

        public override void OnDeactivate()
        {
            base.OnDeactivate();
            foreach (var slot in inputSlots)
            {
                if (!slot.Item.IsAir)
                {
                    Player.QuickSpawnItem(new EntitySource_OverfullInventory(Player, "MCWorkbench"), slot.Item, slot.Item.stack);
                    slot.Item.TurnToAir();
                }
            }
        }
    }
}