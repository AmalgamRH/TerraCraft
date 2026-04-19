using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using TerraCraft.Core.Systems.Smelting;
using Terraria;
using Terraria.UI;

namespace TerraCraft.Core.UI.GridCrafting
{
    internal class UISmeltingProgressBar : UIElement
    {
        public enum FillDirection { LeftToRight, BottomToTop }

        private readonly FillDirection _direction;
        private readonly Color _bgColor;
        private readonly Color _fgColor;

        private readonly Asset<Texture2D> _tex;
        private readonly Asset<Texture2D> _tex2;
        private readonly Rectangle _sourceRect;

        private Func<float> _ratioGetter;

        public UISmeltingProgressBar(
            string texPath, string texPath2 = null,
            float scale = 1f,
            FillDirection direction = FillDirection.LeftToRight,
            Color? bgColor = null,
            Color? fgColor = null)
        {
            _direction = direction;
            _bgColor = bgColor ?? new Color(47, 56, 106);
            _fgColor = fgColor ?? Color.White;

            _tex = TerraCraft.GetAsset2D(texPath, AssetRequestMode.ImmediateLoad);
            _tex2 = (texPath2 == null ? _tex : TerraCraft.GetAsset2D(texPath2, AssetRequestMode.ImmediateLoad));
            _sourceRect = new Rectangle(0, 0, _tex.Width(), _tex.Height());

            Width.Set(_sourceRect.Width * scale, 0f);
            Height.Set(_sourceRect.Height * scale, 0f);
            IgnoresMouseInteraction = true;
        }

        /// <summary>读取熔炼进度（0~1）</summary>
        public void SetFurnace(TEFurnace furnace)
            => _ratioGetter = () => furnace?.GetProgressRatio() ?? 0f;

        /// <summary>读取燃料剩余比例（0~1）</summary>
        public void SetFuelRatio(TEFurnace furnace)
            => _ratioGetter = () => furnace?.GetFuelRatio() ?? 0f;

        /// <summary>自定义比例来源</summary>
        public void SetRatioGetter(Func<float> getter)
            => _ratioGetter = getter;

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            var tex = _tex?.Value;
            var tex2 = _tex2?.Value;
            if (tex2 == null) tex2 = tex;
            if (tex == null) return;


            var dims = GetDimensions();
            var destFull = new Rectangle((int)dims.X, (int)dims.Y, (int)dims.Width, (int)dims.Height);

            spriteBatch.Draw(tex, destFull, _sourceRect, _bgColor);

            float ratio = Math.Clamp(_ratioGetter?.Invoke() ?? 0f, 0f, 1f);
            if (ratio <= 0f) return;

            Rectangle srcFg, destFg;
            if (_direction == FillDirection.LeftToRight)
            {
                int w = (int)(_sourceRect.Width * ratio);
                srcFg = new Rectangle(_sourceRect.X, _sourceRect.Y, w, _sourceRect.Height);
                destFg = new Rectangle(destFull.X, destFull.Y, (int)(dims.Width * ratio), destFull.Height);
            }
            else
            {
                int h = (int)(_sourceRect.Height * ratio);
                srcFg = new Rectangle(_sourceRect.X, _sourceRect.Bottom - h, _sourceRect.Width, h);
                destFg = new Rectangle(destFull.X, destFull.Bottom - (int)(dims.Height * ratio), destFull.Width, (int)(dims.Height * ratio));
            }

            spriteBatch.Draw(tex2, destFg, srcFg, _fgColor);
        }
    }
}