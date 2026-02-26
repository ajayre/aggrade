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
    public partial class FieldPanel : UserControl
    {
        public string FieldNameText
        {
            get { return FieldName.Text; }
            set { FieldName.Text = value; }
        }

        public string LastModifiedText
        {
            get { return  LastModified.Text; }
            set { LastModified.Text = value; }
        }

        private bool _Odd;
        public bool Odd
        {
            get { return  _Odd; }
            set
            {
                _Odd = value;
                if (Odd)
                {
                    FieldName.BackColor = Color.Wheat;
                    LastModified.BackColor = Color.Wheat;
                }
                else
                {
                    FieldName.BackColor = Color.OldLace;
                    LastModified.BackColor = Color.OldLace;
                }
            }
        }

        public FieldPanel()
        {
            InitializeComponent();

            FieldNameText = "Field Name";
            LastModifiedText = "Create New";
        }
    }
}
