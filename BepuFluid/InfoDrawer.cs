using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace BepuFluid
{
    class InfoDrawer
    {
        private static SpriteFont _spriteFont;

        private bool _showFullInfo = false;

        public void ToggleFullInfo()
        {
            _showFullInfo = !_showFullInfo;
        }

        private const string BASIC_INFO = "Press F3 to toggle information";

        public void Draw(SpriteBatch spriteBatch, List<string> fullInfo)
        {
            Color fontColor = Color.Black;
            Vector2 fontPos = new Vector2(0, 0);
            Vector2 FontOrigin = Vector2.Zero;
            FontOrigin.X = FontOrigin.X > 0 ? FontOrigin.X : 0;

            spriteBatch.DrawString(_spriteFont, BASIC_INFO, fontPos, fontColor,
                          0, FontOrigin, 1f, SpriteEffects.None, 0.5f);

            if (_showFullInfo)
            {
                foreach (var info in fullInfo)
                {
                    fontPos.Y += 20;
                    spriteBatch.DrawString(_spriteFont, info, fontPos, fontColor,
                          0, FontOrigin, 1f, SpriteEffects.None, 0.5f);        
                }
            }
        }

        public InfoDrawer(ContentManager content)
        {
            _spriteFont = content.Load<SpriteFont>("fonts/Font");
        }
    }
}
