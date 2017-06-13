
namespace GB
{
    public class Utils
    {
        public static long Usbtsb(long ubyte)
        {
            //Unsigned byte to signed byte:
            return (ubyte > 0x7F) ? ((ubyte & 0x7F) - 0x80) : ubyte;
        }

        public static long Unswtuw(long uword)
        {
            //Keep an unsigned word unsigned:
            if (uword < 0)
                uword += 0x10000;
            return uword; //If this function is called, no wrapping requested.
        }

        public static long Unsbtub(long ubyte)
        {
            //Keep an unsigned byte unsigned:
            if (ubyte < 0)
                ubyte += 0x100;
            return ubyte; //If this function is called, no wrapping requested.
        }

        public static long Nswtuw(long uword)
        {
            //Keep an unsigned word unsigned:
            if (uword < 0)
                uword += 0x10000;
            return uword & 0xFFFF; //Wrap also...
        }
    }
}