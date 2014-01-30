using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace ScreenCaptureTest
{
	class CaptureHelper
	{
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

		[DllImport("user32.dll")]
		private static extern IntPtr ReleaseDC(IntPtr hwnd, IntPtr hdc);

		/// <summary>
		/// プライマリスクリーンの画像を取得する
		/// </summary>
		/// <returns>プライマリスクリーンの画像</returns>
		public static Bitmap CaptureScreen()
		{
			//プライマリモニタのデバイスコンテキストを取得
			IntPtr disDC = GetDC(IntPtr.Zero);
			//Bitmapの作成
			Bitmap bmp = new Bitmap(Screen.PrimaryScreen.Bounds.Width,
					Screen.PrimaryScreen.Bounds.Height);
			//Graphicsの作成
			Graphics g = Graphics.FromImage(bmp);
			//Graphicsのデバイスコンテキストを取得
			IntPtr hDC = g.GetHdc();
			//Bitmapに画像をコピーする
			BitBlt(hDC, 0, 0, bmp.Width, bmp.Height,
					disDC, 0, 0, SRCCOPY);
			//解放
			g.ReleaseHdc(hDC);
			g.Dispose();
			ReleaseDC(IntPtr.Zero, disDC);

			return bmp;
		}

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
		public static extern IntPtr GetForegroundWindow();

		[DllImport("user32.dll")]
		private static extern int GetWindowRect(IntPtr hwnd,
				ref  RECT lpRect);

		/// <summary>アクティブなウィンドウの画像を取得する</summary>
		/// <returns>アクティブなウィンドウの画像</returns>
		public static Bitmap CaptureActiveWindow()
		{
			//アクティブなウィンドウのデバイスコンテキストを取得
			IntPtr hWnd = GetForegroundWindow();
			IntPtr winDC = GetWindowDC(hWnd);

			//ウィンドウの大きさを取得
			RECT winRect = new RECT();
			GetWindowRect(hWnd, ref winRect);

			Bitmap bmp = new Bitmap(winRect.right - winRect.left, winRect.bottom - winRect.top);

			Graphics g = Graphics.FromImage(bmp);
			IntPtr hDC = g.GetHdc();

			//Bitmapに画像をコピーする
			BitBlt(hDC, 0, 0, bmp.Width, bmp.Height, winDC, 0, 0, SRCCOPY);

			//解放
			g.ReleaseHdc(hDC);
			g.Dispose();
			ReleaseDC(hWnd, winDC);

			return bmp;
		}

		public static void CaptureActiveWindow(IntPtr hWnd, Bitmap bmp, Graphics g)
		{
			//アクティブなウィンドウのデバイスコンテキストを取得
			IntPtr winDC = GetWindowDC(hWnd);

			//ウィンドウの大きさを取得
			RECT winRect = new RECT();
			GetWindowRect(hWnd, ref winRect);

			IntPtr hDC = g.GetHdc();

			//Bitmapに画像をコピーする
			BitBlt(hDC, 0, 0, bmp.Width, bmp.Height, winDC, 0, 0, SRCCOPY);
			g.ReleaseHdc(hDC);
		}

		public static Size GetActiveWindowSize()
		{
			return GetWindowSize(GetForegroundWindow());
		}

		public static Size GetWindowSize(IntPtr hWnd)
		{
			IntPtr winDC = GetWindowDC(hWnd);
			RECT rect = new RECT();
			GetWindowRect(hWnd, ref rect);
			return new Size(rect.right - rect.left, rect.bottom - rect.top);
		}
	}

}
