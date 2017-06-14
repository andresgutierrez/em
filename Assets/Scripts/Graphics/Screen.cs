
/*
 * Partially ported video, HDMA, and double speed mode procedure from the STOP opcode from MeBoy 2.2
 * http://arktos.se/meboy/
 * Copyright(C) 2005-2009 Bjorn Carlin
 */

using System;
using UnityEngine;

namespace GB.Graphics
{
    [Serializable]
    public class Screen
    {
        [NonSerialized]
        private Core core;

        [NonSerialized]
        private Drawer drawer;

        private readonly long[] colors = new long[] { 0x80EFFFDE, 0x80ADD794, 0x80529273, 0x80183442 };

        private long[] palette;

        private long tileCountInvalidator;

        private long tileCount = 384;

        private long colorCount = 12;

        private TileData[] tileData;

        private int[] tileReadState;

        private long transparentCutoff = 4;

        private long[] weaveLookup;

        private long width = 160;

        private long height = 144;

        private long[] canvasBuffer;

        private long pixelCount;

        private long frameCount;

        private long rgbCount;

        private long[] frameBuffer;

        private long[] gbPalette;

        private long[] gbColorizedPalette;

        public long[] gbcRawPalette;

        private long[] gbcPalette;

        public long drewBlank;

        public long windowSourceLine = 0;

        public bool spritePriorityEnabled = true;

        public bool gfxWindowDisplay = false;

        public bool gfxWindowY;

        public bool gfxBackgroundX;

        public bool gfxBackgroundY;

        public bool gfxSpriteDouble;

        public bool gfxSpriteShow;

        private bool colorEnabled = false;

        public Screen(Core core, Drawer drawer)
        {
            this.core = core;
            this.drawer = drawer;

            tileCountInvalidator = tileCount * 4;
            frameCount = Settings.frameskipBaseFactor;
            pixelCount = width * height;
            rgbCount = pixelCount * 4;
        }

        public void InitLCD()
        {
            transparentCutoff = (Settings.colorize || core.cGBC) ? 32 : 4;
            if (weaveLookup == null)
            {
                weaveLookup = new long[256];
                for (int i = 0x1; i <= 0xFF; i++)
                {
                    for (int d = 0; d < 0x8; d++)
                        weaveLookup[i] += ((i >> d) & 1) << (d * 2);
                }
            }

            width = 160;
            height = 144;

            //Blank screen
            if (colorEnabled)
            {
                canvasBuffer = new long[4 * width * height];
                for (int i = 0; i < canvasBuffer.Length; i++)
                    canvasBuffer[i] = 255;

                long address = pixelCount;
                long address2 = rgbCount;
                while (address > 0)
                {
                    frameBuffer[--address] = 0x00FFFFFF;
                    canvasBuffer[address2 -= 4] = 0xFF;
                    canvasBuffer[address2 + 1] = 0xFF;
                    canvasBuffer[address2 + 2] = 0xFF;
                    canvasBuffer[address2 + 3] = 0xFF;
                }
            }
            else
            {
                canvasBuffer = new long[4 * width * height];
                frameBuffer = new long[pixelCount];
                for (int i = 0; i < frameBuffer.Length; i++)
                    frameBuffer[i] = 0x00FFFFFF;
            }

            drawer.Draw(canvasBuffer);
        }

        public void InitScreen()
        {
            if (core.cGBC)
            {
                tileCount *= 2;
                tileCountInvalidator = tileCount * 4;
                colorCount = 64;
                transparentCutoff = 32;
            }

            tileData = new TileData[tileCount * colorCount];
            for (int i = 0; i < tileData.Length; i++)
                tileData[i] = new TileData();

            tileReadState = new int[tileCount];
        }

        public void InitVideoMemory()
        {
            frameBuffer = new long[23040];
            for (int i = 0; i < frameBuffer.Length; i++)
                frameBuffer[i] = 0x00FFFFFF;

            gbPalette = new long[12];
            gbColorizedPalette = new long[12];
            gbcRawPalette = new long[0x80];
            for (int i = 0; i < gbcRawPalette.Length; i++)
                gbcRawPalette[i] = -1000;

            gbcPalette = new long[64];
            gbcPalette[0] = 0x40;

            long address = 0x3F;
            while (address >= 0)
            {
                gbcPalette[address] = (address < 0x20) ? -1 : 0;
                --address;
            }
        }

