using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Drawing.Imaging;
using OpenCvSharp;

namespace PlayMinesweeper
{
	public partial class MainForm : Form
	{
		class Data
		{
			int val;
			bool isChanged;

			public int Index { get; set; }
			public int Val 
			{
				get { return val; }
				set
				{
					if (val != value)
					{
						isChanged = true;
					}
					val = value;
				}
			}
			public bool IsChanged { get { return isChanged; } }
			public bool IsFix { get; set; }

			public void Init()
			{
				val = NotYetOpen;
				isChanged = false;
				IsFix = false;
			}

			public void Update()
			{
				isChanged = false;
			}

			public void ChangedMawari()
			{
				if (val > 0)
				{
					isChanged = true;
				}
			}
		}

		StringBuilder debugSb = new StringBuilder();

		const int OriginX = 39;
		const int OriginY = 82;
		const int BlockSize = 18;
		
		public const int GameWidth = 30 + 2;
		public const int GameHeight = 16 + 2;

		const int GameScreenWidth = BlockSize * GameWidth;
		const int GameScreenHeight = BlockSize * GameHeight;

		const int Invalid = -3;
		const int NotYetOpen = -2;
		const int BombFlag = -1;
		const int Opened = 0;
		const int VirtualOpened = 1000;

		const int VirtualSpace = 5 + 2;
		const int SleepCnt = 16;

		Random random = new Random();

		Bitmap bmp;
		Graphics graphics;
		IplImage iplCapture;
		IplImage gray;
		IplImage subColorImg;
		IplImage outputImg;
		Process mineProcess;

		bool needRefresh = false;
		bool isUpdate = false;
		bool isGameOver = false;

		bool isFind負けました = false;
		StringBuilder sb負けました = new StringBuilder();
		bool isReseted = false;
		bool isUpdatedNowFrame = false;

		Data[] data = new Data[GameHeight * GameWidth];
		Data[] dataUpdateBuf = new Data[GameHeight * GameWidth];
		int[] kinbo = new int[] { -GameWidth - 1, -GameWidth, -GameWidth + 1,
															-1, 1,
															GameWidth - 1, GameWidth, GameWidth + 1};

		int[] cntNotOpenBlockBuf = new int[8];
		int[] cntFlagBlockBuf = new int[8];

		VirtualData[] virtualBoard = new VirtualData[GameWidth * GameHeight];

		List<int> notOpenIndex = new List<int>();

		public MainForm()
		{
			InitializeComponent();

			btnRestart.Click += delegate { Restart(); };
		}

		void MainForm_Load(object sender, EventArgs e)
		{
			while (true)
			{
				List<Process> ps = Process.GetProcesses().ToList();
				string a = "MineSweeper";
				//string b = "mpc-hc64";
				mineProcess = ps.Find(p => p.ProcessName == a);
				if (mineProcess == null)
				{
					Process.Start("");
					string text = "マインスイーパを起動してからにしてね\n再試行する？";
					if (MessageBox.Show(text, "error", MessageBoxButtons.YesNo) == DialogResult.No)
					{
						Close();
						break;
						//Application.Exit();
					}
				}
				else
				{
					Shown += delegate { Loop(); };
					break;
				}
			}
			Win32APIHelper.SetForceForegroundWindow(mineProcess.MainWindowHandle);
			GlobalKeybordCapture.KeyDown += (s, eve) => { KeyboardState.Down(eve.KeyCode); };
			GlobalKeybordCapture.KeyUp += (s, eve) => { KeyboardState.Up(eve.KeyCode); };
			GlobalKeybordCapture.BeginCapture();
			this.Left = 10;


			Size windowSize = CaptureHelper.GetWindowSize(mineProcess.MainWindowHandle);
			bmp = new Bitmap(windowSize.Width, windowSize.Height);
			graphics = Graphics.FromImage(bmp);
			iplCapture = new IplImage(new CvSize(bmp.Width, bmp.Height), BitDepth.U8, 4);
			gray = new IplImage(new CvSize(GameScreenWidth, GameScreenHeight), BitDepth.U8, 1);
			subColorImg = new IplImage(new CvSize(GameScreenWidth, GameScreenHeight), BitDepth.U8, 4);
			outputImg = new IplImage(new CvSize(GameScreenWidth, GameScreenHeight), BitDepth.U8, 4);
			Reset();
		}

