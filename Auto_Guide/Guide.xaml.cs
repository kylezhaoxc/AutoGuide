using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using BetterTogether.Device;
using BetterTogether.Media;
using Emgu.CV;
using Emgu.CV.Structure;
using Brushes = System.Windows.Media.Brushes;
using Point = System.Drawing.Point;
using Size = System.Windows.Size;

namespace Auto_Guide
{
    /// <summary>
    /// Interaction logic for Guide.xaml
    /// </summary>
    public partial class Guide 
    {
        public List<Image<Bgr, byte>> NodeImages { get; } = new List<Image<Bgr, byte>>();
        public List<string> NodeDirectives { get; } = new List<string>();
        RouteNode _head;
        IPairedDevice _device;
        ICamera _camera;
        bool _keepmatching = true;
        MemoryStream _stream;
        //two global buffer queue
        StatusQueueChecker _statusQ;
        CenterPositionChecker _centerQ;
        string _txt;
        public int Index { get; } = 0;
        //bitmapsource to buffer modelimage and observed image
        Image<Bgr, Byte> _model, _observed;
        BitmapImage _bitmapImage;
        //parameters for surf 
        SurfProcessor _cpu = new SurfProcessor();
        long _time; double _area; int _areathreshold = 500; Point _center;
        double _flag = -5;
        public Guide()
        {
            InitializeComponent();
            IFormatter formatter = new BinaryFormatter();
            Stream fs = File.OpenRead(AppDomain.CurrentDomain.BaseDirectory + "\\obj\\route_node.obj");
            _head = (RouteNode)formatter.Deserialize(fs);
            fs.Dispose();
            _statusQ = new StatusQueueChecker(4);
            _centerQ = new CenterPositionChecker(10, 420, 380);
            _head.GetNextNode(out _model, out _txt);
            
            InitBetterTogether();
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
            catch (Exception )
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
                            #region MatchAndFindHomography
                            _observed = new Image<Bgr, byte>(UiHandler.Bmimg2Bitmap(_bitmapImage));
                            UiHandler.show_Image(cam, _model);
                            _model.Save("D:\\1.jpg");
                            var mG = new Image<Gray, Byte>(_model.ToBitmap());
                            var oG = new Image<Gray, Byte>(_observed.ToBitmap());
                            if (_keepmatching)
                            {
                                var res = _cpu.DrawResult(mG, oG, out _time, out _area, _areathreshold, out _center/*,out distance*/);
                                //res.Save("D:\\res_" + (++index) + ".jpg");
                                cam_right.Source = UiHandler.ToBitmapSource(res.ToBitmap());
                                #endregion

                                #region StablizeTheResultWithQueue
                                _statusQ.EnQ(_area);
                                if (_statusQ.CheckMatch(_areathreshold))
                                {

                                    lbtime.Content = _time.ToString("f2") + "\tms";
                                    lbarea.Content = _area.ToString(CultureInfo.InvariantCulture);
                                    signal.Fill = Brushes.Green;
                                    MorNM.Content = "Matched";
                                    _centerQ.EnQ(_center);
                                    var indicator = _centerQ.CheckPosition();
                                    UiHandler.TellDirection(direction, txt_direction, indicator);

                                    #region estimate-distance
                                    //use area
                                    if (_area > 128000)
                                    { _flag += 1; _flag = _flag > 5 ? 5 : _flag; }
                                    else
                                    { _flag -= 1; _flag = _flag < -5 ? -5 : _flag; }


                                    if (_flag > -3)
                                    {
                                        _flag = -5;_keepmatching = false;
                                        if ((_head.Count - _head.Index) == 0) { MessageBox.Show("Finished!"); var mw = new MainWindow();
                                            mw.Show();
                                            Close(); }
                                        else MessageBox.Show("Reached Node\t" + (_head.Index) + "\n" + _head.Count + "\tNodes in total\n" + (_head.Count - _head.Index) + "\tNodes Ahead\nDirective:\n" + _txt);
                                        _keepmatching = true;
                                        _head.GetNextNode(out _model, out _txt);
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
                            else UiHandler.show_Image(cam_right, _observed);
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

    }
}
