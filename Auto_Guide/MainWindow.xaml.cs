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
    public partial class MainWindow : Window
    {
        private ICamera _camera;
        private IPairedDevice _device;
        private MemoryStream _stream;
        IBluetooth _bluetoothManager;
        App _thisApp = (App)Application.Current;
        Image<Bgr, byte> observed;
        Navigator nav;
        Thread threadImageProcess, threadtakepic, threadCmd;
        string LobbyStat;
        int Surf_Wait_Count = 0, Bow_Wait_Count = 0,Total_Wait_Count=0;
        int PhotoName = 0;
        private readonly int SURFWAITMAX = 5, BOWWAITMAX = 3,TOTALWAITMAX=10;
        bool bow_timeout = false, surf_timeout = false,total_timeout=false;
        public MainWindow()
        {
            #region FirstTimeRun
            List<byte[]> direc = new List<byte[]>();
            direc.Add(Cmd.stop); direc.Add(Cmd.turn_left);direc.Add(Cmd.turn_right);
            direc.Add(Cmd.stop);
            PrepNodes prep = PrepNodes.GetNavigator(AppDomain.CurrentDomain.BaseDirectory + "nodeimages", direc);
            #endregion
            InitializeComponent();
            InitBetterTogether();
           
            threadImageProcess = new Thread(new ThreadStart(ImageProcess));
            //threadtakepic = new Thread(new ThreadStart(ShowStream));
            threadCmd = new Thread(new ThreadStart(AnalyzeStatus));

        }
        private void clearcounter()
        {
            Bow_Wait_Count = 0;Surf_Wait_Count = 0;Total_Wait_Count = 0;
        }
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
                await _thisApp._MyRobot.SendCommand(_thisApp.btconn, Cmd.SpeedDown);
                byte[] currentNodeDirective = nav.currentCmd;
                switch (LobbyStat)
                {
                    case "WaitBow":
                        Bow_Wait_Count++;
                        await _thisApp._MyRobot.SendCommand(_thisApp.btconn, nav.currentCmd);
                        await _thisApp._MyRobot.SendCommand(_thisApp.btconn, Cmd.stop);
                        await Task.Delay(2000);
                        await Task.Yield();
                       
                        //Thread.Sleep(1000);
                        break;
                    case "WaitSurf":
                        Surf_Wait_Count++;
                        await _thisApp._MyRobot.SendCommand(_thisApp.btconn, nav.currentCmd);
                        await _thisApp._MyRobot.SendCommand(_thisApp.btconn, Cmd.stop);
                        await Task.Delay(2000);
                        await Task.Yield();
                       
                        break;
                    case "ReachNode":
                        clearcounter();
                        await _thisApp._MyRobot.SendCommand(_thisApp.btconn, Cmd.stop);

                        break;
                    default:
                        if (LobbyStat == "Nav_left")
                        {
                            await _thisApp._MyRobot.SendCommand(_thisApp.btconn, Cmd.turn_left);
                            await _thisApp._MyRobot.SendCommand(_thisApp.btconn, Cmd.stop);
                            
                        }
                        if (LobbyStat == "Nav_right")
                        { await _thisApp._MyRobot.SendCommand(_thisApp.btconn,  Cmd.turn_right);
                            await _thisApp._MyRobot.SendCommand(_thisApp.btconn, Cmd.stop);
                            

                        }
                        if (LobbyStat == "Nav_go")
                        { await _thisApp._MyRobot.SendCommand(_thisApp.btconn, Cmd.Go);
                            await Task.Delay(1000);
                            await _thisApp._MyRobot.SendCommand(_thisApp.btconn, Cmd.stop);
                           

                        }
                        if (LobbyStat == "Nav_wait")
                        { await _thisApp._MyRobot.SendCommand(_thisApp.btconn, Cmd.stop); await Task.Delay(500);
                          
                        }

                        clearcounter();
                        await Task.Yield();
                        break;
                }
                if (Total_Wait_Count >= TOTALWAITMAX)
                {
                    total_timeout = true;
                    Total_Wait_Count = 0;
                }
                else if (Surf_Wait_Count >= SURFWAITMAX)
                {
                    surf_timeout = true;
                   
                    Surf_Wait_Count = 0;
                }
                else if (Bow_Wait_Count >= BOWWAITMAX)
                {
                    bow_timeout = true;
                    
                    Bow_Wait_Count = 0;
                }
                //Thread.Sleep(1000);
            }
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
                    if (surf_timeout) { nav.BackToBow(); surf_timeout = false; }
                    if (bow_timeout) { nav.BackToBowZero(); bow_timeout = false; }
                    if (total_timeout) { nav.BackToBowZero(); total_timeout = false; }
                    System.Drawing.Bitmap img_bmp = new System.Drawing.Bitmap(_stream, false);
                    observed = new Image<Bgr, byte>(img_bmp);
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
                                    node.Content = nav._currentNodeIndex;
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
        void OnDeviceDisconnected(object sender, DeviceDisconnectedArgs e)
        {
            //StatusMessage.Visibility = Visibility.Visible;
            _camera.Dispose();
            _camera = null;
            //_controller = null;
            _bluetoothManager = null;
            _device.DeviceDisconnected -= OnDeviceDisconnected;
            _device = null;
            //_controller = null;
            _thisApp.btconn = null;
        }
        private async void OnDeviceConnected(IPairedDevice pairedDevice)
        {
            //StatusMessage.Visibility = Visibility.Collapsed;

            _device = pairedDevice;
            _bluetoothManager = _device.Bluetooth;
            _device.DeviceDisconnected += OnDeviceDisconnected;

            // Tell the camera object the 
            // resolution we want for the incoming video.
            // Here we use the 1st available resolution
            _camera = await _device.CameraManager.OpenAsync(
                    CameraLocation.Back,
                    _device.CameraManager.GetAvailableCaptureResolutions(
                    CameraLocation.Back)[0]
                    );
            ell_flag.Fill = Brushes.Yellow;
            // Please notice the preview resolution is different to capture resolution
            //await _camera.SetPreviewResolutionAsync(new System.Windows.Size(800, 448));
            await _camera.SetPreviewResolutionAsync(new System.Windows.Size(800, 448));


            if (_bluetoothManager != null)
            {
                // this.ConTip.Text = "Connection State: " + this.BlueToothName.Text;
                //_thisApp.btconn = await _bluetoothManager.ConnectAsync("HC-06", 1);
                _thisApp.btconn = await _bluetoothManager.ConnectAsync("Robot-II", 1);
                //_thisApp.btconn = await _bluetoothManager.ConnectAsync("TEST-BT", 1);
                //_controller = new HitchHikerController(new HitchHikerControllerAdapter(_thisApp.btconn));
                ell_flag.Fill = Brushes.Green;
                //_controller.SetSpeedCommand(50);


                _camera.PreviewFrameAvailable += _camera_PreviewFrameAvailable;
               threadImageProcess.Start();
                threadCmd.Start();
            }
            else
            {
                //_camera.PreviewFrameAvailable += _camera_PreviewFrameAvailable;
                //_controller = null;
                ell_flag.Fill = Brushes.Red;
            }
        }
        #endregion
        private void _camera_PreviewFrameAvailable(object sender, PreviewArrivedEventArgs e)
        {
            try
            {
                _stream = new MemoryStream(e.Frame.ImageStream);
                if (null == _stream)
                    return;
                else { 
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
            observed.Save(AppDomain.CurrentDomain.BaseDirectory + "nodeimages\\" + (PhotoName++) + ".jpg");
        }
    }
}