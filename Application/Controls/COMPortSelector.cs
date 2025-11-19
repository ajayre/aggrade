using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO.Ports;

namespace AgGrade.Controls
{
    public partial class COMPortSelector : UserControl
    {
        public COMPortSelector()
        {
            InitializeComponent();

            foreach (string Port in SerialPort.GetPortNames())
            {
                Selector.Items.Add(Port);
            }
        }
    }
}
