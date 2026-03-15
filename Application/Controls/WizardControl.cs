using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AgGrade.Controls
{
    public class WizardControl : UserControl
    {
        public event Action<object> ExitWizard = null;

        /// <summary>
        /// Gets the top-level tab control
        /// </summary>
        public TabControl? TabControl
        {
            get
            {
                if (Controls.Count > 0 && Controls[0] is TabControl)
                {
                    return (TabControl)Controls[0];
                }
                else
                {
                    return null;
                }
            }
        }

        /// <summary>
        /// Ecits the wizard
        /// </summary>
        public void Exit
            (
            )
        {
            ExitWizard?.Invoke(this);
        }

        /// <summary>
        /// Gets the index of the currently showing page or -1 for no pages
        /// </summary>
        public int CurrentPage
        {
            get
            {
                if (TabControl != null)
                {
                    return TabControl.SelectedIndex;
                }
                else
                {
                    return -1;
                }
            }
        }

        /// <summary>
        /// The number of pages in the wizard
        /// </summary>
        public int NumberofPages
        {
            get
            {
                if (TabControl != null)
                {
                    return TabControl.TabPages.Count;
                }
                else
                {
                    return 0;
                }
            }
        }

        /// <summary>
        /// Shows a specific page
        /// </summary>
        /// <param name="PageIndex">Index of page to show</param>
        public void ShowPage
            (
            int PageIndex
            )
        {
            if (TabControl != null)
            {
                int NumPages = TabControl.TabPages.Count;
                if (PageIndex < NumPages)
                {
                    TabControl.SelectedIndex = PageIndex;
                }
            }
        }

        /// <summary>
        /// Hides the tabs
        /// </summary>
        public void HideTabs
            (
            )
        {
            if (TabControl != null)
            {
                TabControl!.Appearance = TabAppearance.FlatButtons;
                TabControl!.ItemSize = new Size(0, 1);
                TabControl!.SizeMode = TabSizeMode.Fixed;
            }
        }
    }
}
