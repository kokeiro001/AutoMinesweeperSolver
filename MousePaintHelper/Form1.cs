using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using System.Diagnostics;

using System.Runtime.InteropServices;
using System.Threading;


namespace MousePaintHelper
{
	public partial class Form1 : Form
	{
		public Form1()
		{
			InitializeComponent();
		}

		Point[] vertex = new Point[] 
		{
			new Point(500, 300),
			new Point(400, 500),
			new Point(600, 400),
			new Point(300, 400),
			new Point(600, 500),
			new Point(500, 300),
		};

		private void button1_Click(object sender, EventArgs e)
		{
			List<Process> ps = Process.GetProcesses().ToList();
			Process paint = ps.Find(p => p.ProcessName == "mspaint");

			// ペイントをアクティブにする
			Win32APIHelper.SetForceForegroundWindow(paint.MainWindowHandle);

			Thread.Sleep(1000);
			MouseEventHelper.Paint(vertex);
		}
	}

	static public class MouseEventHelper
	{
		// マウスカーソルを、指定した位置へ移動したり、クリックしたりする

		// "user32.dll" の SendInput() を使い、マウスイベントを生成するので、
		// 他のアプリケーションのウィンドウ・オブジェクトをクリックすることも可能

		// Note: マウスカーソルの移動方法は、Cursor.Position と SendInput() の2通りあるが、
		//       ドラッグ操作中の「マウスカーソルの移動」は、途中で割り込みが入らないよう
		//       SendInput() で行う方が安全である。

		// Note: MOUSEEVENTF_ABSOLUTE での座標指定は、特殊な座標単位系なので注意せよ。
		//       画面左上のコーナーが (0, 0)、画面右下のコーナーが (65535, 65535)である。

		// Note: No MOUSEEVENTF_ABSOLUTE での座標指定は、相対座標系になるが、単位が必ず
		//       しも 1px ではないので注意せよ。
		//       各 PC で設定された mouse speed と acceleration level に依存する。

		// Note: SendInput()パラメータの詳細は、MSDN『 MOUSEINPUT Structure 』を参照せよ。

		[DllImport("user32.dll")]
		extern static uint SendInput(
				uint nInputs,   // INPUT 構造体の数(イベント数)
				INPUT[] pInputs,   // INPUT 構造体
				int cbSize     // INPUT 構造体のサイズ
				);

		[StructLayout(LayoutKind.Sequential)]  // アンマネージ DLL 対応用 struct 記述宣言
		struct INPUT
		{
			public int type;  // 0 = INPUT_MOUSE(デフォルト), 1 = INPUT_KEYBOARD
			public MOUSEINPUT mi;
		}

		[StructLayout(LayoutKind.Sequential)]  // アンマネージ DLL 対応用 struct 記述宣言
		struct MOUSEINPUT
		{
			public int dx;
			public int dy;
			public int mouseData;  // amount of wheel movement
			public int dwFlags;
			public int time;  // time stamp for the event
			public IntPtr dwExtraInfo;
			// Note: struct の場合、デフォルト(パラメータなしの)コンストラクタは、
			//       言語側で定義済みで、フィールドを 0 に初期化する。
		}

		// dwFlags
		const int MOUSEEVENTF_MOVED = 0x0001;
		const int MOUSEEVENTF_LEFTDOWN = 0x0002;  // 左ボタン Down
		const int MOUSEEVENTF_LEFTUP = 0x0004;  // 左ボタン Up
		const int MOUSEEVENTF_RIGHTDOWN = 0x0008;  // 右ボタン Down
		const int MOUSEEVENTF_RIGHTUP = 0x0010;  // 右ボタン Up
		const int MOUSEEVENTF_MIDDLEDOWN = 0x0020;  // 中ボタン Down
		const int MOUSEEVENTF_MIDDLEUP = 0x0040;  // 中ボタン Up
		const int MOUSEEVENTF_WHEEL = 0x0080;
		const int MOUSEEVENTF_XDOWN = 0x0100;
		const int MOUSEEVENTF_XUP = 0x0200;
		const int MOUSEEVENTF_ABSOLUTE = 0x8000;

		const int screen_length = 0x10000;  // for MOUSEEVENTF_ABSOLUTE (この値は固定)

		public static void Move(int x, int y)
		{
			int h = System.Windows.Forms.Screen.PrimaryScreen.Bounds.Height;
			int w = System.Windows.Forms.Screen.PrimaryScreen.Bounds.Width;

			INPUT[] input = new INPUT[1];
			input[0].mi.dx = (x * screen_length) / w;
			input[0].mi.dy = (y * screen_length) / h;
			input[0].mi.dwFlags = MOUSEEVENTF_MOVED | MOUSEEVENTF_ABSOLUTE;

			SendInput(1, input, Marshal.SizeOf(input[0]));
		}

