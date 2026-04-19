using System.Collections.Generic;
using TerraCraft.Core.UI.GridCrafting;
using TerraCraft.Core.UI;
using Terraria.ID;
using Terraria;
using Terraria.Audio;
using System;
using Terraria.ModLoader.IO;
using Terraria.ModLoader;
using System.Linq;

namespace TerraCraft.Core.Systems.Smelting
{
    internal class SmeltingInputHandler : CustomItemSlotInputHandler
    {
        private readonly TEFurnace _furnace;
        private readonly List<UICustomItemSlot> _materialSlots;
        private readonly UICustomItemSlot _fuelSlot;
        private readonly UICustomItemSlot _outputSlot;

        private Item[] _prevMaterials;
        private Item _prevFuel;
        private Item _prevOutput;

        // 记录上一帧鼠标左键状态
        private bool _lastMouseLeft;

        public SmeltingInputHandler(
            List<UICustomItemSlot> allInputSlots,
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
            HandleOutputSlot();
            base.Update();
            SyncToFurnace();

            // 更新上一帧状态
            _lastMouseLeft = Main.mouseLeft;
        }

        private void HandleOutputSlot()
        {
            if (_outputSlot == null || _outputSlot.Item.IsAir) return;

            var rect = _outputSlot.GetInnerDimensions().ToRectangle();
            if (!rect.Contains(Main.MouseScreen.ToPoint())) return;

            Main.LocalPlayer.mouseInterface = true;

            bool leftJustPressed = Main.mouseLeft && !_lastMouseLeft;
            if (!leftJustPressed) return;

            Item mouseItem = Main.mouseItem;
            Item slotItem = _outputSlot.Item;

            // 鼠标为空，直接拿走
            if (mouseItem.IsAir)
            {
                Main.mouseItem = slotItem.Clone();
                _outputSlot.Item.TurnToAir();
                SoundEngine.PlaySound(SoundID.Grab);
            }
            // 鼠标同类型且未满叠，合并
            else if (IsSameItem(mouseItem, slotItem))
            {
                int canAdd = Math.Min(slotItem.stack, mouseItem.maxStack - mouseItem.stack);
                mouseItem.stack += canAdd;
                slotItem.stack -= canAdd;
                if (slotItem.stack <= 0) _outputSlot.Item.TurnToAir();
                SoundEngine.PlaySound(SoundID.Grab);
            }
            // 鼠标有其他物品，不做交换（熔炉输出槽不允许放入）

            Main.mouseLeftRelease = false;
        }
        private void SyncToFurnace()
        {
            if (_furnace == null) return;
            bool dirty = false;

            for (int i = 0; i < _materialSlots.Count && i < _furnace.material.Length; i++)
            {
                if (_materialSlots[i].Item != _prevMaterials[i])
                {
                    _furnace.material[i] = _materialSlots[i].Item;
                    _prevMaterials[i] = _materialSlots[i].Item;
                    dirty = true;
                }
            }

            if (_fuelSlot.Item != _prevFuel)
            {
                _furnace.fuel = _fuelSlot.Item;
                _prevFuel = _fuelSlot.Item;
                dirty = true;
            }

            if (_outputSlot.Item != _prevOutput)
            {
                _furnace.output = _outputSlot.Item;
                _prevOutput = _outputSlot.Item;
                dirty = true;
            }

            if (dirty) SendSync();
        }

        public void SyncFromFurnace()
        {
            if (_furnace == null) return;

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