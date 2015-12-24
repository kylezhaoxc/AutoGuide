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
    public partial class Navigate : Window
    {
        #region define vars
        #region bettertogether and ui vars
        //for a faster laptop or pc, change this param smaller.
        private int PC_Speed = 2000;
        private ICamera _camera;
        private IPairedDevice _device;
        private MemoryStream _record_stream;
        private BitmapImage _bitmapImage;
        App _thisApp = (App)Application.Current;

        public delegate void PreviewFrameHandler(object sender);
        public event PreviewFrameHandler pevent;

        #endregion

        #region navigate vars
        Image<Bgr, byte> observed;
        Navigator nav;
        Thread threadImageProcess, threadtakepic, threadCmd;
        string LobbyStat;
        int Surf_Wait_Count = 0, Bow_Wait_Count = 0,Total_Wait_Count=0;
        int PhotoName = 0;
        private readonly int SURFWAITMAX = 3, BOWWAITMAX =5,TOTALWAITMAX=100;
        bool bow_timeout = false, surf_timeout = false,total_timeout=false,clear_buffer=false;
        #endregion
        #endregion

        #region constructor and close event
        public Navigate(ICamera __camera)
        {
            
            InitializeComponent();
            InitBetterTogether();
            InitDevice();
            this._camera = __camera;
            threadImageProcess = new Thread(new ThreadStart(ImageProcess));
            threadtakepic = new Thread(new ThreadStart(ShowStream));
            threadCmd = new Thread(new ThreadStart(AnalyzeStatus));

        }
        private void Window_Closed(object sender, EventArgs e)
        {
            pevent(this);
        }
        #endregion

        #region tools and bettertogether
        private void clearcounter()
        {
            Bow_Wait_Count = 0; Surf_Wait_Count = 0; Total_Wait_Count = 0;
        }
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
        [System.Runtime.InteropServices.DllImport("gdi32")]
        private static extern int DeleteObject(IntPtr o);
        private async void InitDevice()
        {
            //_camera = await _device.CameraManager.OpenAsync(
            //    CameraLocation.Back,
            //    _device.CameraManager.GetAvailableCaptureResolutions(
            //        CameraLocation.Back)[0]
            //    );
            ell_flag.Fill = System.Windows.Media.Brushes.Green;
            ell_flag.Stroke = System.Windows.Media.Brushes.Green;
            // Please notice the preview resolution is different to capture resolution
            await _camera.SetPreviewResolutionAsync(new System.Windows.Size(800, 448));
            _camera.PreviewFrameAvailable += _camera_PreviewFrameAvailable;
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
        private void _camera_PreviewFrameAvailable(object sender, PreviewArrivedEventArgs e)
        {
            try
            {
                _record_stream = new MemoryStream(e.Frame.ImageStream);
                if (null == _record_stream)
                    return;
                else
                {
                    threadtakepic.Start();
                }
            }
            catch (Exception)
            {
                // ignored
            }
        }
        #endregion

        #region threads

        private async void AnalyzeStatus()
        {
            //Thread.Sleep(7000);
            //total status contains these:
            //[Wait]+[Bow]or[Surf]
            //ReachNode
            //Nav_+[direction]

            //for wait type, enable a counter.
            while (true)
            {
                if (nav == null) continue;
                await _thisApp._MyRobot.SendCommand(_thisApp.btconn, Cmd.SpeedDown);
               // byte[] currentNodeDirective = new byte[5];
                byte[] currentNodeDirective = nav.currentCmd;
                switch (LobbyStat)
                {
                    case "WaitBow":
                        Bow_Wait_Count++;
                        Total_Wait_Count++;
                        //Thread.Sleep(1000);
                        break;
                    case "WaitSurf":
                        Surf_Wait_Count++;
                        Total_Wait_Count++;
                        break;
                    case "ReachNode":
                    case "Finished":
                        clearcounter();
                        await _thisApp._MyRobot.SendCommand(_thisApp.btconn, Cmd.stop);
                        await Task.Delay(PC_Speed);
                        await Task.Yield();
                        clear_buffer = true;
                        break;
                    default:
                        if (LobbyStat == "Nav_left")
                        {
                            clearcounter();
                            await _thisApp._MyRobot.SendCommand(_thisApp.btconn, Cmd.turn_left);
                            await _thisApp._MyRobot.SendCommand(_thisApp.btconn, Cmd.stop);
                            await Task.Delay(PC_Speed);
                        }
                        if (LobbyStat == "Nav_right")
                        {
                            clearcounter();
                            await _thisApp._MyRobot.SendCommand(_thisApp.btconn, Cmd.turn_right);
                            await _thisApp._MyRobot.SendCommand(_thisApp.btconn, Cmd.stop);
                            await Task.Delay(PC_Speed);
                        }
                        if (LobbyStat == "Nav_go")
                        {
                            clearcounter();
                            await _thisApp._MyRobot.SendCommand(_thisApp.btconn, Cmd.Go);
                            await Task.Delay(PC_Speed/2);
                            await _thisApp._MyRobot.SendCommand(_thisApp.btconn, Cmd.stop);
                            await Task.Delay(PC_Speed);
                        }
                        if (LobbyStat == "Nav_wait")
                        {
                            Surf_Wait_Count++;
                            await _thisApp._MyRobot.SendCommand(_thisApp.btconn, Cmd.stop);
                            await Task.Delay(PC_Speed);
                        }


                        await Task.Yield();
                        break;
                }
                if (Total_Wait_Count >= TOTALWAITMAX)
                {
                    total_timeout = true;
                    if (nav.currentCmd == Cmd.Go)
                    {
                        await _thisApp._MyRobot.SendCommand(_thisApp.btconn, nav.currentCmd);
                        await Task.Delay(PC_Speed);
                        await _thisApp._MyRobot.SendCommand(_thisApp.btconn, Cmd.stop);
                    }
                    else 
                    {
                        await _thisApp._MyRobot.SendCommand(_thisApp.btconn, nav.currentCmd);
                        await _thisApp._MyRobot.SendCommand(_thisApp.btconn, Cmd.stop);
                    }
                    await Task.Delay(PC_Speed);
                    await Task.Yield();
                    Total_Wait_Count = 0;
                }
                else if (Surf_Wait_Count >= SURFWAITMAX)
                {
                    surf_timeout = true;
                    if (nav.currentCmd == Cmd.Go)
                    {
                        await _thisApp._MyRobot.SendCommand(_thisApp.btconn, nav.currentCmd);
                        await Task.Delay(PC_Speed/2);
                        await _thisApp._MyRobot.SendCommand(_thisApp.btconn, Cmd.stop);
                    }
                    else
                    {
                        await _thisApp._MyRobot.SendCommand(_thisApp.btconn, nav.currentCmd);
                        await _thisApp._MyRobot.SendCommand(_thisApp.btconn, Cmd.stop);
                    }
                    await Task.Delay(PC_Speed);
                    await Task.Yield();
                    Surf_Wait_Count = 0;
                }
                else if (Bow_Wait_Count >= BOWWAITMAX)
                {
                    bow_timeout = true;
                    if (nav.currentCmd == Cmd.Go)
                    {
                        await _thisApp._MyRobot.SendCommand(_thisApp.btconn, nav.currentCmd);
                        await Task.Delay(PC_Speed);
                        await _thisApp._MyRobot.SendCommand(_thisApp.btconn, Cmd.stop);
                    }
                    else
                    {
                        await _thisApp._MyRobot.SendCommand(_thisApp.btconn, nav.currentCmd);
                        await _thisApp._MyRobot.SendCommand(_thisApp.btconn, Cmd.stop);
                    }
                    await Task.Delay(PC_Speed);
                    await Task.Yield();
                    Bow_Wait_Count = 0;
                }
                Thread.Sleep(PC_Speed/4);
            }
        }
        private async void ImageProcess()
        {

            nav = new Navigator();
            while (true)
            {
                if (observed == null) continue;
                try
                {
                    if (surf_timeout) { nav.BackToBow(); surf_timeout = false; }
                    if (bow_timeout) { nav.BackToBowZero(); bow_timeout = false; }
                    if (total_timeout) { nav.BackToBowZero(); total_timeout = false; }
                    if (clear_buffer) { nav.ClrBuf(); clear_buffer = false; }
                   
                    nav.Navigate(observed);
                    LobbyStat = nav.Status;

                    await Application.Current.Dispatcher.BeginInvoke(
                            System.Windows.Threading.DispatcherPriority.Background,
                            new Action(() =>
                            {
                                try
                                {
                                    cam.Source = ToBitmapSource(observed);
                                    status.Content = LobbyStat;
                                    node.Content = "Node\t"+nav._currentNodeIndex+"/"+(nav._nodeCount-1);
                                    if (nav.Result != null) cam_right.Source = ToBitmapSource(nav.Result);
                                    else cam_right.Source = null;
                                }
                                catch (Exception ex) { };
                            }));



                    //await Application.Current.Dispatcher.BeginInvoke(
                    //    System.Windows.Threading.DispatcherPriority.Background,
                    //    new Action(() =>
                    //    {
                    //        try
                    //        {
                    //            if (nav.Result != null) cam_right.Source = ToBitmapSource(nav.Result);
                    //            else cam_right.Source = null;
                    //        }
                    //        catch (Exception ex) { };
                    //    }));

                }
                catch (Exception e)
                {
                    continue;
                }
            }


        }
        private async void ShowStream()
        {
            while (true)
            {
                if (nav == null) continue;
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
                                    status.Content = nav.Status;
                                    progress.Value = (double)nav._currentNodeIndex / (nav._nodeCount - 1);
                                }
                                catch (Exception ex) { };
                            }));
                }
                catch (Exception e)
                { }
            }
        }
        #endregion

     
      
    }
}