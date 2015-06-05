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

namespace Match_Surrounding
{
    class RouteNode
    {
        private int count;
        private int index;
        private static RouteNode head;
        private List<Image<Bgr, Byte>> NodeImages;
        private List<string> NodeDirectives;
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
        public void GetNextNode(out Image<Bgr, Byte> img, out string txt)
        {
            img = null;txt = null;
            if (index <= count)
            {
                img = NodeImages[index];
                txt = NodeDirectives[index];
                index++; return;
            }
            else return;
        }
    }
}
