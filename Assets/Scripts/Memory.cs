using System;
using UnityEngine;

namespace GB
{
    public class Memory
    {
        private Core core;

        private byte[] rom;

        private byte[] rawRom;

        public long[] memory;

        private long[] GBCMemory;

        private long currentROMBank;

        private long ROMBank1offs;

        private long numRAMBanks = 0;

        private bool MBCRAMBanksEnabled;

        private long currMBCRAMBank;

        private long currMBCRAMBankPosition = -0xA000;

        private long currVRAMBank;

        private bool MBC1Mode;

        public long[] MBCRam;

        private long gbcRamBankPosition = -0xD000;

        private long gbcRamBankPositionECHO = -0xF000;

        public Memory(Core core, byte[] assetBytes)
        {
            rawRom = assetBytes;
            this.core = core;
        }

        public void InitMemory()
        {
            memory = new long[0x10000];
        }

        public void InitRAM()
        {
            if (core.cMBC2)
                numRAMBanks = 1 / 16;
            else if (core.cMBC1 || core.cRUMBLE || core.cMBC3 || core.cHuC3)
                numRAMBanks = 4;
            else if (core.cMBC5)
                numRAMBanks = 16;
            else if (core.cSRAM)
                numRAMBanks = 1;

            MBCRam = new long[numRAMBanks * 0x2000];
            Debug.Log("MBC RAM allocated=" + MBCRam.Length);

            if (core.cGBC)
            {
                core.VRAM = new long[0x2000];
                GBCMemory = new long[0x7000];
            }
        }

        public byte[] LoadROM()
        {
            rom = new byte[rawRom.Length];
            for (int i = 0; i < rawRom.Length; i++)
            {
                rom[i] = (byte)(rawRom[i] & 0xFF);
                if (i < 0x8000)
                    memory[i] = rom[i]; //Load in the game ROM.                
            }

            return rom;
        }

        public long RawRead(long address)
        {
            return memory[address];
        }

        public void RawWrite(long address, long data)
        {
            memory[address] = data;
        }

        private void SetCurrentMBC1ROMBank()
        {
            switch (ROMBank1offs)
            {
                case 0x00:
                case 0x20:
                case 0x40:
                case 0x60:
                    currentROMBank = ROMBank1offs * 0x4000;
                    break;
                default:
                    currentROMBank = (ROMBank1offs - 1) * 0x4000;
                    break;
            }

            while (currentROMBank + 0x4000 >= rom.Length)
                currentROMBank -= rom.Length;
        }

        private void SetCurrentMBC2AND3ROMBank()
        {
            currentROMBank = (long)Mathf.Max(ROMBank1offs - 1, 0) * 0x4000;
            while (currentROMBank + 0x4000 >= rom.Length)
                currentROMBank -= (long)rom.Length;
        }

        private void SetCurrentMBC5ROMBank()
        {
            currentROMBank = (ROMBank1offs - 1) * 0x4000;
            while (currentROMBank + 0x4000 >= rom.Length)
                currentROMBank -= (long)rom.Length;
        }

