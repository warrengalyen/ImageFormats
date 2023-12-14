﻿using System;

namespace MechanikaDesign.ImageFormats
{
    public static class Util
    {
        public static void log(string str)
        {
            System.Diagnostics.Debug.WriteLine(str);
        }

        public static bool TryParseFloat(string str, out float f)
        {
            return float.TryParse(str, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out f);
        }

        public static UInt16 LittleEndian(UInt16 val)
        {
            return BitConverter.IsLittleEndian ? val : ConvEndian(val);
        }
        public static UInt32 LittleEndian(UInt32 val)
        {
            return BitConverter.IsLittleEndian ? val : ConvEndian(val);
        }

        public static UInt16 BigEndian(UInt16 val)
        {
            return !BitConverter.IsLittleEndian ? val : ConvEndian(val);
        }
        public static UInt32 BigEndian(UInt32 val)
        {
            return !BitConverter.IsLittleEndian ? val : ConvEndian(val);
        }

        private static UInt16 ConvEndian(UInt16 val)
        {
            UInt16 temp;
            temp = (UInt16)(val << 8); temp &= 0xFF00; temp |= (UInt16)((val >> 8) & 0xFF);
            return temp;
        }
        private static UInt32 ConvEndian(UInt32 val)
        {
            UInt32 temp = (val & 0x000000FF) << 24;
            temp |= (val & 0x0000FF00) << 8;
            temp |= (val & 0x00FF0000) >> 8;
            temp |= (val & 0xFF000000) >> 24;
            return (temp);
        }
    }
}