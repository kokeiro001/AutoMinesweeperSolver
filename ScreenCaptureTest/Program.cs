using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace ScreenCaptureTest
{
	static class Program
	{
		/// <summary>
		/// アプリケーションのメイン エントリ ポイントです。
		/// </summary>
		[STAThread]
		static void Main()
		{
			//Application.EnableVisualStyles();
			//Application.SetCompatibleTextRenderingDefault(false);
			//Application.Run(new CaptureForm());
			Game game = new Game();
			game.Run();
		}
	}
}
