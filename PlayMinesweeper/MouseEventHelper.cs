using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

using System.Runtime.InteropServices;
using System.Threading;

using System.Windows.Forms;

namespace PlayMinesweeper
{
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

		public static void RightButtonClick()
		{
			int count = 1;
			// ドラッグ操作の準備 (struct 配列の宣言)
			INPUT[] input = new INPUT[2 * count];

			for (int i = 0; i < count; i++)
			{
				// ドラッグ操作の準備 (第1イベントの定義 = 左ボタン Down)
				input[i * 2].mi.dwFlags = MOUSEEVENTF_RIGHTDOWN;
				// ドラッグ操作の準備 (第2イベントの定義 = 左ボタン Up)
				input[i * 2 + 1].mi.dwFlags = MOUSEEVENTF_RIGHTUP;
			}

			SendInput((uint)(count * 2), input, Marshal.SizeOf(input[0]));
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

		internal static void Move_Click(int x, int y, MouseButtons mouseButtons)
		{
			int h = System.Windows.Forms.Screen.PrimaryScreen.Bounds.Height;
			int w = System.Windows.Forms.Screen.PrimaryScreen.Bounds.Width;

			INPUT[] input = new INPUT[3];
			input[0].mi.dx = (x * screen_length) / w;
			input[0].mi.dy = (y * screen_length) / h;
			input[0].mi.dwFlags = MOUSEEVENTF_MOVED | MOUSEEVENTF_ABSOLUTE;

			if (mouseButtons == MouseButtons.Right)
			{
				input[1].mi.dwFlags = MOUSEEVENTF_RIGHTDOWN;
				input[2].mi.dwFlags = MOUSEEVENTF_RIGHTUP;
			}
			else
			{
				input[1].mi.dwFlags = MOUSEEVENTF_LEFTDOWN;
				input[2].mi.dwFlags = MOUSEEVENTF_LEFTUP;
			}

			SendInput((uint)(3), input, Marshal.SizeOf(input[0]));
		}
	}
}
