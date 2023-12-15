﻿using Mechanika.ImageFormats;
using System.Drawing;
using System.IO;

/*
 
This is a class library that contains image decoders for old and/or
obscure image formats (.TGA, .PCX, .PPM, RAS, etc.). Refer to the
individual source code files for each image type for more information.

Copyright 2013-2023 by Warren Galyen
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
    public static class Picture
    {
        public static Bitmap Load(string fileName)
        {
            Bitmap bmp = null;
            using (FileStream fs = new FileStream(fileName, FileMode.Open, FileAccess.Read))
            {
                bmp = Load(fs);
            }

            if (bmp == null)
            {
                if (Path.GetExtension(fileName).ToLower().Contains("tga"))
                    bmp = TgaReader.Load(fileName);
            }

            if (bmp == null)
            {
                if (Path.GetExtension(fileName).ToLower().Contains("cut"))
                    bmp = CutReader.Load(fileName);
            }

            if (bmp == null)
            {
                if (Path.GetExtension(fileName).ToLower().Contains("sgi") || Path.GetExtension(fileName).ToLower().Contains("rgb") || Path.GetExtension(fileName).ToLower().Contains("bw"))
                    bmp = SgiReader.Load(fileName);
            }

            if (bmp == null)
            {
                if (Path.GetExtension(fileName).ToLower().Contains("xpm"))
                    bmp = XpmReader.Load(fileName);
            }

            if (bmp == null)
            {
                if (Path.GetExtension(fileName).ToLower().Contains("dds"))
                    bmp = DdsReader.Load(fileName);
            }

            return bmp;
        }

        public static Bitmap Load(Stream stream)
        {
            Bitmap bmp = null;

            // read the first few bytes of the file to determine what format it is
            byte[] header = new byte[256];
            stream.Read(header, 0, header.Length);
            stream.Seek(0, SeekOrigin.Begin);
            if ((header[0] == 0xA) && (header[1] <= 0x5) && (header[2] == 0x1) && ((header[3] == 0x1) || (header[3] == 0x2) || (header[3] == 0x4) || (header[3] == 0x8)))
            {
                bmp = PcxReader.Load(stream);
            }
            else if ((header[0] == 'P') && ((header[1] >= '1') && (header[1] <= '6')) && ((header[2] == 0xA) || (header[2] == 0xD) || (header[2] == 0x20)))
            {
                bmp = PnmReader.Load(stream);
            }
            else if ((header[0] == 0x59) && (header[1] == 0xa6) && (header[2] == 0x6a) && (header[3] == 0x95))
            {
                bmp = RasReader.Load(stream);
            }

            else if ((header[0x80] == 'D') && (header[0x81] == 'I') && (header[0x82] == 'C') && (header[0x83] == 'M'))
            {
                bmp = DicomReader.Load(stream);
            }
            else if ((header[0x41] == 'P') && (header[0x42] == 'N') && (header[0x43] == 'T') && (header[0x44] == 'G'))
            {
                bmp = MacPaintReader.Load(stream);
            }
            else if ((header[0] == 'F') && (header[1] == 'O') && (header[2] == 'R') && (header[3] == 'M')
                 && (((header[8] == 'I') && (header[9] == 'L') && (header[10] == 'B') && (header[11] == 'M')) || ((header[8] == 'P') && (header[9] == 'B') && (header[10] == 'M'))))
            {
                bmp = IlbmReader.Load(stream);
            }
            else if ((header[0] == 'F') && (header[1] == 'O') && (header[2] == 'R') && (header[3] == 'M')
                 && (header[8] == 'D') && (header[9] == 'E') && (header[10] == 'E') && (header[11] == 'P'))
            {
                bmp = DeepReader.Load(stream);
            }
            else if ((header[0] == 'S') && (header[1] >= 'I') && (header[2] >= 'M') && (header[3] >= 'P'))
            {
                bmp = FitsReader.Load(stream);
            }
            else if ((header[0x0] == 1) && (header[0x1] == 0xDA))
            {
                bmp = SgiReader.Load(stream);
            }

            return bmp;
        }
    }
}
