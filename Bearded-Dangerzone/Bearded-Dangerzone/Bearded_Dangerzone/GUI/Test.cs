using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Nuclex.Input;

namespace Bearded_Dangerzone.GUI
{
    
    class Test
    {
        InputManager inp;
        Game1 g;
        public Test()
        {
            g = new Game1();
            g.Components.Add(inp);
            inp = new InputManager(g.Window.Handle);
            inp.GetKeyboard().CharacterEntered += Test_CharacterEntered;
        }

        void Test_CharacterEntered(char character)
        {
            
        }

    }
}
