using System;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using ZXing;
using ZXing.Common;

namespace QBasCopier;

public static class Qr
{
    public static Bitmap? Make(string text, int px = 640)
    {
        try
        {
            var bm = new QRCodeWriter().encode(text, BarcodeFormat.QR_CODE, px, px);
            int w = bm.Width, h = bm.Height;
            var data = new byte[w * h * 4];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    bool on = bm[x, y];
                    byte c = on ? (byte)10 : (byte)255;
                    int i = (y * w + x) * 4;
                    data[i] = c; data[i + 1] = c; data[i + 2] = c; data[i + 3] = 255;
                }
            var wb = new WriteableBitmap(new PixelSize(w, h), new Vector(96, 96), PixelFormat.Bgra8888, AlphaFormat.Opaque);
            using (var fb = wb.Lock())
            {
                for (int y = 0; y < h; y++)
                    Marshal.Copy(data, y * w * 4, fb.Address + y * (long)fb.RowBytes, w * 4);
            }
            return wb;
        }
        catch { return null; }
    }
}