		private void Reset()
		{
			for (int i = 0; i < data.Length; i++)
			{
				data[i] = new Data();
				data[i].Init();
				data[i].Index = i;
				int y = i / GameWidth;
				int x = i % GameWidth;
				if (y == 0 || y == GameHeight - 1 || x == 0 || x == GameWidth - 1)
				{
					data[i].Val = Invalid;
				}
				data[i].Update();
			}
			outputImg.Rectangle(new CvRect(0, 0, outputImg.Width, outputImg.Height), new CvScalar(127, 127, 127, 255), -1);
		}

		void Restart()
		{
			Win32APIHelper.SetForceForegroundWindow(mineProcess.MainWindowHandle);
			Reset();
			isUpdate = true;
			isGameOver = false;
			isFind負けました = false;
		}

		private void btnReset_Click(object sender, EventArgs e)
		{
			Reset();
			UpdateData();
			needRefresh = true;
			isUpdate = false;
		}

		private void btnRestart_Click(object sender, EventArgs e)
		{
			Restart();
		}

		void CheckGameOver()
		{
			isFind負けました = false;
			Win32APIHelper.EnumWindows(EnumWindowsProc, 0);

			if (!isGameOver && isFind負けました)
			{
				// ゲームオーバーじゃない状態で例のウィンドウが見つかったらゲームオーバー状態に
				isGameOver = true;
				isUpdate = false;
			}
			else if (isGameOver && !isFind負けました)
			{
				// ゲームオーバー状態で例のウィンドウが見つからなかったらリスタート
				isGameOver = false;
				//Reset();
				Thread.Sleep(1000);
				Restart();
			}
		}

		int EnumWindowsProc(IntPtr hwnd, int lp)
		{
			// 親がメインスイーパーから
			IntPtr parent = Win32APIHelper.GetParent(hwnd);

			if (parent == mineProcess.MainWindowHandle)
			{
				sb負けました.Capacity = 256;
				Win32APIHelper.GetWindowText(hwnd, sb負けました, sb負けました.Capacity);
				string hoge = sb負けました.ToString();
				if (hoge == "負けました" || hoge == "勝ちました")
				{
					isFind負けました = true;
				}
			}

			return 1;
		}

		void Loop()
		{
			outputImg.Rectangle(new CvRect(0, 0, outputImg.Width, outputImg.Height), new CvScalar(127, 127, 127, 255), -1);

			UpdateData();

			while (!IsDisposed && !mineProcess.HasExited)
			{
				
				CheckGameOver();

				if (isUpdate)
				{
					// コピー
					int updatedItemCnt = 0;
					for (int i = 0; i < data.Length; i++)
					{
						if (data[i].IsChanged) dataUpdateBuf[updatedItemCnt++] = data[i];
					}
					if (updatedItemCnt == 0)
					{
						if (!isReseted)
						{
							// もし更新対象が存在しないなら、一度リセットをする
							isReseted = true;
							Restart();
						}
						else if (isReseted && !isUpdatedNowFrame)
						{
							// 一度リセットしたにもかかわらず、状況が変わらなかった場合、少し深く考える
							if (すごい考える() || すごい考える２())
							{
								// 成功。どっか開いたYO
								isUpdatedNowFrame = true;
								isReseted = false;
							}
							else
							{
								//失敗 それでも無理だったらランダムに開く
								MessageBox.Show("わかんねーからランダムに開くよ");
								Thread.Sleep(300);
								int i = 0;
								do
								{
									i = random.Next(data.Length);
								} while (data[i].Val != NotYetOpen);
								Move_Click(i % GameWidth - 1, i / GameWidth - 1, MouseButtons.Left);
								Thread.Sleep(500);
							}
						}
					}
					else
					{
						// 変更された箇所が存在するなら
						isUpdatedNowFrame = false;
						for (int i = 0; i < updatedItemCnt; i++)
						{
							Data tmp = dataUpdateBuf[i];
							if (tmp.IsChanged)
							{
								考える(tmp);
								if (KeyboardState.IsDown(Keys.Escape))
								{
									isUpdate = false;
									break;
								}
							}
						}
					}
					if (KeyboardState.IsDown(Keys.Escape))
					{
						isUpdate = false;
					}
					UpdateData();
				}

				Draw();
				Application.DoEvents();
				Thread.Sleep(16);
			}
			Application.Exit();
		}


