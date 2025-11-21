namespace AgGrade.Controls
{
    partial class StatusBar
    {
        /// <summary> 
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// Clean up any resources being used.
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

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(StatusBar));
            TractorRTKLed = new PictureBox();
            FrontRTKLed = new PictureBox();
            RearRTKLed = new PictureBox();
            TractorIMULed = new PictureBox();
            FrontIMULed = new PictureBox();
            RearIMULed = new PictureBox();
            FrontHeightLed = new PictureBox();
            RearHeightLed = new PictureBox();
            label1 = new Label();
            label2 = new Label();
            label3 = new Label();
            ((System.ComponentModel.ISupportInitialize)TractorRTKLed).BeginInit();
            ((System.ComponentModel.ISupportInitialize)FrontRTKLed).BeginInit();
            ((System.ComponentModel.ISupportInitialize)RearRTKLed).BeginInit();
            ((System.ComponentModel.ISupportInitialize)TractorIMULed).BeginInit();
            ((System.ComponentModel.ISupportInitialize)FrontIMULed).BeginInit();
            ((System.ComponentModel.ISupportInitialize)RearIMULed).BeginInit();
            ((System.ComponentModel.ISupportInitialize)FrontHeightLed).BeginInit();
            ((System.ComponentModel.ISupportInitialize)RearHeightLed).BeginInit();
            SuspendLayout();
            // 
            // TractorRTKLed
            // 
            TractorRTKLed.Image = (Image)resources.GetObject("TractorRTKLed.Image");
            TractorRTKLed.Location = new Point(43, 0);
            TractorRTKLed.Name = "TractorRTKLed";
            TractorRTKLed.Size = new Size(24, 24);
            TractorRTKLed.TabIndex = 0;
            TractorRTKLed.TabStop = false;
            // 
            // FrontRTKLed
            // 
            FrontRTKLed.Image = (Image)resources.GetObject("FrontRTKLed.Image");
            FrontRTKLed.Location = new Point(70, 0);
            FrontRTKLed.Name = "FrontRTKLed";
            FrontRTKLed.Size = new Size(24, 24);
            FrontRTKLed.TabIndex = 1;
            FrontRTKLed.TabStop = false;
            // 
            // RearRTKLed
            // 
            RearRTKLed.Image = (Image)resources.GetObject("RearRTKLed.Image");
            RearRTKLed.Location = new Point(97, 0);
            RearRTKLed.Name = "RearRTKLed";
            RearRTKLed.Size = new Size(24, 24);
            RearRTKLed.TabIndex = 2;
            RearRTKLed.TabStop = false;
            // 
            // TractorIMULed
            // 
            TractorIMULed.Image = (Image)resources.GetObject("TractorIMULed.Image");
            TractorIMULed.Location = new Point(170, 0);
            TractorIMULed.Name = "TractorIMULed";
            TractorIMULed.Size = new Size(24, 24);
            TractorIMULed.TabIndex = 3;
            TractorIMULed.TabStop = false;
            // 
            // FrontIMULed
            // 
            FrontIMULed.Image = (Image)resources.GetObject("FrontIMULed.Image");
            FrontIMULed.Location = new Point(197, 0);
            FrontIMULed.Name = "FrontIMULed";
            FrontIMULed.Size = new Size(24, 24);
            FrontIMULed.TabIndex = 4;
            FrontIMULed.TabStop = false;
            // 
            // RearIMULed
            // 
            RearIMULed.Image = (Image)resources.GetObject("RearIMULed.Image");
            RearIMULed.Location = new Point(224, 0);
            RearIMULed.Name = "RearIMULed";
            RearIMULed.Size = new Size(24, 24);
            RearIMULed.TabIndex = 5;
            RearIMULed.TabStop = false;
            // 
            // FrontHeightLed
            // 
            FrontHeightLed.Image = (Image)resources.GetObject("FrontHeightLed.Image");
            FrontHeightLed.Location = new Point(321, 0);
            FrontHeightLed.Name = "FrontHeightLed";
            FrontHeightLed.Size = new Size(24, 24);
            FrontHeightLed.TabIndex = 6;
            FrontHeightLed.TabStop = false;
            // 
            // RearHeightLed
            // 
            RearHeightLed.Image = (Image)resources.GetObject("RearHeightLed.Image");
            RearHeightLed.Location = new Point(348, 0);
            RearHeightLed.Name = "RearHeightLed";
            RearHeightLed.Size = new Size(24, 24);
            RearHeightLed.TabIndex = 7;
            RearHeightLed.TabStop = false;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Font = new Font("Segoe UI", 12F);
            label1.Location = new Point(3, 1);
            label1.Name = "label1";
            label1.Size = new Size(39, 21);
            label1.TabIndex = 8;
            label1.Text = "RTK:";
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Font = new Font("Segoe UI", 12F);
            label2.Location = new Point(127, 1);
            label2.Name = "label2";
            label2.Size = new Size(42, 21);
            label2.TabIndex = 9;
            label2.Text = "IMU:";
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Font = new Font("Segoe UI", 12F);
            label3.Location = new Point(254, 1);
            label3.Name = "label3";
            label3.Size = new Size(66, 21);
            label3.TabIndex = 10;
            label3.Text = "HEIGHT:";
            // 
            // StatusBar
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            Controls.Add(label3);
            Controls.Add(label2);
            Controls.Add(label1);
            Controls.Add(RearHeightLed);
            Controls.Add(FrontHeightLed);
            Controls.Add(RearIMULed);
            Controls.Add(FrontIMULed);
            Controls.Add(TractorIMULed);
            Controls.Add(RearRTKLed);
            Controls.Add(FrontRTKLed);
            Controls.Add(TractorRTKLed);
            Name = "StatusBar";
            Size = new Size(469, 24);
            ((System.ComponentModel.ISupportInitialize)TractorRTKLed).EndInit();
            ((System.ComponentModel.ISupportInitialize)FrontRTKLed).EndInit();
            ((System.ComponentModel.ISupportInitialize)RearRTKLed).EndInit();
            ((System.ComponentModel.ISupportInitialize)TractorIMULed).EndInit();
            ((System.ComponentModel.ISupportInitialize)FrontIMULed).EndInit();
            ((System.ComponentModel.ISupportInitialize)RearIMULed).EndInit();
            ((System.ComponentModel.ISupportInitialize)FrontHeightLed).EndInit();
            ((System.ComponentModel.ISupportInitialize)RearHeightLed).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private PictureBox TractorRTKLed;
        private PictureBox FrontRTKLed;
        private PictureBox RearRTKLed;
        private PictureBox TractorIMULed;
        private PictureBox FrontIMULed;
        private PictureBox RearIMULed;
        private PictureBox FrontHeightLed;
        private PictureBox RearHeightLed;
        private Label label1;
        private Label label2;
        private Label label3;
    }
}
