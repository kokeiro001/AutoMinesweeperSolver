namespace PlayMinesweeper
{
	partial class MainForm
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
			this.pbxMain = new OpenCvSharp.UserInterface.PictureBoxIpl();
			this.btnReset = new System.Windows.Forms.Button();
			this.btnTest = new System.Windows.Forms.Button();
			this.btnRestart = new System.Windows.Forms.Button();
			((System.ComponentModel.ISupportInitialize)(this.pbxMain)).BeginInit();
			this.SuspendLayout();
			// 
			// pbxMain
			// 
			this.pbxMain.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
									| System.Windows.Forms.AnchorStyles.Left)
									| System.Windows.Forms.AnchorStyles.Right)));
			this.pbxMain.Location = new System.Drawing.Point(12, 41);
			this.pbxMain.Name = "pbxMain";
			this.pbxMain.Size = new System.Drawing.Size(549, 313);
			this.pbxMain.TabIndex = 0;
			this.pbxMain.TabStop = false;
			// 
			// btnReset
			// 
			this.btnReset.Location = new System.Drawing.Point(12, 12);
			this.btnReset.Name = "btnReset";
			this.btnReset.Size = new System.Drawing.Size(75, 23);
			this.btnReset.TabIndex = 1;
			this.btnReset.Text = "reset";
			this.btnReset.UseVisualStyleBackColor = true;
			this.btnReset.Click += new System.EventHandler(this.btnReset_Click);
			// 
			// btnTest
			// 
			this.btnTest.Location = new System.Drawing.Point(93, 12);
			this.btnTest.Name = "btnTest";
			this.btnTest.Size = new System.Drawing.Size(75, 23);
			this.btnTest.TabIndex = 2;
			this.btnTest.Text = "button1";
			this.btnTest.UseVisualStyleBackColor = true;
			this.btnTest.Click += new System.EventHandler(this.btnTest_Click);
			// 
			// btnRestart
			// 
			this.btnRestart.Location = new System.Drawing.Point(174, 12);
			this.btnRestart.Name = "btnRestart";
			this.btnRestart.Size = new System.Drawing.Size(75, 23);
			this.btnRestart.TabIndex = 3;
			this.btnRestart.Text = "restart";
			this.btnRestart.UseVisualStyleBackColor = true;
			// 
			// MainForm
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(573, 366);
			this.Controls.Add(this.btnRestart);
			this.Controls.Add(this.btnTest);
			this.Controls.Add(this.btnReset);
			this.Controls.Add(this.pbxMain);
			this.Name = "MainForm";
			this.Text = "MainForm";
			this.Load += new System.EventHandler(this.MainForm_Load);
			((System.ComponentModel.ISupportInitialize)(this.pbxMain)).EndInit();
			this.ResumeLayout(false);

		}

		#endregion

		private OpenCvSharp.UserInterface.PictureBoxIpl pbxMain;
		private System.Windows.Forms.Button btnReset;
		private System.Windows.Forms.Button btnTest;
		private System.Windows.Forms.Button btnRestart;
	}
}