		bool EnableIndex(int idx)
		{
			return 0 <= idx && idx < data.Length;
		}


		bool すごい考える()
		{
			RegistNotYetOpenForVirtual();
			foreach (var index in notOpenIndex)
			{
				// 仮想盤面にコピー
				const int haba = 5;
				int y1 = (index / GameWidth) - haba;
				int x1 = (index % GameWidth) - haba;
				int y2 = (index / GameWidth) + haba;
				int x2 = (index % GameWidth) + haba;

				if (x1 < 0) x1 = 0;
				if (x2 >= GameWidth) x2 = GameWidth;
				if (y1 < 0) y1 = 0;
				if (y2 >= GameHeight) y2 = GameHeight;

				CopyToVirtualBoard(y1, x1, y2, x2);

				// 自分に旗を立てる
				virtualBoard[index].Value = BombFlag;
				if (CalcVirtualBoard(index, y1, x1, y2, x2, false))
				{
					return true;
				}
			}
			return false;
		}

		bool すごい考える２()
		{
			RegistNotYetOpenForVirtual();
			foreach (var index in notOpenIndex)
			{
				// 仮想盤面にコピー
				const int haba = 5;
				int y1 = (index / GameWidth) - haba;
				int x1 = (index % GameWidth) - haba;
				int y2 = (index / GameWidth) + haba;
				int x2 = (index % GameWidth) + haba;

				if (x1 < 0) x1 = 0;
				if (x2 >= GameWidth) x2 = GameWidth;
				if (y1 < 0) y1 = 0;
				if (y2 >= GameHeight) y2 = GameHeight;

				CopyToVirtualBoard(y1, x1, y2, x2);

				// 自分に旗を立てる
				virtualBoard[index].Value = Opened;
				if (CalcVirtualBoard(index, y1, x1, y2, x2, true))
				{
					return true;
				}
			}
			return false;
		}

		private void RegistNotYetOpenForVirtual()
		{
			notOpenIndex.Clear();
			// 周りに数字がある未開のマスを取得する
			for (int i = 0; i < data.Length; i++)
			{
				if (data[i].Val == NotYetOpen)	// まだ開いてないマス
				{
					for (int j = 0; j < 8; j++)		// その周囲に
					{
						int idx = i + kinbo[j];
						if (data[idx].Val > 0)			// １～９の数字があれば
						{
							notOpenIndex.Add(i);			// 処理対象に追加
							break;
						}
					}
				}
			}
		}

