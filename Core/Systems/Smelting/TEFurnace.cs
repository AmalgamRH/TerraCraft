using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using Terraria.ObjectData;

namespace TerraCraft.Core.Systems.Smelting
{
    public class TEFurnace : ModTileEntity
    {
        public const int MAX_MATERIALS = 3;  // 最多3格原料，后面有需求再改

        /// <summary>烧炼进度</summary>
        public float progress;
        /// <summary>剩余燃料时间</summary>
        public int burnTime;
        /// <summary>绑定的TileID</summary>
        public int tileID;

        public Item[] material = new Item[MAX_MATERIALS];
        public Item fuel = new Item();
        public Item output = new Item();

        public TEFurnace()
        {
            for (int i = 0; i < MAX_MATERIALS; i++)
                material[i] = new Item();
        }

        public override void NetSend(BinaryWriter writer)
        {
            writer.Write(progress);
            writer.Write(tileID);

            for (int i = 0; i < MAX_MATERIALS; i++)
                ItemIO.Send(material[i], writer, writeStack: true);

            ItemIO.Send(fuel, writer, writeStack: true);
            ItemIO.Send(output, writer, writeStack: true);
        }

        public override void NetReceive(BinaryReader reader)
        {
            progress = reader.ReadSingle();
            tileID = reader.ReadInt32();

            for (int i = 0; i < MAX_MATERIALS; i++)
                material[i] = ItemIO.Receive(reader, readStack: true);

            fuel = ItemIO.Receive(reader, readStack: true);
            output = ItemIO.Receive(reader, readStack: true);
        }

        public override void SaveData(TagCompound tag)
        {
            tag["progress"] = progress;
            tag["tileID"] = tileID;

            var materialList = new TagCompound[MAX_MATERIALS];
            for (int i = 0; i < MAX_MATERIALS; i++)     // 用 ItemIO.Save 存储每个 Item
                materialList[i] = ItemIO.Save(material[i]);

            tag["material"] = materialList;
            tag["fuel"] = ItemIO.Save(fuel);
            tag["output"] = ItemIO.Save(output);
        }

        public override void LoadData(TagCompound tag)
        {
            progress = tag.GetFloat("progress");
            tileID = tag.GetInt("tileID");

            var materialList = tag.Get<TagCompound[]>("material");
            if (materialList != null)
            {
                for (int i = 0; i < MAX_MATERIALS && i < materialList.Length; i++)
                    material[i] = ItemIO.Load(materialList[i]);
            }

            var fuelTag = tag.Get<TagCompound>("fuel");
            if (fuelTag != null) fuel = ItemIO.Load(fuelTag);

            var outputTag = tag.Get<TagCompound>("output");
            if (outputTag != null) output = ItemIO.Load(outputTag);
        }

        public override void Update()
        {
            if (!IsTileValidForEntity(Position.X, Position.Y))
            {
                TerraCraft.Instance.Logger.Error($"[TEFurnace] 所在Tile不存在或者type非法。" +
                    $"HasTile：{Main.tile[Position.X, Position.Y].HasTile}，" +
                    $"TileType：{Main.tile[Position.X, Position.Y].TileType}(需要{tileID})");
                Kill(Position.X, Position.Y);
                return;
            }

            /*if (Main.netMode == NetmodeID.Server)
            {
                Console.WriteLine($"TE{this.Position}, Active!");
            }
            else
            {
                Main.NewText($"TE{this.Position}, Active!");
            }*/
        }

        public new void Kill(int x, int y)
        {
            DropContents();
            base.Kill(x, y);
        }
        public void DropContents()
        {
            // 掉落所有材料
            for (int i = 0; i < MAX_MATERIALS; i++)
            {
                if (material[i] != null && !material[i].IsAir)
                {
                    Item.NewItem(new EntitySource_TileBreak(Position.X, Position.Y),
                        Position.X * 16, Position.Y * 16, 16, 16, material[i].type,
                        material[i].stack, false, material[i].prefix);
                    material[i].TurnToAir();
                }
            }
            // 掉落燃料
            if (fuel != null && !fuel.IsAir)
            {
                Item.NewItem(new EntitySource_TileBreak(Position.X, Position.Y),
                    Position.X * 16, Position.Y * 16, 16, 16, fuel.type, fuel.stack, false, fuel.prefix);
                fuel.TurnToAir();
            }
            // 掉落产物
            if (output != null && !output.IsAir)
            {
                Item.NewItem(new EntitySource_TileBreak(Position.X, Position.Y),
                    Position.X * 16, Position.Y * 16, 16, 16, output.type, output.stack, false, output.prefix);
                output.TurnToAir();
            }
        }

        public override void OnNetPlace()
        {
            NetMessage.SendData(MessageID.TileEntitySharing, number: ID, number2: Position.X, number3: Position.Y);
        }

        public override int Hook_AfterPlacement(int i, int j, int type, int style, int direction, int alternate)
        {
            TileObjectData tileData = TileObjectData.GetTileData(type, style, alternate);
            int topLeftX = i - tileData.Origin.X;
            int topLeftY = j - tileData.Origin.Y;

            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                NetMessage.SendTileSquare(Main.myPlayer, topLeftX, topLeftY, tileData.Width, tileData.Height);
                NetMessage.SendData(MessageID.TileEntityPlacement, number: topLeftX, number2: topLeftY, number3: Type);
                return -1;
            }

            return Place(topLeftX, topLeftY);
        }

        public override void NetPlaceEntityAttempt(int x, int y)
        {
            int ID = Place(x, y);
            NetMessage.SendData(MessageID.TileEntitySharing, number: ID, number2: x, number3: y);
        }

        public new int Place(int i, int j)
        {
            int ID = base.Place(i, j);
            return ID;
        }

        public override bool IsTileValidForEntity(int x, int y)
        {
            TileObjectData tileData = TileObjectData.GetTileData(Main.tile[x, y]);
            return Main.tile[x, y].HasTile && Main.tile[x, y].TileType == tileID;
        }
    }
}
