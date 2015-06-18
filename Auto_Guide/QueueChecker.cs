using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace Auto_Guide
{
    internal class QueueChecker<T>
    {
        protected int MaxLen, AccLen;
        protected Queue<T> Target;

        public QueueChecker(int len)
        {
            MaxLen = len;
            Target = new Queue<T>(len);
            AccLen = MaxLen/4;
        }

        public void EnQ(T element)
        {
            Target.Enqueue(element);
            if (Target.Count > MaxLen) Target.Dequeue();
        }
    }

    internal class CenterPositionChecker : QueueChecker<Point>
    {
        private readonly int _xmax; //= 420;
        private readonly int _xmin; //= 220;

        public CenterPositionChecker(int len, int maxX, int maxY) : base(len)
        {
            _xmax = maxX;
            _xmin = maxY;
            AccLen = MaxLen/2;
        }

        public string CheckPosition()
        {
            const string direction = "N/A";
            int leftvote = 0, rightvote = 0, centervote = 0;
            foreach (var center in Target)
            {
                if (center.X > _xmax) rightvote++;
                if (center.X < _xmin) leftvote++;
                if (center.X > _xmin && center.X < _xmax) centervote++;
            }
            if (leftvote > AccLen) return "Turn Left!";
            if (rightvote > AccLen) return "Turn Right!";
            if (centervote > AccLen) return "Go Straight!";

            return direction;
        }
    }

    internal class StatusQueueChecker : QueueChecker<double>
    {
        public StatusQueueChecker(int len) : base(len)
        {
        }

        public bool CheckMatch(int areathreshold)
        {
            var positivematch = Target.Sum(area => area > areathreshold ? 1 : 0);
            return positivematch >= AccLen;
        }
    }
}