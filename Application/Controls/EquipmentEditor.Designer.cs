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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(EquipmentEditor));
            sectionTitle1 = new SectionTitle();
            groupBox1 = new GroupBox();
            label16 = new Label();
            label15 = new Label();
            label13 = new Label();
            TractorWidth = new NumericInput();
            label14 = new Label();
            label11 = new Label();
            TractorTurningCircle = new NumericInput();
            label12 = new Label();
            label5 = new Label();
            TractorAntennaForwardOffset = new NumericInput();
            label6 = new Label();
            label2 = new Label();
            TractorAntennaLeftOffset = new NumericInput();
            label4 = new Label();
            label3 = new Label();
            TractorAntennaHeight = new NumericInput();
            label1 = new Label();
            groupBox2 = new GroupBox();
            label7 = new Label();
            FrontPanMaxCutDepth = new NumericInput();
            FrontPanMaxCutDepthLabel = new Label();
            FrontPanEquippedLabel = new Label();
            FrontPanEquipped = new ComboBox();
            FrontPanRaiseUnitsLabel = new Label();
            FrontPanRaiseHeight = new NumericInput();
            FrontPanEndofCutting = new ComboBox();
            FrontPanEndofCuttingLabel = new Label();
            FrontPanWidthUnitsLabel = new Label();
            FrontPanWidth = new NumericInput();
            FrontPanWidthLabel = new Label();
            FrontPanAntennaHeightUnitsLabel = new Label();
            FrontPanAntennaHeight = new NumericInput();
            FrontPanAntennaHeightLabel = new Label();
            groupBox3 = new GroupBox();
            label9 = new Label();
            RearPanEquippedLabel = new Label();
            RearPanMaxCutDepth = new NumericInput();
            RearPanMaxCutDepthLabel = new Label();
            RearPanEquipped = new ComboBox();
            RearPanRaiseUnitsLabel = new Label();
            RearPanRaiseHeight = new NumericInput();
            RearPanEndofCutting = new ComboBox();
            RearPanEndofCuttingLabel = new Label();
            RearPanWidthUnitsLabel = new Label();
            RearPanWidth = new NumericInput();
            RearPanWidthLabel = new Label();
            RearPanAntennaHeightUnitsLabel = new Label();
            RearPanAntennaHeight = new NumericInput();
            RearPanAntennaHeightLabel = new Label();
            ApplyBtn = new Button();
            groupBox1.SuspendLayout();
            groupBox2.SuspendLayout();
            groupBox3.SuspendLayout();
            SuspendLayout();
            // 
            // sectionTitle1
            // 
            sectionTitle1.Dock = DockStyle.Top;
            sectionTitle1.Location = new Point(0, 0);
            sectionTitle1.Name = "sectionTitle1";
            sectionTitle1.Size = new Size(990, 48);
            sectionTitle1.TabIndex = 4;
            sectionTitle1.TitleText = "Equipment Settings";
            // 
            // groupBox1
            // 
            groupBox1.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            groupBox1.Controls.Add(label16);
            groupBox1.Controls.Add(label15);
            groupBox1.Controls.Add(label13);
            groupBox1.Controls.Add(TractorWidth);
            groupBox1.Controls.Add(label14);
            groupBox1.Controls.Add(label11);
            groupBox1.Controls.Add(TractorTurningCircle);
            groupBox1.Controls.Add(label12);
            groupBox1.Controls.Add(label5);
            groupBox1.Controls.Add(TractorAntennaForwardOffset);
            groupBox1.Controls.Add(label6);
            groupBox1.Controls.Add(label2);
            groupBox1.Controls.Add(TractorAntennaLeftOffset);
            groupBox1.Controls.Add(label4);
            groupBox1.Controls.Add(label3);
            groupBox1.Controls.Add(TractorAntennaHeight);
            groupBox1.Controls.Add(label1);
            groupBox1.Font = new Font("Segoe UI", 14F, FontStyle.Bold);
            groupBox1.Location = new Point(3, 54);
            groupBox1.Name = "groupBox1";
            groupBox1.Size = new Size(984, 216);
            groupBox1.TabIndex = 20;
            groupBox1.TabStop = false;
            groupBox1.Text = "Tractor";
            // 
            // label16
            // 
            label16.AutoSize = true;
            label16.Font = new Font("Segoe UI", 10F);
            label16.Location = new Point(250, 188);
            label16.Name = "label16";
            label16.Size = new Size(157, 19);
            label16.TabIndex = 36;
            label16.Text = "Negative for behind axle";
            // 
            // label15
            // 
            label15.AutoSize = true;
            label15.Font = new Font("Segoe UI", 10F);
            label15.Location = new Point(250, 122);
            label15.Name = "label15";
            label15.Size = new Size(175, 19);
            label15.TabIndex = 35;
            label15.Text = "Negative for right of center";
            // 
            // label13
            // 
            label13.AutoSize = true;
            label13.Font = new Font("Segoe UI", 14F);
            label13.Location = new Point(795, 87);
            label13.Name = "label13";
            label13.Size = new Size(44, 25);
            label13.TabIndex = 34;
            label13.Text = "mm";
            // 
            // TractorWidth
            // 
            TractorWidth.Location = new Point(622, 78);
            TractorWidth.Name = "TractorWidth";
            TractorWidth.Size = new Size(167, 41);
            TractorWidth.TabIndex = 33;
            TractorWidth.Unsigned = true;
            TractorWidth.Value = 0;
            // 
            // label14
            // 
            label14.AutoSize = true;
            label14.Font = new Font("Segoe UI", 14F);
            label14.Location = new Point(549, 87);
            label14.Name = "label14";
            label14.Size = new Size(67, 25);
            label14.TabIndex = 32;
            label14.Text = "Width:";
            // 
            // label11
            // 
            label11.AutoSize = true;
            label11.Font = new Font("Segoe UI", 14F);
            label11.Location = new Point(795, 40);
            label11.Name = "label11";
            label11.Size = new Size(28, 25);
            label11.TabIndex = 31;
            label11.Text = "m";
            // 
            // TractorTurningCircle
            // 
            TractorTurningCircle.Location = new Point(622, 31);
            TractorTurningCircle.Name = "TractorTurningCircle";
            TractorTurningCircle.Size = new Size(167, 41);
            TractorTurningCircle.TabIndex = 30;
            TractorTurningCircle.Unsigned = true;
            TractorTurningCircle.Value = 0;
            // 
            // label12
            // 
            label12.AutoSize = true;
            label12.Font = new Font("Segoe UI", 14F);
            label12.Location = new Point(481, 40);
            label12.Name = "label12";
            label12.Size = new Size(135, 25);
            label12.TabIndex = 29;
            label12.Text = "Turning Circle:";
            // 
            // label5
            // 
            label5.AutoSize = true;
            label5.Font = new Font("Segoe UI", 14F);
            label5.Location = new Point(423, 153);
            label5.Name = "label5";
            label5.Size = new Size(44, 25);
            label5.TabIndex = 28;
            label5.Text = "mm";
            // 
            // TractorAntennaForwardOffset
            // 
            TractorAntennaForwardOffset.Location = new Point(250, 144);
            TractorAntennaForwardOffset.Name = "TractorAntennaForwardOffset";
            TractorAntennaForwardOffset.Size = new Size(167, 41);
            TractorAntennaForwardOffset.TabIndex = 27;
            TractorAntennaForwardOffset.Unsigned = false;
            TractorAntennaForwardOffset.Value = 0;
            // 
            // label6
            // 
            label6.AutoSize = true;
            label6.Font = new Font("Segoe UI", 14F);
            label6.Location = new Point(20, 153);
            label6.Name = "label6";
            label6.Size = new Size(224, 25);
            label6.TabIndex = 26;
            label6.Text = "Antenna Forward of Axle:";
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Font = new Font("Segoe UI", 14F);
            label2.Location = new Point(423, 87);
            label2.Name = "label2";
            label2.Size = new Size(44, 25);
            label2.TabIndex = 25;
            label2.Text = "mm";
            // 
            // TractorAntennaLeftOffset
            // 
            TractorAntennaLeftOffset.Location = new Point(250, 78);
            TractorAntennaLeftOffset.Name = "TractorAntennaLeftOffset";
            TractorAntennaLeftOffset.Size = new Size(167, 41);
            TractorAntennaLeftOffset.TabIndex = 24;
            TractorAntennaLeftOffset.Unsigned = false;
            TractorAntennaLeftOffset.Value = 0;
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Font = new Font("Segoe UI", 14F);
            label4.Location = new Point(38, 87);
            label4.Name = "label4";
            label4.Size = new Size(206, 25);
            label4.TabIndex = 23;
            label4.Text = "Antenna Left of Center:";
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Font = new Font("Segoe UI", 14F);
            label3.Location = new Point(423, 40);
            label3.Name = "label3";
            label3.Size = new Size(44, 25);
            label3.TabIndex = 22;
            label3.Text = "mm";
            // 
            // TractorAntennaHeight
            // 
            TractorAntennaHeight.Location = new Point(250, 31);
            TractorAntennaHeight.Name = "TractorAntennaHeight";
            TractorAntennaHeight.Size = new Size(167, 43);
            TractorAntennaHeight.TabIndex = 21;
            TractorAntennaHeight.Unsigned = true;
            TractorAntennaHeight.Value = 0;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Font = new Font("Segoe UI", 14F);
            label1.Location = new Point(5, 40);
            label1.Name = "label1";
            label1.Size = new Size(239, 25);
            label1.TabIndex = 20;
            label1.Text = "Antenna Height to Ground:";
            // 
            // groupBox2
            // 
            groupBox2.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            groupBox2.Controls.Add(label7);
            groupBox2.Controls.Add(FrontPanMaxCutDepth);
            groupBox2.Controls.Add(FrontPanMaxCutDepthLabel);
            groupBox2.Controls.Add(FrontPanEquippedLabel);
            groupBox2.Controls.Add(FrontPanEquipped);
            groupBox2.Controls.Add(FrontPanRaiseUnitsLabel);
            groupBox2.Controls.Add(FrontPanRaiseHeight);
            groupBox2.Controls.Add(FrontPanEndofCutting);
            groupBox2.Controls.Add(FrontPanEndofCuttingLabel);
            groupBox2.Controls.Add(FrontPanWidthUnitsLabel);
            groupBox2.Controls.Add(FrontPanWidth);
            groupBox2.Controls.Add(FrontPanWidthLabel);
            groupBox2.Controls.Add(FrontPanAntennaHeightUnitsLabel);
            groupBox2.Controls.Add(FrontPanAntennaHeight);
            groupBox2.Controls.Add(FrontPanAntennaHeightLabel);
            groupBox2.Font = new Font("Segoe UI", 14F, FontStyle.Bold);
            groupBox2.Location = new Point(3, 276);
            groupBox2.Name = "groupBox2";
            groupBox2.Size = new Size(984, 205);
            groupBox2.TabIndex = 21;
            groupBox2.TabStop = false;
            groupBox2.Text = "Front Pan";
            // 
            // label7
            // 
            label7.AutoSize = true;
            label7.Font = new Font("Segoe UI", 14F);
            label7.Location = new Point(425, 167);
            label7.Name = "label7";
            label7.Size = new Size(44, 25);
            label7.TabIndex = 38;
            label7.Text = "mm";
            // 
            // FrontPanMaxCutDepth
            // 
            FrontPanMaxCutDepth.Location = new Point(250, 158);
            FrontPanMaxCutDepth.Name = "FrontPanMaxCutDepth";
            FrontPanMaxCutDepth.Size = new Size(167, 43);
            FrontPanMaxCutDepth.TabIndex = 37;
            FrontPanMaxCutDepth.Unsigned = true;
            FrontPanMaxCutDepth.Value = 0;
            // 
            // FrontPanMaxCutDepthLabel
            // 
            FrontPanMaxCutDepthLabel.AutoSize = true;
            FrontPanMaxCutDepthLabel.Font = new Font("Segoe UI", 14F);
            FrontPanMaxCutDepthLabel.Location = new Point(102, 167);
            FrontPanMaxCutDepthLabel.Name = "FrontPanMaxCutDepthLabel";
            FrontPanMaxCutDepthLabel.Size = new Size(142, 25);
            FrontPanMaxCutDepthLabel.TabIndex = 36;
            FrontPanMaxCutDepthLabel.Text = "Max Cut Depth:";
            // 
            // FrontPanEquippedLabel
            // 
            FrontPanEquippedLabel.AutoSize = true;
            FrontPanEquippedLabel.Font = new Font("Segoe UI", 14F);
            FrontPanEquippedLabel.Location = new Point(148, 34);
            FrontPanEquippedLabel.Name = "FrontPanEquippedLabel";
            FrontPanEquippedLabel.Size = new Size(96, 25);
            FrontPanEquippedLabel.TabIndex = 35;
            FrontPanEquippedLabel.Text = "Equipped:";
            // 
            // FrontPanEquipped
            // 
            FrontPanEquipped.DropDownStyle = ComboBoxStyle.DropDownList;
            FrontPanEquipped.Font = new Font("Segoe UI", 14F);
            FrontPanEquipped.FormattingEnabled = true;
            FrontPanEquipped.Items.AddRange(new object[] { "No", "Yes" });
            FrontPanEquipped.Location = new Point(250, 31);
            FrontPanEquipped.Name = "FrontPanEquipped";
            FrontPanEquipped.Size = new Size(74, 33);
            FrontPanEquipped.TabIndex = 34;
            // 
            // FrontPanRaiseUnitsLabel
            // 
            FrontPanRaiseUnitsLabel.AutoSize = true;
            FrontPanRaiseUnitsLabel.Font = new Font("Segoe UI", 14F);
            FrontPanRaiseUnitsLabel.Location = new Point(795, 124);
            FrontPanRaiseUnitsLabel.Name = "FrontPanRaiseUnitsLabel";
            FrontPanRaiseUnitsLabel.Size = new Size(44, 25);
            FrontPanRaiseUnitsLabel.TabIndex = 33;
            FrontPanRaiseUnitsLabel.Text = "mm";
            // 
            // FrontPanRaiseHeight
            // 
            FrontPanRaiseHeight.Location = new Point(622, 115);
            FrontPanRaiseHeight.Name = "FrontPanRaiseHeight";
            FrontPanRaiseHeight.Size = new Size(167, 43);
            FrontPanRaiseHeight.TabIndex = 32;
            FrontPanRaiseHeight.Unsigned = true;
            FrontPanRaiseHeight.Value = 0;
            // 
            // FrontPanEndofCutting
            // 
            FrontPanEndofCutting.DropDownStyle = ComboBoxStyle.DropDownList;
            FrontPanEndofCutting.Font = new Font("Segoe UI", 14F);
            FrontPanEndofCutting.FormattingEnabled = true;
            FrontPanEndofCutting.Items.AddRange(new object[] { "Float on surface", "Raise above surface" });
            FrontPanEndofCutting.Location = new Point(250, 119);
            FrontPanEndofCutting.Name = "FrontPanEndofCutting";
            FrontPanEndofCutting.Size = new Size(366, 33);
            FrontPanEndofCutting.TabIndex = 31;
            // 
            // FrontPanEndofCuttingLabel
            // 
            FrontPanEndofCuttingLabel.AutoSize = true;
            FrontPanEndofCuttingLabel.Font = new Font("Segoe UI", 14F);
            FrontPanEndofCuttingLabel.Location = new Point(107, 123);
            FrontPanEndofCuttingLabel.Name = "FrontPanEndofCuttingLabel";
            FrontPanEndofCuttingLabel.Size = new Size(137, 25);
            FrontPanEndofCuttingLabel.TabIndex = 30;
            FrontPanEndofCuttingLabel.Text = "End of Cutting:";
            // 
            // FrontPanWidthUnitsLabel
            // 
            FrontPanWidthUnitsLabel.AutoSize = true;
            FrontPanWidthUnitsLabel.Font = new Font("Segoe UI", 14F);
            FrontPanWidthUnitsLabel.Location = new Point(795, 79);
            FrontPanWidthUnitsLabel.Name = "FrontPanWidthUnitsLabel";
            FrontPanWidthUnitsLabel.Size = new Size(44, 25);
            FrontPanWidthUnitsLabel.TabIndex = 28;
            FrontPanWidthUnitsLabel.Text = "mm";
            // 
            // FrontPanWidth
            // 
            FrontPanWidth.Location = new Point(622, 70);
            FrontPanWidth.Name = "FrontPanWidth";
            FrontPanWidth.Size = new Size(167, 43);
            FrontPanWidth.TabIndex = 27;
            FrontPanWidth.Unsigned = true;
            FrontPanWidth.Value = 0;
            // 
            // FrontPanWidthLabel
            // 
            FrontPanWidthLabel.AutoSize = true;
            FrontPanWidthLabel.Font = new Font("Segoe UI", 14F);
            FrontPanWidthLabel.Location = new Point(549, 79);
            FrontPanWidthLabel.Name = "FrontPanWidthLabel";
            FrontPanWidthLabel.Size = new Size(67, 25);
            FrontPanWidthLabel.TabIndex = 26;
            FrontPanWidthLabel.Text = "Width:";
            // 
            // FrontPanAntennaHeightUnitsLabel
            // 
            FrontPanAntennaHeightUnitsLabel.AutoSize = true;
            FrontPanAntennaHeightUnitsLabel.Font = new Font("Segoe UI", 14F);
            FrontPanAntennaHeightUnitsLabel.Location = new Point(423, 79);
            FrontPanAntennaHeightUnitsLabel.Name = "FrontPanAntennaHeightUnitsLabel";
            FrontPanAntennaHeightUnitsLabel.Size = new Size(44, 25);
            FrontPanAntennaHeightUnitsLabel.TabIndex = 25;
            FrontPanAntennaHeightUnitsLabel.Text = "mm";
            // 
            // FrontPanAntennaHeight
            // 
            FrontPanAntennaHeight.Location = new Point(248, 70);
            FrontPanAntennaHeight.Name = "FrontPanAntennaHeight";
            FrontPanAntennaHeight.Size = new Size(167, 43);
            FrontPanAntennaHeight.TabIndex = 24;
            FrontPanAntennaHeight.Unsigned = true;
            FrontPanAntennaHeight.Value = 0;
            // 
            // FrontPanAntennaHeightLabel
            // 
            FrontPanAntennaHeightLabel.AutoSize = true;
            FrontPanAntennaHeightLabel.Font = new Font("Segoe UI", 14F);
            FrontPanAntennaHeightLabel.Location = new Point(20, 79);
            FrontPanAntennaHeightLabel.Name = "FrontPanAntennaHeightLabel";
            FrontPanAntennaHeightLabel.Size = new Size(222, 25);
            FrontPanAntennaHeightLabel.TabIndex = 23;
            FrontPanAntennaHeightLabel.Text = "Antenna Height to Blade:";
            // 
            // groupBox3
            // 
            groupBox3.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            groupBox3.Controls.Add(label9);
            groupBox3.Controls.Add(RearPanEquippedLabel);
            groupBox3.Controls.Add(RearPanMaxCutDepth);
            groupBox3.Controls.Add(RearPanMaxCutDepthLabel);
            groupBox3.Controls.Add(RearPanEquipped);
            groupBox3.Controls.Add(RearPanRaiseUnitsLabel);
            groupBox3.Controls.Add(RearPanRaiseHeight);
            groupBox3.Controls.Add(RearPanEndofCutting);
            groupBox3.Controls.Add(RearPanEndofCuttingLabel);
            groupBox3.Controls.Add(RearPanWidthUnitsLabel);
            groupBox3.Controls.Add(RearPanWidth);
            groupBox3.Controls.Add(RearPanWidthLabel);
            groupBox3.Controls.Add(RearPanAntennaHeightUnitsLabel);
            groupBox3.Controls.Add(RearPanAntennaHeight);
            groupBox3.Controls.Add(RearPanAntennaHeightLabel);
            groupBox3.Font = new Font("Segoe UI", 14F, FontStyle.Bold);
            groupBox3.Location = new Point(3, 487);
            groupBox3.Name = "groupBox3";
            groupBox3.Size = new Size(984, 210);
            groupBox3.TabIndex = 36;
            groupBox3.TabStop = false;
            groupBox3.Text = "Rear Pan";
            // 
            // label9
            // 
            label9.AutoSize = true;
            label9.Font = new Font("Segoe UI", 14F);
            label9.Location = new Point(425, 167);
            label9.Name = "label9";
            label9.Size = new Size(44, 25);
            label9.TabIndex = 41;
            label9.Text = "mm";
            // 
            // RearPanEquippedLabel
            // 
            RearPanEquippedLabel.AutoSize = true;
            RearPanEquippedLabel.Font = new Font("Segoe UI", 14F);
            RearPanEquippedLabel.Location = new Point(148, 34);
            RearPanEquippedLabel.Name = "RearPanEquippedLabel";
            RearPanEquippedLabel.Size = new Size(96, 25);
            RearPanEquippedLabel.TabIndex = 35;
            RearPanEquippedLabel.Text = "Equipped:";
            // 
            // RearPanMaxCutDepth
            // 
            RearPanMaxCutDepth.Location = new Point(250, 158);
            RearPanMaxCutDepth.Name = "RearPanMaxCutDepth";
            RearPanMaxCutDepth.Size = new Size(167, 43);
            RearPanMaxCutDepth.TabIndex = 40;
            RearPanMaxCutDepth.Unsigned = true;
            RearPanMaxCutDepth.Value = 0;
            // 
            // RearPanMaxCutDepthLabel
            // 
            RearPanMaxCutDepthLabel.AutoSize = true;
            RearPanMaxCutDepthLabel.Font = new Font("Segoe UI", 14F);
            RearPanMaxCutDepthLabel.Location = new Point(102, 167);
            RearPanMaxCutDepthLabel.Name = "RearPanMaxCutDepthLabel";
            RearPanMaxCutDepthLabel.Size = new Size(142, 25);
            RearPanMaxCutDepthLabel.TabIndex = 39;
            RearPanMaxCutDepthLabel.Text = "Max Cut Depth:";
            // 
            // RearPanEquipped
            // 
            RearPanEquipped.DropDownStyle = ComboBoxStyle.DropDownList;
            RearPanEquipped.Font = new Font("Segoe UI", 14F);
            RearPanEquipped.FormattingEnabled = true;
            RearPanEquipped.Items.AddRange(new object[] { "No", "Yes" });
            RearPanEquipped.Location = new Point(250, 31);
            RearPanEquipped.Name = "RearPanEquipped";
            RearPanEquipped.Size = new Size(74, 33);
            RearPanEquipped.TabIndex = 34;
            // 
            // RearPanRaiseUnitsLabel
            // 
            RearPanRaiseUnitsLabel.AutoSize = true;
            RearPanRaiseUnitsLabel.Font = new Font("Segoe UI", 14F);
            RearPanRaiseUnitsLabel.Location = new Point(795, 124);
            RearPanRaiseUnitsLabel.Name = "RearPanRaiseUnitsLabel";
            RearPanRaiseUnitsLabel.Size = new Size(44, 25);
            RearPanRaiseUnitsLabel.TabIndex = 33;
            RearPanRaiseUnitsLabel.Text = "mm";
            // 
            // RearPanRaiseHeight
            // 
            RearPanRaiseHeight.Location = new Point(622, 115);
            RearPanRaiseHeight.Name = "RearPanRaiseHeight";
            RearPanRaiseHeight.Size = new Size(167, 43);
            RearPanRaiseHeight.TabIndex = 32;
            RearPanRaiseHeight.Unsigned = true;
            RearPanRaiseHeight.Value = 0;
            // 
            // RearPanEndofCutting
            // 
            RearPanEndofCutting.DropDownStyle = ComboBoxStyle.DropDownList;
            RearPanEndofCutting.Font = new Font("Segoe UI", 14F);
            RearPanEndofCutting.FormattingEnabled = true;
            RearPanEndofCutting.Items.AddRange(new object[] { "Float on surface", "Raise above surface" });
            RearPanEndofCutting.Location = new Point(250, 119);
            RearPanEndofCutting.Name = "RearPanEndofCutting";
            RearPanEndofCutting.Size = new Size(366, 33);
            RearPanEndofCutting.TabIndex = 31;
            // 
            // RearPanEndofCuttingLabel
            // 
            RearPanEndofCuttingLabel.AutoSize = true;
            RearPanEndofCuttingLabel.Font = new Font("Segoe UI", 14F);
            RearPanEndofCuttingLabel.Location = new Point(107, 123);
            RearPanEndofCuttingLabel.Name = "RearPanEndofCuttingLabel";
            RearPanEndofCuttingLabel.Size = new Size(137, 25);
            RearPanEndofCuttingLabel.TabIndex = 30;
            RearPanEndofCuttingLabel.Text = "End of Cutting:";
            // 
            // RearPanWidthUnitsLabel
            // 
            RearPanWidthUnitsLabel.AutoSize = true;
            RearPanWidthUnitsLabel.Font = new Font("Segoe UI", 14F);
            RearPanWidthUnitsLabel.Location = new Point(795, 79);
            RearPanWidthUnitsLabel.Name = "RearPanWidthUnitsLabel";
            RearPanWidthUnitsLabel.Size = new Size(44, 25);
            RearPanWidthUnitsLabel.TabIndex = 28;
            RearPanWidthUnitsLabel.Text = "mm";
            // 
            // RearPanWidth
            // 
            RearPanWidth.Location = new Point(622, 70);
            RearPanWidth.Name = "RearPanWidth";
            RearPanWidth.Size = new Size(167, 43);
            RearPanWidth.TabIndex = 27;
            RearPanWidth.Unsigned = true;
            RearPanWidth.Value = 0;
            // 
            // RearPanWidthLabel
            // 
            RearPanWidthLabel.AutoSize = true;
            RearPanWidthLabel.Font = new Font("Segoe UI", 14F);
            RearPanWidthLabel.Location = new Point(549, 79);
            RearPanWidthLabel.Name = "RearPanWidthLabel";
            RearPanWidthLabel.Size = new Size(67, 25);
            RearPanWidthLabel.TabIndex = 26;
            RearPanWidthLabel.Text = "Width:";
            // 
            // RearPanAntennaHeightUnitsLabel
            // 
            RearPanAntennaHeightUnitsLabel.AutoSize = true;
            RearPanAntennaHeightUnitsLabel.Font = new Font("Segoe UI", 14F);
            RearPanAntennaHeightUnitsLabel.Location = new Point(423, 79);
            RearPanAntennaHeightUnitsLabel.Name = "RearPanAntennaHeightUnitsLabel";
            RearPanAntennaHeightUnitsLabel.Size = new Size(44, 25);
            RearPanAntennaHeightUnitsLabel.TabIndex = 25;
            RearPanAntennaHeightUnitsLabel.Text = "mm";
            // 
            // RearPanAntennaHeight
            // 
            RearPanAntennaHeight.Location = new Point(248, 70);
            RearPanAntennaHeight.Name = "RearPanAntennaHeight";
            RearPanAntennaHeight.Size = new Size(167, 43);
            RearPanAntennaHeight.TabIndex = 24;
            RearPanAntennaHeight.Unsigned = true;
            RearPanAntennaHeight.Value = 0;
            // 
            // RearPanAntennaHeightLabel
            // 
            RearPanAntennaHeightLabel.AutoSize = true;
            RearPanAntennaHeightLabel.Font = new Font("Segoe UI", 14F);
            RearPanAntennaHeightLabel.Location = new Point(20, 79);
            RearPanAntennaHeightLabel.Name = "RearPanAntennaHeightLabel";
            RearPanAntennaHeightLabel.Size = new Size(222, 25);
            RearPanAntennaHeightLabel.TabIndex = 23;
            RearPanAntennaHeightLabel.Text = "Antenna Height to Blade:";
            // 
            // ApplyBtn
            // 
            ApplyBtn.Image = (Image)resources.GetObject("ApplyBtn.Image");
            ApplyBtn.Location = new Point(246, 7);
            ApplyBtn.Name = "ApplyBtn";
            ApplyBtn.Size = new Size(32, 32);
            ApplyBtn.TabIndex = 37;
            ApplyBtn.UseVisualStyleBackColor = true;
            ApplyBtn.Click += ApplyBtn_Click;
            // 
            // EquipmentEditor
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.FromArgb(224, 224, 224);
            Controls.Add(ApplyBtn);
            Controls.Add(groupBox3);
            Controls.Add(groupBox2);
            Controls.Add(groupBox1);
            Controls.Add(sectionTitle1);
            Name = "EquipmentEditor";
            Size = new Size(990, 700);
            Load += EquipmentEditor_Load;
            groupBox1.ResumeLayout(false);
            groupBox1.PerformLayout();
            groupBox2.ResumeLayout(false);
            groupBox2.PerformLayout();
            groupBox3.ResumeLayout(false);
            groupBox3.PerformLayout();
            ResumeLayout(false);
        }

        #endregion
        private SectionTitle sectionTitle1;
        private GroupBox groupBox1;
        private Label label5;
        private NumericInput TractorAntennaForwardOffset;
        private Label label6;
        private Label label2;
        private NumericInput TractorAntennaLeftOffset;
        private Label label4;
        private Label label3;
        private NumericInput TractorAntennaHeight;
        private Label label1;
        private GroupBox groupBox2;
        private Label FrontPanAntennaHeightUnitsLabel;
        private NumericInput FrontPanAntennaHeight;
        private Label FrontPanAntennaHeightLabel;
        private Label label11;
        private NumericInput TractorTurningCircle;
        private Label label12;
        private Label FrontPanWidthUnitsLabel;
        private NumericInput FrontPanWidth;
        private Label FrontPanWidthLabel;
        private Label label13;
        private NumericInput TractorWidth;
        private Label label14;
        private Label label16;
        private Label label15;
        private Label FrontPanRaiseUnitsLabel;
        private NumericInput FrontPanRaiseHeight;
        private ComboBox FrontPanEndofCutting;
        private Label FrontPanEndofCuttingLabel;
        private Label FrontPanEquippedLabel;
        private ComboBox FrontPanEquipped;
        private GroupBox groupBox3;
        private Label RearPanEquippedLabel;
        private ComboBox RearPanEquipped;
        private Label RearPanRaiseUnitsLabel;
        private NumericInput RearPanRaiseHeight;
        private ComboBox RearPanEndofCutting;
        private Label RearPanEndofCuttingLabel;
        private Label RearPanWidthUnitsLabel;
        private NumericInput RearPanWidth;
        private Label RearPanWidthLabel;
        private Label RearPanAntennaHeightUnitsLabel;
        private NumericInput RearPanAntennaHeight;
        private Label RearPanAntennaHeightLabel;
        private Label label7;
        private NumericInput FrontPanMaxCutDepth;
        private Label FrontPanMaxCutDepthLabel;
        private Label label9;
        private NumericInput RearPanMaxCutDepth;
        private Label RearPanMaxCutDepthLabel;
        private Button ApplyBtn;
    }
}
