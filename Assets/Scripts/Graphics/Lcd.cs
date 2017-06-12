
using System;
using UnityEngine;

namespace GB.Graphics
{
    public class Lcd
    {
        private Core core;

        private Memory memory;

        public long currentScanLine = 0;

        public bool LCDisOn = false;

        public bool LYCMatchTriggerSTAT = false;

        public long modeSTAT = 0;

        public bool mode0TriggerSTAT = false;

        public bool mode1TriggerSTAT = false;

        public bool mode2TriggerSTAT = false;

        public long STATTracker = 0;

        public Lcd(Core core)
        {
            this.core = core;
            this.memory = core.memory;
        }

        public void CheckIfLYCMatch()
        {
            // LY - LYC Compare
            // If LY==LCY
            if (memory.memory[0xFF44] == memory.memory[0xFF45])
            {
                memory.memory[0xFF41] |= 0x04; // set STAT bit 2: LY-LYC coincidence flag
                if (LYCMatchTriggerSTAT)
                    memory.memory[0xFF0F] |= 0x2; // set IF bit 1                
            }
            else
            {
                core.memory.memory[0xFF41] &= 0xFB; // reset STAT bit 2 (LY!=LYC)
            }
        }

        private void NotifyScanline()
        {
            if (currentScanLine == 0)
                core.screen.windowSourceLine = 0;

            // determine the left edge of the window (160 if window is inactive)
            long windowLeft = (core.screen.gfxWindowDisplay && core.memory.memory[0xFF4A] <= currentScanLine) ? Math.Min(160, core.memory.memory[0xFF4B] - 7) : 160;

            // step 1: background+window
            bool skippedAnything = core.screen.DrawBackgroundForLine(currentScanLine, windowLeft, 0);

            // At this point, the high (alpha) byte in the frameBuffer is 0xff for colors 1,2,3 and
            // 0x00 for color 0. Foreground sprites draw on all colors, background sprites draw on
            // top of color 0 only.
            // step 2: sprites
            core.screen.DrawSpritesForLine(currentScanLine);

            // step 3: prio tiles+windo
            if (skippedAnything)
                core.screen.DrawBackgroundForLine(currentScanLine, windowLeft, 0x80);

            if (windowLeft < 160)
                ++core.screen.windowSourceLine;
        }

        public void ScanLine(long line)
        {
            //When turned off = Do nothing!
            if (LCDisOn)
            {
                if (line < 143)
                {
                    //We're on a normal scan line:
                    if (core.LCDTicks < 20)
                        ScanLineMode2(); // mode2: 80 cycles                    
                    else if (core.LCDTicks < 63)
                        ScanLineMode3(); // mode3: 172 cycles                    
                    else if (core.LCDTicks < 114)
                        ScanLineMode0(); // mode0: 204 cycles                    
                    else
                    {
                        core.LCDTicks -= 114;
                        currentScanLine = ++memory.memory[0xFF44];
                        CheckIfLYCMatch();
                        if (STATTracker != 2)
                        {
                            if (core.hdmaRunning && !core.halt && LCDisOn)
                            {
                                //core.performHdma(); 
                                Debug.Log("not implemented"); //H-Blank DMA
                            }
                            if (mode0TriggerSTAT)
                                memory.memory[0xFF0F] |= 0x2; // set IF bit 1                            
                        }
                        STATTracker = 0;
                        ScanLineMode2();
                        if (core.LCDTicks >= 114)
                        {
                            core.NotifyScanline();
                            ScanLine(currentScanLine);
                        }
                    }
                }
                else if (line == 143)
                {
                    if (core.LCDTicks < 20)
                        ScanLineMode2();
                    else if (core.LCDTicks < 63)
                        ScanLineMode3();
                    else if (core.LCDTicks < 114)
                        ScanLineMode0();
                    else
                    {
                        //Starting V-Blank:
                        //Just finished the last visible scan line:
                        core.LCDTicks -= 114;
                        currentScanLine = ++memory.memory[0xFF44];
                        CheckIfLYCMatch();

                        if (mode1TriggerSTAT)
                            memory.memory[0xFF0F] |= 0x2; // set IF bit 1                        

                        if (STATTracker != 2)
                        {
                            if (core.hdmaRunning && !core.halt && LCDisOn)
                            {
                                //core.performHdma(); //H-Blank DMA
                                Debug.Log("not implemented");
                            }

                            if (mode0TriggerSTAT)
                                memory.memory[0xFF0F] |= 0x2; // set IF bit 1                            
                        }

                        STATTracker = 0;
                        modeSTAT = 1;
                        memory.memory[0xFF0F] |= 0x1;

                        if (core.screen.drewBlank > 0)
                            --core.screen.drewBlank;

                        if (core.LCDTicks >= 114)
                            ScanLine(currentScanLine);
                    }
                }
                else if (line < 153)
                {
                    //In VBlank
                    if (core.LCDTicks >= 114)
                    {
                        //We're on a new scan line:
                        core.LCDTicks -= 114;
                        currentScanLine = ++core.memory.memory[0xFF44];
                        CheckIfLYCMatch();
                        if (core.LCDTicks >= 114)
                            ScanLine(currentScanLine);                        
                    }
                }
                else
                {
                    //VBlank Ending (We're on the last actual scan line)
                    if (memory.memory[0xFF44] == 153)
                    {
                        memory.memory[0xFF44] = 0; //LY register resets to 0 early.
                        CheckIfLYCMatch(); //LY==LYC Test is early here (Fixes specific one-line glitches (example: Kirby2 intro)).
                    }
                    if (core.LCDTicks >= 114)
                    {
                        //We reset back to the beginning:
                        core.LCDTicks -= 114;
                        currentScanLine = 0;
                        ScanLineMode2(); // mode2: 80 cycles
                        if (core.LCDTicks >= 114)
                        {
                            //We need to skip 1 or more scan lines:
                            ScanLine(currentScanLine); //Scan Line and STAT Mode Control
                        }
                    }
                }
            }
        }

        public void ScanLineMode0()
        {
            // H-Blank
            if (modeSTAT != 0)
            {
                if (core.hdmaRunning && !core.halt && LCDisOn)
                {
                    //this.performHdma(); //H-Blank DMA
                    Debug.Log("not implemented");
                }

                if (mode0TriggerSTAT || (mode2TriggerSTAT && STATTracker == 0))
                    memory.memory[0xFF0F] |= 0x2; // if STAT bit 3 . set IF bit1  
                
                NotifyScanline();
                STATTracker = 2;
                modeSTAT = 0;
            }
        }

        public void ScanLineMode2()
        {
            if (modeSTAT != 2)
            {
                if (mode2TriggerSTAT)
                    memory.memory[0xFF0F] |= 0x2; // set IF bit 1				
                STATTracker = 1;
                modeSTAT = 2;
            }
        }

        public void ScanLineMode3()
        {
            if (modeSTAT != 3)
                if (mode2TriggerSTAT && STATTracker == 0)
                    memory.memory[0xFF0F] |= 0x2; // set IF bit 1				
            STATTracker = 1;
            modeSTAT = 3;
        }
    }
}