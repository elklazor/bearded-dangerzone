using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Bearded_Dangerzone.GUI
{
    public abstract class InputBox: IGUIComponent
    {
        public virtual void Draw(SpriteBatch spriteBatch)
        { }
        public virtual void Update(GameTime gameTime)
        { }
        public string Text { get; set; }
        public bool IsActive { get; set; }
        private int maxLength;
        public int MaxLength { get { return maxLength; } protected set { maxLength = value; } }
    }
}