		private bool CalcVirtualBoard(int index, int y1, int x1, int y2, int x2, bool originOpened)
		{
			bool changed = true;
			while (changed)
			{
				changed = false;
				// 左上から考えまーす
				for (int y = y1; y < y2; y++)
				{
					for (int x = x1; x < x2; x++)
					{
						int targetIdx = y * GameWidth + x;
						if (!EnableIndex(targetIdx)) continue;
						if (virtualBoard[targetIdx].Value <= 0) continue;

						int notOpenCnt = 0;
						int flagCnt = 0;
						for (int i = 0; i < 8; i++)
						{
							int buf = targetIdx + kinbo[i];
							if (!EnableIndex(buf)) continue;
							if (virtualBoard[buf].Value == NotYetOpen) notOpenCnt++;
							if (virtualBoard[buf].Value == BombFlag) flagCnt++;
						}

						// 自分の数とフラグの数が等しいなら、未開のマスを全部開ける
						if (virtualBoard[targetIdx].Value == flagCnt)
						{
							for (int i = 0; i < 8; i++)
							{
								int buf = targetIdx + kinbo[i];
								if (!EnableIndex(buf)) continue;
								if (virtualBoard[buf].Value == NotYetOpen)
								{
									virtualBoard[buf].Value = VirtualOpened;
									changed = true;
								}
							}
						}

						// 自分の数と「フラグの数＋未開マス」なら全部にフラグを立てる
						else if (virtualBoard[targetIdx].Value == notOpenCnt + flagCnt)
						{
							for (int j = 0; j < 8; j++)
							{
								int buf = targetIdx + kinbo[j];
								if (!EnableIndex(buf)) continue;
								if (virtualBoard[buf].Value == NotYetOpen)
								{
									virtualBoard[buf].Value = BombFlag;
									changed = true;
								}
							}
						}
						//else if (flagCnt > virtualBoard[targetIdx].Value ||
						//         flagCnt + notOpenCnt < virtualBoard[targetIdx].Value)
						// 旗の数が自分より多い場合、不正な状況である
						else if (flagCnt >= virtualBoard[targetIdx].Value ||
										(virtualBoard[targetIdx].Value != VirtualOpened &&  flagCnt + notOpenCnt < virtualBoard[targetIdx].Value))
						{
							// 始点を開けてた場合はフラグを立てる必要がある
							if (originOpened)
							{
								Move_Click(index % GameWidth - 1, index / GameWidth - 1, MouseButtons.Right);
								//Thread.Sleep(SleepCnt);

								//Thread.Sleep(100);
								Win32APIHelper.EnumWindows(EnumWindowsProc, 0);
								if (isFind負けました)
								{
								}
								
								data[index].Val = BombFlag;
							}
							// 始点に旗を立てていた場合は開ける必要がある
							else
							{
								Move_Click(index % GameWidth - 1, index / GameWidth - 1, MouseButtons.Left);
								//Thread.Sleep(SleepCnt);

								//Thread.Sleep(100);
								Win32APIHelper.EnumWindows(EnumWindowsProc, 0);
								if (isFind負けました)
								{
								 }
				
								data[index].Val = Opened;
							}
							//Thread.Sleep(SleepCnt);
							return true;
						}
					}
				}
			}
			return false;
		}

		private void CopyToVirtualBoard(int y1, int x1, int y2, int x2)
		{
			for (int y = y1 - 1; y <= y2; y++)
			{
				for (int x = x1 - 1; x <= x2; x++)
				{
					int buf = y * GameWidth + x;
					if (EnableIndex(buf))
					{
						virtualBoard[buf].Index = buf;
						virtualBoard[buf].IsChanged = true;

						virtualBoard[buf].Value = data[buf].Val;
					}
				}
			}
		}

