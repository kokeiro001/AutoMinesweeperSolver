using System.Runtime.InteropServices;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using TestClassLibrary;
namespace MouseBeatTest
{
	public partial class MouseBeatForm : Form
	{
		const int Sec = 5;

		List<Timer> timers = new List<Timer>();
		public MouseBeatForm()
		{
			InitializeComponent();

			for (int i = 0; i < 10; i++)
			{
				timers.Add(new Timer());
			}

			StopAllTimer();
		}

		private void Form1_Load(object sender, EventArgs e)
		{
			beginTimer.Tick += delegate
			{
				remainingTimer.Interval = Sec * 1000;
				remainingTimer.Start();

				foreach (var item in timers)
				{
					item.Interval = 1;
					item.Start();
				}
			};

			foreach (var item in timers)
			{
				item.Tick += delegate
				{
					MouseEventHelper.LeftButtonClick();
				};
			}

			remainingTimer.Tick += delegate
			{
				StopAllTimer();
			};

			StopAllTimer();
		}

		private void btnBeginBeat_Click(object sender, EventArgs e)
		{
			beginTimer.Interval = 1000;
			beginTimer.Enabled = true;
			beginTimer.Start();
		}
		
		void StopAllTimer()
		{
			beginTimer.Stop();

			foreach (var item in timers)
			{
				item.Stop();
			}
			remainingTimer.Stop();
		}

	}
}
