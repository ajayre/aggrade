namespace AgGrade
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            panel3 = new Panel();
            panel1 = new Panel();
            EditEquipmentBtn = new Button();
            ContentPanel = new Panel();
            StatusPanel = new Panel();
            panel1.SuspendLayout();
            ContentPanel.SuspendLayout();
            SuspendLayout();
            // 
            // panel3
            // 
            panel3.Dock = DockStyle.Left;
            panel3.Location = new Point(0, 0);
            panel3.Name = "panel3";
            panel3.Size = new Size(76, 495);
            panel3.TabIndex = 2;
            // 
            // panel1
            // 
            panel1.Controls.Add(EditEquipmentBtn);
            panel1.Dock = DockStyle.Right;
            panel1.Location = new Point(669, 0);
            panel1.Name = "panel1";
            panel1.Size = new Size(76, 495);
            panel1.TabIndex = 3;
            // 
            // EditEquipmentBtn
            // 
            EditEquipmentBtn.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            EditEquipmentBtn.Image = (Image)resources.GetObject("EditEquipmentBtn.Image");
            EditEquipmentBtn.Location = new Point(8, 429);
            EditEquipmentBtn.Name = "EditEquipmentBtn";
            EditEquipmentBtn.Size = new Size(60, 60);
            EditEquipmentBtn.TabIndex = 0;
            EditEquipmentBtn.UseVisualStyleBackColor = true;
            EditEquipmentBtn.Click += EditEquipmentBtn_Click;
            // 
            // ContentPanel
            // 
            ContentPanel.BackColor = SystemColors.ControlDark;
            ContentPanel.Controls.Add(StatusPanel);
            ContentPanel.Dock = DockStyle.Fill;
            ContentPanel.Location = new Point(76, 0);
            ContentPanel.Name = "ContentPanel";
            ContentPanel.Size = new Size(593, 495);
            ContentPanel.TabIndex = 4;
            // 
            // StatusPanel
            // 
            StatusPanel.Dock = DockStyle.Bottom;
            StatusPanel.Location = new Point(0, 463);
            StatusPanel.Name = "StatusPanel";
            StatusPanel.Size = new Size(593, 32);
            StatusPanel.TabIndex = 0;
            // 
            // MainForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.Green;
            ClientSize = new Size(745, 495);
            Controls.Add(ContentPanel);
            Controls.Add(panel1);
            Controls.Add(panel3);
            Name = "MainForm";
            Text = "AgGrade";
            panel1.ResumeLayout(false);
            ContentPanel.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion

        private Panel panel3;
        private Panel panel1;
        private Panel ContentPanel;
        private Panel StatusPanel;
        private Button EditEquipmentBtn;
    }
}