		void 考える(Data d)
		{
			d.Update();
			if (d.Val == 0 || d.Val == Opened) return;

			// 周囲の開いていないマスを数える
			int cntNotYetOpen = 0;
			int cntFlag = 0;

			for (int i = 0; i < 8; i++)
			{
				int idx = d.Index + kinbo[i];
				int val = data[idx].Val;
				if (val == NotYetOpen) cntNotOpenBlockBuf[cntNotYetOpen++] = idx;
				else if (val == BombFlag) cntFlagBlockBuf[cntFlag++] = idx;
			}

			if(d.Val == cntFlag)
			{
				 //周囲に十分な旗が立っているなら
				for (int i = 0; i < 8; i++)
				{
				  int idx = d.Index + kinbo[i];
				  if (data[idx].Val == NotYetOpen)
				  {
				    data[idx].Val = Opened;
				    Move_Click(idx % GameWidth - 1, idx / GameWidth - 1, MouseButtons.Left);
						Thread.Sleep(SleepCnt);
						isUpdatedNowFrame = true;
				  }
				}
			}
			else if (d.Val == cntNotYetOpen + cntFlag)
			{
				// 旗が確定するなら
				for (int i = 0; i < cntNotYetOpen; i++)
				{
					int idx = cntNotOpenBlockBuf[i];
					data[idx].Val = BombFlag;
					data[idx].IsFix = true;
					for (int j = 0; j < 8; j++)
					{
						int hoge = idx + kinbo[j];
						if (data[hoge].Val > 0)
						{
							data[hoge].ChangedMawari();
						}
					}
					Move_Click(idx % GameWidth - 1, idx / GameWidth - 1, MouseButtons.Right);
					isUpdatedNowFrame = true;
					Thread.Sleep(SleepCnt);
				}
			}
		}

		void MoveMouse(int x, int y)
		{
			Rectangle rect = CaptureHelper.GetWindowRect(mineProcess.MainWindowHandle);
			int bufX = rect.Left + OriginX + x * BlockSize + BlockSize / 2;
			int bufY = rect.Top + OriginY + y * BlockSize + BlockSize / 2;
			MouseEventHelper.Move(bufX, bufY);
		}
		void Move_Click(int x, int y, MouseButtons button)
		{
			Rectangle rect = CaptureHelper.GetWindowRect(mineProcess.MainWindowHandle);
			int bufX = rect.Left + OriginX + x * BlockSize + BlockSize / 2;
			int bufY = rect.Top + OriginY + y * BlockSize + BlockSize / 2;
			MouseEventHelper.Move_Click(bufX, bufY, button);
		}

		void UpdateData()
		{
			CaptureHelper.CaptureWindow(mineProcess.MainWindowHandle, bmp, graphics);
			BitmapConverter.ToIplImage(bmp, iplCapture);

			Cv.SetImageROI(iplCapture, new CvRect(OriginX, OriginY, GameScreenWidth, GameScreenHeight));
			Cv.SetImageCOI(iplCapture, 3);
			Cv.ResetImageROI(gray);
			Cv.Copy(iplCapture, gray);

			Cv.SetImageROI(iplCapture, new CvRect(OriginX, OriginY, GameScreenWidth, GameScreenHeight));
			Cv.SetImageCOI(iplCapture, 0);
			Cv.ResetImageROI(subColorImg);
			Cv.Copy(iplCapture, subColorImg);
			Cv.ResetImageROI(iplCapture);

			// 各ブロックの取得
			for (int i = 0; i < data.Length; i++)
			{
				Data tmp = data[i];
				if ((tmp.Val == NotYetOpen || tmp.Val == Opened || tmp.Val == BombFlag || tmp.Val == Opened) && !tmp.IsFix)
				{
					int x = i % GameWidth - 1;
					int y = i / GameWidth - 1;
					Cv.SetImageROI(gray, new CvRect(x * BlockSize, y * BlockSize, BlockSize, BlockSize));
					Cv.SetImageROI(subColorImg, new CvRect(x * BlockSize, y * BlockSize, BlockSize, BlockSize));
					Cv.EqualizeHist(gray, gray);

					int tmpVal = tmp.Val;
					if (CheckFlag(subColorImg)) tmp.Val = BombFlag;
					else if (gray.Get2D(9, 8).Val0 >= 200) tmp.Val = NotYetOpen;
					else if (Check1(subColorImg)) tmp.Val = 1;
					else if (Check2(subColorImg)) tmp.Val = 2;
					else if (Check5(subColorImg)) tmp.Val = 5;
					else if (Check7(subColorImg)) tmp.Val = 7;
					else if (Check3(subColorImg)) tmp.Val = 3;
					else if (Check4(subColorImg)) tmp.Val = 4;
					else if (Check6(subColorImg)) tmp.Val = 6;
					else tmp.Val = Opened;

					if (tmp.Val != tmpVal)
					{
						if (tmp.Val > 0 || tmp.Val == BombFlag)
						{
							for (int j = 0; j < 8; j++)
							{
								int idx = i + kinbo[j];
								data[idx].ChangedMawari();
							}
						}
						if (tmp.Val > 0 && tmp.Val <= 9)
						{
							tmp.IsFix = true;
						}
					}
				}
			}
		}

