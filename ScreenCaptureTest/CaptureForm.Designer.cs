namespace ScreenCaptureTest
{
	partial class CaptureForm
	{
		/// <summary>
		/// 必要なデザイナー変数です。
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary>
		/// 使用中のリソースをすべてクリーンアップします。
		/// </summary>
		/// <param name="disposing">マネージ リソースが破棄される場合 true、破棄されない場合は false です。</param>
		protected override void Dispose(bool disposing)
		{
			if (disposing && (components != null))
			{
				components.Dispose();
			}
			base.Dispose(disposing);
		}

		#region Windows フォーム デザイナーで生成されたコード

		/// <summary>
		/// デザイナー サポートに必要なメソッドです。このメソッドの内容を
		/// コード エディターで変更しないでください。
		/// </summary>
		private void InitializeComponent()
		{
			this.btnReset = new System.Windows.Forms.Button();
			this.glcScreen = new OpenTK.GLControl();
			this.btnDefShader = new System.Windows.Forms.Button();
			this.btnGrayScale = new System.Windows.Forms.Button();
			this.btnSepia = new System.Windows.Forms.Button();
			this.SuspendLayout();
			// 
			// btnReset
			// 
			this.btnReset.ForeColor = System.Drawing.Color.Black;
			this.btnReset.Location = new System.Drawing.Point(13, 12);
			this.btnReset.Name = "btnReset";
			this.btnReset.Size = new System.Drawing.Size(49, 23);
			this.btnReset.TabIndex = 0;
			this.btnReset.Text = "リセット";
			this.btnReset.UseVisualStyleBackColor = true;
			this.btnReset.Click += new System.EventHandler(this.btnBeginRecode_Click);
			// 
			// glcScreen
			// 
			this.glcScreen.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
									| System.Windows.Forms.AnchorStyles.Left)
									| System.Windows.Forms.AnchorStyles.Right)));
			this.glcScreen.BackColor = System.Drawing.Color.Black;
			this.glcScreen.Location = new System.Drawing.Point(13, 41);
			this.glcScreen.Name = "glcScreen";
			this.glcScreen.Size = new System.Drawing.Size(470, 442);
			this.glcScreen.TabIndex = 1;
			this.glcScreen.VSync = false;
			// 
			// btnDefShader
			// 
			this.btnDefShader.Location = new System.Drawing.Point(68, 12);
			this.btnDefShader.Name = "btnDefShader";
			this.btnDefShader.Size = new System.Drawing.Size(48, 23);
			this.btnDefShader.TabIndex = 2;
			this.btnDefShader.Text = "通常";
			this.btnDefShader.UseVisualStyleBackColor = true;
			this.btnDefShader.Click += new System.EventHandler(this.btnDefShader_Click);
			// 
			// btnGrayScale
			// 
			this.btnGrayScale.Location = new System.Drawing.Point(122, 12);
			this.btnGrayScale.Name = "btnGrayScale";
			this.btnGrayScale.Size = new System.Drawing.Size(44, 23);
			this.btnGrayScale.TabIndex = 3;
			this.btnGrayScale.Text = "グレー";
			this.btnGrayScale.UseVisualStyleBackColor = true;
			this.btnGrayScale.Click += new System.EventHandler(this.btnGrayScale_Click);
			// 
			// btnSepia
			// 
			this.btnSepia.Location = new System.Drawing.Point(172, 12);
			this.btnSepia.Name = "btnSepia";
			this.btnSepia.Size = new System.Drawing.Size(51, 23);
			this.btnSepia.TabIndex = 4;
			this.btnSepia.Text = "セピア";
			this.btnSepia.UseVisualStyleBackColor = true;
			this.btnSepia.Click += new System.EventHandler(this.btnSepia_Click);
			// 
			// CaptureForm
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(498, 485);
			this.Controls.Add(this.btnSepia);
			this.Controls.Add(this.btnGrayScale);
			this.Controls.Add(this.btnDefShader);
			this.Controls.Add(this.glcScreen);
			this.Controls.Add(this.btnReset);
			this.Name = "CaptureForm";
			this.Text = "Form1";
			this.Load += new System.EventHandler(this.CaptureForm_Load);
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.Button btnReset;
		private OpenTK.GLControl glcScreen;
		private System.Windows.Forms.Button btnDefShader;
		private System.Windows.Forms.Button btnGrayScale;
		private System.Windows.Forms.Button btnSepia;
	}
}

