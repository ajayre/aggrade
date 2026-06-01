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
            ValueInput = new TextBox();
            UpBtn = new Button();
            DownBtn = new Button();
            ClearBtn = new Button();
            SuspendLayout();
            // 
            // ValueInput
            // 
            ValueInput.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            ValueInput.Font = new Font("Segoe UI", 14F);
            ValueInput.Location = new Point(3, 4);
            ValueInput.Name = "ValueInput";
            ValueInput.PlaceholderText = "0";
            ValueInput.Size = new Size(116, 32);
            ValueInput.TabIndex = 0;
            ValueInput.TextAlign = HorizontalAlignment.Right;
            ValueInput.TextChanged += ValueInput_TextChanged;
            // 
            // UpBtn
            // 
            UpBtn.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            UpBtn.Image = (Image)resources.GetObject("UpBtn.Image");
            UpBtn.Location = new Point(125, 1);
            UpBtn.Name = "UpBtn";
            UpBtn.Size = new Size(41, 39);
            UpBtn.TabIndex = 1;
            UpBtn.UseVisualStyleBackColor = true;
            UpBtn.Click += UpBtn_Click;
            // 
            // DownBtn
            // 
            DownBtn.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            DownBtn.Image = (Image)resources.GetObject("DownBtn.Image");
            DownBtn.Location = new Point(168, 1);
            DownBtn.Name = "DownBtn";
            DownBtn.Size = new Size(41, 39);
            DownBtn.TabIndex = 2;
            DownBtn.UseVisualStyleBackColor = true;
            DownBtn.Click += DownBtn_Click;
            // 
            // ClearBtn
            // 
            ClearBtn.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            ClearBtn.Image = Properties.Resources.clear_32px;
            ClearBtn.Location = new Point(211, 1);
            ClearBtn.Name = "ClearBtn";
            ClearBtn.Size = new Size(41, 39);
            ClearBtn.TabIndex = 3;
            ClearBtn.UseVisualStyleBackColor = true;
            ClearBtn.Click += ClearBtn_Click;
            // 
            // NumericInput
            // 
            AutoScaleMode = AutoScaleMode.Inherit;
            Controls.Add(ClearBtn);
            Controls.Add(ValueInput);
            Controls.Add(DownBtn);
            Controls.Add(UpBtn);
            Name = "NumericInput";
            Size = new Size(253, 41);
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private TextBox ValueInput;
        private Button UpBtn;
        private Button DownBtn;
        private Button ClearBtn;
    }
}
