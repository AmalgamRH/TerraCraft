using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace TerraCraft.Core.VanillaExt
{
    public class CopperToolModify : GlobalItem
    {
        public override void SetDefaults(Item item)
        {
            base.SetDefaults(item);

            switch (item.type)
            {
                case ItemID.CopperAxe: 
                case ItemID.CopperAxeOld:
                    item.tileBoost = 0;
                    item.scale = 1;
                    break;

                case ItemID.CopperPickaxe:
                case ItemID.CopperPickaxeOld:
                    item.tileBoost = 0;
                    item.scale = 1;
                    break;
                case ItemID.CactusPickaxe:
                    item.pick = 20;
                    break;
            }
        }
        public override void UpdateInventory(Item item, Player player)
        {
            int maxpick = player.GetModPlayer<MaxPickPowerPlayer>().MaxPickPowerEver;
            if (!item.IsAir && item.pick > maxpick)
            {
                player.GetModPlayer<MaxPickPowerPlayer>().SetMaxPowerEver(item.pick);
            }
        }
    }
    public class MaxPickPowerPlayer : ModPlayer
    {
        private int _maxPickPowerEver = 0;
        public int MaxPickPowerEver => _maxPickPowerEver;
        public void SetMaxPowerEver(int pick)
        {
            int currentMaxPick = pick;
            if (currentMaxPick > _maxPickPowerEver)
            {
                _maxPickPowerEver = currentMaxPick;
            }
        }

        public override void SaveData(TagCompound tag)
        {
            tag["maxPickPower"] = _maxPickPowerEver;
        }
        public override void LoadData(TagCompound tag)
        {
            _maxPickPowerEver = tag.GetInt("maxPickPower");
        }

        /*public override void SyncPlayer(int toWho, int fromWho, bool newPlayer)
        {
            ModPacket packet = Mod.GetPacket();
            packet.Write((byte)PlayerMessageType.MaxPickPower);
            packet.Write(_maxPickPowerEver);
            packet.Send(toWho, fromWho);
        }*/
    }
}