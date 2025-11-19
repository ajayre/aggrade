namespace AgGrade.Controls
{
    partial class VehicleSettingsEditor
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
            label1 = new Label();
            numericInput1 = new NumericInput();
            label3 = new Label();
            label2 = new Label();
            numericInput2 = new NumericInput();
            label4 = new Label();
            label5 = new Label();
            numericInput3 = new NumericInput();
            label6 = new Label();
            SuspendLayout();
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Font = new Font("Segoe UI", 14F);
            label1.Location = new Point(80, 21);
            label1.Name = "label1";
            label1.Size = new Size(148, 25);
            label1.TabIndex = 0;
            label1.Text = "Antenna Height:";
            // 
            // numericInput1
            // 
            numericInput1.Location = new Point(234, 12);
            numericInput1.Name = "numericInput1";
            numericInput1.Size = new Size(167, 43);
            numericInput1.TabIndex = 3;
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Font = new Font("Segoe UI", 14F);
            label3.Location = new Point(407, 21);
            label3.Name = "label3";
            label3.Size = new Size(37, 25);
            label3.TabIndex = 4;
            label3.Text = "cm";
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Font = new Font("Segoe UI", 14F);
            label2.Location = new Point(407, 68);
            label2.Name = "label2";
            label2.Size = new Size(37, 25);
            label2.TabIndex = 7;
            label2.Text = "cm";
            // 
            // numericInput2
            // 
            numericInput2.Location = new Point(234, 59);
            numericInput2.Name = "numericInput2";
            numericInput2.Size = new Size(167, 41);
            numericInput2.TabIndex = 6;
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Font = new Font("Segoe UI", 14F);
            label4.Location = new Point(22, 68);
            label4.Name = "label4";
            label4.Size = new Size(206, 25);
            label4.TabIndex = 5;
            label4.Text = "Antenna Left of Center:";
            // 
            // label5
            // 
            label5.AutoSize = true;
            label5.Font = new Font("Segoe UI", 14F);
            label5.Location = new Point(407, 115);
            label5.Name = "label5";
            label5.Size = new Size(37, 25);
            label5.TabIndex = 10;
            label5.Text = "cm";
            // 
            // numericInput3
            // 
            numericInput3.Location = new Point(234, 106);
            numericInput3.Name = "numericInput3";
            numericInput3.Size = new Size(167, 41);
            numericInput3.TabIndex = 9;
            // 
            // label6
            // 
            label6.AutoSize = true;
            label6.Font = new Font("Segoe UI", 14F);
            label6.Location = new Point(4, 115);
            label6.Name = "label6";
            label6.Size = new Size(224, 25);
            label6.TabIndex = 8;
            label6.Text = "Antenna Forward of Axle:";
            // 
            // VehicleSettingsEditor
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            Controls.Add(label5);
            Controls.Add(numericInput3);
            Controls.Add(label6);
            Controls.Add(label2);
            Controls.Add(numericInput2);
            Controls.Add(label4);
            Controls.Add(label3);
            Controls.Add(numericInput1);
            Controls.Add(label1);
            Name = "VehicleSettingsEditor";
            Size = new Size(757, 358);
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Label label1;
        private NumericInput numericInput1;
        private Label label3;
        private Label label2;
        private NumericInput numericInput2;
        private Label label4;
        private Label label5;
        private NumericInput numericInput3;
        private Label label6;
    }
}
