using System;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using BetterTogether.Device;
using BetterTogether.Media;
using Emgu.CV;
using Localization;
using Emgu.CV.Structure;
using System.Collections.Generic;
using System.Threading;

namespace Auto_Guide
{
    /// <summary>
    ///     Interaction logic for MainWindow.xaml
    /// </summary>
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private ICamera _camera;
        private IPairedDevice _device;
        private MemoryStream _stream;
        private BitmapImage _bitmapImage;
        Image<Bgr, byte> observed;
        Navigator nav;
        Thread threadImageProcess, threadtakepic;
        int i = 0;
        public MainWindow()
        {
            List<byte[]> direc = new List<byte[]>();
            direc.Add(Cmd.stop); direc.Add(Cmd.stop);direc.Add(Cmd.stop);
            direc.Add(Cmd.stop); direc.Add(Cmd.stop); direc.Add(Cmd.stop);
            PrepNodes prep = PrepNodes.GetNavigator(AppDomain.CurrentDomain.BaseDirectory + "nodeimages", direc);
            Console.WriteLine(111);
            InitializeComponent();
            InitBetterTogether();
            threadImageProcess = new Thread(new ThreadStart(ImageProcess));
            threadtakepic = new Thread(new ThreadStart(ShowStream));

        }

        private async void ShowStream()
        {
            while (true)
            {
                try
                {
                    System.Drawing.Bitmap img_bmp = new System.Drawing.Bitmap(_stream, false);
                    observed = new Image<Bgr, byte>(img_bmp);

                    await Application.Current.Dispatcher.BeginInvoke(
                            System.Windows.Threading.DispatcherPriority.Background,
                            new Action(() =>
                            {
                                try
                                {
                                    //PreviewWindow.Source = ToBitmapSource(img_result);
                                    cam.Source = ToBitmapSource(observed);
                                    status.Content = nav.Status;
                                }
                                catch (Exception ex) { };
                            }));
                }
                catch (Exception e)
                { }
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
        private async void ImageProcess()
        {

            nav = new Navigator();
            while (true)
            {
                try
                {
                    System.Drawing.Bitmap img_bmp = new System.Drawing.Bitmap(_stream, false);
                    observed = new Image<Bgr, byte>(img_bmp);

                    await Application.Current.Dispatcher.BeginInvoke(
                            System.Windows.Threading.DispatcherPriority.Background,
                            new Action(() =>
                            {
                                try
                                {
                                    //PreviewWindow.Source = ToBitmapSource(img_result);
                                    cam.Source = ToBitmapSource(observed);
                                    status.Content = nav.Status;
                                }
                                catch (Exception ex) { };
                            }));

                    nav.Navigate(observed);
                    //status.Content = nav.Status;
                    if (nav.Result != null)
                    {
                        await Application.Current.Dispatcher.BeginInvoke(
                            System.Windows.Threading.DispatcherPriority.Background,
                            new Action(() =>
                            {
                                try
                                {
                                    //PreviewWindow.Source = ToBitmapSource(img_result);
                                    cam_right.Source = ToBitmapSource(nav.Result);
                                }
                                catch (Exception ex) { };
                            }));
                    }
                }
                catch (Exception e)
                { }
            }


        }

        #region BETTER_TOGETHER
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
        private void _camera_PreviewFrameAvailable(object sender, PreviewArrivedEventArgs e)
        {
            try
            {
                _stream = new MemoryStream(e.Frame.ImageStream);
                if (null == _stream)
                    return;
                else { threadImageProcess.Start(); 
                   // threadtakepic.Start();
                }
            }
            catch (Exception)
            {
                // ignored
            }
        }

        private void Window_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            observed.Save(AppDomain.CurrentDomain.BaseDirectory + "nodeimages\\" + (i++) + ".jpg");
        }
    }
}