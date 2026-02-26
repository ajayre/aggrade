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
        private Color COLOR1 = Color.MintCream;
        private Color COLOR2 = Color.PapayaWhip;

        public event Action<object> OnClicked = null;

        public string FieldNameText
        {
            get { return FieldName.Text; }
            set { FieldName.Text = value; }
        }

        public string LastModifiedText
        {
            get { return LastModified.Text; }
            set { LastModified.Text = value; }
        }

        public bool ShowIcon
        {
            get { return Icon.Visible; }
            set { Icon.Visible = value; }
        }

        private string _Folder;
        public string Folder
        {
            get { return _Folder; }
            set { _Folder = value; }
        }

        private string? _DbFile;
        public string? DbFile
        {
            get { return _DbFile; }
            set { _DbFile = value; }
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
                    FieldName.BackColor = COLOR1;
                    LastModified.BackColor = COLOR1;
                    Icon.BackColor = COLOR1;
                }
                else
                {
                    FieldName.BackColor = COLOR2;
                    LastModified.BackColor = COLOR2;
                    Icon.BackColor = COLOR2;
                }
            }
        }

        public FieldPanel()
        {
            InitializeComponent();

            FieldNameText = "Field Name";
            LastModifiedText = "Create New";

            DbFile = null;
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
