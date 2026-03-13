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
    public partial class ButtonPanel : UserControl
    {
        private Color COLOR1 = Color.MintCream;
        private Color COLOR2 = Color.PapayaWhip;

        public event Action<object> OnClicked = null;

        public string CaptionText
        {
            get { return Caption.Text; }
            set { Caption.Text = value; }
        }

        public Image? DisplayIcon
        {
            get { return Icon.BackgroundImage; }
            set { Icon.BackgroundImage = value; }
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
                    Caption.BackColor = COLOR1;
                    Icon.BackColor = COLOR1;
                }
                else
                {
                    Caption.BackColor = COLOR2;
                    Icon.BackColor = COLOR2;
                }
            }
        }

        public ButtonPanel()
        {
            InitializeComponent();

            CaptionText = "Caption";
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
        /// Called when field name is tapped
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Caption_Click(object sender, EventArgs e)
        {
            OnClicked?.Invoke(this);
        }
    }
}
