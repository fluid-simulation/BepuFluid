using System;

namespace BepuFluid
{
#if WINDOWS || XBOX
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main(string[] args)
        {
            using (BepuFluidGame game = new BepuFluidGame())
            {
                game.Run();
            }
        }
    }
#endif
}

