using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AgGrade
{
    public partial class SplashForm : Form
    {
        private bool Windowed;

        public SplashForm
            (
            bool Windowed
            )
        {
            this.Windowed = Windowed;

            InitializeComponent();

            if (!Windowed)
            {
                // Set the form to not have a border, allowing it to cover the entire screen
                this.FormBorderStyle = FormBorderStyle.None;
                // Specify that the form's position and size will be set manually
                this.StartPosition = FormStartPosition.Manual;
                // Set the form's location to the top-left corner of the primary screen
                this.Location = Screen.PrimaryScreen!.Bounds.Location;
                // Set the form's size to the full resolution of the primary screen
                this.Size = Screen.PrimaryScreen.Bounds.Size;
            }
        }

        private void SplashForm_Shown(object sender, EventArgs e)
        {
        }

        private void SplashForm_FormClosed(object sender, FormClosedEventArgs e)
        {
        }
    }
}
