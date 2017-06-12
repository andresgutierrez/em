
namespace GB
{
    public class Settings
    {
        //Audio granularity setting (Sampling of audio every x many machine cycles)
        public static int audioGranularity = 20;

        //Auto Frame Skip
        public static bool autoFrameskip = true;

        //Colorize GB mode?
        public static bool colorize = false;

        //Frameskip Amount (Auto frameskip setting allows the script to change this.)
        public static int frameskipAmout = 0;

        //Frameskip base factor
        public static int frameskipBaseFactor = 10;

        //Maximum Frame Skip
        public static int frameskipMax = 29;

        //Interval for the emulator loop.
        public static int loopInterval = 17;

        //Target number of machine cycles per loop. (4,194,300 / 1000 * 17)
#if UNITY_EDITOR
        public static int machineCyclesPerLoop = 17826;
#else
        public static int machineCyclesPerLoop = (int)(17826 * 2.5);
#endif

        //Override MBC RAM disabling and always allow reading and writing to the banks.
        public static bool overrideMBC = true;

        //Give priority to GameBoy mode
        public static bool priorizeGameBoyMode = true;
    }
}