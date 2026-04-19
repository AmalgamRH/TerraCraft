using System.Collections.Generic;
using TerraCraft.Core.Systems.GridCrafting;
using TerraCraft.Core.UI.GridCrafting;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ObjectData;

namespace TerraCraft.Core.Systems.Smelting
{
    public class SmeltingTileGlobal : GlobalTile
    {
        private WorkstationUIRegister UIRegister => ModContent.GetInstance<WorkstationUIRegister>();

        /// <summary>
        /// 判断该 tileType 是否在我们支持的合成台列表里
        /// </summary>
        private static bool IsSupportedSmeltingStation(int type)
            => SmeltingTileDataBase.IsSupported(type);

        public override void MouseOver(int i, int j, int type)
        {
            if (!IsSupportedSmeltingStation(type))
                return;

            int style = TileObjectData.GetTileStyle(Main.tile[i, j]);
            int itemID = TileLoader.GetItemDropFromTypeAndStyle(type, style);

            Player player = Main.LocalPlayer;
            player.cursorItemIconEnabled = true;
            player.cursorItemIconID = itemID;
            player.mouseInterface = true;
        }

        public override void RightClick(int i, int j, int type)
        {
            if (!IsSupportedSmeltingStation(type))
                return;

            int style = TileObjectData.GetTileStyle(Main.tile[i, j]);
            int itemID = TileLoader.GetItemDropFromTypeAndStyle(type, style);

            Main.playerInventory = true;
            UIRegister.OpenSmeltingUI(i, j, type, itemID);
        }

        public override void PlaceInWorld(int i, int j, int type, Item item)
        {
            if (!IsSupportedSmeltingStation(type)) return;

            // 找到多格物块的左上角
            TileObjectData data = TileObjectData.GetTileData(Main.tile[i, j]);
            int topLeftX = i;
            int topLeftY = j;

            if (data != null)
            {
                // 减去物块内偏移，得到真正的左上角
                topLeftX = i - Main.tile[i, j].TileFrameX / 18 % data.Width;
                topLeftY = j - Main.tile[i, j].TileFrameY / 18 % data.Height;
            }

            // 多人模式下客户端不直接 Place，发包给服务端处理
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                NetMessage.SendTileSquare(Main.myPlayer, topLeftX, topLeftY,
                    data?.Width ?? 1, data?.Height ?? 1);
                NetMessage.SendData(MessageID.TileEntityPlacement,
                    number: topLeftX, number2: topLeftY,
                    number3: ModContent.TileEntityType<TEFurnace>());
                return;
            }

            // 单人或服务端：直接放置 TE
            int id = ModContent.GetInstance<TEFurnace>().Place(topLeftX, topLeftY);

            // 拿到实例后写入 tileID
            if (TileEntity.ByID.TryGetValue(id, out TileEntity te) && te is TEFurnace furnace)
            {
                furnace.tileID = type; // 传入触发放置的物块类型
                NetMessage.SendData(MessageID.TileEntitySharing,
                    number: id, number2: topLeftX, number3: topLeftY);
            }
        }
        public override void KillTile(int i, int j, int type, ref bool fail, ref bool effectOnly, ref bool noItem)
        {
            if (!IsSupportedSmeltingStation(type)) return;
            if (fail || effectOnly) return;

            if (TileEntity.ByPosition.TryGetValue(new Point16(i, j), out TileEntity te) && te is TEFurnace furnace)
            {
                furnace.Kill(i, j);
            }
        }
    }
}