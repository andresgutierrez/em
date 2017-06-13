
namespace GB
{
    public class Settings
    {
        public static int audioGranularity = 20;

        public static bool autoFrameskip = true;

        public static bool colorize = false;

        public static int frameskipAmout = 0;

        public static int frameskipBaseFactor = 10;

        public static int frameskipMax = 29;

        public static int loopInterval = 17;

#if UNITY_EDITOR
        public static int machineCyclesPerLoop = 17826;
#else
        public static int machineCyclesPerLoop = (int)(17826 * 2.5);
#endif

        public static bool overrideMBC = true;

        public static bool priorizeGameBoyMode = true;
    }
}