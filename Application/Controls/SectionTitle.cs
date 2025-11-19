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
    public partial class SectionTitle : UserControl
    {
        public string TitleText
        {
            get { return Title.Text; }
            set { Title.Text = value; }
        }

        public SectionTitle()
        {
            InitializeComponent();

            TitleText = "Section Title";
        }
    }
}
