using System;
using System.Collections.Generic;
using Emgu.CV;
using Emgu.CV.Structure;

namespace Auto_Guide
{
    [Serializable]
    class RouteNode :IDisposable
    {
        public int Count { get; private set; }

        public int Index { get;  set; }

        private static RouteNode _head;
        private List<Image<Bgr, byte>> _nodeImages=new List<Image<Bgr, byte>>();
        private List<string> _nodeDirectives=new List<string>();
        public static RouteNode GetHead()
        {
            if (_head == null) {
                _head = new RouteNode
                {
                    Count = 0,
                    Index = 0
                };
                return _head; }
            return _head;
        }

        public void AddNode(Image<Bgr,byte> img, string txt)
        {
            if (img != null && txt != null)
            {
                _nodeImages.Add(img);
                _nodeDirectives.Add(txt);
                Count++;
            }
        }
        public void GetNextNode(out Image<Bgr, byte> image, out string txt)
        {
            image = null;txt = null;
            if (Index <Count)
            {
                image=_nodeImages[Index];
                txt=_nodeDirectives[Index];
                Index++; 
            }
        }

        public void Dispose()
        {
            
        }
    }
}
