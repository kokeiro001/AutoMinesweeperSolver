using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace PlayMinesweeper
{
	public static class GlobalKeybordCapture
	{
		[System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
		public struct KBDLLHOOKSTRUCT
		{
			public int vkCode;
			public int scanCode;
			public int flags;
			public int time;
			public IntPtr dwExtraInfo;
		}
		[System.Runtime.InteropServices.UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.Cdecl)]
		delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, ref KBDLLHOOKSTRUCT lParam);
		[System.Runtime.InteropServices.DllImport("kernel32.dll")]
		static extern IntPtr GetModuleHandle(string lpModuleName);
		[System.Runtime.InteropServices.DllImport("user32.dll")]
		static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, int dwThreadId);
		[System.Runtime.InteropServices.DllImport("user32.dll")]
		static extern IntPtr CallNextHookEx(IntPtr hHook, int nCode, IntPtr wParam, ref KBDLLHOOKSTRUCT lParam);
		[System.Runtime.InteropServices.DllImport("user32.dll")]
		static extern bool UnhookWindowsHookEx(IntPtr hHook);

		public const int WH_KEYBOARD_LL = 13;
		public const int HC_ACTION = 0;
		public const int WM_KEYDOWN = 0x0100;
		public const int WM_KEYUP = 0x0101;
		public const int WM_SYSKEYDOWN = 0x0104;
		public const int WM_SYSKEYUP = 0x0105;

		public sealed class KeybordCaptureEventArgs : EventArgs
		{
			private int m_keyCode;
			private int m_scanCode;
			private int m_flags;
			private int m_time;
			private bool m_cancel;

			internal KeybordCaptureEventArgs(KBDLLHOOKSTRUCT keyData)
			{
				this.m_keyCode = keyData.vkCode;
				this.m_scanCode = keyData.scanCode;
				this.m_flags = keyData.flags;
				this.m_time = keyData.time;
				this.m_cancel = false;
			}

			public int KeyCode { get { return this.m_keyCode; } }
			public int ScanCode { get { return this.m_scanCode; } }
			public int Flags { get { return this.m_flags; } }
			public int Time { get { return this.m_time; } }
			public bool Cancel
			{
				set { this.m_cancel = value; }
				get { return this.m_cancel; }
			}
		}

		private static IntPtr s_hook;
		private static LowLevelKeyboardProc s_proc;
		public static event EventHandler<KeybordCaptureEventArgs> SysKeyDown;
		public static event EventHandler<KeybordCaptureEventArgs> KeyDown;
		public static event EventHandler<KeybordCaptureEventArgs> SysKeyUp;
		public static event EventHandler<KeybordCaptureEventArgs> KeyUp;

		public static void BeginCapture()
		{
			s_proc = new LowLevelKeyboardProc(HookProc);
			s_hook = SetWindowsHookEx(WH_KEYBOARD_LL,
					s_proc,
					GetModuleHandle(Process.GetCurrentProcess().MainModule.ModuleName),
					0);
			AppDomain.CurrentDomain.DomainUnload += delegate
			{
				if (s_hook != IntPtr.Zero)
					UnhookWindowsHookEx(s_hook);
			};
		}

		static IntPtr HookProc(int nCode, IntPtr wParam, ref KBDLLHOOKSTRUCT lParam)
		{
			bool cancel = false;
			if (nCode == HC_ACTION)
			{
				KeybordCaptureEventArgs ev = new KeybordCaptureEventArgs(lParam);
				switch (wParam.ToInt32())
				{
					case WM_KEYDOWN:
						CallEvent(KeyDown, ev);
						break;

					case WM_KEYUP:
						CallEvent(KeyUp, ev);
						break;

					case WM_SYSKEYDOWN:
						CallEvent(SysKeyDown, ev);
						break;

					case WM_SYSKEYUP:
						CallEvent(SysKeyUp, ev);
						break;
				}
				cancel = ev.Cancel;
			}
			return cancel ? (IntPtr)1 : CallNextHookEx(s_hook, nCode, wParam, ref lParam);
		}

		public static bool IsCapture { get { return s_hook != IntPtr.Zero; } }

		private static void CallEvent(EventHandler<KeybordCaptureEventArgs> eh, KeybordCaptureEventArgs ev)
		{
			if (eh != null)
				eh(null, ev);
		}
	}

	public enum KeyState
	{
		KeyDown,
		KeyUp
	}
	
	static class KeyboardState
	{
		static Dictionary<int, KeyState> state = new Dictionary<int, KeyState>();

		static public void Down(int keycode)
		{
			if (state.ContainsKey(keycode))
			{
				state[keycode] = KeyState.KeyDown;
			}
			else
			{
				state.Add(keycode, KeyState.KeyDown);
			}
		}
		static public void Up(int keycode)
		{
			if (state.ContainsKey(keycode))
			{
				state[keycode] = KeyState.KeyUp;
			}
			else
			{
				state.Add(keycode, KeyState.KeyUp);
			}
		}

		static public KeyState GetKeyState(int keycode)
		{
			if (!state.ContainsKey(keycode))
			{
				state.Add(keycode, KeyState.KeyUp);
			}
			return state[keycode];
		}

		static public KeyState GetKeyState(System.Windows.Forms.Keys keys)
		{
			return GetKeyState((int)keys);
		}

		static public bool IsDown(System.Windows.Forms.Keys keys)
		{
			return GetKeyState((int)keys) == KeyState.KeyDown;
		}

		static public bool IsUp(System.Windows.Forms.Keys keys)
		{
			return GetKeyState((int)keys) == KeyState.KeyUp;
		}
	}
}
