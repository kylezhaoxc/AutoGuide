using System;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using Emgu.CV;
using System.Windows;
using System.Windows.Media;
using Emgu.CV.CvEnum;
using Emgu.CV.Features2D;
using Emgu.CV.Structure;
using Emgu.CV.Util;

namespace Match_Surrounding
{
    class UIHandler
    {
        public static  void show_Image(System.Windows.Controls.Image DEST_UI_TO_SHOW, Image<Bgr, Byte> IMAGE_TO_DISPLAY)
        {
            if (IMAGE_TO_DISPLAY != null)
                DEST_UI_TO_SHOW.Source = ToBitmapSource(IMAGE_TO_DISPLAY.ToBitmap());
        }
        public static System.Drawing.Bitmap bmimg2bitmap(System.Windows.Media.Imaging.BitmapImage img)
        {
            using (MemoryStream outstream = new MemoryStream())
            {
                System.Windows.Media.Imaging.BitmapEncoder enc = new System.Windows.Media.Imaging.BmpBitmapEncoder();
                enc.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(img));
                enc.Save(outstream);
                System.Drawing.Bitmap bitmap = new System.Drawing.Bitmap(outstream);
                return new Bitmap(bitmap);
            }
        }
        [System.Runtime.InteropServices.DllImport("gdi32")]
        private  static extern int DeleteObject(IntPtr o);
        public static ImageSource ToBitmapSource(System.Drawing.Bitmap image)
        {
            IntPtr ptr = image.GetHbitmap();

            ImageSource bs = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
                ptr,
                IntPtr.Zero,
                Int32Rect.Empty,
                System.Windows.Media.Imaging.BitmapSizeOptions.FromEmptyOptions());

            DeleteObject(ptr);

            return bs;
        }
        public static void TellDirection(System.Windows.Controls.Image DEST_IMG_TO_SHOW,System.Windows.Controls.Label DEST_TXT_TO_SHOW,string DIRECTION_TXT)
        {
            switch (DIRECTION_TXT)
            {
                case "Turn Left!":
                    UIHandler.show_Image(DEST_IMG_TO_SHOW, new Image<Bgr, byte>(AppDomain.CurrentDomain.BaseDirectory + "\\images\\left.jpg"));
                    DEST_TXT_TO_SHOW.Content = "Turn Left!";
                    break;
                case "Turn Right!":
                    UIHandler.show_Image(DEST_IMG_TO_SHOW, new Image<Bgr, byte>(AppDomain.CurrentDomain.BaseDirectory + "\\images\\right.jpg"));
                    DEST_TXT_TO_SHOW.Content = "Turn Right!";
                    break;
                case "Go Straight!":
                    UIHandler.show_Image(DEST_IMG_TO_SHOW, new Image<Bgr, byte>(AppDomain.CurrentDomain.BaseDirectory + "\\images\\straight.jpg"));
                    DEST_TXT_TO_SHOW.Content = "Go Straight!";
                    break;
                default: break;
            }
        }
    }
}
