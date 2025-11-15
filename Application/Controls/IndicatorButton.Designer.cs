namespace AgGrade.Controls
{
    partial class IndicatorButton
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(IndicatorButton));
            IndicatorPanel = new Panel();
            button1 = new Button();
            SuspendLayout();
            // 
            // IndicatorPanel
            // 
            IndicatorPanel.BackColor = Color.Red;
            IndicatorPanel.Dock = DockStyle.Bottom;
            IndicatorPanel.Location = new Point(0, 50);
            IndicatorPanel.Name = "IndicatorPanel";
            IndicatorPanel.Size = new Size(60, 10);
            IndicatorPanel.TabIndex = 0;
            // 
            // button1
            // 
            button1.Dock = DockStyle.Fill;
            button1.Image = (Image)resources.GetObject("button1.Image");
            button1.Location = new Point(0, 0);
            button1.Name = "button1";
            button1.Size = new Size(60, 50);
            button1.TabIndex = 1;
            button1.UseVisualStyleBackColor = true;
            // 
            // IndicatorButton
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            Controls.Add(button1);
            Controls.Add(IndicatorPanel);
            Name = "IndicatorButton";
            Size = new Size(60, 60);
            ResumeLayout(false);
        }

        #endregion

        private Panel IndicatorPanel;
        private Button button1;
    }
}
