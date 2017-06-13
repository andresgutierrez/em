
using UnityEngine;

namespace GB.Input
{
    public class Keyboard
    {
        private Core core;

        public long JoyPad = 0xFF;

        public Keyboard(Core core)
        {
            this.core = core;
        }

        public void JoyPadEvent(KeyCode key, bool down)
        {
            int keyCode = 0;

            switch (key)
            {
                case KeyCode.RightArrow:
                    keyCode = 0;
                    break;
                case KeyCode.LeftArrow:
                    keyCode = 1;
                    break;
                case KeyCode.UpArrow:
                    keyCode = 2;
                    break;
                case KeyCode.DownArrow:
                    keyCode = 3;
                    break;
                case KeyCode.Z:
                    keyCode = 4;
                    break;
                case KeyCode.X:
                    keyCode = 5;
                    break;
                case KeyCode.RightShift:
                    keyCode = 6;
                    break;
                case KeyCode.Return:
                    keyCode = 7;
                    break;
            }

            if (down)
                JoyPad &= 0xFF ^ (1 << keyCode);
            else
                JoyPad |= (1 << keyCode);

            core.memory.memory[0xFF00] = (core.memory.memory[0xFF00] & 0x30) + ((((core.memory.memory[0xFF00] & 0x20) == 0) ? (JoyPad >> 4) : 0xF) & (((core.memory.memory[0xFF00] & 0x10) == 0) ? (JoyPad & 0xF) : 0xF));
        }
    }

}
