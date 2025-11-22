namespace AgGrade.Controls
{
    partial class NumericInput
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(NumericInput));
            textBox1 = new TextBox();
            UpBtn = new Button();
            DownBtn = new Button();
            SuspendLayout();
            // 
            // textBox1
            // 
            textBox1.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            textBox1.Font = new Font("Segoe UI", 14F);
            textBox1.Location = new Point(3, 4);
            textBox1.Name = "textBox1";
            textBox1.PlaceholderText = "0";
            textBox1.Size = new Size(146, 32);
            textBox1.TabIndex = 0;
            textBox1.TextAlign = HorizontalAlignment.Right;
            // 
            // UpBtn
            // 
            UpBtn.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            UpBtn.Image = (Image)resources.GetObject("UpBtn.Image");
            UpBtn.Location = new Point(155, 1);
            UpBtn.Name = "UpBtn";
            UpBtn.Size = new Size(41, 39);
            UpBtn.TabIndex = 1;
            UpBtn.UseVisualStyleBackColor = true;
            // 
            // DownBtn
            // 
            DownBtn.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            DownBtn.Image = (Image)resources.GetObject("DownBtn.Image");
            DownBtn.Location = new Point(198, 1);
            DownBtn.Name = "DownBtn";
            DownBtn.Size = new Size(41, 39);
            DownBtn.TabIndex = 2;
            DownBtn.UseVisualStyleBackColor = true;
            // 
            // NumericInput
            // 
            AutoScaleMode = AutoScaleMode.Inherit;
            Controls.Add(textBox1);
            Controls.Add(DownBtn);
            Controls.Add(UpBtn);
            Name = "NumericInput";
            Size = new Size(240, 41);
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private TextBox textBox1;
        private Button UpBtn;
        private Button DownBtn;
    }
}