        public bool DrawBackgroundForLine(long line, long windowLeft, long priority)
        {
            bool skippedTile = false;
            long tileNum = 0;
            long tileXCoord = 0;
            long tileAttrib = 0;
            long sourceY = line + core.memory.memory[0xFF42];
            long sourceImageLine = sourceY & 0x7;
            long tileX = core.memory.memory[0xFF43] >> 3;
            long memStart = ((gfxBackgroundY) ? 0x1C00 : 0x1800) + ((sourceY & 0xF8) << 2);
            long screenX = -(core.memory.memory[0xFF43] & 7);

            for (; screenX < windowLeft; tileX++, screenX += 8)
            {
                tileXCoord = (tileX & 0x1F);
                long baseAddress = core.memory.memory[0x8000 + memStart + tileXCoord];
                tileNum = (gfxBackgroundX) ? baseAddress : ((baseAddress > 0x7F) ? ((baseAddress & 0x7F) + 0x80) : (baseAddress + 0x100));
                if (core.cGBC)
                {
                    long mapAttrib = core.VRAM[memStart + tileXCoord];
                    if ((mapAttrib & 0x80) != priority)
                    {
                        skippedTile = true;
                        continue;
                    }
                    tileAttrib = ((mapAttrib & 0x07) << 2) + ((mapAttrib >> 5) & 0x03);
                    tileNum += 384 * ((mapAttrib >> 3) & 0x01);
                }
                DrawPartCopy(tileNum, screenX, line, sourceImageLine, tileAttrib);
            }

            if (windowLeft < 160)
            {
                long windowStartAddress = (gfxWindowY) ? 0x1C00 : 0x1800;
                long windowSourceTileY = windowSourceLine >> 3;
                long tileAddress = windowStartAddress + (windowSourceTileY * 0x20);
                long windowSourceTileLine = windowSourceLine & 0x7;
                for (screenX = windowLeft; screenX < 160; tileAddress++, screenX += 8)
                {
                    long baseaddr = core.memory.memory[0x8000 + tileAddress];
                    tileNum = (gfxBackgroundX) ? baseaddr : ((baseaddr > 0x7F) ? ((baseaddr & 0x7F) + 0x80) : (baseaddr + 0x100));
                    if (core.cGBC)
                    {
                        long mapAttrib = core.VRAM[tileAddress];
                        if ((mapAttrib & 0x80) != priority)
                        {
                            skippedTile = true;
                            continue;
                        }
                        tileAttrib = ((mapAttrib & 0x07) << 2) + ((mapAttrib >> 5) & 0x03);
                        tileNum += 384 * ((mapAttrib >> 3) & 0x01);
                    }
                    DrawPartCopy(tileNum, screenX, line, windowSourceTileLine, tileAttrib);
                }
            }

            return skippedTile;
        }

        private void DrawPartCopy(long tileIndex, long x, long y, long sourceLine, long attribs)
        {
            TileData image;

            if (tileData[tileIndex + tileCount * attribs].initialized)
                image = tileData[tileIndex + tileCount * attribs];
            else
                image = UpdateImage(tileIndex, attribs);

            long dst = x + y * 160;
            long src = sourceLine * 8;
            long dstEnd = (x > 152) ? ((y + 1) * 160) : (dst + 8);

            if (x < 0)
            {
                dst -= x;
                src -= x;
            }

            while (dst < dstEnd)
                frameBuffer[dst++] = image.tiles[src++];
        }

        private long VRAMReadGFX(long address, bool gbcBank)
        {
            return (!gbcBank ? core.memory.memory[0x8000 + address] : core.VRAM[address]);
        }

