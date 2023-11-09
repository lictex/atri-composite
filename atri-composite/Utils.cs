using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace atri_composite
{
    public static class Utils
    {
        public static BitmapSource ToBitmapSource(this Bitmap bitmap, bool disposeSource = false)
        {
            IntPtr p = bitmap.GetHbitmap();
            try
            {
                return System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
                    p,
                    IntPtr.Zero,
                    System.Windows.Int32Rect.Empty,
                    BitmapSizeOptions.FromWidthAndHeight(bitmap.Width, bitmap.Height));
            }
            finally
            {
                DeleteObject(p);
                if (disposeSource) try { bitmap.Dispose(); } catch { }
            }
        }

        public static Bitmap Crop(this Bitmap bitmap, bool disposeSource = false)
        {
            if (bitmap.PixelFormat != PixelFormat.Format32bppArgb) throw new NotSupportedException();

            var bitmapData = bitmap.LockBits(new Rectangle(Point.Empty, bitmap.Size), ImageLockMode.ReadOnly, bitmap.PixelFormat);
            var bpp = Image.GetPixelFormatSize(bitmapData.PixelFormat) / 8;

            unsafe byte AlphaAt(int x, int y) => *((byte*)bitmapData.Scan0 + bitmapData.Stride * y + bpp * x + 3);

            int l = -1, t = -1, r = -1, b = -1;
            Parallel.Invoke(() =>
            {
                for (var ln = 0; ln < bitmapData.Width; ln++)
                    for (var s = 0; s < bitmapData.Height; s++)
                        if (AlphaAt(ln, s) != 0) { l = ln; return; }
            }, () =>
            {
                for (var ln = 0; ln < bitmapData.Height; ln++)
                    for (var s = 0; s < bitmapData.Width; s++)
                        if (AlphaAt(s, ln) != 0) { t = ln; return; }
            }, () =>
            {
                for (var ln = 0; ln < bitmapData.Width; ln++)
                    for (var s = 0; s < bitmapData.Height; s++)
                        if (AlphaAt(bitmapData.Width - 1 - ln, s) != 0) { r = ln; return; }
            }, () =>
            {
                for (var ln = 0; ln < bitmapData.Height; ln++)
                    for (var s = 0; s < bitmapData.Width; s++)
                        if (AlphaAt(s, bitmapData.Height - 1 - ln) != 0) { b = ln; return; }
            });

            bitmap.UnlockBits(bitmapData);

            if (l < 0 || t < 0 || r < 0 || b < 0) throw new ArgumentException();

            var cropBound = new Rectangle(l, t, bitmap.Width - l - r, bitmap.Height - t - b);
            var newBitmap = new Bitmap(cropBound.Width, cropBound.Height);
            using (var g = Graphics.FromImage(newBitmap)) g.DrawImage(bitmap, new Rectangle(Point.Empty, newBitmap.Size), cropBound, GraphicsUnit.Pixel);

            if (disposeSource) try { bitmap.Dispose(); } catch { }
            return newBitmap;
        }

        private static readonly ConcurrentDictionary<string, JArray> pbdCache = new ConcurrentDictionary<string, JArray>();
        public static JArray LoadPBDFile(string pbdPath, bool normalize = false)
        {
            // also allow stands to be placed in the data root
            if (!File.Exists(pbdPath))
            {
                pbdPath = Path.Combine(Directory.GetParent(Path.GetDirectoryName(pbdPath)).FullName, Path.GetFileName(pbdPath));
            }

            return pbdCache.GetOrAdd(pbdPath, o =>
              {
                  var proc = Process.Start(new ProcessStartInfo()
                  {
                      FileName = "pbd2json.exe",
                      Arguments = $"\"{pbdPath}\"",
                      UseShellExecute = false,
                      RedirectStandardOutput = true,
                      CreateNoWindow = true
                  });
                  var json = proc.StandardOutput.ReadToEnd();
                  proc.WaitForExit();
                  return JArray.Parse(normalize ? json.Normalize(NormalizationForm.FormKC) : json);
              });
        }

        [System.Runtime.InteropServices.DllImport("gdi32.dll")]
        private static extern bool DeleteObject(IntPtr hObject);
    }
}
