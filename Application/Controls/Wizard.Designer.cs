namespace AgGrade.Controls
{
    partial class Wizard
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
            NextBtn = new Button();
            BackBtn = new Button();
            panel1 = new Panel();
            WizardName = new Label();
            WizardBody = new Panel();
            panel1.SuspendLayout();
            SuspendLayout();
            // 
            // NextBtn
            // 
            NextBtn.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            NextBtn.Font = new Font("Segoe UI", 18F);
            NextBtn.Image = Properties.Resources.next_48px;
            NextBtn.ImageAlign = ContentAlignment.MiddleRight;
            NextBtn.Location = new Point(635, 3);
            NextBtn.Name = "NextBtn";
            NextBtn.Size = new Size(123, 60);
            NextBtn.TabIndex = 10;
            NextBtn.Text = "Next";
            NextBtn.TextAlign = ContentAlignment.MiddleLeft;
            NextBtn.UseVisualStyleBackColor = true;
            NextBtn.Click += NextBtn_Click;
            // 
            // BackBtn
            // 
            BackBtn.Font = new Font("Segoe UI", 18F);
            BackBtn.Image = Properties.Resources.back_48px;
            BackBtn.ImageAlign = ContentAlignment.MiddleLeft;
            BackBtn.Location = new Point(3, 3);
            BackBtn.Name = "BackBtn";
            BackBtn.Size = new Size(123, 60);
            BackBtn.TabIndex = 11;
            BackBtn.Text = "Back";
            BackBtn.TextAlign = ContentAlignment.MiddleRight;
            BackBtn.UseVisualStyleBackColor = true;
            BackBtn.Click += BackBtn_Click;
            // 
            // panel1
            // 
            panel1.BackColor = Color.FromArgb(224, 224, 224);
            panel1.Controls.Add(WizardName);
            panel1.Controls.Add(BackBtn);
            panel1.Controls.Add(NextBtn);
            panel1.Dock = DockStyle.Top;
            panel1.Location = new Point(0, 0);
            panel1.Name = "panel1";
            panel1.Size = new Size(762, 66);
            panel1.TabIndex = 12;
            // 
            // WizardName
            // 
            WizardName.AutoSize = true;
            WizardName.Font = new Font("Segoe UI", 18F, FontStyle.Bold);
            WizardName.Location = new Point(132, 17);
            WizardName.Name = "WizardName";
            WizardName.Size = new Size(169, 32);
            WizardName.TabIndex = 12;
            WizardName.Text = "Wizard Name";
            // 
            // WizardBody
            // 
            WizardBody.Dock = DockStyle.Fill;
            WizardBody.Location = new Point(0, 66);
            WizardBody.Name = "WizardBody";
            WizardBody.Size = new Size(762, 457);
            WizardBody.TabIndex = 13;
            // 
            // Wizard
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            Controls.Add(WizardBody);
            Controls.Add(panel1);
            Name = "Wizard";
            Size = new Size(762, 523);
            panel1.ResumeLayout(false);
            panel1.PerformLayout();
            ResumeLayout(false);
        }

        #endregion
        private Button NextBtn;
        private Button BackBtn;
        private Panel panel1;
        private Label WizardName;
        private Panel WizardBody;
    }
}
