namespace AgGrade.Controls
{
    partial class CreateSurveyPage
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
            label1 = new Label();
            NameInput = new TextBox();
            CreateSurveyBtn = new Button();
            panel1 = new Panel();
            ErrorMessage = new Label();
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
            sectionTitle1.TitleText = "Create Field Survey";
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Font = new Font("Segoe UI", 16F);
            label1.Location = new Point(17, 57);
            label1.Name = "label1";
            label1.Size = new Size(148, 30);
            label1.TabIndex = 1;
            label1.Text = "Survey Name:";
            // 
            // NameInput
            // 
            NameInput.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            NameInput.Font = new Font("Segoe UI", 16F);
            NameInput.Location = new Point(171, 54);
            NameInput.Name = "NameInput";
            NameInput.Size = new Size(742, 36);
            NameInput.TabIndex = 2;
            // 
            // CreateSurveyBtn
            // 
            CreateSurveyBtn.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            CreateSurveyBtn.Font = new Font("Segoe UI", 18F);
            CreateSurveyBtn.Image = Properties.Resources.createnewfield_48px;
            CreateSurveyBtn.ImageAlign = ContentAlignment.MiddleLeft;
            CreateSurveyBtn.Location = new Point(770, 96);
            CreateSurveyBtn.Name = "CreateSurveyBtn";
            CreateSurveyBtn.Size = new Size(143, 60);
            CreateSurveyBtn.TabIndex = 13;
            CreateSurveyBtn.Text = "Create";
            CreateSurveyBtn.TextAlign = ContentAlignment.MiddleRight;
            CreateSurveyBtn.UseVisualStyleBackColor = true;
            CreateSurveyBtn.Click += CreateSurveyBtn_Click;
            // 
            // panel1
            // 
            panel1.BackColor = Color.FromArgb(224, 224, 224);
            panel1.Controls.Add(ErrorMessage);
            panel1.Dock = DockStyle.Bottom;
            panel1.Location = new Point(0, 432);
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
            // CreateSurveyPage
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            Controls.Add(panel1);
            Controls.Add(CreateSurveyBtn);
            Controls.Add(NameInput);
            Controls.Add(label1);
            Controls.Add(sectionTitle1);
            Name = "CreateSurveyPage";
            Size = new Size(916, 476);
            panel1.ResumeLayout(false);
            panel1.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private SectionTitle sectionTitle1;
        private Label label1;
        private TextBox NameInput;
        private Button CreateSurveyBtn;
        private Panel panel1;
        private Label ErrorMessage;
    }
}
