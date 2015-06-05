using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Auto_Guide
{
    class QueueChecker<T>
    {
        protected Queue<T> target;
        protected int Max_Len, Acc_Len;
        public QueueChecker(int len)
        {
            Max_Len = len;
            target = new Queue<T>(len);
            Acc_Len = Max_Len /4;
        }
        public void EnQ(T element)
        {
            target.Enqueue(element);
            if (target.Count > Max_Len) target.Dequeue();
        }
       
    }
    class CenterPositionChecker : QueueChecker<System.Drawing.Point>
    {
        private int xmax;//= 420;
        private int xmin; //= 220;
        public CenterPositionChecker(int len,int max_x,int max_y) :base(len)
        {
            xmax = max_x;
            xmin = max_y;
            Acc_Len = Max_Len / 2;
        }
        public string CheckPosition()
        {
            string Direction = null;
            Direction = "N/A";
            int leftvote = 0, rightvote = 0, centervote = 0;
            foreach (System.Drawing.Point center in target)
            {
                if (center.X > xmax) rightvote++;
                if (center.X < xmin) leftvote++;
                if (center.X > xmin && center.X < xmax) centervote++;
            }
            if (leftvote > Acc_Len) return "Turn Left!";
            if (rightvote >  Acc_Len) return "Turn Right!";
            if (centervote >  Acc_Len) return "Go Straight!";

            return Direction;
        }
    }
     class StatusQueueChecker : QueueChecker<double>
    {
        public StatusQueueChecker(int len) : base(len)
        { }
        public bool CheckMatch(int areathreshold)
        {
        int positivematch = 0;
            foreach (double area in target)
            {
                positivematch += area > areathreshold ? 1 : 0;
            }
            if (positivematch < Acc_Len) return false;
            else return true;
        }
    }
}
