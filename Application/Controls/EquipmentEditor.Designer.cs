namespace AgGrade.Controls
{
    partial class EquipmentEditor
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
            button1 = new Button();
            button2 = new Button();
            button3 = new Button();
            SuspendLayout();
            // 
            // button1
            // 
            button1.BackColor = Color.Green;
            button1.Font = new Font("Segoe UI", 14F, FontStyle.Bold);
            button1.ForeColor = SystemColors.Control;
            button1.Location = new Point(3, 3);
            button1.Name = "button1";
            button1.Size = new Size(150, 45);
            button1.TabIndex = 0;
            button1.Text = "VEHICLE";
            button1.UseVisualStyleBackColor = false;
            // 
            // button2
            // 
            button2.BackColor = Color.Green;
            button2.Font = new Font("Segoe UI", 14F, FontStyle.Bold);
            button2.ForeColor = SystemColors.Control;
            button2.Location = new Point(159, 3);
            button2.Name = "button2";
            button2.Size = new Size(150, 45);
            button2.TabIndex = 1;
            button2.Text = "FRONT PAN";
            button2.UseVisualStyleBackColor = false;
            // 
            // button3
            // 
            button3.BackColor = Color.Green;
            button3.Font = new Font("Segoe UI", 14F, FontStyle.Bold);
            button3.ForeColor = SystemColors.Control;
            button3.Location = new Point(315, 3);
            button3.Name = "button3";
            button3.Size = new Size(150, 45);
            button3.TabIndex = 2;
            button3.Text = "REAR PAN";
            button3.UseVisualStyleBackColor = false;
            // 
            // EquipmentEditor
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.FromArgb(224, 224, 224);
            Controls.Add(button3);
            Controls.Add(button2);
            Controls.Add(button1);
            Name = "EquipmentEditor";
            Size = new Size(744, 459);
            ResumeLayout(false);
        }

        #endregion

        private Button button1;
        private Button button2;
        private Button button3;
    }
}
