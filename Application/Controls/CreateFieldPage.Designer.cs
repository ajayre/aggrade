namespace AgGrade.Controls
{
    partial class CreateFieldPage
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
            CreateFieldBtn = new Button();
            panel1 = new Panel();
            ErrorMessage = new Label();
            SurveyChooser = new ComboBox();
            label2 = new Label();
            MainSlopeDirectionLabel = new Label();
            MainSlopeDirectionUnitsLabel = new Label();
            MainSlopeLabel = new Label();
            MainSlopeUnitsLabel = new Label();
            CrossSlopeLabel = new Label();
            CrossSlopeUnitsLabel = new Label();
            CutFillRatioLabel = new Label();
            ImportToFieldLabel = new Label();
            ImportToFieldUnitsLabel = new Label();
            ExportFromFieldLabel = new Label();
            ExportFromFieldUnitsLabel = new Label();
            MainSlopeDirection = new NumericInput();
            ImportToField = new NumericInput();
            ExportFromField = new NumericInput();
            MainSlope = new NumericInputF();
            CrossSlope = new NumericInputF();
            CutFillRatio = new NumericInputF();
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
            sectionTitle1.TitleText = "Create Field";
            // 
            // CreateFieldBtn
            // 
            CreateFieldBtn.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            CreateFieldBtn.Font = new Font("Segoe UI", 18F);
            CreateFieldBtn.Image = Properties.Resources.createnewfield_48px;
            CreateFieldBtn.ImageAlign = ContentAlignment.MiddleLeft;
            CreateFieldBtn.Location = new Point(765, 281);
            CreateFieldBtn.Name = "CreateFieldBtn";
            CreateFieldBtn.Size = new Size(148, 60);
            CreateFieldBtn.TabIndex = 29;
            CreateFieldBtn.Text = "Create";
            CreateFieldBtn.TextAlign = ContentAlignment.MiddleRight;
            CreateFieldBtn.UseVisualStyleBackColor = true;
            CreateFieldBtn.Click += CreateFieldBtn_Click;
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
            // MainSlopeDirectionLabel
            // 
            MainSlopeDirectionLabel.AutoSize = true;
            MainSlopeDirectionLabel.Font = new Font("Segoe UI", 14F);
            MainSlopeDirectionLabel.Location = new Point(28, 101);
            MainSlopeDirectionLabel.Name = "MainSlopeDirectionLabel";
            MainSlopeDirectionLabel.Size = new Size(193, 25);
            MainSlopeDirectionLabel.TabIndex = 17;
            MainSlopeDirectionLabel.Text = "Main Slope Direction:";
            // 
            // MainSlopeDirectionUnitsLabel
            // 
            MainSlopeDirectionUnitsLabel.AutoSize = true;
            MainSlopeDirectionUnitsLabel.Font = new Font("Segoe UI", 14F);
            MainSlopeDirectionUnitsLabel.Location = new Point(400, 101);
            MainSlopeDirectionUnitsLabel.Name = "MainSlopeDirectionUnitsLabel";
            MainSlopeDirectionUnitsLabel.Size = new Size(79, 25);
            MainSlopeDirectionUnitsLabel.TabIndex = 19;
            MainSlopeDirectionUnitsLabel.Text = "degrees";
            // 
            // MainSlopeLabel
            // 
            MainSlopeLabel.AutoSize = true;
            MainSlopeLabel.Font = new Font("Segoe UI", 14F);
            MainSlopeLabel.Location = new Point(112, 148);
            MainSlopeLabel.Name = "MainSlopeLabel";
            MainSlopeLabel.Size = new Size(109, 25);
            MainSlopeLabel.TabIndex = 20;
            MainSlopeLabel.Text = "Main slope:";
            // 
            // MainSlopeUnitsLabel
            // 
            MainSlopeUnitsLabel.AutoSize = true;
            MainSlopeUnitsLabel.Font = new Font("Segoe UI", 14F);
            MainSlopeUnitsLabel.Location = new Point(400, 148);
            MainSlopeUnitsLabel.Name = "MainSlopeUnitsLabel";
            MainSlopeUnitsLabel.Size = new Size(28, 25);
            MainSlopeUnitsLabel.TabIndex = 22;
            MainSlopeUnitsLabel.Text = "%";
            // 
            // CrossSlopeLabel
            // 
            CrossSlopeLabel.AutoSize = true;
            CrossSlopeLabel.Font = new Font("Segoe UI", 14F);
            CrossSlopeLabel.Location = new Point(501, 147);
            CrossSlopeLabel.Name = "CrossSlopeLabel";
            CrossSlopeLabel.Size = new Size(112, 25);
            CrossSlopeLabel.TabIndex = 23;
            CrossSlopeLabel.Text = "Cross slope:";
            // 
            // CrossSlopeUnitsLabel
            // 
            CrossSlopeUnitsLabel.AutoSize = true;
            CrossSlopeUnitsLabel.Font = new Font("Segoe UI", 14F);
            CrossSlopeUnitsLabel.Location = new Point(792, 147);
            CrossSlopeUnitsLabel.Name = "CrossSlopeUnitsLabel";
            CrossSlopeUnitsLabel.Size = new Size(28, 25);
            CrossSlopeUnitsLabel.TabIndex = 25;
            CrossSlopeUnitsLabel.Text = "%";
            // 
            // CutFillRatioLabel
            // 
            CutFillRatioLabel.AutoSize = true;
            CutFillRatioLabel.Font = new Font("Segoe UI", 14F);
            CutFillRatioLabel.Location = new Point(106, 195);
            CutFillRatioLabel.Name = "CutFillRatioLabel";
            CutFillRatioLabel.Size = new Size(117, 25);
            CutFillRatioLabel.TabIndex = 26;
            CutFillRatioLabel.Text = "Cut/fill ratio:";
            // 
            // ImportToFieldLabel
            // 
            ImportToFieldLabel.AutoSize = true;
            ImportToFieldLabel.Font = new Font("Segoe UI", 14F);
            ImportToFieldLabel.Location = new Point(87, 242);
            ImportToFieldLabel.Name = "ImportToFieldLabel";
            ImportToFieldLabel.Size = new Size(136, 25);
            ImportToFieldLabel.TabIndex = 28;
            ImportToFieldLabel.Text = "Import to field:";
            // 
            // ImportToFieldUnitsLabel
            // 
            ImportToFieldUnitsLabel.AutoSize = true;
            ImportToFieldUnitsLabel.Font = new Font("Segoe UI", 14F);
            ImportToFieldUnitsLabel.Location = new Point(402, 242);
            ImportToFieldUnitsLabel.Name = "ImportToFieldUnitsLabel";
            ImportToFieldUnitsLabel.Size = new Size(35, 25);
            ImportToFieldUnitsLabel.TabIndex = 31;
            ImportToFieldUnitsLabel.Text = "CY";
            // 
            // ExportFromFieldLabel
            // 
            ExportFromFieldLabel.AutoSize = true;
            ExportFromFieldLabel.Font = new Font("Segoe UI", 14F);
            ExportFromFieldLabel.Location = new Point(456, 242);
            ExportFromFieldLabel.Name = "ExportFromFieldLabel";
            ExportFromFieldLabel.Size = new Size(157, 25);
            ExportFromFieldLabel.TabIndex = 32;
            ExportFromFieldLabel.Text = "Export from field:";
            // 
            // ExportFromFieldUnitsLabel
            // 
            ExportFromFieldUnitsLabel.AutoSize = true;
            ExportFromFieldUnitsLabel.Font = new Font("Segoe UI", 14F);
            ExportFromFieldUnitsLabel.Location = new Point(792, 242);
            ExportFromFieldUnitsLabel.Name = "ExportFromFieldUnitsLabel";
            ExportFromFieldUnitsLabel.Size = new Size(35, 25);
            ExportFromFieldUnitsLabel.TabIndex = 34;
            ExportFromFieldUnitsLabel.Text = "CY";
            // 
            // MainSlopeDirection
            // 
            MainSlopeDirection.Location = new Point(227, 93);
            MainSlopeDirection.Name = "MainSlopeDirection";
            MainSlopeDirection.Size = new Size(167, 41);
            MainSlopeDirection.TabIndex = 35;
            MainSlopeDirection.Unsigned = false;
            MainSlopeDirection.Value = 0;
            // 
            // ImportToField
            // 
            ImportToField.Location = new Point(229, 234);
            ImportToField.Name = "ImportToField";
            ImportToField.Size = new Size(167, 41);
            ImportToField.TabIndex = 36;
            ImportToField.Unsigned = false;
            ImportToField.Value = 0;
            // 
            // ExportFromField
            // 
            ExportFromField.Location = new Point(619, 234);
            ExportFromField.Name = "ExportFromField";
            ExportFromField.Size = new Size(167, 41);
            ExportFromField.TabIndex = 37;
            ExportFromField.Unsigned = false;
            ExportFromField.Value = 0;
            // 
            // MainSlope
            // 
            MainSlope.ButtonChangeAmount = 0.1D;
            MainSlope.DecimalPlaces = 1U;
            MainSlope.Location = new Point(227, 140);
            MainSlope.Maximum = 20D;
            MainSlope.Minimum = 0D;
            MainSlope.Name = "MainSlope";
            MainSlope.Size = new Size(167, 41);
            MainSlope.TabIndex = 38;
            MainSlope.Value = 0D;
            // 
            // CrossSlope
            // 
            CrossSlope.ButtonChangeAmount = 0.1D;
            CrossSlope.DecimalPlaces = 1U;
            CrossSlope.Location = new Point(619, 140);
            CrossSlope.Maximum = 20D;
            CrossSlope.Minimum = 0D;
            CrossSlope.Name = "CrossSlope";
            CrossSlope.Size = new Size(167, 41);
            CrossSlope.TabIndex = 39;
            CrossSlope.Value = 0D;
            // 
            // CutFillRatio
            // 
            CutFillRatio.ButtonChangeAmount = 0.1D;
            CutFillRatio.DecimalPlaces = 1U;
            CutFillRatio.Location = new Point(229, 187);
            CutFillRatio.Maximum = 2D;
            CutFillRatio.Minimum = 0.5D;
            CutFillRatio.Name = "CutFillRatio";
            CutFillRatio.Size = new Size(167, 41);
            CutFillRatio.TabIndex = 40;
            CutFillRatio.Value = 0.5D;
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
            // CreateFieldPage
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            Controls.Add(ProgressOutput);
            Controls.Add(CutFillRatio);
            Controls.Add(CrossSlope);
            Controls.Add(MainSlope);
            Controls.Add(ExportFromField);
            Controls.Add(ImportToField);
            Controls.Add(MainSlopeDirection);
            Controls.Add(ExportFromFieldUnitsLabel);
            Controls.Add(ExportFromFieldLabel);
            Controls.Add(ImportToFieldUnitsLabel);
            Controls.Add(ImportToFieldLabel);
            Controls.Add(CutFillRatioLabel);
            Controls.Add(CrossSlopeUnitsLabel);
            Controls.Add(CrossSlopeLabel);
            Controls.Add(MainSlopeUnitsLabel);
            Controls.Add(MainSlopeLabel);
            Controls.Add(MainSlopeDirectionUnitsLabel);
            Controls.Add(MainSlopeDirectionLabel);
            Controls.Add(label2);
            Controls.Add(SurveyChooser);
            Controls.Add(panel1);
            Controls.Add(CreateFieldBtn);
            Controls.Add(sectionTitle1);
            Name = "CreateFieldPage";
            Size = new Size(916, 553);
            panel1.ResumeLayout(false);
            panel1.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private SectionTitle sectionTitle1;
        private Button CreateFieldBtn;
        private Panel panel1;
        private Label ErrorMessage;
        private ComboBox SurveyChooser;
        private Label label2;
        private Label MainSlopeDirectionLabel;
        private Label MainSlopeDirectionUnitsLabel;
        private Label MainSlopeLabel;
        private Label MainSlopeUnitsLabel;
        private Label CrossSlopeLabel;
        private Label CrossSlopeUnitsLabel;
        private Label CutFillRatioLabel;
        private Label ImportToFieldLabel;
        private Label ImportToFieldUnitsLabel;
        private Label ExportFromFieldLabel;
        private Label ExportFromFieldUnitsLabel;
        private NumericInput MainSlopeDirection;
        private NumericInput ImportToField;
        private NumericInput ExportFromField;
        private NumericInputF MainSlope;
        private NumericInputF CrossSlope;
        private NumericInputF CutFillRatio;
        private TextBox ProgressOutput;
    }
}
