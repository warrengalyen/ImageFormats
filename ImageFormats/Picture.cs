using System;
using System.Drawing;
using System.IO;
using System.Text;

/*
 
This is a class library that contains image decoders for old and/or
obscure image formats (.TGA, .PCX, .PPM, RAS, etc.). Refer to the
individual source code files for each image type for more information.

Copyright 2013 by Warren Galyen
You may use this source code in your application(s) free of charge,
as long as attribution is given to me (Warren Galyen) and my URL
(https://mechanikadesign.com) in your application's "about" box and/or
documentation.

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

            return bmp;
        }

        public static Bitmap Load(Stream stream)
        {
            Bitmap bmp = null;

            // read the first few bytes of the file to determine what format it is
            byte[] header = new byte[256];
            stream.Read(header, 0, header.Length);
            stream.Seek(0, SeekOrigin.Begin);

            if ((header[0] == 0xA) && (header[1] == 0x5) && (header[2] == 0x1) && (header[4] == 0) && (header[5] == 0))
            {
                bmp = PcxReader.Load(stream);
            }
            else if ((header[0] == 'P') && ((header[1] >= '1') && (header[1] <= '6')) && ((header[2] == 0xA) || (header[2] == 0xD)))
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

            return bmp;
        }
    }
}