		void Draw()
		{
			CvRect rect = new CvRect(0, 0, BlockSize - 1, BlockSize - 1);
			for (int i = 0; i < data.Length; i++)
			{
				//if (data[i].IsChanged)
				if (data[i].Val != Invalid)
				{
					int x = i % GameWidth - 1;
					int y = i / GameWidth - 1;
					Cv.SetImageROI(outputImg, new CvRect(x * BlockSize, y * BlockSize, BlockSize, BlockSize));
					int tmp = data[i].Val;
					switch (tmp)
					{
						case BombFlag: outputImg.Rectangle(rect, new CvScalar(0, 0, 0, 255), -1); break;
						case NotYetOpen: outputImg.Rectangle(rect, new CvScalar(127, 127, 127, 255), -1); break;
						case Opened: outputImg.Rectangle(rect, new CvScalar(200, 200, 200, 255), -1); break;
						case 1: outputImg.Rectangle(rect, new CvScalar(190, 80, 64, 255), -1); break;
						case 2: outputImg.Rectangle(rect, new CvScalar(0, 255, 0, 255), -1); break;
						case 3: outputImg.Rectangle(rect, new CvScalar(0, 0, 255, 255), -1); break;
						case 4: outputImg.Rectangle(rect, new CvScalar(127, 0, 0, 255), -1); break;
						case 5: outputImg.Rectangle(rect, new CvScalar(0, 0, 127, 255), -1); break;
						case 6: outputImg.Rectangle(rect, new CvScalar(127, 127, 0, 255), -1); break;
						case 7: outputImg.Rectangle(rect, new CvScalar(0, 255, 255, 255), -1); break;
					}
					needRefresh = true;
				}
			}
			Cv.ResetImageROI(outputImg);
			if (needRefresh || true)
			{
				pbxMain.RefreshIplImage(outputImg);
				needRefresh = false;
			}
		}

		#region check helpers

