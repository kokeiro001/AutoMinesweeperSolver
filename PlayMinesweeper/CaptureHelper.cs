using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace PlayMinesweeper
{
  static class CaptureHelper
  {
    #region private

    private const int SRCCOPY = 13369376;
    private const int CAPTUREBLT = 1073741824;

    [DllImport("user32.dll")]
    private static extern IntPtr GetDC(IntPtr hwnd);

    [DllImport("gdi32.dll")]
    private static extern int BitBlt(IntPtr hDestDC,
        int x,
        int y,
        int nWidth,
        int nHeight,
        IntPtr hSrcDC,
        int xSrc,
        int ySrc,
        int dwRop);
    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
      public int left;
      public int top;
      public int right;
      public int bottom;
    }

    [DllImport("user32.dll")]
    private static extern IntPtr GetWindowDC(IntPtr hwnd);


    [DllImport("user32.dll")]
    private static extern IntPtr ReleaseDC(IntPtr hwnd, IntPtr hdc);

    [DllImport("user32.dll")]
    private static extern int GetWindowRect(IntPtr hwnd, ref RECT lpRect);

    #endregion

    public static Bitmap CaptureScreen()
    {
      IntPtr disDC = GetDC(IntPtr.Zero);
      Bitmap bmp = new Bitmap(Screen.PrimaryScreen.Bounds.Width, Screen.PrimaryScreen.Bounds.Height);
      using (Graphics g = Graphics.FromImage(bmp))
      {
        IntPtr hDC = g.GetHdc();
        BitBlt(hDC, 0, 0, bmp.Width, bmp.Height, disDC, 0, 0, SRCCOPY);
        g.ReleaseHdc(hDC);
      }
      ReleaseDC(IntPtr.Zero, disDC);

      return bmp;
    }

    public static void CaptureWindow(IntPtr hWnd, Bitmap bmp, Graphics g)
    {
      IntPtr winDC = GetWindowDC(hWnd);

      RECT winRect = new RECT();
      GetWindowRect(hWnd, ref winRect);

      IntPtr hDC = g.GetHdc();

      BitBlt(hDC, 0, 0, bmp.Width, bmp.Height, winDC, 0, 0, SRCCOPY);
      g.ReleaseHdc(hDC);
    }

    public static Size GetWindowSize(IntPtr hWnd)
    {
      IntPtr winDC = GetWindowDC(hWnd);
      RECT rect = new RECT();
      GetWindowRect(hWnd, ref rect);
      return new Size(rect.right - rect.left, rect.bottom - rect.top);
    }

    public static Rectangle GetWindowRect(IntPtr hWnd)
    {
      RECT rect = new RECT();
      GetWindowRect(hWnd, ref rect);
      return new Rectangle(rect.left, rect.top, rect.right - rect.left, rect.bottom - rect.top);
    }
  }

}
