using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Emgu.CV;
using Emgu.CV.Structure;
using Localization;
namespace TestNode
{
    class Program
    {
        
        static void Main(string[] args)
        {
            Navigator nav = Navigator.GetNavigator(AppDomain.CurrentDomain.BaseDirectory+"\\nodeimages");
            Console.Read();
        }
      
    }
}
