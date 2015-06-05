using BetterTogether.Device;
using BetterTogether.Media;
using System;
using System.IO;
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
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Features2D;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using System.Runtime.Serialization.Formatters;

namespace Auto_Guide
{
    /// <summary>
    /// Interaction logic for Guide.xaml
    /// </summary>
    public partial class Guide : Window
    {
        private MainWindow father;
        private List<Image<Bgr, Byte>> NodeImages = new List<Image<Bgr, byte>>();
        private List<string> NodeDirectives = new List<string>();
        RouteNode head;
        IPairedDevice _device;
        ICamera _camera;
        bool keepmatching = true;
        MemoryStream stream;
        //two global buffer queue
        StatusQueueChecker statusQ;
        CenterPositionChecker centerQ;
        string txt = null;
        int index = 0;
        //bitmapsource to buffer modelimage and observed image
        Image<Bgr, Byte> model, observed;
        BitmapImage bitmapImage;
        //parameters for surf 
        SurfProcessor cpu = new SurfProcessor();
        long time; double area; int areathreshold = 500; System.Drawing.Point center;
        double flag = -5;
        public Guide(MainWindow mw)
        {
            father = mw;
            InitializeComponent();
            System.Runtime.Serialization.IFormatter formatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
            Stream fs = File.OpenRead(AppDomain.CurrentDomain.BaseDirectory + "\\obj\\route_node.obj");
            head = (RouteNode)formatter.Deserialize(fs);
            fs.Dispose();
            statusQ = new StatusQueueChecker(4);
            centerQ = new CenterPositionChecker(10, 420, 380);
            head.GetNextNode(out model, out txt);
            
            InitBetterTogether();
        }
        private void auto_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            this.Hide();
            father.Show();
        }
        private void InitBetterTogether()
        {
            // Initializes the device discovery service. By default NFC pairing is disabled, and WiFi broadcast pairing is enabled.
            DeviceFinder.Initialize("Robot 01");

            // Subscribe to an event that indicates that a connection request has arrived.
            DeviceFinder.DeviceConnectionAccepting += DeviceFinder_DeviceConnectionAccepting;

            // Subscribe to an event that indicates that connection status has changed.
            DeviceFinder.ConnectionStatusChanged += DeviceFinder_ConnectionStatusChanged;

            try
            {
                // Start device discovery through NFC pairing. The connection will be established using Wi-Fi.
                DeviceFinder.Start(ConnectionActionType.WIFI);
            }
            catch (Exception exp)
            {
                //MessageBox.Show(exp.Message);
            }
        }

        void DeviceFinder_DeviceConnectionAccepting(object sender, ConnectionAcceptingEventArgs e)
        {
            e.ConnectionDeferral.AcceptAlways();
        }

        void DeviceFinder_ConnectionStatusChanged(object sender, ConnectionStatusChangedEventArgs e)
        {
            switch (e.ConnectionStatus)
            {
                case ConnectionStatus.NFC_TAPPED:
                    // User performed an NFC tap with the local device.
                    break;
                case ConnectionStatus.CONNECTED:
                    // Connection succeeded.

                    OnDeviceConnected(e.Device);
                    break;
                case ConnectionStatus.FAILED:

                    // Connection failed.
                    break;
            }
        }

        private async void OnDeviceConnected(IPairedDevice pairedDevice)
        {
            //StatusMessage.Visibility = Visibility.Collapsed;

            _device = pairedDevice;

            // Tell the camera object the 
            // resolution we want for the incoming video.
            // Here we use the 1st available resolution
            _camera = await _device.CameraManager.OpenAsync(
                    CameraLocation.Back,
                    _device.CameraManager.GetAvailableCaptureResolutions(
                    CameraLocation.Back)[0]
                    );
            ell_flag.Fill = System.Windows.Media.Brushes.Green;
            ell_flag.Stroke = System.Windows.Media.Brushes.Green;
            // Please notice the preview resolution is different to capture resolution
            await _camera.SetPreviewResolutionAsync(new System.Windows.Size(800, 448));
            _camera.PreviewFrameAvailable += _camera_PreviewFrameAvailable;
        }

        private async void _camera_PreviewFrameAvailable(object sender, PreviewArrivedEventArgs e)
        {
            try
            {
                stream = new System.IO.MemoryStream(e.Frame.ImageStream);
                if (null == stream)
                    return;
                await Application.Current.Dispatcher.BeginInvoke(
                    System.Windows.Threading.DispatcherPriority.Background,
                    new Action(() =>
                    {
                        try
                        {
                            bitmapImage = new BitmapImage();
                            bitmapImage.BeginInit();
                            bitmapImage.StreamSource = stream;   // Copy stream to local
                            bitmapImage.EndInit();
                            #region MatchAndFindHomography
                            observed = new Image<Bgr, byte>(UIHandler.bmimg2bitmap(bitmapImage));
                            UIHandler.show_Image(cam, model);
                            model.Save("D:\\1.jpg");
                            Image<Gray, Byte> m_g = new Image<Gray, Byte>(model.ToBitmap());
                            Image<Gray, Byte> o_g = new Image<Gray, Byte>(observed.ToBitmap());
                            if (keepmatching)
                            {
                                Image<Bgr, Byte> res = cpu.DrawResult(m_g, o_g, out time, out area, areathreshold, out center/*,out distance*/);
                                //res.Save("D:\\res_" + (++index) + ".jpg");
                                cam_right.Source = UIHandler.ToBitmapSource(res.ToBitmap());
                                #endregion

                                #region StablizeTheResultWithQueue
                                statusQ.EnQ(area);
                                if (statusQ.CheckMatch(areathreshold))
                                {

                                    lbtime.Content = time.ToString("f2") + "\tms";
                                    lbarea.Content = area.ToString();
                                    signal.Fill = Brushes.Green;
                                    MorNM.Content = "Matched";
                                    centerQ.EnQ(center);
                                    string Indicator = centerQ.CheckPosition();
                                    UIHandler.TellDirection(direction, txt_direction, Indicator);

                                    #region estimate-distance
                                    //use area
                                    if (area > 250000)
                                    { flag += 1; flag = flag > 5 ? 5 : flag; }
                                    else
                                    { flag -= 1; flag = flag < -5 ? -5 : flag; };


                                    if (flag > -3)
                                    {
                                        flag = -5;keepmatching = false;
                                        if ((head.Count - head.Index) == 0) { MessageBox.Show("Finished!");this.Close(); }
                                        else MessageBox.Show("Reached Node\t" + (head.Index) + "\n" + head.Count + "\tNodes in total\n" + (head.Count - head.Index) + "\tNodes Ahead\nDirective:" + txt);
                                        keepmatching = true;
                                        head.GetNextNode(out model, out txt);
                                    }
                                    else txt_dist.Content = null;
                                }
                                #endregion
                                else
                                {
                                    signal.Fill = Brushes.Red;
                                    MorNM.Content = "No Match";
                                    txt_direction.Content = "Wait for matching......";
                                    direction.Source = null;
                                }

                                #endregion

                            }
                            else UIHandler.show_Image(cam_right, observed);
                        }
                        catch (Exception ex) { };
                    }));
            }
            catch (Exception ex) { } 
        }

    }
}