        public void Write(long address, long data)
        {
            if (address < 0x8000)
            {
                if (core.cMBC1)
                {
                    if (address < 0x2000)
                        MBCRAMBanksEnabled = ((data & 0x0F) == 0x0A);
                    else if (address < 0x4000)
                    {
                        ROMBank1offs = (ROMBank1offs & 0x60) | (data & 0x1F);
                        SetCurrentMBC1ROMBank();
                    }
                    else if (address < 0x6000)
                    {
                        if (MBC1Mode)
                        {
                            //4/32 Mode
                            currMBCRAMBank = data & 0x3;
                            currMBCRAMBankPosition = (currMBCRAMBank << 13) - 0xA000;
                        }
                        else
                        {
                            //16/8 Mode
                            ROMBank1offs = ((data & 0x03) << 5) | (ROMBank1offs & 0x1F);
                            SetCurrentMBC1ROMBank();
                        }
                    }
                    else
                    {
                        //MBC1WriteType
                        //MBC1 mode setting:
                        MBC1Mode = ((data & 0x1) == 0x1);
                    }
                }
                else if (core.cMBC2)
                {
                    if (address < 0x1000) // cMBC2 mode
                    {
                        //MBC RAM Bank Enable/Disable:
                        MBCRAMBanksEnabled = ((data & 0x0F) == 0x0A); //If lower nibble is 0x0A, then enable, otherwise disable.
                    }
                    else if (address >= 0x2100 && address < 0x2200)
                    {
                        ROMBank1offs = data & 0x0F;
                        SetCurrentMBC2AND3ROMBank();
                    }
                }
                else if (core.cMBC3)
                {
                    if (address < 0x2000)
                    {
                        MBCRAMBanksEnabled = ((data & 0x0F) == 0x0A); //If lower nibble is 0x0A, then enable, otherwise disable.
                    }
                    else if (address < 0x4000)
                    {
                        ROMBank1offs = data & 0x7F;
                        SetCurrentMBC2AND3ROMBank();
                    }
                    else if (address < 0x6000)
                    {
                        //MBC3WriteRAMBank
                        currMBCRAMBank = data;
                        if (data < 4) //MBC3 RAM bank switching                     
                            currMBCRAMBankPosition = ((int)currMBCRAMBank << 13) - 0xA000;
                    }
                    else
                    {
                        core.clock.CheckLatched(data);
                    }
                }
                else if (core.cMBC5 || core.cRUMBLE)
                {
                    // cMBC5 || cRUMBLE mode
                    Debug.Log("not implemented");
                }
                else if (core.cHuC3)
                {
                    // cHuC3
                    Debug.Log("not implemented");
                }
            }
            else if (address < 0xA000)
            {
                //VRAM cannot be written to during mode 3
                if (core.lcd.modeSTAT < 3)
                {
                    // Bkg Tile data area
                    if (address < 0x9800)
                        core.screen.ChangeTileDataArea(address, currVRAMBank);

                    if (currVRAMBank == 0)
                        memory[address] = data;
                    else
                        core.VRAM[address - 0x8000] = data;
                }
            }
            else if (address < 0xC000)
            {
                if ((numRAMBanks == 1 / 16 && address < 0xA200) || numRAMBanks >= 1)
                {
                    if (!core.cMBC3)
                    {
                        //memoryWriteMBCRAM
                        if (MBCRAMBanksEnabled || Settings.overrideMBC)
                            MBCRam[address + currMBCRAMBankPosition] = data;
                    }
                    else
                    {
                        //MBC3 RTC + RAM:
                        //memoryWriteMBC3RAM
                        if (MBCRAMBanksEnabled || Settings.overrideMBC)
                            core.clock.SetClock(address, data, currMBCRAMBank, currMBCRAMBankPosition);
                    }
                }
            }
            else if (address < 0xE000)
            {
                if (core.cGBC && address >= 0xD000) //memoryWriteGBCRAM              
                    GBCMemory[address + gbcRamBankPosition] = data;
                else
                    memory[address] = data; //memoryWriteNormal             
            }
            else if (address < 0xFE00)
            {
                if (core.cGBC && address >= 0xF000)  //memoryWriteECHOGBCRAM             
                    GBCMemory[address + gbcRamBankPositionECHO] = data;
                else
                    memory[address - 0x2000] = data; // //memoryWriteECHONormal             
            }
            else if (address <= 0xFEA0)
            {
                //memoryWriteOAMRAM
                if (core.lcd.modeSTAT < 2)
                    memory[address] = data;
            }
            else if (address < 0xFF00)
            {
                if (core.cGBC)
                    memory[address] = data; //memoryWriteNormal
            }
            else if (address == 0xFF00) // Keyboard
                memory[0xFF00] = ((data & 0x30) | ((((data & 0x20) == 0) ? (core.keyboard.JoyPad >> 4) : 0xF) & (((data & 0x10) == 0) ? (core.keyboard.JoyPad & 0xF) : 0xF)));
            else if (address == 0xFF02)
            {
                // Clocks
                if (((data & 0x1) == 0x1))
                {
                    //Internal
                    memory[0xFF02] = (data & 0x7F);
                    memory[0xFF0F] |= 0x8;
                }
                else
                {
                    //External
                    memory[0xFF02] = data;
                }
            }
            else if (address == 0xFF04)
                memory[0xFF04] = 0;
            else if (address == 0xFF07)
            {
                memory[0xFF07] = data & 0x07;
                core.TIMAEnabled = (data & 0x04) == 0x04;
                core.TACClocker = (int)Mathf.Pow(4, ((data & 0x3) != 0) ? (data & 0x3) : 4);
            }
            else if (address == 0xFF40)
            {
                if (core.cGBC)
                {
                    bool active = (data & 0x80) == 0x80;
                    if (active != core.lcd.LCDisOn)
                    {
                        core.lcd.LCDisOn = active;
                        memory[0xFF41] &= 0xF8;
                        core.lcd.STATTracker = core.lcd.modeSTAT = core.LCDTicks = core.lcd.currentScanLine = memory[0xFF44] = 0;
                        if (core.lcd.LCDisOn)
                            core.lcd.CheckIfLYCMatch();
                        else
                            core.screen.DisplayShowOff();
                        memory[0xFF0F] &= 0xFD;
                    }
                    core.screen.gfxWindowY = (data & 0x40) == 0x40;
                    core.screen.gfxWindowDisplay = (data & 0x20) == 0x20;
                    core.screen.gfxBackgroundX = (data & 0x10) == 0x10;
                    core.screen.gfxBackgroundY = (data & 0x08) == 0x08;
                    core.screen.gfxSpriteDouble = (data & 0x04) == 0x04;
                    core.screen.gfxSpriteShow = (data & 0x02) == 0x02;
                    core.screen.spritePriorityEnabled = (data & 0x01) == 0x01;
                    memory[0xFF40] = data;
                }
                else
                {
                    bool active = (data & 0x80) == 0x80;
                    if (active != core.lcd.LCDisOn)
                    {
                        core.lcd.LCDisOn = active;
                        memory[0xFF41] &= 0xF8;
                        memory[0xFF44] = 0;
                        core.lcd.currentScanLine = 0;
                        core.lcd.STATTracker = core.lcd.modeSTAT = core.LCDTicks = 0;
                        if (core.lcd.LCDisOn)
                            core.lcd.CheckIfLYCMatch();
                        else
                            core.screen.DisplayShowOff();
                        memory[0xFF0F] &= 0xFD;
                    }
                    core.screen.gfxWindowY = (data & 0x40) == 0x40;
                    core.screen.gfxWindowDisplay = (data & 0x20) == 0x20;
                    core.screen.gfxBackgroundX = (data & 0x10) == 0x10;
                    core.screen.gfxBackgroundY = (data & 0x08) == 0x08;
                    core.screen.gfxSpriteDouble = (data & 0x04) == 0x04;
                    core.screen.gfxSpriteShow = (data & 0x02) == 0x02;
                    if ((data & 0x01) == 0)
                    {
                        core.bgEnabled = false;
                        core.screen.gfxWindowDisplay = false;
                    }
                    else
                    {
                        core.bgEnabled = true;
                    }
                    memory[0xFF40] = data;
                }
            }
            else if (address == 0xFF41)
            {
                if (core.cGBC)
                {
                    core.lcd.LYCMatchTriggerSTAT = ((data & 0x40) == 0x40);
                    core.lcd.mode2TriggerSTAT = ((data & 0x20) == 0x20);
                    core.lcd.mode1TriggerSTAT = ((data & 0x10) == 0x10);
                    core.lcd.mode0TriggerSTAT = ((data & 0x08) == 0x08);
                    memory[0xFF41] = (data & 0xF8);
                }
                else
                {
                    core.lcd.LYCMatchTriggerSTAT = ((data & 0x40) == 0x40);
                    core.lcd.mode2TriggerSTAT = ((data & 0x20) == 0x20);
                    core.lcd.mode1TriggerSTAT = ((data & 0x10) == 0x10);
                    core.lcd.mode0TriggerSTAT = ((data & 0x08) == 0x08);
                    memory[0xFF41] = (data & 0xF8);
                    if (core.lcd.LCDisOn && core.lcd.modeSTAT < 2)
                        memory[0xFF0F] |= 0x2;
                }
            }
            else if (address == 0xFF45)
            {
                memory[0xFF45] = data;
                if (core.lcd.LCDisOn)
                    core.lcd.CheckIfLYCMatch();
            }
            else if (address == 0xFF46)
            {
                memory[0xFF46] = data;
                if (core.cGBC || data > 0x7F)
                {
                    data <<= 8;
                    address = 0xFE00;
                    while (address < 0xFEA0)
                        memory[address++] = Read(data++);
                }
            }
            else if (address == 0xFF47)
            {
                core.screen.DecodePalette(0, data);
                if (memory[0xFF47] != data)
                {
                    memory[0xFF47] = data;
                    core.screen.InvalidateAll(0);
                }
            }
            else if (address == 0xFF48)
            {
                core.screen.DecodePalette(4, data);
                if (memory[0xFF48] != data)
                {
                    memory[0xFF48] = data;
                    core.screen.InvalidateAll(1);
                }
            }
            else if (address == 0xFF49)
            {
                core.screen.DecodePalette(8, data);
                if (memory[0xFF49] != data)
                {
                    memory[0xFF49] = data;
                    core.screen.InvalidateAll(2);
                }
            }
            else if (address == 0xFF4D)
            {
                if (core.cGBC)
                    memory[0xFF4D] = (data & 0x7F) + (memory[0xFF4D] & 0x80);
                else
                    memory[0xFF4D] = data;
            }
            else if (address == 0xFF4F)
            {
                if (core.cGBC)
                    currVRAMBank = data & 0x01;
            }
            else if (address == 0xFF50)
            {
                if (core.inBootstrap)
                {
                    core.inBootstrap = false;
                    //disableBootROM(); 
                    memory[0xFF50] = data;
                    Debug.Log("NOT IMPLEMENTED");
                }
            }
            else if (address == 0xFF51)
            {
                if (core.cGBC)
                {
                    if (!core.hdmaRunning)
                        memory[0xFF51] = data;
                }
            }
            else if (address == 0xFF52)
            {
                if (core.cGBC)
                {
                    if (!core.hdmaRunning)
                        memory[0xFF52] = data & 0xF0;
                }
            }
            else if (address == 0xFF53)
            {
                if (core.cGBC)
                {
                    if (!core.hdmaRunning)
                        memory[0xFF53] = data & 0x1F;
                }
            }
            else if (address == 0xFF54)
            {
                if (core.cGBC)
                {
                    if (!core.hdmaRunning)
                        memory[0xFF54] = data & 0xF0;
                }
            }
            else if (address == 0xFF55)
            {
                if (core.cGBC)
                {
                    if (!core.hdmaRunning)
                    {
                        if ((data & 0x80) == 0)
                        {
                            Debug.Log("not implemented");
                        }
                        else
                        {
                            //H-Blank DMA
                            if (data > 0x80)
                            {
                                core.hdmaRunning = true;
                                memory[0xFF55] = data & 0x7F;
                            }
                            else
                            {
                                memory[0xFF55] = 0xFF;
                            }
                        }
                    }
                    else if ((data & 0x80) == 0) //Stop H-Blank DMA
                    {
                        core.hdmaRunning = false;
                        memory[0xFF55] |= 0x80;
                    }
                }
                else
                {
                    memory[0xFF55] = data;
                }
            }
            else if (address == 0xFF68)
            {
                if (core.cGBC)
                {
                    memory[0xFF69] = (long)(0xFF & core.screen.gbcRawPalette[data & 0x3F]);
                    memory[0xFF68] = data;
                }
                else
                {
                    memory[0xFF68] = data;
                }
            }
            else if (address == 0xFF69)
            {
                if (core.cGBC)
                {
                    Debug.Log("not implemented");
                }
                else
                {
                    memory[0xFF69] = data;
                }
            }
            else if (address == 0xFF6A)
            {
                if (core.cGBC)
                {
                    memory[0xFF6B] = 0xFF & core.screen.gbcRawPalette[(data & 0x3F) | 0x40];
                    memory[0xFF6A] = data;
                }
                else
                    memory[0xFF6A] = data;
            }
            else if (address == 0xFF6B)
            {
                if (core.cGBC)
                    Debug.Log("not implemented");
                else
                    memory[0xFF6B] = data;
            }
            else if (address == 0xFF6C)
            {
                if (core.inBootstrap)
                {
                    if (core.inBootstrap)
                    {
                        core.cGBC = (data == 0x80);
                        Debug.Log("Booted to GBC mode");
                    }
                    memory[0xFF6C] = data;
                }
            }
            else if (address == 0xFF70)
            {
                if (core.cGBC)
                {
                    long addressCheck = (memory[0xFF51] << 8) | memory[0xFF52];
                    if (!core.hdmaRunning || addressCheck < 0xD000 || addressCheck >= 0xE000)
                    {
                        long gbcRamBank = Math.Max(data & 0x07, 1);
                        gbcRamBankPosition = ((gbcRamBank - 1) * 0x1000) - 0xD000;
                        gbcRamBankPositionECHO = ((gbcRamBank - 1) * 0x1000) - 0xF000;
                    }
                    memory[0xFF70] = (data | 0x40);
                }
                else
                    memory[0xFF70] = data;
            }
            else
            {
                memory[address] = data;
            }
        }

