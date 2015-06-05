using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Features2D;
using Emgu.CV.Structure;
using Emgu.CV.Util;

namespace Auto_Guide
{
    [Serializable]
    class RouteNode
    {
        private int count;
        private int index;
        public int Count { get { return count; } }
        public int Index { get { return index; } }
        private static RouteNode head;
        private List<Image<Bgr, Byte>> NodeImages=new List<Image<Bgr, byte>>();
        private List<string> NodeDirectives=new List<string>();
        public static RouteNode GetHead()
        {
            if (head == null) { head = new RouteNode(); head.count = 0;head.index = 0; return head; }
            else return head;
        }
        public void AddNode(Image<Bgr,Byte> img, string txt)
        {
            if (img != null && txt != null)
            {
                NodeImages.Add(img);
                NodeDirectives.Add(txt);
                count++;
            }
        }
        public void GetNextNode(out Image<Bgr, Byte> image, out string txt)
        {
            image = null;txt = null;
            if (index <=count)
            {
                image=NodeImages[index];
                txt=NodeDirectives[index];
                index++; 
            }
        }
    }
}
