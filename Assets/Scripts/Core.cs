using System;
using GB.Graphics;
using GB.Input;
using UnityEngine;
using Screen = GB.Graphics.Screen;

namespace GB
{
    public class Core
    {
        public bool initialized;

        // 1 Bank = 16 KBytes = 256 Kbits
        public readonly long[] ROMBanks = new long[] { 2, 4, 8, 16, 32, 64, 128, 256, 512 };

        public readonly long[] RAMBanks = new long[] { 0, 1, 2, 4, 16 };

        public long[] VRAM;

        public string name;

        public string gameCode;

        public byte cartridgeType;

        public bool cMBC1;

        public bool cMBC2;

        public bool cMBC3;

        public bool cMBC5;

        public bool cHuC3;

        public bool cSRAM;

        public bool cBATT;

        public bool cRUMBLE;

        public bool cTIMER;

        public bool IME = true;

        public bool cGBC;

        public bool inBootstrap = true;

        public bool hdmaRunning;

        public bool TIMAEnabled;

        public bool bgEnabled = true;

        public long stopEmulator = 3;

        public long CPUTicks;

        public bool halt;

        public long multiplier = 1;

        public bool skipPCIncrement;

        public long untilEnable;

        private long audioTicks = 0;

        private long emulatorTicks = 0;

        private long DIVTicks = 14;

        public long LCDTicks = 15;

        private long timerTicks = 0;

        public long TACClocker = 256;

        public Lcd lcd;

        public Memory memory;

        public Screen screen;

        public Clock clock;

        public CPU cpu;

        public Keyboard keyboard;

        public Core(TextAsset asset, Drawer drawer)
        {
            cpu = new CPU(this);
            memory = new Memory(this, asset.bytes);
            clock = new Clock(this);
            screen = new Screen(this, drawer);
            lcd = new Lcd(this);
            keyboard = new Keyboard(this);
        }

        public void Start()
        {
            Settings.frameskipAmout = 0;
            memory.InitMemory();
            screen.InitVideoMemory();
            CheckROM(memory.LoadROM());
            screen.InitLCD();
            Run();

            if ((stopEmulator & 2) == 0)
                throw new Exception("GameBoy is already running");

            if ((stopEmulator & 2) != 2)
                throw new Exception("GameBoy is not initialized");

            stopEmulator &= 1;
            clock.lastIteration = (int)(Time.time);

            initialized = true;
        }

        private void CheckROM(byte[] rom)
        {
            name = "";
            for (int address = 0x134; address < 0x13F; address++)
            {
                if (rom[address] > 0)
                    name += Convert.ToChar(rom[address]);
            }

            gameCode = "";
            for (int address = 0x13F; address < 0x143; address++)
            {
                if (rom[address] > 0)
                    gameCode += Convert.ToChar(rom[address]);
            }

            cartridgeType = rom[0x147];

            Debug.Log(name + " " + gameCode.Length + " " + cartridgeType);

            string MBCType = SetCartridgeFlags();

            long numROMBanks = ROMBanks[rom[0x148]];

            Debug.Log("Cartridge Type=" + MBCType);
            Debug.Log("Memory RAM=" + RAMBanks[rom[0x149]]);

            switch (rom[0x143])
            {
                case 0x00:
                    cGBC = false;
                    break;
                case 0x80:
                    cGBC = !Settings.priorizeGameBoyMode;
                    break;
                case 0xC0:
                    cGBC = true;
                    break;
                default:
                    cGBC = false;
                    break;
            }

            Debug.Log("GBC Mode=" + rom[0x143] + " " + cGBC);

            inBootstrap = false;
            memory.InitRAM();
            screen.InitScreen();
            InitSkipBootstrap();

            screen.CheckPaletteType();
        }

        private string SetCartridgeFlags()
        {
            string MBCType = "";

            switch (cartridgeType)
            {
                case 0x01:
                    cMBC1 = true;
                    MBCType = "MBC1";
                    break;
                case 0x03:
                    cMBC1 = true;
                    cSRAM = true;
                    cBATT = true;
                    MBCType = "MBC1 + SRAM + BATT";
                    break;
                case 0x06:
                    cMBC2 = true;
                    cBATT = true;
                    MBCType = "MBC2 + BATT";
                    break;
                case 0x0F:
                    cMBC3 = true;
                    cTIMER = true;
                    cBATT = true;
                    MBCType = "MBC3 + TIMER + BATT";
                    break;
				case 0x10:
                    cMBC3 = true;
                    cTIMER = true;
                    cBATT = true;
                    cSRAM = true;
                    MBCType = "MBC3 + TIMER + BATT + SRAM";
					break;
                case 0x13:
                    cMBC3 = true;
                    cSRAM = true;
                    cBATT = true;
                    MBCType = "MBC3 + SRAM + BATT";
                    break;
                case 0x1B:
                    cMBC5 = true;
                    cSRAM = true;
                    cBATT = true;
                    MBCType = "MBC5 + SRAM + BATT";
                    break;
            }

            if (cTIMER)
                clock.Enabled = true;

            return MBCType;
        }

