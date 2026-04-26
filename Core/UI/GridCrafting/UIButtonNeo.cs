using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.UI;

namespace TerraCraft.Core.UI.GridCrafting
{
    internal class UIButtonNeo : UIElement
    {
        private readonly Asset<Texture2D> _normalTex;
        private readonly Asset<Texture2D> _glowTex;
        public float Rotation;
        public SpriteEffects SpriteEffects;
        public bool Active;
        public UIButtonNeo(Asset<Texture2D> normalTex, Asset<Texture2D> glowTex, float rotation = 0f, SpriteEffects spriteEffects = 0f)
        {
            _normalTex = normalTex;
            _glowTex = glowTex;
            SpriteEffects = spriteEffects;
            Rotation = rotation;
            Width.Set(_normalTex.Width(), 0f);
            Height.Set(_normalTex.Height(), 0f);
        }

        public override void MouseOver(UIMouseEvent evt)
        {
            base.MouseOver(evt);
            SoundEngine.PlaySound(SoundID.MenuTick);
        }

        public override void MouseOut(UIMouseEvent evt)
        {
            base.MouseOut(evt);
        }
        public override void Update(GameTime gameTime)
        {
            if (GetDimensions().Width == 0)
            {
                Width.Set(_normalTex.Width(), 0f);
            }
            if (GetDimensions().Height == 0)
            {
                Height.Set(_normalTex.Width(), 0f);
            }
            base.Update(gameTime);
        }
        protected override void DrawSelf(SpriteBatch sb)
        {
            if (!Active) return;
            var dims = GetDimensions();
            // 以元素左上角为基准，居中绘制
            Vector2 center = new Vector2(dims.X + dims.Width / 2f, dims.Y + dims.Height / 2f);

            Vector2 origin = new Vector2(_normalTex.Value.Width / 2f, _normalTex.Value.Height / 2f);
            sb.Draw(_normalTex.Value, center, null, Color.White, Rotation, origin, 1f, SpriteEffects, 0f);
            if (IsMouseHovering)
            {
                sb.Draw(_glowTex.Value, center, null, Color.White, Rotation, origin, 1f, SpriteEffects, 0f);
            }
        }
    }
}