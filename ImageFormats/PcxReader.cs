﻿using System;
using System.Drawing;
using System.IO;

/*
 
Decoder for ZSoft Paintbrush (.PCX) images.
Supports pretty much the full PCX specification (all bit
depths, etc).  At the very least, it decodes all PCX images that
I've found in the wild.  If you find one that it fails to decode,
let me know!

Copyright 2013-2020 by Warren Galyen
https://www.mechanikadesign.com

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

   http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.

*/

namespace MechanikaDesign.ImageFormats
{
    /// <summary>
    /// Handles reading ZSoft PCX images.
    /// </summary>
    public static class PcxReader
    {
        /// <summary>
        /// Reads a PCX image from a file.
        /// </summary>
        /// <param name="fileName">Name of the file to read.</param>
        /// <returns>Bitmap that contains the image that was read.</returns>
        public static Bitmap Load(string fileName)
        {
            using (var f = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                return Load(f);
            }
        }

        /// <summary>
        /// Reads a PCX image from a stream.
        /// </summary>
        /// <param name="stream">Stream from which to read the image.</param>
        /// <returns>Bitmap that contains the image that was read.</returns>
        public static Bitmap Load(Stream stream)
        {
            BinaryReader reader = new BinaryReader(stream);

            byte tempByte = (byte)stream.ReadByte();
            if (tempByte != 10)
                throw new ApplicationException("This is not a valid PCX file.");

            var version = (byte)stream.ReadByte();
            if (version > 5)
                throw new ApplicationException("Only Version 5 or lower PCX files are supported.");


            // This variable controls whether the bit plane values are interpreted as literal color states
            // instead of indices into the palette. In other words, this controls whether the palette is
            // used or ignored. As far as I can tell the only way to determine whether the palette is used
            // is by the version number of the file.
            // If the colors in your decoded picture look weird, try tweaking this variable.
            bool bitPlanesLiteral = version == 3 || version == 4; // PaintBrush 2.8 without palette information.

            tempByte = (byte)stream.ReadByte();
            if (tempByte != 1)
                throw new ApplicationException("Invalid PCX compression type.");

            int imgBpp = stream.ReadByte();
            if (imgBpp != 8 && imgBpp != 4 && imgBpp != 2 && imgBpp != 1)
                throw new ApplicationException("Only 8, 4, 2, and 1-bit PCX samples are supported.");

            UInt16 xmin = Util.LittleEndian(reader.ReadUInt16());
            UInt16 ymin = Util.LittleEndian(reader.ReadUInt16());
            UInt16 xmax = Util.LittleEndian(reader.ReadUInt16());
            UInt16 ymax = Util.LittleEndian(reader.ReadUInt16());

            int imgWidth = xmax - xmin + 1;
            int imgHeight = ymax - ymin + 1;

            if ((imgWidth < 1) || (imgHeight < 1) || (imgWidth > 32767) || (imgHeight > 32767))
                throw new ApplicationException("This PCX file appears to have invalid dimensions.");

            Util.LittleEndian(reader.ReadUInt16()); // hdpi
            Util.LittleEndian(reader.ReadUInt16()); // vdpi

            byte[] colorPalette = new byte[48];
            stream.Read(colorPalette, 0, 48);

            stream.ReadByte();

            int numPlanes = stream.ReadByte();
            int bytesPerLine = Util.LittleEndian(reader.ReadUInt16());
            if (bytesPerLine == 0) bytesPerLine = xmax - xmin + 1;

            // TODO: use this for something? It doesn't seem to be consistent or meaningful between different versions.
            int paletteInfo = Util.LittleEndian(reader.ReadUInt16());

            if (imgBpp == 8 && numPlanes == 1)
            {
                colorPalette = new byte[768];
                stream.Seek(-768, SeekOrigin.End);
                stream.Read(colorPalette, 0, 768);
            }

            if (imgBpp == 1 && numPlanes == 1 && bitPlanesLiteral == false)
            {
                bitPlanesLiteral = true;
                // With 1-bpp images that claim to have palette information, we have a bit of a problem:
                // Images seen in the wild don't seem to be consistent about whether they actually use
                // the palette for 1-bit images.
                // This is a hacky way to detect whether the palette should be used. We look at the first
                // three RGB triplets, and if they are nonzero, *and* if the rest of the palette is zero,
                // then the palette will be used. Otherwise, the 1-bit image will be treated as black/white.
                bool remainingZeros = true;
                for (int c = 6; c < colorPalette.Length; c++)
                {
                    if (colorPalette[c] != 0)
                    {
                        remainingZeros = false;
                        break;
                    }
                }
                if (remainingZeros && (colorPalette[0] != 0 || colorPalette[1] != 0 || colorPalette[2] != 0
                    || colorPalette[3] != 0 || colorPalette[4] != 0 || colorPalette[5] != 0))
                {
                    bitPlanesLiteral = false;
                }
            }

            byte[] bmpData = new byte[(imgWidth + 1) * 4 * imgHeight];
            stream.Seek(128, SeekOrigin.Begin);
            int x, y, i;

            RleReader rleReader = new RleReader(stream);

            try
            {
                if (imgBpp == 1)
                {
                    int b, p;
                    byte val;
                    byte[] scanline = new byte[bytesPerLine];
                    byte[] realscanline = new byte[bytesPerLine * 8];

                    for (y = 0; y < imgHeight; y++)
                    {
                        // add together all the planes
                        Array.Clear(realscanline, 0, realscanline.Length);
                        for (p = 0; p < numPlanes; p++)
                        {
                            x = 0;
                            for (i = 0; i < bytesPerLine; i++)
                            {
                                scanline[i] = (byte)rleReader.ReadByte();

                                for (b = 7; b >= 0; b--)
                                {
                                    if ((scanline[i] & (1 << b)) != 0) val = 1; else val = 0;
                                    realscanline[x] |= (byte)(val << p);
                                    x++;
                                }
                            }
                        }

                        for (x = 0; x < imgWidth; x++)
                        {
                            i = realscanline[x];

                            if (bitPlanesLiteral && numPlanes == 1)
                            {
                                b = i != 0 ? 0xFF : 0;
                                bmpData[4 * (y * imgWidth + x)] = (byte)b;
                                bmpData[4 * (y * imgWidth + x) + 1] = (byte)b;
                                bmpData[4 * (y * imgWidth + x) + 2] = (byte)b;
                            }
                            else if (bitPlanesLiteral && numPlanes == 3)
                            {
                                bmpData[4 * (y * imgWidth + x)] = (byte)((i & 1) != 0 ? 0xFF : 0);
                                bmpData[4 * (y * imgWidth + x) + 1] = (byte)((i & 2) != 0 ? 0xFF : 0);
                                bmpData[4 * (y * imgWidth + x) + 2] = (byte)((i & 4) != 0 ? 0xFF : 0);
                            }
                            else if (bitPlanesLiteral && numPlanes == 4)
                            {
                                b = (i & 8) != 0 ? 0xFF : 0x80; // plane 3 = intensity
                                bmpData[4 * (y * imgWidth + x)] = (byte)((i & 1) != 0 ? b : 0);
                                bmpData[4 * (y * imgWidth + x) + 1] = (byte)((i & 2) != 0 ? b : 0);
                                bmpData[4 * (y * imgWidth + x) + 2] = (byte)((i & 4) != 0 ? b : 0);
                            }
                            else
                            {
                                bmpData[4 * (y * imgWidth + x)] = colorPalette[i * 3 + 2];
                                bmpData[4 * (y * imgWidth + x) + 1] = colorPalette[i * 3 + 1];
                                bmpData[4 * (y * imgWidth + x) + 2] = colorPalette[i * 3];
                            }
                        }
                    }
                }
                else
                {
                    if (numPlanes == 1)
                    {
                        if (imgBpp == 8)
                        {
                            byte[] scanline = new byte[bytesPerLine];
                            for (y = 0; y < imgHeight; y++)
                            {
                                for (i = 0; i < bytesPerLine; i++)
                                    scanline[i] = (byte)rleReader.ReadByte();

                                for (x = 0; x < imgWidth; x++)
                                {
                                    i = scanline[x];
                                    bmpData[4 * (y * imgWidth + x)] = colorPalette[i * 3 + 2];
                                    bmpData[4 * (y * imgWidth + x) + 1] = colorPalette[i * 3 + 1];
                                    bmpData[4 * (y * imgWidth + x) + 2] = colorPalette[i * 3];
                                }
                            }
                        }
                        else if (imgBpp == 4)
                        {
                            byte[] scanline = new byte[bytesPerLine];
                            for (y = 0; y < imgHeight; y++)
                            {
                                for (i = 0; i < bytesPerLine; i++)
                                    scanline[i] = (byte)rleReader.ReadByte();

                                for (x = 0; x < imgWidth; x++)
                                {
                                    i = scanline[x / 2];
                                    bmpData[4 * (y * imgWidth + x)] = colorPalette[((i >> 4) & 0xF) * 3 + 2];
                                    bmpData[4 * (y * imgWidth + x) + 1] = colorPalette[((i >> 4) & 0xF) * 3 + 1];
                                    bmpData[4 * (y * imgWidth + x) + 2] = colorPalette[((i >> 4) & 0xF) * 3];
                                    x++;
                                    bmpData[4 * (y * imgWidth + x)] = colorPalette[(i & 0xF) * 3 + 2];
                                    bmpData[4 * (y * imgWidth + x) + 1] = colorPalette[(i & 0xF) * 3 + 1];
                                    bmpData[4 * (y * imgWidth + x) + 2] = colorPalette[(i & 0xF) * 3];
                                }
                            }
                        }
                        else if (imgBpp == 2)
                        {
                            byte[] scanline = new byte[bytesPerLine];
                            for (y = 0; y < imgHeight; y++)
                            {
                                for (i = 0; i < bytesPerLine; i++)
                                    scanline[i] = (byte)rleReader.ReadByte();

                                for (x = 0; x < imgWidth; x++)
                                {
                                    i = scanline[x / 4];
                                    bmpData[4 * (y * imgWidth + x)] = colorPalette[((i >> 6) & 0x3) * 3 + 2];
                                    bmpData[4 * (y * imgWidth + x) + 1] = colorPalette[((i >> 6) & 0x3) * 3 + 1];
                                    bmpData[4 * (y * imgWidth + x) + 2] = colorPalette[((i >> 6) & 0x3) * 3];
                                    x++;
                                    bmpData[4 * (y * imgWidth + x)] = colorPalette[((i >> 4) & 0x3) * 3 + 2];
                                    bmpData[4 * (y * imgWidth + x) + 1] = colorPalette[((i >> 4) & 0x3) * 3 + 1];
                                    bmpData[4 * (y * imgWidth + x) + 2] = colorPalette[((i >> 4) & 0x3) * 3];
                                    x++;
                                    bmpData[4 * (y * imgWidth + x)] = colorPalette[((i >> 2) & 0x3) * 3 + 2];
                                    bmpData[4 * (y * imgWidth + x) + 1] = colorPalette[((i >> 2) & 0x3) * 3 + 1];
                                    bmpData[4 * (y * imgWidth + x) + 2] = colorPalette[((i >> 2) & 0x3) * 3];
                                    x++;
                                    bmpData[4 * (y * imgWidth + x)] = colorPalette[(i & 0x3) * 3 + 2];
                                    bmpData[4 * (y * imgWidth + x) + 1] = colorPalette[(i & 0x3) * 3 + 1];
                                    bmpData[4 * (y * imgWidth + x) + 2] = colorPalette[(i & 0x3) * 3];
                                }
                            }
                        }
                    }
                    else if (numPlanes == 3)
                    {
                        byte[] scanlineR = new byte[bytesPerLine];
                        byte[] scanlineG = new byte[bytesPerLine];
                        byte[] scanlineB = new byte[bytesPerLine];
                        int bytePtr = 0;

                        for (y = 0; y < imgHeight; y++)
                        {
                            for (i = 0; i < bytesPerLine; i++)
                                scanlineR[i] = (byte)rleReader.ReadByte();
                            for (i = 0; i < bytesPerLine; i++)
                                scanlineG[i] = (byte)rleReader.ReadByte();
                            for (i = 0; i < bytesPerLine; i++)
                                scanlineB[i] = (byte)rleReader.ReadByte();

                            for (int n = 0; n < imgWidth; n++)
                            {
                                bmpData[bytePtr++] = scanlineB[n];
                                bmpData[bytePtr++] = scanlineG[n];
                                bmpData[bytePtr++] = scanlineR[n];
                                bytePtr++;
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                // give a partial image in case of unexpected end-of-file

                System.Diagnostics.Debug.WriteLine("Error while processing PCX file: " + e.Message);
            }

            var bmp = new Bitmap(imgWidth, imgHeight, System.Drawing.Imaging.PixelFormat.Format32bppRgb);
            System.Drawing.Imaging.BitmapData bmpBits = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), System.Drawing.Imaging.ImageLockMode.ReadWrite, System.Drawing.Imaging.PixelFormat.Format32bppRgb);
            System.Runtime.InteropServices.Marshal.Copy(bmpData, 0, bmpBits.Scan0, imgWidth * 4 * imgHeight);
            bmp.UnlockBits(bmpBits);
            return bmp;
        }

        /// <summary>
        /// Helper class for reading a run-length encoded stream in a PCX file.
        /// </summary>
        private class RleReader
        {
            private int currentByte = 0;
            private int runLength = 0;
            private readonly Stream stream;

            public RleReader(Stream stream)
            {
                this.stream = stream;
            }

            public int ReadByte()
            {
                runLength--;
                if (runLength <= 0)
                {
                    currentByte = stream.ReadByte();
                    if (currentByte > 191)
                    {
                        runLength = currentByte - 192;
                        currentByte = stream.ReadByte();
                    }
                }
                return currentByte;
            }
        }
    }
}
