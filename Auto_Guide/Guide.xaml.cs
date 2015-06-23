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
    ///     Interaction logic for Guide.xaml
    /// </summary>
    public partial class Guide
    {
        #region DEFINE_VARIETIES_OF_THE_CLASS
        public int Index { get; set; } = -1;
        public int Count { get; set; }
        private readonly int _areathreshold = 500;
        private readonly CenterPositionChecker _centerQ;
        //parameters for surf 
        private readonly SurfProcessor _cpu = new SurfProcessor();
        //two global buffer queue
        private readonly StatusQueueChecker _statusQ;
        private double _area;
        private BitmapImage _bitmapImage;
        private ICamera _camera;
        private Point _center;
        private IPairedDevice _device;
        private double _flag = -5;
        private bool _keepmatching = true;
        //bitmapsource to buffer modelimage and observed image
        private Image<Bgr, byte> _model, _observed;
        private MemoryStream _stream;
        private long _time;
        private string _txt;
        public List<string> DirectiveList;
        #endregion
        public Guide()
        {
            InitializeComponent();
            #region LOAD_REFERENCE_TO_DISK_AND_INIT_VARS
            IFormatter formatter = new BinaryFormatter();
            Stream fs = File.OpenRead(AppDomain.CurrentDomain.BaseDirectory + "\\obj\\route_node.obj");
            var head = (RouteNode) formatter.Deserialize(fs);
            Count = head.Count;
            DirectiveList = new List<string>(Count);
            fs.Dispose();
            _statusQ = new StatusQueueChecker(4);
            _centerQ = new CenterPositionChecker(10, 420, 380);
            while (head.Index < head.Count)
            {
                head.GetNextNode(out _model, out _txt);
                DirectiveList.Add(_txt);
                _model.Save(AppDomain.CurrentDomain.BaseDirectory + "\\obj\\images\\ref_" + head.Index + ".jpg");
            }
            head.Dispose();
            SwitchToNextRef(out _txt, out _model);
            _model.Save("D:\\temp\\" + (Index + 1) + ".jpg");
            #endregion
            InitBetterTogether();
        }

        

        private void SwitchToNextRef(out string txt, out Image<Bgr, byte> refImage)
        {
            refImage = null;
            txt = null;
            if (Index > Count) return;
            txt = DirectiveList[++Index];
            refImage =
                new Image<Bgr, byte>(AppDomain.CurrentDomain.BaseDirectory + "\\obj\\images\\ref_" + (Index + 1) +
                                     ".jpg");
        }
        #region INIT_BETTER_TOGETHER__BINDING_DELEGATES
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
            catch (Exception)
            {
                //MessageBox.Show(exp.Message);
            }
        }
        #endregion


        #region DEFINE_ACTIONS_WHEN_CONNECTION_STATUS_CHANGED
        private static void DeviceFinder_DeviceConnectionAccepting(object sender, ConnectionAcceptingEventArgs e)
        {
            e.ConnectionDeferral.AcceptAlways();
        }

        private void DeviceFinder_ConnectionStatusChanged(object sender, ConnectionStatusChangedEventArgs e)
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
        #endregion
        
        #region <!!!PROCEED_FRAMES!!!>
        private async void _camera_PreviewFrameAvailable(object sender, PreviewArrivedEventArgs e)
        {
            try
            {
                #region LOAD_THE_FRAME_IN_ASYNC_WAY
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
                            _bitmapImage.StreamSource = _stream; // Copy stream to local
                            _bitmapImage.EndInit();
                            #endregion

                            #region Match_And_Find_Homography

                            _observed = new Image<Bgr, byte>(UiHandler.Bmimg2Bitmap(_bitmapImage));
                            UiHandler.show_Image(cam, _model);
                            var mG = new Image<Gray, byte>(_model.ToBitmap());
                            var oG = new Image<Gray, byte>(_observed.ToBitmap());
                            if (_keepmatching)
                            {
                                var res = _cpu.DrawResult(mG, oG, out _time, out _area, _areathreshold, out _center
                                    /*,out distance*/);
                                //res.Save("D:\\res_" + (++index) + ".jpg");
                                cam_right.Source = UiHandler.ToBitmapSource(res.ToBitmap());

                                #endregion

                                // The matching result already comes out now, but I need to make it more stable
                                // And trigger the stop signal at the right time.


                                #region Stablize_The_Result_With_Queue

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
                                    {
                                        _flag += 1;
                                        _flag = _flag > 5 ? 5 : _flag;
                                    }
                                    else
                                    {
                                        _flag -= 1;
                                        _flag = _flag < -5 ? -5 : _flag;
                                    }


                                    if (_flag > -3)
                                    {
                                        _flag = -5;
                                        _keepmatching = false;
                                        //this shows there are enough positive match in the queue
                                        // and there are enough matching area with the proper size
                                        // which shows the object is close enough to the camera.
                                        if (Count == (Index + 1))
                                        {
                                            MessageBox.Show("Finished!");
                                            var mw = new MainWindow();
                                            mw.Show();
                                            Close();
                                        }
                                        else
                                        {
                                            MessageBox.Show("Reached Node\t" + (Index + 1) + "\n" + Count +
                                                            "\tNodes in total\n" + (Count - Index - 1) +
                                                            "\tNodes Ahead\nDirective:\n" + _txt);
                                            _keepmatching = true;
                                            SwitchToNextRef(out _txt, out _model);
                                            _model.Save("D:\\temp\\" + (Index + 1) + ".jpg");
                                        }
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
                        catch (Exception)
                        {
                            // ignored
                        }
                    }));
            }
            catch (Exception)
            {
                // ignored
            }
        }
        #endregion
    }
}