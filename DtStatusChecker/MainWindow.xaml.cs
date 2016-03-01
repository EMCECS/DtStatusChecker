using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace DtStatusChecker
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Initialized(object sender, EventArgs e)
        {
            // Read the app settings and insert the VDCs.
            string vdcConfig = System.Configuration.ConfigurationManager.AppSettings["vdcs"];
            string[] vdcs = vdcConfig.Split(',');
            foreach(string vdc in vdcs) {
                string[] pair = vdc.Split(':');
                VdcStatus stat = new VdcStatus();
                stat.VdcName = pair[0];
                stat.VdcIp = pair[1];

                VdcStack.Children.Add(stat);
                stat.start();
            }
        }
    }
}
