using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

using System.Runtime.InteropServices;
using System.Threading;

using System.Windows.Forms;
using TestClassLibrary;

namespace NotepadSaver
{
	class Program
	{
		static void Main(string[] args)
		{
			NotepadSaver hoge = new NotepadSaver();
			hoge.Run();
		}
	}

	class NotepadSaver
	{
		public void Run()
		{
			List<Process> ps = Process.GetProcesses().ToList();
			Process notepad = ps.Find(p => p.ProcessName == "notepad");
			if (notepad == null) return;
			TextSendSample(notepad);

			//MouseEventHelper.Move(0, -100);
		}

		private void TextSendSample(Process notepad)
		{
			Win32APIHelper.SetForceForegroundWindow(notepad.MainWindowHandle);
			string sample = @"
そういえば先ほど紹介するのを忘れてました
キーボードのシュミレーターとして使うことが出来ます
ボクはたけのこ派です
明日は明日の風が吹きます
スーパーギャルデリックアワー
情報工の3年次です

これ使えばゲームのマクロとかできると思います
コケいろ
さ；ｌｄｋｊｆぁ；ｋｊｄぁｓｋｄｇｆぁｋｓｄｆ
亜sdｆｋ；あｊｓｄｋｆ；あｓｋｆｌ；あｋｓｊふぁｓ
亜sdｌｋｆｊぁｓｄｆｊぁｓｄｊｌｋ；さｊｆｌｋ；あｓｊｆかｓ
差lkｄｆじゃ；ｓｌｄｋｊｆｄぁｓｋｊｆｌ；あｓｊｆｌ；あｓｋｊｌｄ；ふぁ
ｄくぃおるうぇくぉぷｒくぉえいｓｚｌｋｄｋｊｆ
";
			SendKeys.SendWait(sample);
			//SendKeys.SendWait("^s");
		}

		void SendKeyCode(string text)
		{
			text = text.ToUpper();
			// 取得された文字列を１文字ずつ検査し、
			// シミュレート可能文字列であればkeybd_event() APIに
			// 渡す。
			foreach (char pi in text)
			{
				// 入力対象の文字列かどうかを調べる
				if (checkKey(pi))
				{
					// キーの押し下げをシミュレートする。
					Win32APIHelper.keybd_event((byte)pi, 0, 0, (UIntPtr)0);
					// キーの解放をシミュレートする。
					Win32APIHelper.keybd_event((byte)pi, 0, 2, (UIntPtr)0);

					// dwWaitTimeミリ秒間待機する
					// (キーの取りこぼしを防ぐため)
					Thread.Sleep(20);

				}
			}
		}

		/// <summary>
		/// キーボードシミュレート対象文字列かどうかを調べる。
		/// </summary>
		/// <returns>対象ならばtrueを返し、そうでなければfalseを返す</returns>
		public static bool checkKey(char c)
		{
			if ('0' <= c && c <= '9')
			{ // 数字ならＯＫ
				return true;
			}
			else if ('A' <= c && c <= 'Z')
			{ // 英大文字ならＯＫ
				return true;
			}
			else if (c == ' ')
			{ // スペースならＯＫ
				return true;
			}
			else if (c == '\t')
			{ // タブならＯＫ
				return true;
			}

			// それら以外の場合はＮＧ
			return false;
		}
	}

}