        private void InitSkipBootstrap()
        {
            IME = true;
            LCDTicks = 15;
            DIVTicks = 14;
            cpu.Initialize();

            long address = 0xFF;
            while (address >= 0)
            {
                if (address >= 0x30 && address < 0x40)
                    memory.Write(0xFF00 + address, Data.ffxxDump[address]);
                else
                {
                    switch (address)
                    {
                        case 0x00:
                        case 0x01:
                        case 0x02:
                        case 0x07:
                        case 0x0F:
                        case 0x40:
                        case 0xFF:
                            memory.Write(0xFF00 + address, Data.ffxxDump[address]);
                            break;
                        default:
                            memory.RawWrite(0xFF00 + address, Data.ffxxDump[address]);
                            break;
                    }
                }
                --address;
            }
        }

        public void Run()
        {
            try
            {
                if ((stopEmulator & 2) == 0)
                {
                    if ((stopEmulator & 1) == 1)
                    {
                        stopEmulator = 0;
                        clock.Update();

                        if (!halt)
                            ExecuteStep();
                        else
                        {
                            CPUTicks = 1;
                            MainVM.Execute(this, 0x76);

                            RunInterrupt();

                            UpdateCore();
                            ExecuteStep();
                        }
                    }
                    else
                        throw new Exception("restarted core");
                }
            }
            catch (Exception e)
            {
                if (e.Message != "HALT_OVERRUN")
                    Debug.LogError(e.Message + " " + e.StackTrace);
            }
        }

        private void RunInterrupt()
        {
            int bitShift = 0;
            long testbit = 1;
            long interrupts = memory.memory[0xFFFF] & memory.memory[0xFF0F];

            while (bitShift < 5)
            {
                if ((testbit & interrupts) == testbit)
                {
                    IME = false;
                    memory.memory[0xFF0F] -= testbit;

                    cpu.stackPointer = Utils.Unswtuw(cpu.stackPointer - 1);
                    memory.Write(cpu.stackPointer, (cpu.programCounter >> 8));
                    cpu.stackPointer = Utils.Unswtuw(cpu.stackPointer - 1);
                    memory.Write(cpu.stackPointer, (cpu.programCounter & 0xFF));

                    cpu.programCounter = 0x0040 + (bitShift * 0x08);
                    CPUTicks += 5;
                    break;
                }
                testbit = 1 << ++bitShift;
            }
        }

        public void UpdateCore()
        {
            DIVTicks += CPUTicks;
            if (DIVTicks >= 0x40)
            {
                DIVTicks -= 0x40;
                memory.memory[0xFF04] = (memory.memory[0xFF04] + 1) & 0xFF; // inc DIV
            }

            long timedTicks = CPUTicks / multiplier;

            LCDTicks += timedTicks;
            lcd.ScanLine(lcd.currentScanLine);

            audioTicks += timedTicks;

            if (audioTicks >= Settings.audioGranularity)
            {
                emulatorTicks += audioTicks;
                if (emulatorTicks >= Settings.machineCyclesPerLoop)
                {
                    if ((stopEmulator & 1) == 0)
                    {
                        if (screen.drewBlank == 0)
                            screen.DrawToCanvas();
                    }
                    stopEmulator |= 1;
                    emulatorTicks = 0;
                }
                audioTicks = 0;
            }

            if (TIMAEnabled)
            {
                timerTicks += CPUTicks;
                while (timerTicks >= TACClocker)
                {
                    timerTicks -= TACClocker;
                    if (memory.memory[0xFF05] == 0xFF)
                    {
                        memory.memory[0xFF05] = memory.memory[0xFF06];
                        memory.memory[0xFF0F] |= 0x4; // set IF bit 2
                    }
                    else
                        ++memory.memory[0xFF05];
                }
            }
        }

        private void ExecuteStep()
        {
            long op = 0;

            while (stopEmulator == 0)
            {
                op = memory.Read(cpu.programCounter);

                if (!skipPCIncrement)
                    cpu.programCounter = (cpu.programCounter + 1) & 0xFFFF;

                skipPCIncrement = false;
                CPUTicks = TICKTables.primary[op];

                //Debug.Log(op + " " + CPUTicks + " " + programCounter);

                MainVM.Execute(this, op);

                switch (untilEnable)
                {
                    case 1:
                        IME = true;
                        untilEnable--;
                        break;
                    case 2:
                        untilEnable--;
                        break;
                }

                if (IME)
                    RunInterrupt();

                UpdateCore();
            }
        }

        public void NotifyScanline()
        {
            Debug.Log("not implemented");
        }
    }
}