        private TileData UpdateImage(long tileIndex, long attribs)
        {
            long address_ = tileIndex + tileCount * attribs;
            bool otherBank = (tileIndex >= 384);
            long offset = otherBank ? ((tileIndex - 384) << 4) : (tileIndex << 4);
            long paletteStart = attribs & 0xFC;
            bool transparent = attribs >= transparentCutoff;
            long pixix = 0;
            long pixixdx = 1;
            long pixixdy = 0;
            int[] tempPix = new int[64];

            if ((attribs & 2) != 0)
            {
                pixixdy = -16;
                pixix = 56;
            }

            if ((attribs & 1) == 0)
            {
                pixixdx = -1;
                pixix += 7;
                pixixdy += 16;
            }

            for (int y = 8; --y >= 0;)
            {
                long num = weaveLookup[VRAMReadGFX(offset++, otherBank)] + (weaveLookup[this.VRAMReadGFX(offset++, otherBank)] << 1);
                if (num != 0)
                    transparent = false;

                for (int x = 8; --x >= 0;)
                {
                    long d = paletteStart + (num & 3);
                    tempPix[pixix] = (int)(palette[d] & -1);
                    pixix += pixixdx;
                    num >>= 2;
                }

                pixix += pixixdy;
            }

            tileData[address_].initialized = true;
            if (transparent)
                tileData[address_].transparent = true;
            else
            {
                tileData[address_].transparent = false;
                tileData[address_].tiles = tempPix;
            }

            tileReadState[tileIndex] = 1;
            return tileData[address_];
        }

        public void DrawSpritesForLine(long line)
        {
            if (!gfxSpriteShow)
                return;

            long minSpriteY = line - ((gfxSpriteDouble) ? 15 : 7);
            long priorityFlag = spritePriorityEnabled ? 0x80 : 0;

            for (; priorityFlag >= 0; priorityFlag -= 0x80)
            {
                long oamIx = 159;
                while (oamIx >= 0)
                {
                    long attributes = 0xFF & core.memory.memory[0xFE00 + oamIx--];
                    if ((attributes & 0x80) == priorityFlag || !spritePriorityEnabled)
                    {
                        long tileNum = (0xFF & core.memory.memory[0xFE00 + oamIx--]);
                        long spriteX = (0xFF & core.memory.memory[0xFE00 + oamIx--]) - 8;
                        long spriteY = (0xFF & core.memory.memory[0xFE00 + oamIx--]) - 16;
                        long offset = line - spriteY;

                        if (spriteX >= 160 || spriteY < minSpriteY || offset < 0)
                            continue;

                        if (gfxSpriteDouble)
                            tileNum = tileNum & 0xFE;

                        long spriteAttrib = (attributes >> 5) & 0x03; // flipx: from bit 0x20 to 0x01, flipy: from bit 0x40 to 0x02
                        if (core.cGBC)
                        {
                            spriteAttrib += 0x20 + ((attributes & 0x07) << 2); // palette
                            tileNum += (384 >> 3) * (attributes & 0x08); // tile vram bank
                        }
                        else
                        {
                            // attributes 0x10: 0x00 = OBJ1 palette, 0x10 = OBJ2 palette
                            // spriteAttrib: 0x04: OBJ1 palette, 0x08: OBJ2 palette
                            spriteAttrib += 0x4 + ((attributes & 0x10) >> 2);
                        }

                        if (priorityFlag == 0x80)
                        {
                            // background
                            if (gfxSpriteDouble)
                            {
                                if ((spriteAttrib & 2) != 0)
                                    DrawPartBgSprite((tileNum | 1) - (offset >> 3), spriteX, line, offset & 7, spriteAttrib);
                                else
                                    DrawPartBgSprite((tileNum & -2) + (offset >> 3), spriteX, line, offset & 7, spriteAttrib);
                            }
                            else
                                DrawPartBgSprite(tileNum, spriteX, line, offset, spriteAttrib);
                        }
                        else
                        {
                            // foreground
                            if (gfxSpriteDouble)
                            {
                                if ((spriteAttrib & 2) != 0)
                                    DrawPartFgSprite((tileNum | 1) - (offset >> 3), spriteX, line, offset & 7, spriteAttrib);
                                else
                                    DrawPartFgSprite((tileNum & -2) + (offset >> 3), spriteX, line, offset & 7, spriteAttrib);
                            }
                            else
                                DrawPartFgSprite(tileNum, spriteX, line, offset, spriteAttrib);
                        }
                    }
                    else
                    {
                        oamIx -= 3;
                    }
                }
            }
        }

