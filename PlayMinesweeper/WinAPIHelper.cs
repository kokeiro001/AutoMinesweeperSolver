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
	public delegate int WNDENUMPROC(IntPtr hwnd, int lParam);

	// Win32APIを呼び出すためのクラス
	public static class Win32APIHelper
	{
		public const int GA_PARENT = 1;			// 親ウィンドウのハンドルを取得する
		public const int GA_ROOT = 2;				// 親ウィンドウを辿っていき、最もトップにあるウィンドウのハンドルを取得する
		public const int GA_ROOTOWNER = 3;		// 親ウィンドウとオーナーウィンドウの両方を辿っていき、最もトップにあるウィンドウのハンドルを取得する

		public const int EM_SETPASSWORDCHAR = 0xCC;

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

		[DllImport("user32.dll")]
		static public extern int EnumChildWindows(IntPtr hWndParent, WNDENUMPROC lpEnumFunc, int lParam);

		[DllImport("user32.dll")]
		static public extern bool EnumWindows(WNDENUMPROC lpEnumFunc, int lParam);

		[DllImport("user32.dll")]
		public static extern bool IsWindowVisible(IntPtr hWnd);

		[DllImport("user32.dll")]
		public static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);


		[DllImport("user32.dll")]
		public static extern IntPtr GetParent(IntPtr hWnd);

		[DllImport("user32.dll")]
		public static extern IntPtr GetParent(IntPtr hWnd, uint gaFlags);

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
