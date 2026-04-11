using System.ComponentModel;
using Terraria.ModLoader.Config;

namespace TerraCraft.Core.Configs
{
    public class TCGameplayConfig : ModConfig
    {
        public override ConfigScope Mode => ConfigScope.ServerSide;

        [Header("Gameplay")]

        [DefaultValue(false)]
        /// <summary>
        /// 完全启用新流程
        /// </summary>
        public float UseOverhaulGameplay;

    }
}