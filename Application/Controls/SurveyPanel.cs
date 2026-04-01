using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AgGrade.Controls
{
    public partial class SurveyPanel : UserControl
    {
        private Color COLOR1 = Color.FromArgb(0xCB, 0x9C, 0x52);
        private Color COLOR2 = Color.FromArgb(0xD6, 0xB2, 0x79);

        public event Action<object> OnClicked = null;

        public string SurveyNameText
        {
            get { return SurveyName.Text; }
            set { SurveyName.Text = value; }
        }

        public string LastModifiedText
        {
            get { return LastModified.Text; }
            set { LastModified.Text = value; }
        }

        private string? _FileName;
        public string? FileName
        {
            get { return _FileName; }
            set { _FileName = value; }
        }

        private bool _Odd;
        public bool Odd
        {
            get { return _Odd; }
            set
            {
                _Odd = value;
                if (Odd)
                {
                    SurveyName.BackColor = COLOR1;
                    LastModified.BackColor = COLOR1;
                    Icon.BackColor = COLOR1;
                }
                else
                {
                    SurveyName.BackColor = COLOR2;
                    LastModified.BackColor = COLOR2;
                    Icon.BackColor = COLOR2;
                }
            }
        }

        public SurveyPanel()
        {
            InitializeComponent();

            SurveyNameText = "Survey Name";
            LastModifiedText = "";
        }

        /// <summary>
        /// Called when icon is tapped
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Icon_Click(object sender, EventArgs e)
        {
            OnClicked?.Invoke(this);
        }

        /// <summary>
        /// Called when modified text is tapped
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void LastModified_Click(object sender, EventArgs e)
        {
            OnClicked?.Invoke(this);
        }

        /// <summary>
        /// Called when field name is tapped
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void FieldName_Click(object sender, EventArgs e)
        {
            OnClicked?.Invoke(this);
        }
    }
}
