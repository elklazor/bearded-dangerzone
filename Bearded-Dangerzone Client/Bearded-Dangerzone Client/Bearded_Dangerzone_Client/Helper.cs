using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Bearded_Dangerzone_Client
{
    static class Helper
    {
        public static ScreenMode GameScreenMode = ScreenMode.Two;
        public static int Transform(int value)
        {
            return value * (int)GameScreenMode;
            
        }
        public static Rectangle TransformRectangle(Rectangle baseRectangle)
        {
            return new Rectangle(baseRectangle.X * (int)GameScreenMode, baseRectangle.Y * (int)GameScreenMode, baseRectangle.Width * (int)GameScreenMode, baseRectangle.Height * (int)GameScreenMode);
        }
        public static Vector2 TransformVector2(Vector2 baseVector)
        {
            return baseVector * (int)GameScreenMode;
        }
    }
}