        public void DrawPartBgSprite(long tileIndex, long x, long y, long sourceLine, long attribs)
        {
            TileData im = tileData[tileIndex + tileCount * attribs].initialized ? tileData[tileIndex + tileCount * attribs] : UpdateImage(tileIndex, attribs);
            if (im.transparent)
                return;

            long dst = x + y * 160;
            long src = sourceLine * 8;
            long dstEnd = (x > 152) ? ((y + 1) * 160) : (dst + 8);
            // adjust left
            if (x < 0)
            {
                dst -= x;
                src -= x;
            }
            while (dst < dstEnd)
            {
                //if (im[src] < 0 && this.frameBuffer[dst] >= 0) {
                frameBuffer[dst] = im.tiles[src];
                // }
                ++dst;
                ++src;
            }
        }

        public void DrawPartFgSprite(long tileIndex, long x, long y, long sourceLine, long attribs)
        {
            TileData im = tileData[tileIndex + tileCount * attribs].initialized ? tileData[tileIndex + tileCount * attribs] : UpdateImage(tileIndex, attribs);
            if (im.transparent)
                return;

            long dst = x + y * 160;
            long src = sourceLine * 8;
            long dstEnd = (x > 152) ? ((y + 1) * 160) : (dst + 8);

            if (x < 0)
            {
                dst -= x;
                src -= x;
            }

            while (dst < dstEnd)
            {
                if (im.tiles[src] < 0)
                    frameBuffer[dst] = im.tiles[src];
                ++dst;
                ++src;
            }
        }

        public void DisplayShowOff()
        {
            if (drewBlank == 0)
            {
                canvasBuffer = new long[4 * width * height];
                if (colorEnabled)
                {
                    for (int i = 0; i < canvasBuffer.Length; i++)
                        canvasBuffer[i] = 255;
                }
                else
                {
                    for (int i = 0; i < canvasBuffer.Length; i++)
                        canvasBuffer[i] = 0;
                }

                drawer.Draw(canvasBuffer);
                drewBlank = 2;
            }
        }

        public void DrawToCanvas()
        {
            if (Settings.frameskipAmout == 0 || frameCount > 0)
            {
                long bufferIndex = pixelCount;
                long canvasIndex = rgbCount;

                if (colorEnabled)
                {
                    while (canvasIndex > 3)
                    {
                        canvasBuffer[canvasIndex -= 4] = (frameBuffer[--bufferIndex] >> 16) & 0xFF; //Red                                                                                                   
                        canvasBuffer[canvasIndex + 1] = (frameBuffer[bufferIndex] >> 8) & 0xFF; //Green
                        canvasBuffer[canvasIndex + 2] = frameBuffer[bufferIndex] & 0xFF; //Blue
                    }
                }
                else
                {
                    // Generate gray version
                    while (bufferIndex > 0)
                    {
                        // average for black and white
                        canvasBuffer[bufferIndex] = frameBuffer[--bufferIndex]; //(r + g + b) / 3;
                    }
                }

                drawer.Draw(canvasBuffer);

                if (Settings.frameskipAmout > 0)
                    frameCount -= Settings.frameskipAmout;
            }
            else
                frameCount += Settings.frameskipBaseFactor;
        }

        public void InvalidateAll(long pal)
        {
            long stop = (pal + 1) * tileCountInvalidator;
            for (long r = pal * tileCountInvalidator; r < stop; ++r)
                tileData[r].initialized = false;
        }

