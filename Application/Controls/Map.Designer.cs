namespace AgGrade.Controls
{
    partial class Map
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
            MapCanvas = new PictureBox();
            panel1 = new Panel();
            FrontBladeHeightLabel = new Label();
            RearBladeHeightLabel = new Label();
            FrontLoadLabel = new Label();
            RearLoadLabel = new Label();
            HeadingLabel = new Label();
            SpeedLabel = new Label();
            FieldNameLabel = new Label();
            panel2 = new Panel();
            ((System.ComponentModel.ISupportInitialize)MapCanvas).BeginInit();
            panel1.SuspendLayout();
            SuspendLayout();
            // 
            // MapCanvas
            // 
            MapCanvas.Dock = DockStyle.Fill;
            MapCanvas.Location = new Point(0, 49);
            MapCanvas.Name = "MapCanvas";
            MapCanvas.Size = new Size(862, 307);
            MapCanvas.SizeMode = PictureBoxSizeMode.CenterImage;
            MapCanvas.TabIndex = 0;
            MapCanvas.TabStop = false;
            MapCanvas.SizeChanged += MapCanvas_SizeChanged;
            // 
            // panel1
            // 
            panel1.BackColor = Color.PaleGoldenrod;
            panel1.Controls.Add(FrontBladeHeightLabel);
            panel1.Controls.Add(RearBladeHeightLabel);
            panel1.Controls.Add(FrontLoadLabel);
            panel1.Controls.Add(RearLoadLabel);
            panel1.Controls.Add(HeadingLabel);
            panel1.Controls.Add(SpeedLabel);
            panel1.Controls.Add(FieldNameLabel);
            panel1.Dock = DockStyle.Top;
            panel1.Location = new Point(0, 0);
            panel1.Name = "panel1";
            panel1.Size = new Size(862, 45);
            panel1.TabIndex = 1;
            // 
            // FrontBladeHeightLabel
            // 
            FrontBladeHeightLabel.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            FrontBladeHeightLabel.AutoSize = true;
            FrontBladeHeightLabel.Font = new Font("Segoe UI", 14F, FontStyle.Bold);
            FrontBladeHeightLabel.ForeColor = Color.RoyalBlue;
            FrontBladeHeightLabel.Location = new Point(340, 10);
            FrontBladeHeightLabel.Name = "FrontBladeHeightLabel";
            FrontBladeHeightLabel.Size = new Size(73, 25);
            FrontBladeHeightLabel.TabIndex = 6;
            FrontBladeHeightLabel.Text = "10 mm";
            FrontBladeHeightLabel.TextAlign = ContentAlignment.MiddleRight;
            // 
            // RearBladeHeightLabel
            // 
            RearBladeHeightLabel.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            RearBladeHeightLabel.AutoSize = true;
            RearBladeHeightLabel.Font = new Font("Segoe UI", 14F, FontStyle.Bold);
            RearBladeHeightLabel.ForeColor = Color.DarkGoldenrod;
            RearBladeHeightLabel.Location = new Point(503, 10);
            RearBladeHeightLabel.Name = "RearBladeHeightLabel";
            RearBladeHeightLabel.Size = new Size(73, 25);
            RearBladeHeightLabel.TabIndex = 5;
            RearBladeHeightLabel.Text = "10 mm";
            RearBladeHeightLabel.TextAlign = ContentAlignment.MiddleRight;
            // 
            // FrontLoadLabel
            // 
            FrontLoadLabel.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            FrontLoadLabel.AutoSize = true;
            FrontLoadLabel.Font = new Font("Segoe UI", 14F, FontStyle.Bold);
            FrontLoadLabel.ForeColor = Color.RoyalBlue;
            FrontLoadLabel.Location = new Point(419, 10);
            FrontLoadLabel.Name = "FrontLoadLabel";
            FrontLoadLabel.Size = new Size(78, 25);
            FrontLoadLabel.TabIndex = 4;
            FrontLoadLabel.Text = "8.0 LCY";
            FrontLoadLabel.TextAlign = ContentAlignment.MiddleRight;
            FrontLoadLabel.Click += FrontLoadLabel_Click;
            // 
            // RearLoadLabel
            // 
            RearLoadLabel.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            RearLoadLabel.AutoSize = true;
            RearLoadLabel.Font = new Font("Segoe UI", 14F, FontStyle.Bold);
            RearLoadLabel.ForeColor = Color.DarkGoldenrod;
            RearLoadLabel.Location = new Point(582, 10);
            RearLoadLabel.Name = "RearLoadLabel";
            RearLoadLabel.Size = new Size(89, 25);
            RearLoadLabel.TabIndex = 3;
            RearLoadLabel.Text = "12.0 LCY";
            RearLoadLabel.TextAlign = ContentAlignment.MiddleRight;
            RearLoadLabel.Click += RearLoadLabel_Click;
            // 
            // HeadingLabel
            // 
            HeadingLabel.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            HeadingLabel.AutoSize = true;
            HeadingLabel.Font = new Font("Segoe UI", 14F, FontStyle.Bold);
            HeadingLabel.Location = new Point(677, 10);
            HeadingLabel.Name = "HeadingLabel";
            HeadingLabel.Size = new Size(68, 25);
            HeadingLabel.TabIndex = 2;
            HeadingLabel.Text = "359.5°";
            HeadingLabel.TextAlign = ContentAlignment.MiddleRight;
            // 
            // SpeedLabel
            // 
            SpeedLabel.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            SpeedLabel.AutoSize = true;
            SpeedLabel.Font = new Font("Segoe UI", 14F, FontStyle.Bold);
            SpeedLabel.Location = new Point(751, 10);
            SpeedLabel.Name = "SpeedLabel";
            SpeedLabel.Size = new Size(100, 25);
            SpeedLabel.TabIndex = 1;
            SpeedLabel.Text = "25.9 MPH";
            SpeedLabel.TextAlign = ContentAlignment.MiddleRight;
            // 
            // FieldNameLabel
            // 
            FieldNameLabel.AutoSize = true;
            FieldNameLabel.Font = new Font("Segoe UI", 14F, FontStyle.Bold);
            FieldNameLabel.Location = new Point(11, 10);
            FieldNameLabel.Name = "FieldNameLabel";
            FieldNameLabel.Size = new Size(111, 25);
            FieldNameLabel.TabIndex = 0;
            FieldNameLabel.Text = "Field Name";
            // 
            // panel2
            // 
            panel2.BackColor = Color.SeaGreen;
            panel2.Dock = DockStyle.Top;
            panel2.Location = new Point(0, 45);
            panel2.Name = "panel2";
            panel2.Size = new Size(862, 4);
            panel2.TabIndex = 2;
            // 
            // Map
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.LightGray;
            Controls.Add(MapCanvas);
            Controls.Add(panel2);
            Controls.Add(panel1);
            Name = "Map";
            Size = new Size(862, 356);
            ((System.ComponentModel.ISupportInitialize)MapCanvas).EndInit();
            panel1.ResumeLayout(false);
            panel1.PerformLayout();
            ResumeLayout(false);
        }

        #endregion

        private PictureBox MapCanvas;
        private Panel panel1;
        private Panel panel2;
        private Label FieldNameLabel;
        private Label HeadingLabel;
        private Label SpeedLabel;
        private Label FrontLoadLabel;
        private Label RearLoadLabel;
        private Label FrontBladeHeightLabel;
        private Label RearBladeHeightLabel;
    }
}
