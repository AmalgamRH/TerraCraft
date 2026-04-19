using System.Collections.Generic;
using TerraCraft.Core.UI.GridCrafting;
using TerraCraft.Core.UI;
using Terraria.ID;
using Terraria;
using Terraria.Audio;

namespace TerraCraft.Core.Systems.Smelting
{
    internal class SmeltingInputHandler : CustomItemSlotInputHandler
    {
        private readonly TEFurnace _furnace;
        private readonly List<UICustomItemSlot> _materialSlots;
        private readonly UICustomItemSlot _fuelSlot;
        private readonly UICustomItemSlot _outputSlot;

        // 记录上一帧的 Item 引用，用于检测变化
        private Item[] _prevMaterials;
        private Item _prevFuel;
        private Item _prevOutput;

        public SmeltingInputHandler(
            List<UICustomItemSlot> allInputSlots,   // material + fuel，传给 base 处理交互
            List<UICustomItemSlot> materialSlots,
            UICustomItemSlot fuelSlot,
            UICustomItemSlot outputSlot,
            TEFurnace furnace)
            : base(allInputSlots, onChanged: null)
        {
            _furnace = furnace;
            _materialSlots = materialSlots;
            _fuelSlot = fuelSlot;
            _outputSlot = outputSlot;

            _prevMaterials = new Item[materialSlots.Count];
            for (int i = 0; i < materialSlots.Count; i++)
                _prevMaterials[i] = materialSlots[i].Item;
            _prevFuel = fuelSlot.Item;
            _prevOutput = outputSlot.Item;
        }

        public override void Update()
        {
            // 1. output槽拦截放入：鼠标手持物品时禁止交换，只允许空手取出
            HandleOutputSlot();

            // 2. base处理 material/fuel 的鼠标交互
            base.Update();

            // 3. 检测变化并同步到TE
            SyncToFurnace();
        }

        private void HandleOutputSlot()
        {
            if (_outputSlot == null) return;

            var rect = _outputSlot.GetInnerDimensions().ToRectangle();
            if (!rect.Contains(Main.MouseScreen.ToPoint())) return;

            Main.LocalPlayer.mouseInterface = true;

            // 手持有物品时直接拦截，不允许放入/交换
            if (!Main.mouseItem.IsAir) return;

            // 空手 + 左键：取出 output
            if (Main.mouseLeftRelease && !_outputSlot.Item.IsAir)
            {
                Main.mouseItem = _outputSlot.Item.Clone();
                _outputSlot.Item.TurnToAir();
                SoundEngine.PlaySound(SoundID.Grab);
            }
        }

        private void SyncToFurnace()
        {
            if (_furnace == null) return;

            bool dirty = false;

            // 同步 material
            for (int i = 0; i < _materialSlots.Count && i < _furnace.material.Length; i++)
            {
                if (_materialSlots[i].Item != _prevMaterials[i])
                {
                    _furnace.material[i] = _materialSlots[i].Item;
                    _prevMaterials[i] = _materialSlots[i].Item;
                    dirty = true;
                }
            }

            // 同步 fuel
            if (_fuelSlot.Item != _prevFuel)
            {
                _furnace.fuel = _fuelSlot.Item;
                _prevFuel = _fuelSlot.Item;
                dirty = true;
            }

            // 同步 output（玩家取出后通知TE）
            if (_outputSlot.Item != _prevOutput)
            {
                _furnace.output = _outputSlot.Item;
                _prevOutput = _outputSlot.Item;
                dirty = true;
            }

            if (dirty)
                SendSync();
        }

        // TE → UI，由外部（UISmeltingPanel.Update）调用
        public void SyncFromFurnace()
        {
            if (_furnace == null) return;

            // 只在 output 不被玩家操作时（鼠标不在槽上）才刷新，避免闪烁
            var rect = _outputSlot.GetInnerDimensions().ToRectangle();
            bool mouseOnOutput = rect.Contains(Main.MouseScreen.ToPoint());

            if (!mouseOnOutput)
            {
                _outputSlot.Item = _furnace.output;
                _prevOutput = _furnace.output;
            }
        }

        private void SendSync()
        {
            if (Main.netMode != NetmodeID.MultiplayerClient) return;
            NetMessage.SendData(MessageID.TileEntitySharing,
                number: _furnace.ID,
                number2: _furnace.Position.X,
                number3: _furnace.Position.Y);
        }
    }
}