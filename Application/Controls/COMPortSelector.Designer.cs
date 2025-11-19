namespace AgGrade.Controls
{
    partial class COMPortSelector
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
            Selector = new ComboBox();
            SuspendLayout();
            // 
            // Selector
            // 
            Selector.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            Selector.DropDownStyle = ComboBoxStyle.DropDownList;
            Selector.Font = new Font("Segoe UI", 14F);
            Selector.FormattingEnabled = true;
            Selector.Location = new Point(0, 0);
            Selector.Name = "Selector";
            Selector.Size = new Size(233, 33);
            Selector.TabIndex = 0;
            // 
            // COMPortSelector
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            Controls.Add(Selector);
            Name = "COMPortSelector";
            Size = new Size(233, 33);
            ResumeLayout(false);
        }

        #endregion

        private ComboBox Selector;
    }
}
