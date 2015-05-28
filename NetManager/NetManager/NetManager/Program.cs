using System;
using System.Linq;
namespace NetManager
{
#if WINDOWS || XBOX
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main(string[] args)
        {
            bool server = false;
            if (args.Length > 0)
                if (args.Contains("server"))
                    server = true;
            using (Game1 game = new Game1(server))
            {
                game.Run();
            }
        }
    }
#endif
}

