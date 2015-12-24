using System;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using BetterTogether.Bluetooth;
using BetterTogether.Device;
using BetterTogether.Media;
using BetterTogether.UI;
using Emgu.CV;
using Localization;
using Emgu.CV.Structure;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Auto_Guide
{
    /// <summary>
    ///     Interaction logic for MainWindow.xaml
    /// </summary>
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class RouteDef : Window
    {
        #region var define

        #region bettertogether vars
        private ICamera _camera;
        private IPairedDevice _device;
        private MemoryStream _record_stream;
        private BitmapImage _bitmapImage;
        App _thisApp = (App)Application.Current;
        #endregion

        #region other vars
        public delegate void PreviewFrameHandler(object sender);
        public event PreviewFrameHandler pevent;
        Image<Bgr, byte> observed;
        Navigator nav;
        Thread threadShowStream;
        Image<Bgr, byte> ref_1;
        Image<Bgr, byte> ref_2;
        List<Image<Bgr, byte>> refs = new List<Image<Bgr, byte>>();
        List<byte[]> directives = new List<byte[]>();
        byte[] directive = new byte[5];
        bool r1, r2;
        int count = 0;
        #endregion 

        #endregion

        #region constructor and close event
        public RouteDef(ICamera __camera)
        {
            InitializeComponent();
            InitBetterTogether();
            ell_cannav.Fill = Brushes.Red;
            threadShowStream = new Thread(new ThreadStart(ShowStream));
            this._camera = __camera;
            btn_Confirm.IsEnabled = false; comboBox.IsEnabled = false;
            r1 = false; r2 = false; btn_train.IsEnabled = false;
            directives.Add(Cmd.stop);
            clearall();
        }
        private void Window_Closed(object sender, EventArgs e)
        {
            pevent(this);
        }
        #endregion

        #region RouteDef events
        private async void btn_ref2_browse_Click(object sender, RoutedEventArgs e)
        {
            string ref_url = null;

            using (System.Windows.Forms.OpenFileDialog dialog = new System.Windows.Forms.OpenFileDialog())
            {
                System.Windows.Forms.DialogResult result = dialog.ShowDialog();
                if (result == System.Windows.Forms.DialogResult.OK)
                    ref_url = dialog.FileName;

            }
            ref_2 = ref_url == null ? null : new Image<Bgr, byte>(ref_url);
            await Application.Current.Dispatcher.BeginInvoke(
                          System.Windows.Threading.DispatcherPriority.Background,
                          new Action(() =>
                          {
                              try
                              {
                                  if (ref_2 == null) ref2.Source = null;
                                  //PreviewWindow.Source = ToBitmapSource(img_result);
                                  ref2.Source = ToBitmapSource(ref_2);
                                  r2 = true;
                                  checkconfirm();
                              }
                              catch (Exception ex) { };
                          }));

        }
        private async void btn_ref1_browse_Click(object sender, RoutedEventArgs e)
        {
            string ref_url = null;
            using (System.Windows.Forms.OpenFileDialog dialog = new System.Windows.Forms.OpenFileDialog())
            {
                System.Windows.Forms.DialogResult result = dialog.ShowDialog();
                if (result == System.Windows.Forms.DialogResult.OK)
                    ref_url = dialog.FileName;

            }
            ref_1 = ref_url == null ? null : new Image<Bgr, byte>(ref_url);
            await Application.Current.Dispatcher.BeginInvoke(
                          System.Windows.Threading.DispatcherPriority.Background,
                          new Action(() =>
                          {
                              try
                              {
                                  if (ref_1 == null) ref1.Source = null;
                                  //PreviewWindow.Source = ToBitmapSource(img_result);
                                  ref1.Source = ToBitmapSource(ref_1);
                                  r1 = true;
                                  checkconfirm();
                              }
                              catch (Exception ex) { };
                          }));

        }
        private async void btn_ref1_cap_Click(object sender, RoutedEventArgs e)
        {
            ref_1 = observed;
            await Application.Current.Dispatcher.BeginInvoke(
                         System.Windows.Threading.DispatcherPriority.Background,
                         new Action(() =>
                         {
                             try
                             {
                                 //PreviewWindow.Source = ToBitmapSource(img_result);
                                 ref1.Source = ToBitmapSource(ref_1);
                                 r1 = true;
                                 checkconfirm();
                             }
                             catch (Exception ex) { };
                         }));
        }
        private async void btn_ref2_cap_Click(object sender, RoutedEventArgs e)
        {
            ref_2 = observed;
            await Application.Current.Dispatcher.BeginInvoke(
                         System.Windows.Threading.DispatcherPriority.Background,
                         new Action(() =>
                         {
                             try
                             {
                                 //PreviewWindow.Source = ToBitmapSource(img_result);
                                 ref2.Source = ToBitmapSource(ref_2);
                             }
                             catch (Exception ex) { };
                         }));
            r2 = true;
            checkconfirm();
        }

        private void comboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            switch (comboBox.SelectedIndex)
            {
                case 0: directive = Cmd.turn_left_faster; break;
                case 1: directive = Cmd.turn_right_faster; break;
                case 2: directive = Cmd.Go; break;
                case 3: directive = Cmd.stop; break;
            }
            btn_Confirm.IsEnabled = true;
        }

        private async void btn_Confirm_Click(object sender, RoutedEventArgs e)
        {
            refs.Add(ref_1);
            refs.Add(ref_2);
            directives.Add(directive);
            count++;
            await Application.Current.Dispatcher.BeginInvoke(
                        System.Windows.Threading.DispatcherPriority.Background,
                        new Action(() =>
                        {
                            try
                            {
                                //PreviewWindow.Source = ToBitmapSource(img_result);
                                nodecount.Content = count;
                                ref1.Source = null; ref2.Source = null; r1 = false; r2 = false;
                                comboBox.SelectedIndex = -1;
                                checkconfirm();
                                btn_Confirm.IsEnabled = false;
                                btn_train.IsEnabled = true;
                            }
                            catch (Exception ex) { };
                        }));
        }
        private async void btn_train_Click(object sender, RoutedEventArgs e)
        {
            for (int i = 0; i < refs.Count; i += 1)
            {
                refs[i].Save(AppDomain.CurrentDomain.BaseDirectory + "nodeimages\\" + (i).ToString().PadLeft(2, '0') + ".jpg");
            }
            await Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
            {
                try { PrepNodes prep = PrepNodes.GetNavigator(AppDomain.CurrentDomain.BaseDirectory + "nodeimages", directives); }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }));
            ell_cannav.Fill = Brushes.Green; btn_nav.IsEnabled = true;
        }
        #endregion

        #region UI control
        private async void ShowStream()
        {
            while (true)
            {
                try
                {
                    System.Drawing.Bitmap img_bmp = new System.Drawing.Bitmap(_record_stream, false);
                    observed = new Image<Bgr, byte>(img_bmp);

                    await Application.Current.Dispatcher.BeginInvoke(
                            System.Windows.Threading.DispatcherPriority.Background,
                            new Action(() =>
                            {
                                try
                                {
                                    //PreviewWindow.Source = ToBitmapSource(img_result);
                                    cam.Source = ToBitmapSource(observed);
                                }
                                catch (Exception ex) { };
                            }));
                }
                catch (Exception e)
                { }
            }
        }
        private void checkconfirm()
        {
            if (r1 && r2) comboBox.IsEnabled = true;
            else comboBox.IsEnabled = false;

            btn_train.IsEnabled = false;
        }

        #endregion

        #region manual_controll
        private async void btn_man_go_Click(object sender, RoutedEventArgs e)
        {
            await _thisApp._MyRobot.SendCommand(_thisApp.btconn, Cmd.Go);
            await Task.Delay(1000);
            await _thisApp._MyRobot.SendCommand(_thisApp.btconn, Cmd.stop);
        }

        private async void btn_man_left_Click(object sender, RoutedEventArgs e)
        {
            await _thisApp._MyRobot.SendCommand(_thisApp.btconn, Cmd.turn_left_faster);
            await _thisApp._MyRobot.SendCommand(_thisApp.btconn, Cmd.stop);
        }

        private async void btn_man_Right_Click(object sender, RoutedEventArgs e)
        {
            await _thisApp._MyRobot.SendCommand(_thisApp.btconn, Cmd.turn_right_faster);
            await _thisApp._MyRobot.SendCommand(_thisApp.btconn, Cmd.stop);
        }

        private async void btn_man_stop_Click(object sender, RoutedEventArgs e)
        {
            await _thisApp._MyRobot.SendCommand(_thisApp.btconn, Cmd.stop);
        }
        #endregion

        #region tools and betterTogether
        private void clearall()
        {
            Directory.Delete(AppDomain.CurrentDomain.BaseDirectory + "nodeimages", true);
            Directory.CreateDirectory(AppDomain.CurrentDomain.BaseDirectory + "nodeimages");
        }

        private void _camera_PreviewFrameAvailable(object sender, PreviewArrivedEventArgs e)
        {
            try
            {
                _record_stream = new MemoryStream(e.Frame.ImageStream);
                if (null == _record_stream)
                    return;
                else
                {
                    threadShowStream.Start();
                }
            }
            catch (Exception)
            {
                // ignored
            }
        }
        [System.Runtime.InteropServices.DllImport("gdi32")]
        private static extern int DeleteObject(IntPtr o);

        public static BitmapSource ToBitmapSource(IImage image)
        {
            using (System.Drawing.Bitmap sourc = image.Bitmap)
            {
                IntPtr ptr = sourc.GetHbitmap();

                BitmapSource bs = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
                    ptr,
                    IntPtr.Zero,
                    Int32Rect.Empty,
                    System.Windows.Media.Imaging.BitmapSizeOptions.FromEmptyOptions());

                DeleteObject(ptr);

                return bs;
            }
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
            catch (Exception)
            {
                //MessageBox.Show(exp.Message);
            }
        }

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
        void OnDeviceDisconnected(object sender, DeviceDisconnectedArgs e)
        {
            //StatusMessage.Visibility = Visibility.Visible;
            _camera.Dispose();
            _camera = null;
            //_controller = null;
            _device.DeviceDisconnected -= OnDeviceDisconnected;
            _device = null;
            //_controller = null;
            _thisApp.btconn = null;
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

        #endregion

        #region passing _camera to next window
        private void RestartCPFA(object sender)
        {
            _camera.PreviewFrameAvailable += _camera_PreviewFrameAvailable;
        }
        private void btn_nav_Click(object sender, RoutedEventArgs e)
        {
            _camera.PreviewFrameAvailable -= _camera_PreviewFrameAvailable;
            Navigate nav = new Navigate(_camera);
            nav.pevent += new Navigate.PreviewFrameHandler(RestartCPFA);
            nav.Show();
        }
        #endregion

    }
}