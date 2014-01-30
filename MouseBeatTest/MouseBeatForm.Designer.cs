namespace MouseBeatTest
{
	partial class MouseBeatForm
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
			this.components = new System.ComponentModel.Container();
			this.btnBeginBeat = new System.Windows.Forms.Button();
			this.beginTimer = new System.Windows.Forms.Timer(this.components);
			this.remainingTimer = new System.Windows.Forms.Timer(this.components);
			this.SuspendLayout();
			// 
			// btnBeginBeat
			// 
			this.btnBeginBeat.Location = new System.Drawing.Point(12, 12);
			this.btnBeginBeat.Name = "btnBeginBeat";
			this.btnBeginBeat.Size = new System.Drawing.Size(75, 23);
			this.btnBeginBeat.TabIndex = 0;
			this.btnBeginBeat.Text = "button1";
			this.btnBeginBeat.UseVisualStyleBackColor = true;
			this.btnBeginBeat.Click += new System.EventHandler(this.btnBeginBeat_Click);
			// 
			// Form1
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(284, 262);
			this.Controls.Add(this.btnBeginBeat);
			this.Name = "Form1";
			this.Text = "Form1";
			this.Load += new System.EventHandler(this.Form1_Load);
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.Button btnBeginBeat;
		private System.Windows.Forms.Timer beginTimer;
		private System.Windows.Forms.Timer remainingTimer;
	}
}