		public static void Paint(Point[] points)
		{
			INPUT[] input = new INPUT[points.Length + 2];

			// 移動
			int h = System.Windows.Forms.Screen.PrimaryScreen.Bounds.Height;
			int w = System.Windows.Forms.Screen.PrimaryScreen.Bounds.Width;

			// 初期位置
			input[0].mi.dx = (points[0].X * screen_length) / w;
			input[0].mi.dy = (points[0].Y * screen_length) / h;
			input[0].mi.dwFlags = MOUSEEVENTF_MOVED | MOUSEEVENTF_ABSOLUTE;

			// ドラッグ開始
			input[1].mi.dwFlags = MOUSEEVENTF_LEFTDOWN;

			for (int i = 1; i < points.Length; i++)
			{
				input[i + 1].mi.dx = (points[i].X * screen_length) / w;
				input[i + 1].mi.dy = (points[i].Y * screen_length) / h;
				input[i + 1].mi.dwFlags = MOUSEEVENTF_MOVED | MOUSEEVENTF_ABSOLUTE;
			}

			// ドラッグ終了
			input[points.Length + 1].mi.dwFlags = MOUSEEVENTF_LEFTUP;

			SendInput((uint)(input.Length), input, Marshal.SizeOf(input[0]));
		}

		public static void LeftButtonClick()
		{
			LeftButtonClick(1);
			//// ドラッグ操作の準備 (struct 配列の宣言)
			//INPUT[] input = new INPUT[2];

			//// ドラッグ操作の準備 (第1イベントの定義 = 左ボタン Down)
			//input[0].mi.dwFlags = MOUSEEVENTF_LEFTDOWN;
			//// ドラッグ操作の準備 (第2イベントの定義 = 左ボタン Up)
			//input[1].mi.dwFlags = MOUSEEVENTF_LEFTUP;

			//SendInput(2, input, Marshal.SizeOf(input[0]));
		}

		public static void LeftButtonClick(int count)
		{
			// ドラッグ操作の準備 (struct 配列の宣言)
			INPUT[] input = new INPUT[2 * count];

			for (int i = 0; i < count; i++)
			{
				// ドラッグ操作の準備 (第1イベントの定義 = 左ボタン Down)
				input[i * 2].mi.dwFlags = MOUSEEVENTF_LEFTDOWN;
				// ドラッグ操作の準備 (第2イベントの定義 = 左ボタン Up)
				input[i * 2 + 1].mi.dwFlags = MOUSEEVENTF_LEFTUP;
			}

			SendInput((uint)(count * 2), input, Marshal.SizeOf(input[0]));
		}
	}


	// Win32APIを呼び出すためのクラス
	public static class Win32APIHelper
	{

		// Win32Apiの宣言
		[DllImport("user32.dll", SetLastError = true)]
		static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

		[DllImport("user32.dll")]
		static extern IntPtr GetForegroundWindow();

		[DllImport("user32.dll")]
		[return: MarshalAs(UnmanagedType.Bool)]
		static extern bool SetForegroundWindow(IntPtr hWnd);

		[DllImport("user32.dll", SetLastError = true)]
		static extern bool BringWindowToTop(IntPtr hWnd);

		[DllImport("user32.dll")]
		extern static bool AttachThreadInput(uint idAttach, uint idAttachTo, bool fAttach);

		[return: MarshalAs(UnmanagedType.Bool)]
		[DllImport("user32.dll", SetLastError = true)]
		extern static bool PostMessage(HandleRef hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

		/// <summary>
		/// 強制的にウィンドウをアクティブにする際に利用される独自ウィンドウメッセージ (WM_USERの値に1加算)
		/// </summary>
		const int MY_FORCE_FOREGROUND_MESSAGE = 0x400 + 1;



		[DllImport("user32.dll")]
		public static extern uint keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);


		/// <summary>
		/// 指定されたハンドルを持つウィンドウを強制的にアクティブにします。
		/// </summary>
		/// <param name="targetHandle">対象となるウィンドウハンドルオブジェクト</param>
		static public void SetForceForegroundWindow(IntPtr targetHandle)
		{
			uint nullProcessId = 0;

			// ターゲットとなるハンドルのスレッドIDを取得.
			uint targetThreadId = GetWindowThreadProcessId(targetHandle, out nullProcessId);
			// 現在アクティブとなっているウィンドウのスレッドIDを取得.
			uint currentActiveThreadId = GetWindowThreadProcessId(GetForegroundWindow(), out nullProcessId);

			// アクティブ処理
			SetForegroundWindow(targetHandle);
			if (targetThreadId == currentActiveThreadId)
			{
				// 現在アクティブなのが自分の場合は前面に持ってくる。
				BringWindowToTop(targetHandle);
			}
			else
			{
				// 別のプロセスがアクティブな場合は、そのプロセスにアタッチし、入力を奪う.
				AttachThreadInput(targetThreadId, currentActiveThreadId, true);
				try
				{
					// 自分を前面に持ってくる。
					BringWindowToTop(targetHandle);
				}
				finally
				{
					// アタッチを解除.
					AttachThreadInput(targetThreadId, currentActiveThreadId, false);
				}
			}
		}

	}
}
