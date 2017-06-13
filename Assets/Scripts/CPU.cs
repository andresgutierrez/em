
namespace GB
{
    public class CPU
    {
        private Core core;

        public long registerA;

        public long registerB;

        public long registerC;

        public long registerD;

        public long registerE;

        public bool flagZero;

        public bool flagSubtract;

        public bool flagHalfCarry;

        public bool flagCarry;

        public long registersHL;

        public long programCounter;

        public long stackPointer;

        public CPU(Core core)
        {
            this.core = core;
        }

        public void Initialize()
        {
            programCounter = 0x100;
            stackPointer = 0xFFFE;
            registerA = core.cGBC ? 0x11 : 0x1;
            registerB = 0;
            registerC = 0x13;
            registerD = 0;
            registerE = 0xD8;
            flagZero = true;
            flagSubtract = false;
            flagHalfCarry = true;
            flagCarry = true;
            registersHL = 0x014D;
        }
    }
}