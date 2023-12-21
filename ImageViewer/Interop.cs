using SixLabors.ImageSharp;
using System.IO;
using SixImg = SixLabors.ImageSharp.Image;
using WinBit = System.Drawing.Bitmap;
using WinImg = System.Drawing.Image;

namespace ImageViewer
{
    internal static class Interop
    {
        public static WinBit AsNative(this SixImg image)
        {
            if (image == null)
                return null;
            using var mem = new MemoryStream();
            image.SaveAsPng(mem);
            var bitmap = WinImg.FromStream(mem);
            return (WinBit)bitmap;
        }
    }
}