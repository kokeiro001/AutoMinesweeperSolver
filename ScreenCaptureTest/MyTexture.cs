using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Text;
using OpenTK;
using OpenTK.Graphics.OpenGL;


namespace ScreenCaptureTest
{
	class MyTexture
	{
		public int Handle { get; private set; }
		public int Width { get; protected set; }
		public int Height { get; protected set; }

		static Plane plane;
		static GLControl renderTarget;

		public string ShaderName { get; set; }

		static Dictionary<string, int> shaders = new Dictionary<string, int>();

		public void Draw(float x, float y, float z)
		{
			int shader = shaders[ShaderName];
			GL.UseProgram(shader);
			GL.UniformMatrix4(GL.GetUniformLocation(shader, "viewProjection"), false, ref Matrix4.Identity);

			//float aspect = (float)renderTarget.Width / renderTarget.Height;

			Matrix4 worldMatrix = Matrix4.Identity;

			// 拡大
			float xbuf = (float)(Width) / (renderTarget.Width);
			float ybuf = (float)(Height) / (renderTarget.Height);
			worldMatrix *= Matrix4.Scale(xbuf, ybuf, 1);

			// 回転
			// 移動
			xbuf = -1 + ((2 * x) / (float)renderTarget.Width);
			ybuf = -1 + ((2 * y) / (float)renderTarget.Height);
			worldMatrix *= Matrix4.CreateTranslation(xbuf, ybuf, z);

			GL.UniformMatrix4(GL.GetUniformLocation(shader, "world"), false, ref worldMatrix);

			// テクスチャの描画
			GL.ActiveTexture(TextureUnit.Texture0);
			GL.BindTexture(TextureTarget.Texture2D, Handle);
			GL.Uniform1(GL.GetUniformLocation(shader, "tex"), 0);
			GL.Color3(Color.Gray);
			plane.Render();
		}

		static public void Init(GLControl renderTarget)
		{
			shaders.Add("def", CreateShader(Encoding.UTF8.GetString(Properties.Resources.textured_vert), Encoding.UTF8.GetString(Properties.Resources.textured_frag)));
			shaders.Add("gray", CreateShader(Encoding.UTF8.GetString(Properties.Resources.textured_vert), Encoding.UTF8.GetString(Properties.Resources.gray_frag)));
			shaders.Add("sepia", CreateShader(Encoding.UTF8.GetString(Properties.Resources.textured_vert), Encoding.UTF8.GetString(Properties.Resources.sepia_frag)));

			plane = new Plane(shaders["def"]);

			MyTexture.renderTarget = renderTarget;
		}

		// シェーダを作成
		static int CreateShader(string vertexShaderCode, string fragmentShaderCode)
		{
			int vshader = GL.CreateShader(ShaderType.VertexShader);
			int fshader = GL.CreateShader(ShaderType.FragmentShader);

			string info;
			int status_code;

			// Vertex shader
			GL.ShaderSource(vshader, vertexShaderCode);
			GL.CompileShader(vshader);
			GL.GetShaderInfoLog(vshader, out info);
			GL.GetShader(vshader, ShaderParameter.CompileStatus, out status_code);
			if (status_code != 1)
			{
				throw new ApplicationException(info);
			}

			// Fragment shader
			GL.ShaderSource(fshader, fragmentShaderCode);
			GL.CompileShader(fshader);
			GL.GetShaderInfoLog(fshader, out info);
			GL.GetShader(fshader, ShaderParameter.CompileStatus, out status_code);
			if (status_code != 1)
			{
				throw new ApplicationException(info);
			}

			int program = GL.CreateProgram();
			GL.AttachShader(program, vshader);
			GL.AttachShader(program, fshader);

			GL.LinkProgram(program);

			return program;
		}

		static public MyTexture Load(string filename)
		{
			MyTexture tmpTex = new MyTexture();

			int tmp;
			GL.GenTextures(1, out tmp);
			tmpTex.Handle = tmp;

			GL.BindTexture(TextureTarget.Texture2D, tmpTex.Handle);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);

			using (Bitmap bitmap = new Bitmap(filename))
			{
				BitmapData data = bitmap.LockBits(new System.Drawing.Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
				GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, data.Width, data.Height, 0, OpenTK.Graphics.OpenGL.PixelFormat.Bgra, PixelType.UnsignedByte, data.Scan0);
				bitmap.UnlockBits(data);

				tmpTex.Width = data.Width;
				tmpTex.Height = data.Height;
			}
			return tmpTex;
		}

		public static MyTexture Create(int width, int height)
		{
			MyTexture tmpTex = new MyTexture();
			tmpTex.ShaderName = "sepia";

			int tmp;
			GL.GenTextures(1, out tmp);
			tmpTex.Handle = tmp;

			GL.BindTexture(TextureTarget.Texture2D, tmpTex.Handle);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);

			using (Bitmap bitmap = new Bitmap(width, height))
			{
				BitmapData data = bitmap.LockBits(new System.Drawing.Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
				GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, data.Width, data.Height, 0, OpenTK.Graphics.OpenGL.PixelFormat.Bgra, PixelType.UnsignedByte, data.Scan0);
				bitmap.UnlockBits(data);

				tmpTex.Width = data.Width;
				tmpTex.Height = data.Height;
			}
			return tmpTex;
		}
	}


}
