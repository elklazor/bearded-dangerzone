using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Bearded_Dangerzone_Client
{
    static class Textures
    {
        private static Dictionary<string, Texture2D> textures = new Dictionary<string, Texture2D>();
        public static void Load(ContentManager content)
        {
            string[] sheets = new string[1] { "Sheet1" };
            foreach (var s in sheets)
            {
                textures.Add(s, content.Load<Texture2D>(s));
            }
            GameFont16 = content.Load<SpriteFont>("GameFont16");
            GameFont24 = content.Load<SpriteFont>("GameFont24");
            GameFont32 = content.Load<SpriteFont>("GameFont32");
            GameFont = GameFont16;
            
        }
        public static Texture2D GetTexture(string name)
        {
            return textures[name];
        }
        public static SpriteFont GameFont,GameFont16,GameFont24,GameFont32;
    }
    public enum ScreenMode
    { 
        One = 1,Two = 2,Three = 3
    }
}
