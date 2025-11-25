namespace HardwareSim
{
    partial class MainForm
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            EStopBtn = new Button();
            ClearEStopBtn = new Button();
            SuspendLayout();
            // 
            // EStopBtn
            // 
            EStopBtn.Location = new Point(12, 12);
            EStopBtn.Name = "EStopBtn";
            EStopBtn.Size = new Size(75, 23);
            EStopBtn.TabIndex = 0;
            EStopBtn.Text = "Set ESTOP";
            EStopBtn.UseVisualStyleBackColor = true;
            EStopBtn.Click += EStopBtn_Click;
            // 
            // ClearEStopBtn
            // 
            ClearEStopBtn.Location = new Point(93, 12);
            ClearEStopBtn.Name = "ClearEStopBtn";
            ClearEStopBtn.Size = new Size(92, 23);
            ClearEStopBtn.TabIndex = 1;
            ClearEStopBtn.Text = "Clear ESTOP";
            ClearEStopBtn.UseVisualStyleBackColor = true;
            ClearEStopBtn.Click += ClearEStopBtn_Click;
            // 
            // MainForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(800, 450);
            Controls.Add(ClearEStopBtn);
            Controls.Add(EStopBtn);
            Name = "MainForm";
            Text = "Hardware Simulator";
            ResumeLayout(false);
        }

        #endregion

        private Button EStopBtn;
        private Button ClearEStopBtn;
    }
}