        public long Read(long address)
        {
            if (address < 0x4000)
                return memory[address];

            if (address < 0x8000)
                return rom[currentROMBank + address];

            if (address >= 0x8000 && address < 0xA000)
            {
                if (core.cGBC)
                    return (core.lcd.modeSTAT > 2) ? 0xFF : ((currVRAMBank == 0) ? memory[address] : core.VRAM[address - 0x8000]);

                return (core.lcd.modeSTAT > 2) ? 0xFF : memory[address];
            }

            if (address >= 0xA000 && address < 0xC000)
            {
                if ((numRAMBanks == 1 / 16 && address < 0xA200) || numRAMBanks >= 1)
                {
                    if (!core.cMBC3)
                    {
                        if (MBCRAMBanksEnabled || Settings.overrideMBC)
                            return MBCRam[address + currMBCRAMBankPosition];

                        //cout("Reading from disabled RAM.", 1);
                        return 0xFF;
                    }
                    else
                    {
                        if (MBCRAMBanksEnabled || Settings.overrideMBC)
                            return core.clock.GetTime(currMBCRAMBank, currMBCRAMBankPosition, address);

                        //cout("Reading from invalid or disabled RAM.", 1);
                        return 0xFF;
                    }
                }
                else
                {
                    return 0xFF;
                }
            }

            if (address >= 0xC000 && address < 0xE000)
            {
                if (!core.cGBC || address < 0xD000)
                    return memory[address];
                return GBCMemory[address + gbcRamBankPosition]; //memoryReadGBCMemory               
            }

            if (address >= 0xE000 && address < 0xFE00)
            {
                if (!core.cGBC || address < 0xF000)
                    return memory[address - 0x2000]; //memoryReadECHONormal                             
                return GBCMemory[address + gbcRamBankPositionECHO]; //memoryReadECHOGBCMemory
            }

            if (address < 0xFEA0)
            {
                //memoryReadOAM
                return core.lcd.modeSTAT > 1 ? 0xFF : memory[address];
            }

            if (core.cGBC && address >= 0xFEA0 && address < 0xFF00)
            {
                //memoryReadNormal
                return memory[address];
            }

            if (address >= 0xFF00)
            {
                switch (address)
                {
                    case 0xFF00:
                        return 0xC0 | memory[0xFF00]; //Top nibble returns as set.
                    case 0xFF01:
                        return ((memory[0xFF02] & 0x1) == 0x1) ? 0xFF : memory[0xFF01];
                    case 0xFF02:
                        if (core.cGBC)
                            return 0x7C | memory[0xFF02];
                        return 0x7E | memory[0xFF02];
                    case 0xFF07:
                        return 0xF8 | memory[0xFF07];
                    case 0xFF0F:
                        return 0xE0 | memory[0xFF0F];
                    case 0xFF10:
                        return 0x80 | memory[0xFF10];
                    case 0xFF11:
                        return 0x3F | memory[0xFF11];
                    case 0xFF14:
                        return 0xBF | memory[0xFF14];
                    case 0xFF16:
                        return 0x3F | memory[0xFF16];
                    case 0xFF19:
                        return 0xBF | memory[0xFF19];
                    case 0xFF1A:
                        return 0x7F | memory[0xFF1A];
                    case 0xFF1B:
                        return 0xFF;
                    case 0xFF1C:
                        return 0x9F | memory[0xFF1C];
                    case 0xFF1E:
                        return 0xBF | memory[0xFF1E];
                    case 0xFF20:
                        return 0xFF;
                    case 0xFF23:
                        return 0xBF | memory[0xFF23];
                    case 0xFF26:
                        return 0x70 | memory[0xFF26];
                    case 0xFF30:
                    case 0xFF31:
                    case 0xFF32:
                    case 0xFF33:
                    case 0xFF34:
                    case 0xFF35:
                    case 0xFF36:
                    case 0xFF37:
                    case 0xFF38:
                    case 0xFF39:
                    case 0xFF3A:
                    case 0xFF3B:
                    case 0xFF3C:
                    case 0xFF3D:
                    case 0xFF3E:
                    case 0xFF3F:
                        return ((memory[0xFF26] & 0x4) == 0x4) ? 0xFF : memory[address];
                    case 0xFF41:
                        return 0x80 | memory[0xFF41] | core.lcd.modeSTAT;
                    case 0xFF44:
                        return core.lcd.LCDisOn ? memory[0xFF44] : 0;
                    case 0xFF4F:
                        return currVRAMBank;
                    default:
                        return memory[address]; //memoryReadNormal
                }
            }

            return 0xFF; //memoryReadBAD
        }
    }
}