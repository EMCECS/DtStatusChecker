using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
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
using System.Windows.Threading;
using System.Xml;

namespace DtStatusChecker
{
    /// <summary>
    /// Interaction logic for VdcStatus.xaml
    /// </summary>
    public partial class VdcStatus : UserControl
    {

        // Timer to control updates
        private DispatcherTimer timer;
        enum VdcState { STOPPED, RUNNING, REFRESHING };
        private VdcState currentStatus;


        public string VdcName
        {
            get { return (string)GetValue(VdcNameProperty); }
            set { SetValue(VdcNameProperty, value); }
        }

        // Using a DependencyProperty as the backing store for VdcName.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty VdcNameProperty =
            DependencyProperty.Register("VdcName", typeof(string), typeof(VdcStatus), new PropertyMetadata("VDC Name (192.168.1.1)"));



        public int TotalDts
        {
            get { return (int)GetValue(TotalDtsProperty); }
            set {
                SetValue(TotalDtsProperty, value);
                if(value == 0 && InitProgress != null)
                {
                    InitProgress.IsIndeterminate = true;
                } else
                {
                    InitProgress.IsIndeterminate = false;
                }
            }
        }

        // Using a DependencyProperty as the backing store for TotalDts.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty TotalDtsProperty =
            DependencyProperty.Register("TotalDts", typeof(int), typeof(VdcStatus), new PropertyMetadata(0));



        public int UnreadyDts
        {
            get { return (int)GetValue(UnreadyDtsProperty); }
            set { SetValue(UnreadyDtsProperty, value); }
        }

        // Using a DependencyProperty as the backing store for UnreadyDts.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty UnreadyDtsProperty =
            DependencyProperty.Register("UnreadyDts", typeof(int), typeof(VdcStatus), new PropertyMetadata(0));


        public int UnknownDts
        {
            get { return (int)GetValue(UnknownDtsProperty); }
            set { SetValue(UnknownDtsProperty, value); }
        }

        // Using a DependencyProperty as the backing store for UnknownDts.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty UnknownDtsProperty =
            DependencyProperty.Register("UnknownDts", typeof(int), typeof(VdcStatus), new PropertyMetadata(0));



        public double PercentInit
        {
            get { return (double)GetValue(PercentInitProperty); }
            set { SetValue(PercentInitProperty, value); }
        }

        // Using a DependencyProperty as the backing store for PercentInit.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty PercentInitProperty =
            DependencyProperty.Register("PercentInit", typeof(double), typeof(VdcStatus), new PropertyMetadata(0.0));



        public string VdcIp
        {
            get { return (string)GetValue(VdcIpProperty); }
            set { SetValue(VdcIpProperty, value); }
        }

        // Using a DependencyProperty as the backing store for VdcIp.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty VdcIpProperty =
            DependencyProperty.Register("VdcIp", typeof(string), typeof(VdcStatus), new PropertyMetadata("localhost"));


        public VdcStatus()
        {
            InitializeComponent();
        }

        public void start()
        {
            currentStatus = VdcState.RUNNING;
            timer = new DispatcherTimer();
            timer.Interval = new TimeSpan(0, 0, 1);
            timer.Tick += Timer_Tick;
            timer.Start();
            
        }

        private async void Timer_Tick(object sender, EventArgs e)
        {
            // Check current state:
            switch(currentStatus)
            {
                case VdcState.RUNNING:
                    // Do a check
                    await updateStatus();
                    break;
                case VdcState.REFRESHING:
                    // Currently refreshing
                    break;
                case VdcState.STOPPED:
                    break;
            }
        }

        private async Task updateStatus()
        {
            timer.Interval = new TimeSpan(0, 0, 30);
            currentStatus = VdcState.REFRESHING;
            string url = "http://" + VdcIp + ":9101/stats/dt/DTInitStat/";
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            byte[] result;
            byte[] buffer = new byte[4096];
            using (var response = (HttpWebResponse)await request.GetResponseAsync()) try
            {
                using(Stream responseStream = response.GetResponseStream())
                {
                    using(MemoryStream memoryStream = new MemoryStream())
                    {
                        int count = 0;
                        do
                        {
                            count = responseStream.Read(buffer, 0, buffer.Length);
                            memoryStream.Write(buffer, 0, count);
                        } while (count != 0);
                        result = memoryStream.ToArray();
                    }
                }

                if(response.StatusCode == HttpStatusCode.OK)
                {
                    // Parse XML
                    XmlDocument doc = new XmlDocument();
                    doc.Load(new MemoryStream(result));
                    XmlNode node = doc.SelectSingleNode("//total_dt_num");
                    int total = int.Parse(node.InnerText);
                    node = doc.SelectSingleNode("//unready_dt_num");
                    int unready = int.Parse(node.InnerText);
                    node = doc.SelectSingleNode("//unknown_dt_num");
                    int unknown = int.Parse(node.InnerText);

                        // Dispatch onto UI thread.
                        totalValue.Dispatcher.Invoke(DispatcherPriority.DataBind, new updateDelegate(update), total, unready, unknown); 
                } else
                {
                    if(result != null)
                    {
                        Debug.WriteLine(string.Format("HTTP request failed.  Status: {0} Message: {1}", response.StatusCode, Encoding.UTF8.GetString(result)));

                    } else
                    {
                        Debug.WriteLine(string.Format("HTTP request failed: {0} ", response.StatusCode));
                    }
                }
            } catch(WebException e) {
                Debug.WriteLine(string.Format("HTTP request failed: {0} ", e.Status));
            }
            currentStatus = VdcState.RUNNING;
        }

        public delegate void updateDelegate(int total, int unready, int unkown);

        public void update(int total, int unready, int unkown)
        {
            TotalDts = total;
            UnreadyDts = unready;
            UnknownDts = unkown;
            if(total <= 0)
            {
                InitProgress.IsIndeterminate = true;
            } else
            {
                int ready = total - unready - unkown;
                double percent = ready * 100 / total;
                InitProgress.IsIndeterminate = false;
                PercentInit = percent;
                
            }
        }
    }


}