        public void DecodePalette(long startIndex, long data)
        {
            if (!core.cGBC)
            {
                gbPalette[startIndex] = colors[data & 0x03] & 0x00FFFFFF; // color 0: transparent
                gbPalette[startIndex + 1] = colors[(data >> 2) & 0x03];
                gbPalette[startIndex + 2] = colors[(data >> 4) & 0x03];
                gbPalette[startIndex + 3] = colors[data >> 6];
                CheckPaletteType();
            }
        }

        public void CheckPaletteType()
        {
            if (core.cGBC)
                palette = gbcPalette;
            else
                palette = Settings.colorize ? gbColorizedPalette : gbPalette;
        }

        public void ChangeTileDataArea(long address, long currVRAMBank)
        {
            long tileIndex = ((address - 0x8000) >> 4) + (384 * currVRAMBank);
            if (tileReadState[tileIndex] == 1)
            {
                long r = tileData.Length - tileCount + tileIndex;
                do
                {
                    tileData[r].initialized = false;
                    r -= tileCount;
                } while (r >= 0);
                tileReadState[tileIndex] = 0;
            }
        }

        public void SetGBCPalettePre(long address, long data)
        {
            if (gbcRawPalette[address] == data)
                return;

            gbcRawPalette[address] = data;
            if (address >= 0x40 && (address & 0x6) == 0)
                return;

            long value = (gbcRawPalette[address | 1] << 8) + gbcRawPalette[address & -2];
            gbcPalette[address >> 1] = 0x80000000 + ((value & 0x1F) << 19) + ((value & 0x3E0) << 6) + ((value & 0x7C00) >> 7);

            InvalidateAll(address >> 3);
        }

        public void SetGBCPalette(long address, long data)
        {
            SetGBCPalettePre(address, data);
            if ((address & 0x6) == 0)
                gbcPalette[address >> 1] &= 0x00FFFFFF;
        }

        public void PerformHDMA()
        {
            Memory memory = core.memory;

            core.CPUTicks += 1 + (8 * core.multiplier);

            long dmaSrc = (memory.memory[0xFF51] << 8) + memory.memory[0xFF52];
            long dmaDstRelative = (memory.memory[0xFF53] << 8) + memory.memory[0xFF54];
            long dmaDstFinal = dmaDstRelative + 0x10;
            long tileRelative = tileData.Length - tileCount;

            if (memory.currVRAMBank == 1)
            {
                while (dmaDstRelative < dmaDstFinal)
                {
                    if (dmaDstRelative < 0x1800)
                    {
                        long tileIndex = (dmaDstRelative >> 4) + 384;
                        if (tileReadState[tileIndex] == 1)
                        {
                            long r = tileRelative + tileIndex;
                            do
                            {
                                tileData[r].initialized = false;
                                r -= tileCount;
                            } while (r >= 0);
                            tileReadState[tileIndex] = 0;
                        }
                    }
                    core.VRAM[dmaDstRelative++] = memory.Read(dmaSrc++);
                }
            }
            else
            {
                while (dmaDstRelative < dmaDstFinal)
                {
                    // Bkg Tile data area
                    if (dmaDstRelative < 0x1800)
                    {
                        long tileIndex = dmaDstRelative >> 4;
                        if (tileReadState[tileIndex] == 1)
                        {
                            long r = tileRelative + tileIndex;
                            do
                            {
                                tileData[r].initialized = false;
                                r -= tileCount;
                            } while (r >= 0);
                            tileReadState[tileIndex] = 0;
                        }
                    }
                    memory.memory[0x8000 + dmaDstRelative++] = memory.Read(dmaSrc++);
                }
            }

            memory.memory[0xFF51] = (dmaSrc & 0xFF00) >> 8;
            memory.memory[0xFF52] = dmaSrc & 0x00F0;
            memory.memory[0xFF53] = ((dmaDstFinal & 0x1F00) >> 8);
            memory.memory[0xFF54] = dmaDstFinal & 0x00F0;

            if (memory.memory[0xFF55] == 0)
            {
                core.hdmaRunning = false;
                memory.memory[0xFF55] = 0xFF;
            }
            else
            {
                --memory.memory[0xFF55];
            }
        }
    }
}