		bool Check1(IplImage img)
		{ 
			return img.Get2D(8, 9) == new CvScalar(190, 80, 64, 255);
		}
		bool Check2(IplImage img)
		{
			CvScalar tmp = img.Get2D(7, 12);
			return (tmp.Val0 < 50 && tmp.Val1 > 50 && tmp.Val2 < 60);
		}
		bool Check3(IplImage img)
		{
			CvScalar tmp = img.Get2D(8, 8);
			CvScalar tmp2 = img.Get2D(9, 9);
			return (tmp.Val0 < 20 && 
							tmp.Val1 < 20 && 
							tmp.Val2 > 150) || 
						 (tmp2.Val0 < 20 && 
							tmp2.Val1 < 20 && 
							tmp2.Val2 > 150);
		}
		bool Check4(IplImage img)
		{
			CvScalar tmp0 = img.Get2D(10, 10);
			CvScalar tmp1 = img.Get2D(11, 11);
			return (tmp0.Val0 > 100 && tmp0.Val1 < 10 && tmp0.Val2 < 10)
					 || (tmp1.Val0 > 100 && tmp1.Val1 < 10 && tmp1.Val2 < 10);
		}
		bool Check5(IplImage img)
		{
			for (int y = 4; y < 8; y++)
			{
				for (int x = 3; x < 9; x++)
				{
					CvScalar tmp0 = img.Get2D(x, y);
					if (tmp0.Val0 < 10 && 
						  tmp0.Val1 < 10 && 
							120 < tmp0.Val2 && tmp0.Val2 < 130)
					{
						return true;
					}
				}
			}
			return false;
		}
		bool Check6(IplImage img)
		{
			CvScalar tmp0 = img.Get2D(11, 6);
			CvScalar tmp1 = img.Get2D(11, 7);
			return (90 < tmp0.Val0 && tmp0.Val0 < 144 &&
							90 < tmp0.Val1 && tmp0.Val1 < 144 &&
							tmp0.Val2 < 10) ||
						 (90 < tmp1.Val0 && tmp1.Val0 < 144 &&
							90 < tmp1.Val1 && tmp1.Val1 < 144 &&
							tmp1.Val2 < 10);
			//|| (tmp1.Val0 < 10 && tmp1.Val1 < 10 && 110 < tmp1.Val2 && tmp1.Val2 < 140);
		}
		bool Check7(IplImage img)
		{
			int x = 9;
			for (int y = 1; y < 10; y++)
			{
				if (Check7Helper(img.Get2D(x, y)))
				{
					int len = 0;
					int xbuf = x;
					// xの左端を探す
					try
					{
						while (!Check7Helper(img.Get2D(--x, y))) { len++; }
						len++;
						x = xbuf;
						while (!Check7Helper(img.Get2D(++x, y))) { len++; }
						len++;

						return len == 7;
					}
					catch (Exception e)
					{
						return false;
					}
				}
			}
			return false;
			//|| (tmp1.Val0 < 10 && tmp1.Val1 < 10 && 110 < tmp1.Val2 && tmp1.Val2 < 140);
		}
		bool Check7Helper(CvScalar tmp)
		{
			return (tmp.Val0 < 20 &&
							tmp.Val1 < 20 &&
							150 < tmp.Val2 && tmp.Val2 < 190);
		}
		bool CheckFlag(IplImage img)
		{
			// 旗の赤い部分を探す
			bool hit = false;
			for (int y = 3; y < 9; y++)
			{
				for (int x = 5; x < 11; x++)
				{
					if (CheckFlagHelper(img, x, y))
					{
						hit = true;
						goto FlagHit;
					}
				}
			}
			FlagHit:
			if (!hit) return false;

			// ポールの部分を探す
			for (int y = 8; y < 12; y++)
			{
				for (int x = 10; x < 14; x++)
				{
					if (CheckFlagHelper2(img, x, y))
					{
						return true;
					}
				}
			}

			return false;
		}
		bool CheckFlagHelper(IplImage img, int x, int y)
		{ 
			CvScalar tmp = img.Get2D(x, y);
			return tmp.Val0 < 90 &&
						 tmp.Val1 < 90 &&
						 240 < tmp.Val2;
		}
		bool CheckFlagHelper2(IplImage img, int x, int y)
		{
			CvScalar tmp = img.Get2D(x, y);
			return 240 < tmp.Val0 &&
						 240 < tmp.Val1 &&
						 240 < tmp.Val2;
		}

		#endregion

	}


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
		private static extern int GetWindowRect(IntPtr hwnd, ref  RECT lpRect);

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
			GetWindowRect(hWnd,ref rect);
			return new Rectangle(rect.left, rect.top, rect.right - rect.left, rect.bottom - rect.top);
		}
	}


	class VirtualDataViewer
	{
		public VirtualData[] Data;

		public int Index;
		public int Top;
		public int Left;
		public int Width;
		public int Height;

		public override string ToString()
		{
			StringBuilder sb = new StringBuilder();
			for (int x = 0; x < Width; x++)
			{
				for (int y = 0; y < Height; y++)
				{

				}
			}
			return "";
		}
	}

	struct VirtualData
	{
		public int Index;
		public int Value;
		public bool IsChanged;
		public int X { get { return MainForm.GameWidth % Index - 1; } }
		public int Y { get { return MainForm.GameWidth / Index - 1; ; } }

		public override string ToString()
		{
			return string.Format("{0}, {1} val={2}", X, Y, Value);
		}
	}

}
