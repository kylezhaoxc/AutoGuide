using System;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using BetterTogether.Device;
using BetterTogether.Media;
using Emgu.CV;
using Emgu.CV.Structure;

namespace Auto_Guide
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class NewRoute : IDisposable
    {
        IPairedDevice _device;
        ICamera _camera;
        BitmapImage _bitmapImage;
        MemoryStream _stream;
        Image<Bgr, Byte> _nodepic;
        RouteNode _head = RouteNode.GetHead();
        int _count;
        public NewRoute( )
        {
            InitializeComponent();
            btn_unlock.IsEnabled = false;
            btn_update_node.IsEnabled = false ;
            btn_up_all_node.IsEnabled = false;
            InitBetterTogether();
        }
        void IDisposable.Dispose() { }
        private void InitBetterTogether()
        {
            // Initializes the device discovery service. By default NFC pairing is disabled, and WiFi broadcast pairing is enabled.
            DeviceFinder.Initialize("Route_Node");

            // Subscribe to an event that indicates that a connection request has arrived.
            DeviceFinder.DeviceConnectionAccepting += DeviceFinder_DeviceConnectionAccepting;

            // Subscribe to an event that indicates that connection status has changed.
            DeviceFinder.ConnectionStatusChanged += DeviceFinder_ConnectionStatusChanged;

            try
            {
                // Start device discovery through NFC pairing. The connection will be established using Wi-Fi.
                DeviceFinder.Start(ConnectionActionType.WIFI);
            }
            catch (Exception)
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
            ell_flag.Fill = Brushes.Green;
            ell_flag.Stroke = Brushes.Green;
            // Please notice the preview resolution is different to capture resolution
            await _camera.SetPreviewResolutionAsync(new Size(800, 448));
            _camera.PreviewFrameAvailable += _camera_PreviewFrameAvailable;
        }
        private async void _camera_PreviewFrameAvailable(object sender, PreviewArrivedEventArgs e)
        {
            try
            {
                _stream = new MemoryStream(e.Frame.ImageStream);

                if (null == _stream)

                    return;

                await Application.Current.Dispatcher.BeginInvoke(
                    DispatcherPriority.Background,
                    new Action(() =>
                    {
                        try
                        {
                            _bitmapImage = new BitmapImage();
                            _bitmapImage.BeginInit();
                            _bitmapImage.StreamSource = _stream;   // Copy stream to local
                            _bitmapImage.EndInit();
                            if (_nodepic == null) cam.Source = _bitmapImage;
                            else cam.Source = UiHandler.ToBitmapSource(_nodepic.ToBitmap());
                        }

                        catch (Exception )
                        {
                            // ignored
                        }
                
                    }));
            }
            catch (Exception )
            {
                // ignored
            }
        }

        private void btn_cam_Click(object sender, RoutedEventArgs e)
        {
           _nodepic= new Image<Bgr, byte>(UiHandler.Bmimg2Bitmap(_bitmapImage));
            btn_unlock.IsEnabled = true;
            btn_cam.IsEnabled = false;
            btn_update_node.IsEnabled = true;
        }

        private void btn_unlock_Click(object sender, RoutedEventArgs e)
        {
            _nodepic = null;
            btn_cam.IsEnabled = true;
            btn_unlock.IsEnabled = false;
            btn_update_node.IsEnabled = false;
        }

        private void btn_update_node_Click(object sender, RoutedEventArgs e)
        {
            _head.AddNode(_nodepic, txt_directive.Text);
            _count++;
            nodecount.Content = _count.ToString();
            txt_directive.Text = null;
            _nodepic = null;
            btn_cam.IsEnabled = true;
            btn_unlock.IsEnabled = false;
            btn_up_all_node.IsEnabled = true;
        }

        private void btn_up_all_node_Click(object sender, RoutedEventArgs e)
        {
            IFormatter formatter = new BinaryFormatter();
            var rn = RouteNode.GetHead();
            Stream fs = File.OpenWrite(AppDomain.CurrentDomain.BaseDirectory + "\\obj\\route_node.obj");
            formatter.Serialize(fs, rn);
            fs.Dispose();
            var mw=new MainWindow();
            mw.Show();
            Close();
        }
    }
}
