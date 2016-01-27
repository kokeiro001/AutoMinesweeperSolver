using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace PlayMinesweeper
{
  /// <summary>
  /// マインスイーパーを自動で解く機能を提供します
  /// </summary>
  public partial class MainForm : Form
  {
    private class CellData
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

    private StringBuilder debugSb = new StringBuilder();

    #region Board Size Properties

    /// <summary>ゲームウィンドウの左端から、最初のセルの左端までのX距離(pix)</summary>
    private static readonly int OriginX = 39;

    /// <summary>ゲームウィンドウの上端から、最初のセルの上端までのY距離(pix)</summary>
    private static readonly int OriginY = 82;

    /// <summary>ゲームウィンドウの上端から、最初のセルの上端までのY距離(pix)</summary>
    private static readonly int CellSize = 18;

    /// <summary>盤面の横のセル数</summary>
    public static readonly int BoardWidth = 30 + 2;
    /// <summary>盤面の縦のセル数</summary>
		public static readonly int BoardHeight = 16 + 2;

    /// <summary>盤面の横幅(ピクセル)</summary>
    private static readonly int BoardWidthPix = CellSize * BoardWidth;
    /// <summary>盤面の高さ(ピクセル)</summary>
    private static readonly int BoardHeightPix = CellSize * BoardHeight;

    #endregion

    #region セルの状態

    private const int Invalid = -3;
    private const int NotYetOpen = -2;
    private const int BombFlag = -1;
    private const int Opened = 0;
    private const int VirtualOpened = 1000;

    #endregion

    private static readonly int SleepCnt = 16;

    private Random random = new Random();

    private bool CheckRandomOpenCell = false;

    #region 描画用フィールド

    private Bitmap bmp;
    private Graphics graphics;
    private IplImage iplRawCapturedImg;
    private IplImage iplGrayImg;
    private IplImage subColorImg;
    private IplImage outputImg;

    #endregion

    private Process mineProcess;

    private bool needRefresh = false;
    private bool isUpdate = false;
    private bool isGameOver = false;

    private bool isFind負けました = false;
    private StringBuilder sb負けました = new StringBuilder();
    private bool isReseted = false;
    private bool isUpdatedNowFrame = false;

    #region 盤面に関するフィールド

    private CellData[] data = new CellData[BoardHeight * BoardWidth];
    private CellData[] dataUpdateBuf = new CellData[BoardHeight * BoardWidth];
    private int[] kinbo = new int[] { -BoardWidth - 1, -BoardWidth, -BoardWidth + 1,
                                      -1, 1,
                                       BoardWidth - 1, BoardWidth, BoardWidth + 1};

    private int[] cntNotOpenBlockBuf = new int[8];
    private int[] cntFlagBlockBuf = new int[8];

    private static readonly int VirtualSpace = 5 + 2;
    private VirtualData[] virtualBoard = new VirtualData[BoardWidth * BoardHeight];

    private List<int> notOpenIndex = new List<int>();

    #endregion

    public MainForm()
    {
      InitializeComponent();

      btnRestart.Click += delegate { Restart(); };
    }

    private void MainForm_Load(object sender, EventArgs e)
    {
      while (true)
      {
        List<Process> ps = Process.GetProcesses().ToList();
        string processName = "MineSweeper";
        mineProcess = ps.Find(p => p.ProcessName == processName);
        if (mineProcess == null)
        {
          string text = "マインスイーパを起動してからにしてね\n再試行する？";
          if (MessageBox.Show(text, "error", MessageBoxButtons.YesNo) == DialogResult.No)
          {
            Close();
            return;
          }
        }
        else
        {
          Shown += delegate { MainLoop(); };
          break;
        }
      }

      // マインスイーパーのウィンドウを一番手前に表示する
      Win32APIHelper.SetForceForegroundWindow(mineProcess.MainWindowHandle);

      // キーボードの入力をキャプチャするようにする
      GlobalKeybordCapture.KeyDown += (s, eve) => { KeyboardState.Down(eve.KeyCode); };
      GlobalKeybordCapture.KeyUp += (s, eve) => { KeyboardState.Up(eve.KeyCode); };
      GlobalKeybordCapture.BeginCapture();

      // じゃまにならないように自身のウィンドウを移動する
      this.Left = 10;

      // 描画用の情報を初期化する
      Size windowSize = CaptureHelper.GetWindowSize(mineProcess.MainWindowHandle);
      bmp = new Bitmap(windowSize.Width, windowSize.Height);
      graphics = Graphics.FromImage(bmp);
      iplRawCapturedImg = new IplImage(new CvSize(bmp.Width, bmp.Height), BitDepth.U8, 4);
      iplGrayImg = new IplImage(new CvSize(BoardWidthPix, BoardHeightPix), BitDepth.U8, 1);
      subColorImg = new IplImage(new CvSize(BoardWidthPix, BoardHeightPix), BitDepth.U8, 4);
      outputImg = new IplImage(new CvSize(BoardWidthPix, BoardHeightPix), BitDepth.U8, 4);
      Reset();
    }

    /// <summary>盤面、描画に関する情報を初期化します</summary>
		private void Reset()
    {
      for (int i = 0; i < data.Length; i++)
      {
        data[i] = new CellData();
        data[i].Init();
        data[i].Index = i;
        int y = i / BoardWidth;
        int x = i % BoardWidth;
        if (y == 0 || y == BoardHeight - 1 || x == 0 || x == BoardWidth - 1)
        {
          data[i].Val = Invalid;
        }
        data[i].Update();
      }
      outputImg.Rectangle(new CvRect(0, 0, outputImg.Width, outputImg.Height), new CvScalar(127, 127, 127, 255), -1);
    }

    /// <summary>自動モードを再開します</summary>
		private void Restart()
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
      UpdateBoardDataFromWindowView();
      needRefresh = true;
      isUpdate = false;
    }

    private void btnRestart_Click(object sender, EventArgs e)
    {
      Restart();
    }

    /// <summary>負けたかどうか確認します</summary>
		private void CheckGameOver()
    {
      isFind負けました = false;

      // ウィンドウを列挙します。負けていた場合、ウィンドウが表示されるためです
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
        Thread.Sleep(1000);
        Restart();
      }
    }

    private int EnumWindowsProc(IntPtr hwnd, int lp)
    {
      // 親がマインスイーパーなら、そのウィンドウに対して探索を行う
      IntPtr parent = Win32APIHelper.GetParent(hwnd);

      if (parent == mineProcess.MainWindowHandle)
      {
        // ウィンドウのタイトルが特定の文字列なら、フラグを変更する
        sb負けました.Capacity = 256;
        Win32APIHelper.GetWindowText(hwnd, sb負けました, sb負けました.Capacity);
        string windowText = sb負けました.ToString();
        if (windowText == "負けました" || windowText == "勝ちました")
        {
          isFind負けました = true;
        }
      }

      return 1;
    }

    /// <summary>メインループ</summary>
    private void MainLoop()
    {
      // 描画用の画像をクリアする
      outputImg.Rectangle(new CvRect(0, 0, outputImg.Width, outputImg.Height), new CvScalar(127, 127, 127, 255), -1);

      // ボードの情報を読み込む
      UpdateBoardDataFromWindowView();

      // マインスイーパーのボードが生きている間、処理を行う
      while (!IsDisposed && !mineProcess.HasExited)
      {
        // ゲームオーバーかどうか確認する
        CheckGameOver();

        // 更新するモードなら、更新する。
        if (isUpdate)
        {
          UpdateFrame();
        }

        // 描画、メッセージ処理、待機を行う
        DrawDebugInfo();
        Application.DoEvents();

        // todo: 処理にかかった時間を考えみて、スリープ時間を考慮すると高速化に繋がる
        Thread.Sleep(16);
      }
      Application.Exit();
    }

    /// <summary>毎フレーム呼び出される更新用メソッド</summary>
    private void UpdateFrame()
    {
      // 盤面の情報をコピー
      int updatedItemCnt = 0;
      for (int i = 0; i < data.Length; i++)
      {
        if (data[i].IsChanged) dataUpdateBuf[updatedItemCnt++] = data[i];
      }

      // 直前のフレームで更新されたセル数が０の場合、思考ルーチンを切り替える
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
            if (CheckRandomOpenCell)
            {
              MessageBox.Show("わかんねーからランダムに開くよ");
              Thread.Sleep(300);
            }
            int i = 0;
            do
            {
              i = random.Next(data.Length);
            } while (data[i].Val != NotYetOpen);
            Move_Click(i % BoardWidth - 1, i / BoardWidth - 1, MouseButtons.Left);

            Thread.Sleep(CheckRandomOpenCell ? 500 : 32);
          }
        }
      }
      else
      {
        // 直前のフレームで変更された箇所が存在するなら、引き続きシンプルな思考ルーチンで処理を行う
        isUpdatedNowFrame = false;

        // 直前のフレームで更新されたセル群を中心に、思考を開始する
        for (int i = 0; i < updatedItemCnt; i++)
        {
          CellData tmp = dataUpdateBuf[i];
          if (tmp.IsChanged)
          {
            シンプルに考える(tmp);
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
      UpdateBoardDataFromWindowView();
    }

    private bool EnableIndex(int idx)
    {
      return 0 <= idx && idx < data.Length;
    }

    /// <summary>
    /// シンプルに考えるメソッドで開けるセルが見つからなかった場合に呼び出される。
    /// 背理法を用いた思考ルーチンを行う。
    /// このメソッドでは、特定のセルに爆弾が「ある」と仮定して処理を行い、
    /// 矛盾が見つかった場合に、このセルには爆弾がない！と判断する
    /// </summary>
		private bool すごい考える()
    {
      RegistNotYetOpenForVirtual();
      foreach (var index in notOpenIndex)
      {
        // 仮想盤面にコピー
        const int haba = 5;
        int y1 = (index / BoardWidth) - haba;
        int x1 = (index % BoardWidth) - haba;
        int y2 = (index / BoardWidth) + haba;
        int x2 = (index % BoardWidth) + haba;

        if (x1 < 0) x1 = 0;
        if (x2 >= BoardWidth) x2 = BoardWidth;
        if (y1 < 0) y1 = 0;
        if (y2 >= BoardHeight) y2 = BoardHeight;

        CopyToVirtualBoard(y1, x1, y2, x2);

        // 自分が爆弾であると仮定する
        virtualBoard[index].Value = BombFlag;
        if (CalcVirtualBoard(index, y1, x1, y2, x2, false))
        {
          return true;
        }
      }
      return false;
    }

    /// <summary>
    /// シンプルに考えるメソッドで開けるセルが見つからなかった場合に呼び出される。
    /// 背理法を用いた思考ルーチンを行う。
    /// このメソッドでは、特定のセルに爆弾が「ない」と仮定して処理を行い、
    /// 矛盾が見つかった場合に、このセルには爆弾がない！と判断する
    /// </summary>
		private bool すごい考える２()
    {
      RegistNotYetOpenForVirtual();
      foreach (var index in notOpenIndex)
      {
        // 仮想盤面にコピー
        const int haba = 5;
        int y1 = (index / BoardWidth) - haba;
        int x1 = (index % BoardWidth) - haba;
        int y2 = (index / BoardWidth) + haba;
        int x2 = (index % BoardWidth) + haba;

        if (x1 < 0) x1 = 0;
        if (x2 >= BoardWidth) x2 = BoardWidth;
        if (y1 < 0) y1 = 0;
        if (y2 >= BoardHeight) y2 = BoardHeight;

        CopyToVirtualBoard(y1, x1, y2, x2);

        // 自分が旗であると仮定する
        virtualBoard[index].Value = Opened;
        if (CalcVirtualBoard(index, y1, x1, y2, x2, true))
        {
          return true;
        }
      }
      return false;
    }

    /// <summary>
    /// まだ開いていないセルが周囲に含まれるnotOpenIndexリストに登録します。
    /// 取得したデータは仮想盤面で使用します。
    /// </summary>
		private void RegistNotYetOpenForVirtual()
    {
      notOpenIndex.Clear();
      // 周りに数字がある未開のマスを取得する
      for (int i = 0; i < data.Length; i++)
      {
        if (data[i].Val == NotYetOpen)  // まだ開いてないマス
        {
          for (int j = 0; j < 8; j++)   // その周囲に
          {
            int idx = i + kinbo[j];
            if (data[idx].Val > 0)      // １～９の数字があれば
            {
              notOpenIndex.Add(i);      // 処理対象に追加
              break;
            }
          }
        }
      }
    }

    /// <summary>
    /// 仮想盤面でシミュレーションを行います
    /// </summary>
    /// <param name="index">シミュレーションを開始するセル</param>
    /// <param name="y1">シミュレーションを行う範囲におけるY座標(min)</param>
    /// <param name="x1">シミュレーションを行う範囲におけるX座標(min)</param>
    /// <param name="y2">シミュレーションを行う範囲におけるY座標(max)</param>
    /// <param name="x2">シミュレーションを行う範囲におけるX座標(max)</param>
    /// <param name="originOpened">trueなら、シミュレーションを開始するセルが開いたものと仮定してシミュレーションを行う。falseなら、開いていないものと仮定して。</param>
    /// <returns>不正な状態/矛盾が見つかったらfalse。それ以外ならtrue</returns>
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
            // 有効なセルか判断する
            int targetIdx = y * BoardWidth + x;
            if (!EnableIndex(targetIdx)) continue;
            if (virtualBoard[targetIdx].Value <= 0) continue;

            // 周辺における、開いていないセルと、旗が立っているセルを数える
            int notOpenCnt = 0;
            int flagCnt = 0;
            for (int i = 0; i < 8; i++)
            {
              int buf = targetIdx + kinbo[i];
              if (!EnableIndex(buf)) continue;
              if (virtualBoard[buf].Value == NotYetOpen) notOpenCnt++;
              if (virtualBoard[buf].Value == BombFlag) flagCnt++;
            }

            // 自分の数とフラグの数が等しいなら、未開のセルを全部開ける
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

            // 旗の数が自分より多い場合、不正な状況である
            else if (flagCnt >= virtualBoard[targetIdx].Value ||
                     (virtualBoard[targetIdx].Value != VirtualOpened && 
                      flagCnt + notOpenCnt < virtualBoard[targetIdx].Value))
            {
              // 始点を開けてた場合はフラグを立てる必要がある
              if (originOpened)
              {
                Move_Click(index % BoardWidth - 1, index / BoardWidth - 1, MouseButtons.Right);
                data[index].Val = BombFlag;
              }

              // 始点に旗を立てていた場合は開ける必要がある
              else
              {
                Move_Click(index % BoardWidth - 1, index / BoardWidth - 1, MouseButtons.Left);

                Win32APIHelper.EnumWindows(EnumWindowsProc, 0);

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

    /// <summary>
    /// 現在のボードの状況を仮想盤面にコピーする
    /// </summary>
    /// <param name="y1">コピーする範囲のY座標(min)</param>
    /// <param name="x1">コピーする範囲のX座標(min)</param>
    /// <param name="y2">コピーする範囲のY座標(max)</param>
    /// <param name="x2">コピーする範囲のX座標(max)</param>
    private void CopyToVirtualBoard(int y1, int x1, int y2, int x2)
    {
      for (int y = y1 - 1; y <= y2; y++)
      {
        for (int x = x1 - 1; x <= x2; x++)
        {
          int buf = y * BoardWidth + x;
          if (EnableIndex(buf))
          {
            virtualBoard[buf].Index = buf;
            virtualBoard[buf].IsChanged = true;

            virtualBoard[buf].Value = data[buf].Val;
          }
        }
      }
    }

    /// <summary>ターゲットセルの周辺８マスを見て思考する</summary>
    private void シンプルに考える(CellData d)
    {
      d.Update();
      if (d.Val == 0 || d.Val == Opened) return;

      // 周囲の開いていないセル、旗が立っているセルを数える
      int cntNotYetOpen = 0;
      int cntFlag = 0;

      for (int i = 0; i < 8; i++)
      {
        int idx = d.Index + kinbo[i];
        int val = data[idx].Val;
        if (val == NotYetOpen) cntNotOpenBlockBuf[cntNotYetOpen++] = idx;
        else if (val == BombFlag) cntFlagBlockBuf[cntFlag++] = idx;
      }

      //周囲に十分な旗が立っているなら、周辺の開いていないセルを開く
      if (d.Val == cntFlag)
      {
        for (int i = 0; i < 8; i++)
        {
          int idx = d.Index + kinbo[i];
          if (data[idx].Val == NotYetOpen)
          {
            data[idx].Val = Opened;
            Move_Click(idx % BoardWidth - 1, idx / BoardWidth - 1, MouseButtons.Left);
            Thread.Sleep(SleepCnt);
            isUpdatedNowFrame = true;
          }
        }
      }

      // 旗が確定するなら、周辺に旗を立てる
      else if (d.Val == cntNotYetOpen + cntFlag)
      {
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
          Move_Click(idx % BoardWidth - 1, idx / BoardWidth - 1, MouseButtons.Right);
          isUpdatedNowFrame = true;
          Thread.Sleep(SleepCnt);
        }
      }
    }

    /// <summary>指定したセルの座標にマウスカーソルを移動する</summary>
    private void MoveMouse(int x, int y)
    {
      Rectangle rect = CaptureHelper.GetWindowRect(mineProcess.MainWindowHandle);
      int bufX = rect.Left + OriginX + x * CellSize + CellSize / 2;
      int bufY = rect.Top + OriginY + y * CellSize + CellSize / 2;
      MouseEventHelper.Move(bufX, bufY);
    }

    /// <summary>指定したセルの座標にマウスカーソルを移動したあと、マウスのボタンを押下する</summary>
    private void Move_Click(int x, int y, MouseButtons button)
    {
      Rectangle rect = CaptureHelper.GetWindowRect(mineProcess.MainWindowHandle);
      int bufX = rect.Left + OriginX + x * CellSize + CellSize / 2;
      int bufY = rect.Top + OriginY + y * CellSize + CellSize / 2;
      MouseEventHelper.Move_Click(bufX, bufY, button);
    }

    /// <summary>マインスイーパーのキャプチャ画像からデータを読み込みます</summary>
    private void UpdateBoardDataFromWindowView()
    {
      // ウィンドウの画像をキャプチャし、OpenCVで扱いやすい画像に変換する
      CaptureHelper.CaptureWindow(mineProcess.MainWindowHandle, bmp, graphics);
      BitmapConverter.ToIplImage(bmp, iplRawCapturedImg);

      // セル群の範囲だけ、グレースケールでキャプチャする
      Cv.SetImageROI(iplRawCapturedImg, new CvRect(OriginX, OriginY, BoardWidthPix, BoardHeightPix));
      Cv.SetImageCOI(iplRawCapturedImg, 3);
      Cv.ResetImageROI(iplGrayImg);
      Cv.Copy(iplRawCapturedImg, iplGrayImg);

      // 
      Cv.SetImageROI(iplRawCapturedImg, new CvRect(OriginX, OriginY, BoardWidthPix, BoardHeightPix));
      Cv.SetImageCOI(iplRawCapturedImg, 0);
      Cv.ResetImageROI(subColorImg);
      Cv.Copy(iplRawCapturedImg, subColorImg);
      Cv.ResetImageROI(iplRawCapturedImg);

      // 各ブロックの取得
      for (int i = 0; i < data.Length; i++)
      {
        CellData tmp = data[i];
        if ((tmp.Val == NotYetOpen || // マダ開いていない
             tmp.Val == Opened ||     // 開いた判定を置いただけ
             tmp.Val == BombFlag) &&  // 旗を立てた
            !tmp.IsFix)               // 数値固定フラグが立っていない
        {
          // 処理しやすいように画像を加工する
          int x = i % BoardWidth - 1;
          int y = i / BoardWidth - 1;
          Cv.SetImageROI(iplGrayImg, new CvRect(x * CellSize, y * CellSize, CellSize, CellSize));
          Cv.SetImageROI(subColorImg, new CvRect(x * CellSize, y * CellSize, CellSize, CellSize));
          Cv.EqualizeHist(iplGrayImg, iplGrayImg);

          int tmpVal = tmp.Val;

          if (CheckFlag(subColorImg)) tmp.Val = BombFlag; // 旗か確認する
          else if (iplGrayImg.Get2D(9, 8).Val0 >= 200) tmp.Val = NotYetOpen;  // マダ開いてないか確認する
          else if (Check1(subColorImg)) tmp.Val = 1;  // 各数字かどうか確認する。
          else if (Check2(subColorImg)) tmp.Val = 2;  // 処理しやすい順番で確認を行う
          else if (Check5(subColorImg)) tmp.Val = 5;
          else if (Check7(subColorImg)) tmp.Val = 7;
          else if (Check3(subColorImg)) tmp.Val = 3;
          else if (Check4(subColorImg)) tmp.Val = 4;
          else if (Check6(subColorImg)) tmp.Val = 6;
          else tmp.Val = Opened;

          // 値が書き換えられたなら、周辺のセルに「周辺の状況が変わったぞ」と通知を出す。
          // この通知が行われたセルに対して積極的にシミュレーションを行う
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

            // １～９の数値が代入された場合、この値を固定する。
            // 以降、読み込みの対象としない
            if (tmp.Val > 0 && tmp.Val <= 9)
            {
              tmp.IsFix = true;
            }
          }
        }
      }
    }

    /// <summary>デバッグ用の情報を描画する</summary>
    private void DrawDebugInfo()
    {
      CvRect rect = new CvRect(0, 0, CellSize - 1, CellSize - 1);
      for (int i = 0; i < data.Length; i++)
      {
        if (data[i].Val != Invalid)
        {
          int x = i % BoardWidth - 1;
          int y = i / BoardWidth - 1;
          Cv.SetImageROI(outputImg, new CvRect(x * CellSize, y * CellSize, CellSize, CellSize));
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

    #region check cell number helpers

    private bool Check1(IplImage img)
    {
      return img.Get2D(8, 9) == new CvScalar(190, 80, 64, 255);
    }
    private bool Check2(IplImage img)
    {
      CvScalar tmp = img.Get2D(7, 12);
      return (tmp.Val0 < 50 && tmp.Val1 > 50 && tmp.Val2 < 60);
    }
    private bool Check3(IplImage img)
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
    private bool Check4(IplImage img)
    {
      CvScalar tmp0 = img.Get2D(10, 10);
      CvScalar tmp1 = img.Get2D(11, 11);
      return (tmp0.Val0 > 100 && tmp0.Val1 < 10 && tmp0.Val2 < 10)
           || (tmp1.Val0 > 100 && tmp1.Val1 < 10 && tmp1.Val2 < 10);
    }
    private bool Check5(IplImage img)
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
    private bool Check6(IplImage img)
    {
      CvScalar tmp0 = img.Get2D(11, 6);
      CvScalar tmp1 = img.Get2D(11, 7);
      return (90 < tmp0.Val0 && tmp0.Val0 < 144 &&
              90 < tmp0.Val1 && tmp0.Val1 < 144 &&
              tmp0.Val2 < 10) ||
             (90 < tmp1.Val0 && tmp1.Val0 < 144 &&
              90 < tmp1.Val1 && tmp1.Val1 < 144 &&
              tmp1.Val2 < 10);
    }
    private bool Check7(IplImage img)
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
    }
    private bool Check7Helper(CvScalar tmp)
    {
      return (tmp.Val0 < 20 &&
              tmp.Val1 < 20 &&
              150 < tmp.Val2 && tmp.Val2 < 190);
    }
    private bool CheckFlag(IplImage img)
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
    private bool CheckFlagHelper(IplImage img, int x, int y)
    {
      CvScalar tmp = img.Get2D(x, y);
      return tmp.Val0 < 90 &&
             tmp.Val1 < 90 &&
             240 < tmp.Val2;
    }
    private bool CheckFlagHelper2(IplImage img, int x, int y)
    {
      CvScalar tmp = img.Get2D(x, y);
      return 240 < tmp.Val0 &&
             240 < tmp.Val1 &&
             240 < tmp.Val2;
    }

    #endregion

  }

  /// <summary>
  /// 仮想盤面で使用するセルデータ。
  /// 
  /// </summary>
  struct VirtualData
  {
    public int Index;
    public int Value;
    public bool IsChanged;
    public int X { get { return MainForm.BoardWidth % Index - 1; } }
    public int Y { get { return MainForm.BoardWidth / Index - 1; ; } }

    public override string ToString()
    {
      return string.Format("{0}, {1} val={2}", X, Y, Value);
    }
  }
}
