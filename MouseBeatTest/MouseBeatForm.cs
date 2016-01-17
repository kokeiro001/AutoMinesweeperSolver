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
    const int PerClick = 10;
    const int TimerNumbers = 10;
    const int DurationSec = 3 * 60;
    const int IntervalSec = 1;

		List<Timer> timers = new List<Timer>();
		public MouseBeatForm()
		{
			InitializeComponent();

      for (int i = 0; i < TimerNumbers; i++)
			{
        timers.Add(new Timer() 
          { 
            Interval = 1
          }
        );
			}

			StopAllTimer();
		}

		private void Form1_Load(object sender, EventArgs e)
		{
			beginTimer.Tick += delegate
			{
        Invoke((MethodInvoker)delegate
        {
          Text = "running";
        });
        

				remainingTimer.Interval = DurationSec * 1000;
				remainingTimer.Start();

				foreach (var item in timers)
				{
					item.Start();
				}
			};

			foreach (var item in timers)
			{
				item.Tick += delegate
				{
          for (int i = 0; i < PerClick; i++)
          {
            MouseEventHelper.LeftButtonClick();

          }
				};
			}

			remainingTimer.Tick += delegate
			{
				StopAllTimer();
        Invoke((MethodInvoker)delegate
        {
          Text = "stopped";
        });
      };

			StopAllTimer();
		}

		private void btnBeginBeat_Click(object sender, EventArgs e)
		{
      // 一定時間待ってから開始
      beginTimer.Interval = IntervalSec * 1000;
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
