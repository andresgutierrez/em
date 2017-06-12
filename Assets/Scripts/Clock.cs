
using UnityEngine;

namespace GB
{
    public class Clock
    {
        private Core core;

        private bool RTCisLatched = true;

        private long RTCSeconds;

        private long RTCMinutes;

        private long RTCHours = 0;

        private long RTCDays = 0;

        private bool RTCDayOverFlow = false;

        private bool RTCHALT = false;

        private long latchedSeconds;

        private long latchedMinutes;

        private long latchedHours;

        private long latchedLDays;

        private long latchedHDays;

        private bool enabled;

        public long lastIteration;

        public Clock(Core core, bool enabled = false)
        {
            this.core = core;
            this.enabled = enabled;
        }

        public void Update()
        {
            if (Settings.autoFrameskip || enabled)
            {
                long timeElapsed = ((int)Time.time) - lastIteration;
                if (enabled && !RTCHALT)
                {
                    RTCSeconds += timeElapsed;

                    while (RTCSeconds >= 60)
                    {
                        RTCSeconds -= 60;
                        ++RTCMinutes;
                        if (RTCMinutes >= 60)
                        {
                            RTCMinutes -= 60;
                            ++RTCHours;
                            if (RTCHours >= 24)
                            {
                                RTCHours -= 24;
                                ++RTCDays;
                                if (RTCDays >= 512)
                                {
                                    RTCDays -= 512;
                                    RTCDayOverFlow = true;
                                }
                            }
                        }
                    }
                }

                if (Settings.autoFrameskip)
                {                    
                    if (timeElapsed > Settings.loopInterval)
                    {
                        if (Settings.frameskipAmout < Settings.frameskipMax)
                            ++Settings.frameskipAmout;
                    }
                    else if (Settings.frameskipAmout > 0)
                    {
                        --Settings.frameskipAmout;
                    }
                }

                lastIteration = (int)Time.time;
            }
        }

        public void CheckLatched(long data)
        {
            if (data == 0)
            {
                RTCisLatched = false;
                return;
            }

            if (!RTCisLatched)
            {
                RTCisLatched = true;
                latchedSeconds = (long)Mathf.Floor(RTCSeconds);
                latchedMinutes = RTCMinutes;
                latchedHours = RTCHours;
                latchedLDays = RTCDays & 0xFF;
                latchedHDays = RTCDays >> 8;
            }
        }

        public void SetClock(long address, long data, long currMBCRAMBank, long currMBCRAMBankPosition)
        {
            switch (currMBCRAMBank)
            {
                case 0x00:
                case 0x01:
                case 0x02:
                case 0x03:
                    core.memory.MBCRam[address + currMBCRAMBankPosition] = data;
                    break;

                case 0x08:
                    if (data < 60)
                        RTCSeconds = data;
                    break;

                case 0x09:
                    if (data < 60)
                        RTCMinutes = data;
                    break;

                case 0x0A:
                    if (data < 24)
                        RTCHours = data;
                    break;

                case 0x0B:
                    RTCDays = (data & 0xFF) | (RTCDays & 0x100);
                    break;

                case 0x0C:
                    RTCDayOverFlow = (data & 0x80) == 0x80;
                    RTCHALT = (data & 0x40) == 0x40;
                    RTCDays = ((data & 0x1) << 8) | (RTCDays & 0xFF);
                    break;
            }
        }

        public long GetTime(long currMBCRAMBank, long currMBCRAMBankPosition, long address)
        {
			switch (currMBCRAMBank)
			{
				case 0x00:
				case 0x01:
				case 0x02:
				case 0x03:
					return core.memory.MBCRam[address + currMBCRAMBankPosition];
				case 0x08:
					return latchedSeconds;
				case 0x09:
					return latchedMinutes;
				case 0x0A:
					return latchedHours;
				case 0x0B:
					return latchedLDays;
				case 0x0C:
					return (((RTCDayOverFlow) ? 0x80 : 0) + ((RTCHALT) ? 0x40 : 0)) + latchedHDays;
			}
            return 0xFF; //memoryReadBAD
		}
    }
}