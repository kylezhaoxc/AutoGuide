using System;
using System.Collections.Generic;
using Emgu.CV;
using Emgu.CV.Structure;

namespace Auto_Guide
{
    [Serializable]
    class RouteNode
    {
        private int _count;
        private int _index;
        public int Count { get { return _count; } }
        public int Index { get { return _index; } }
        private static RouteNode _head;
        private List<Image<Bgr, Byte>> _nodeImages=new List<Image<Bgr, byte>>();
        private List<string> _nodeDirectives=new List<string>();
        public static RouteNode GetHead()
        {
            if (_head == null) { _head = new RouteNode(); _head._count = 0;_head._index = 0; return _head; }
            return _head;
        }

        public void AddNode(Image<Bgr,Byte> img, string txt)
        {
            if (img != null && txt != null)
            {
                _nodeImages.Add(img);
                _nodeDirectives.Add(txt);
                _count++;
            }
        }
        public void GetNextNode(out Image<Bgr, Byte> image, out string txt)
        {
            image = null;txt = null;
            if (_index <=_count)
            {
                image=_nodeImages[_index];
                txt=_nodeDirectives[_index];
                _index++; 
            }
        }
    }
}
