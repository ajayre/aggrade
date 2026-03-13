namespace AgGrade.Controls
{
    partial class ButtonPanel
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
            panel1 = new Panel();
            Caption = new Label();
            Icon = new Panel();
            panel1.SuspendLayout();
            SuspendLayout();
            // 
            // panel1
            // 
            panel1.Controls.Add(Caption);
            panel1.Controls.Add(Icon);
            panel1.Dock = DockStyle.Top;
            panel1.Location = new Point(0, 0);
            panel1.Name = "panel1";
            panel1.Size = new Size(686, 64);
            panel1.TabIndex = 3;
            // 
            // Caption
            // 
            Caption.BackColor = Color.OldLace;
            Caption.Dock = DockStyle.Fill;
            Caption.Font = new Font("Segoe UI", 18F, FontStyle.Bold);
            Caption.Location = new Point(64, 0);
            Caption.Name = "Caption";
            Caption.Padding = new Padding(5, 0, 0, 0);
            Caption.Size = new Size(622, 64);
            Caption.TabIndex = 0;
            Caption.Text = "Caption";
            Caption.TextAlign = ContentAlignment.MiddleLeft;
            Caption.Click += Caption_Click;
            // 
            // Icon
            // 
            Icon.BackColor = Color.OldLace;
            Icon.BackgroundImage = Properties.Resources.createnewfield_48px;
            Icon.BackgroundImageLayout = ImageLayout.Center;
            Icon.Dock = DockStyle.Left;
            Icon.Location = new Point(0, 0);
            Icon.Name = "Icon";
            Icon.Size = new Size(64, 64);
            Icon.TabIndex = 2;
            Icon.Click += Icon_Click;
            // 
            // ButtonPanel
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            Controls.Add(panel1);
            Name = "ButtonPanel";
            Size = new Size(686, 64);
            panel1.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion

        private Panel panel1;
        private Label Caption;
        private Panel Icon;
    }
}
