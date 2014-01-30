using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using OpenTK;
using OpenTK.Graphics.OpenGL;

namespace ScreenCaptureTest
{
	public partial class CaptureForm : Form
	{
		IntPtr targetWindowHnadle;
		Size targetWindowSize;

		Data frameBuf;
		MyTexture sampleTex;
		MyTexture backTex;
		bool enable = false;

		public CaptureForm()
		{
			InitializeComponent();
		}

		private void CaptureForm_Load(object sender, EventArgs e)
		{
			// Get Target Procces
			List<Process> ps = Process.GetProcesses().ToList();
			Process target = ps.Find(p => p.ProcessName == "mpc-hc64");
			if (target == null) throw new Exception("ターゲットプロセスが見つかりません");

			targetWindowHnadle = target.MainWindowHandle;
			targetWindowSize = CaptureHelper.GetWindowSize(targetWindowHnadle);

			frameBuf = Data.Create(targetWindowSize);

			// Init OpenGL
			glcScreen.MakeCurrent();

			GL.Enable(EnableCap.DepthTest);
			GL.Enable(EnableCap.CullFace);
			GL.FrontFace(FrontFaceDirection.Cw);
			GL.CullFace(CullFaceMode.Back);

			// load textures
			SetupViewport();
			sampleTex = MyTexture.Create(frameBuf.Bitmap.Width, frameBuf.Bitmap.Height);

			MyTexture.Init(glcScreen);

			//sampleTex = MyTexture.Load("../../texture/tex.jpg");
			// backTex = MyTexture.Load("../../texture/back.png");
		}


		public void MyUpdate()
		{
			if (enable)
			{
				// capture
				Bitmap bitmap = frameBuf.Bitmap;
				CaptureHelper.CaptureActiveWindow(targetWindowHnadle, bitmap, frameBuf.Graphics);

				GL.BindTexture(TextureTarget.Texture2D, sampleTex.Handle);
				GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
				GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);

				BitmapData data = bitmap.LockBits(new System.Drawing.Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
				GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, data.Width, data.Height, 0, OpenTK.Graphics.OpenGL.PixelFormat.Bgra, PixelType.UnsignedByte, data.Scan0);
				bitmap.UnlockBits(data);
			}
			else
			{
				enable = false;
			}
		}

		private void btnBeginRecode_Click(object sender, EventArgs e)
		{
			frameBuf = Data.Create(targetWindowSize);
			sampleTex = MyTexture.Create(frameBuf.Bitmap.Width, frameBuf.Bitmap.Height);
		}

		public void BeginCapture()
		{
			enable = true;
		}
	
		void GrayScale(Bitmap bmp)
		{
			for (int y = 0; y < bmp.Height; y++)
			{
				for (int x = 0; x < bmp.Width; x++)
				{
					Color srcCol = bmp.GetPixel(x, y);
					int Y = (int)(0.298912 * srcCol.R + 0.586611 * srcCol.G + 0.114478 * srcCol.B);
					Color gray = Color.FromArgb(Y, Y, Y);
					bmp.SetPixel(x, y, gray);
				}
			}
		}

		private void SetupViewport()
		{
			// 視体積の設定
			GL.MatrixMode(MatrixMode.Projection);
			float h = 4.0f;
			float w = h * (glcScreen.Width / (float)glcScreen.Height);
			Matrix4 proj = Matrix4.CreateOrthographic(w, h, 0.1f, 2.0f);
			GL.LoadMatrix(ref proj);

			// 視界の設定
			GL.MatrixMode(MatrixMode.Modelview);
			Matrix4 look = Matrix4.LookAt(Vector3.UnitZ, Vector3.Zero, Vector3.UnitY);
			GL.LoadMatrix(ref look);

			GL.Viewport(0, 0, glcScreen.Width, glcScreen.Height);
		}

		public void Render()
		{
			glcScreen.MakeCurrent();
			GL.Enable(EnableCap.DepthTest);
			GL.Enable(EnableCap.CullFace);
			GL.FrontFace(FrontFaceDirection.Cw);
			GL.CullFace(CullFaceMode.Back);
			// バッファクリア
			SetupViewport();
			GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
			sampleTex.Draw(sampleTex.Width / 2, sampleTex.Height / 2, 0.5f);
			//backTex.Draw(322, 0, 0.2f);

			glcScreen.SwapBuffers();
		}

		#region color button

		private void btnDefShader_Click(object sender, EventArgs e)
		{
			sampleTex.ShaderName = "def";
		}

		private void btnGrayScale_Click(object sender, EventArgs e)
		{
			sampleTex.ShaderName = "gray";
		}

		private void btnSepia_Click(object sender, EventArgs e)
		{
			sampleTex.ShaderName = "sepia";
		}

		#endregion
	}

	struct Data
	{
		public Bitmap Bitmap;
		public Graphics Graphics;

		public static Data Create(Size size)
		{
			Data tmp = new Data();
			tmp.Bitmap = new Bitmap(size.Width, size.Height);
			tmp.Graphics = Graphics.FromImage(tmp.Bitmap);
			return tmp;
		}

	}

	public class Game
	{

		public void Run()
		{
			using (CaptureForm form = new CaptureForm())
			{
				const int Fps = 60;
				const double Wait = 1000.0 / Fps;

				const int CaptureTimeSec = 30;

				double endCount = System.Environment.TickCount + CaptureTimeSec * 1000;
				double nextFrame = System.Environment.TickCount + Wait;

				form.Show();
				form.BeginCapture();
				while (form.Created)
				{
					try
					{
						int now = System.Environment.TickCount;
						if (now >= nextFrame)
						{
							if (now < nextFrame + Wait)
							{
								// 更新タイミングなら更新
								form.MyUpdate();
								form.Render();
							}
							nextFrame += Wait;
						}
						int sleepTime = (int)(nextFrame - now);
						if (sleepTime > 0) Thread.Sleep(sleepTime);
						Application.DoEvents();
					}
					catch (Exception e)
					{ 
						MessageBox.Show(e.Message);
					}
				}
			}
		}
	}

}
