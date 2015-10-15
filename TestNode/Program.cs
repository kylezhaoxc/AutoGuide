using System;
using Localization;
using Emgu.CV;
using Emgu.CV.Structure;

namespace TestNode
{
    class Program
    {
        
        static void Main(string[] args)
        {
            //List<byte[]> direc = new List<byte[]>();
            //direc.Add(Cmd.stop); direc.Add(Cmd.stop); direc.Add(Cmd.stop); direc.Add(Cmd.stop); direc.Add(Cmd.stop); direc.Add(Cmd.stop);
           // PrepNodes prep = PrepNodes.GetNavigator(AppDomain.CurrentDomain.BaseDirectory+"\\nodeimages",direc);

            Navigator nav = new Navigator();

            while (true)
            {
                nav.Navigate(new Image<Bgr, byte>(AppDomain.CurrentDomain.BaseDirectory + "\\nodeimages\\1-1.jpg"));
            }
            Console.Read();
        }
      
    }
}
