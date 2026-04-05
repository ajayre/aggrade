namespace AgGrade.Controls
{
    partial class ImportFieldPage
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
            sectionTitle1 = new SectionTitle();
            ImportFieldBtn = new Button();
            panel1 = new Panel();
            ErrorMessage = new Label();
            SurveyChooser = new ComboBox();
            label2 = new Label();
            ProgressOutput = new TextBox();
            panel1.SuspendLayout();
            SuspendLayout();
            // 
            // sectionTitle1
            // 
            sectionTitle1.Dock = DockStyle.Top;
            sectionTitle1.Location = new Point(0, 0);
            sectionTitle1.Name = "sectionTitle1";
            sectionTitle1.Size = new Size(916, 48);
            sectionTitle1.TabIndex = 0;
            sectionTitle1.TitleText = "Import Field";
            // 
            // ImportFieldBtn
            // 
            ImportFieldBtn.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            ImportFieldBtn.Font = new Font("Segoe UI", 18F);
            ImportFieldBtn.Image = Properties.Resources.import_48px;
            ImportFieldBtn.ImageAlign = ContentAlignment.MiddleLeft;
            ImportFieldBtn.Location = new Point(765, 281);
            ImportFieldBtn.Name = "ImportFieldBtn";
            ImportFieldBtn.Size = new Size(148, 60);
            ImportFieldBtn.TabIndex = 29;
            ImportFieldBtn.Text = "Import";
            ImportFieldBtn.TextAlign = ContentAlignment.MiddleRight;
            ImportFieldBtn.UseVisualStyleBackColor = true;
            ImportFieldBtn.Click += ImportFieldBtn_Click;
            // 
            // panel1
            // 
            panel1.BackColor = Color.FromArgb(224, 224, 224);
            panel1.Controls.Add(ErrorMessage);
            panel1.Dock = DockStyle.Bottom;
            panel1.Location = new Point(0, 509);
            panel1.Name = "panel1";
            panel1.Size = new Size(916, 44);
            panel1.TabIndex = 14;
            // 
            // ErrorMessage
            // 
            ErrorMessage.AutoSize = true;
            ErrorMessage.Font = new Font("Segoe UI", 14F, FontStyle.Bold);
            ErrorMessage.ForeColor = Color.Red;
            ErrorMessage.Location = new Point(10, 9);
            ErrorMessage.Name = "ErrorMessage";
            ErrorMessage.Size = new Size(139, 25);
            ErrorMessage.TabIndex = 0;
            ErrorMessage.Text = "Error Message";
            // 
            // SurveyChooser
            // 
            SurveyChooser.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            SurveyChooser.DropDownStyle = ComboBoxStyle.DropDownList;
            SurveyChooser.Font = new Font("Segoe UI", 14F);
            SurveyChooser.FormattingEnabled = true;
            SurveyChooser.Location = new Point(229, 54);
            SurveyChooser.Name = "SurveyChooser";
            SurveyChooser.Size = new Size(684, 33);
            SurveyChooser.TabIndex = 15;
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Font = new Font("Segoe UI", 14F);
            label2.Location = new Point(151, 57);
            label2.Name = "label2";
            label2.Size = new Size(72, 25);
            label2.TabIndex = 16;
            label2.Text = "Survey:";
            // 
            // ProgressOutput
            // 
            ProgressOutput.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            ProgressOutput.Font = new Font("Segoe UI", 16F);
            ProgressOutput.Location = new Point(3, 347);
            ProgressOutput.Multiline = true;
            ProgressOutput.Name = "ProgressOutput";
            ProgressOutput.Size = new Size(910, 156);
            ProgressOutput.TabIndex = 41;
            // 
            // ImportFieldPage
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            Controls.Add(ProgressOutput);
            Controls.Add(label2);
            Controls.Add(SurveyChooser);
            Controls.Add(panel1);
            Controls.Add(ImportFieldBtn);
            Controls.Add(sectionTitle1);
            Name = "ImportFieldPage";
            Size = new Size(916, 553);
            panel1.ResumeLayout(false);
            panel1.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private SectionTitle sectionTitle1;
        private Button ImportFieldBtn;
        private Panel panel1;
        private Label ErrorMessage;
        private ComboBox SurveyChooser;
        private Label label2;
        private TextBox ProgressOutput;
    }
}
