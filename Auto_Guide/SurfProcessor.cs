﻿using System;
using System.Diagnostics;
using System.Drawing;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Features2D;
using Emgu.CV.Structure;

namespace Auto_Guide
{
    interface IMatchFilterInterface
    {
        int CountContours(Bitmap temp);
 
        double Getarea(PointF[] pts);
    }
    class SurfProcessor : IMatchFilterInterface
    {
        public int CountContours(Bitmap temp)
        {
            var contourNumber = 0;
            var gray = new Image<Gray, byte>(temp);
            gray.ThresholdBinary(new Gray(149), new Gray(255));
            gray._Dilate(1);
            gray._Erode(1);
            Contour<Point> contours;
            using (var canny = gray.Canny(150, 50))
            {
                var stor = new MemStorage();
                contours = canny.FindContours(CHAIN_APPROX_METHOD.CV_CHAIN_APPROX_SIMPLE, RETR_TYPE.CV_RETR_CCOMP, stor);
            }
            if (contours == null) return 0;
            for (; contours!=null; contours = contours.HNext)
            {
                contourNumber++;
            }

            return contourNumber;
        }
        public  double Getarea( PointF[] pts)
        {
            var zero = new PointF(0, 0); var zerocount = 0;
            //过滤掉明显不合理的坐标点，以及全为0的点
            foreach (var points in pts)
            {
                if ( points.X < -10 || points.Y < -10) { return 0; }
                if (points == zero) zerocount++;
            }
            if (zerocount == 4) return 0;
            var homolt = pts[3]; var homort = pts[2]; var homorb = pts[1]; var homolb = pts[0];
            //海伦公式求两个三角形面积之和，即求出四边形面积
            var top = Math.Sqrt(Math.Pow((homort.X - homolt.X), 2) + Math.Pow((homort.Y - homolt.Y), 2));
            var right = Math.Sqrt(Math.Pow((homort.X - homorb.X), 2) + Math.Pow((homort.Y - homorb.Y), 2));
            var left = Math.Sqrt(Math.Pow((homolb.X - homolt.X), 2) + Math.Pow((homolb.Y - homolt.Y), 2));
            var bottom = Math.Sqrt(Math.Pow((homolb.X - homorb.X), 2) + Math.Pow((homolb.Y - homorb.Y), 2));
            var middle = Math.Sqrt(Math.Pow((homorb.X - homolt.X), 2) + Math.Pow((homorb.Y - homolt.Y), 2));
            var pT=(top+right+middle)/2;var pB=(left+bottom+middle)/2;
            //边长明显过长，返回面积为0，即不会勾画单应矩阵
            if (top > 800 || right > 800 || left > 800 || right > 800) return 0;

            var obarea = Math.Sqrt(pT * (pT - top) * (pT - right) * (pT - middle)) + Math.Sqrt(pB * (pB - left) * (pB - bottom) * (pB - middle));
            return obarea;
        }
        #region distance-estimation
        /*public double GetDist(PointF[] pts)
        {
            double dist = 0;
            double max_y = 0;
            foreach (PointF points in pts)
            {
                max_y = max_y > points.Y ? max_y : points.Y; 
            }
            if (max_y > 448) return 0;
            else
            {
                dist = RobotProperties.Hoc / Math.Tan(Math.Atan(RobotProperties.Hoc / RobotProperties.Lob) * ((2 * max_y - 448) / 896));
            }
            //item is in the upper part of the picture.
           
            return dist;
        }*/
        #endregion
        public  Image<Bgr, Byte> DrawResult(Image<Gray, Byte> modelImage, Image<Gray, byte> observedImage, out long matchTime,out double area,int minarea,out Point center)
        {
            //double estimated_dist =99999;
            center = new Point(400,224);
            area = 0;
            //modelImage.Save("D:\\temp\\modelimage.jpg");
            //observedImage.Save("D:\\temp\\observedimage.jpg");

           
            //单应矩阵
            HomographyMatrix homography = null;

            //surf算法检测器
            var surfCpu = new SURFDetector(500, false);

            //原图与实际图中的关键点

            Matrix<byte> mask;

            //knn匹配的系数
            var k = 2;
            //滤波系数
            var uniquenessThreshold = 0.8;

             
            //从标记图中，提取surf特征点与描述子
                var modelKeyPoints = surfCpu.DetectKeyPointsRaw(modelImage, null);
                var modelDescriptors = surfCpu.ComputeDescriptorsRaw(modelImage, null, modelKeyPoints);

                var watch = Stopwatch.StartNew();

                // 从实际图片提取surf特征点与描述子
                var observedKeyPoints = surfCpu.DetectKeyPointsRaw(observedImage, null);
                var observedDescriptors = surfCpu.ComputeDescriptorsRaw(observedImage, null, observedKeyPoints);
                if (observedDescriptors == null) {
                    watch.Stop(); matchTime = watch.ElapsedMilliseconds; //dst = estimated_dist;
                    return null; }
            
            //使用BF匹配算法，匹配特征向量    
            //var bfmatcher = new BruteForceMatcher<float>(DistanceType.L2);
            //bfmatcher.Add(modelDescriptors);
                var indices = new Matrix<int>(observedDescriptors.Rows, k);
            var flannMatcher= new Emgu.CV.Flann.Index(modelDescriptors,4);
            //通过特征向量筛选匹配对
                using (var dist = new Matrix<float>(observedDescriptors.Rows, k))
                {
                //最近邻2点特征向量匹配
                //bfmatcher.KnnMatch(observedDescriptors, indices, dist, k, null);
                flannMatcher.KnnSearch(observedDescriptors,indices,dist,k,24);
                    //匹配成功的，将特征点存入mask
                    mask = new Matrix<byte>(dist.Rows, 1);
                    mask.SetValue(255);
                    //通过滤波系数，过滤非特征点，剩余特征点存入mask
                    Features2DToolbox.VoteForUniqueness(dist, uniquenessThreshold, mask);
                }

                var nonZeroCount = CvInvoke.cvCountNonZero(mask);
                if (nonZeroCount >= 10)
                {
                    //过滤旋转与变形系数异常的特征点，剩余存入mask
                    nonZeroCount = Features2DToolbox.VoteForSizeAndOrientation(modelKeyPoints, observedKeyPoints, indices, mask, 1.5, 20);
                    if (nonZeroCount >= 10)
                        //使用剩余特征点，构建单应矩阵
                        homography = Features2DToolbox.GetHomographyMatrixFromMatchedFeatures(modelKeyPoints, observedKeyPoints, indices, mask,2);
                }

                watch.Stop();
           // }

            //画出匹配的特征点
            //Image<Bgr, Byte> result = Features2DToolbox.DrawMatches(modelImage, modelKeyPoints, observedImage, observedKeyPoints,indices, new Bgr(0, 0, 255), new Bgr(0, 255, 0), mask, Features2DToolbox.KeypointDrawType.DEFAULT);
           // result.Save("D:\\temp\\matchedpoints.jpg");
            observedImage.ToBitmap();
                var result = Features2DToolbox.DrawMatches(modelImage, modelKeyPoints, observedImage, observedKeyPoints, indices, new Bgr(0, 0, 255), new Bgr(0, 255, 0), mask, Features2DToolbox.KeypointDrawType.DEFAULT);

            #region draw the projected region on the Image
            //画出单应矩阵
                if (homography != null)
                {
                    var rect = modelImage.ROI;
                    /*PointF[] pts = new PointF[] { 
               new PointF(rect.Left, rect.Bottom),
               new PointF(rect.Right, rect.Bottom),
               new PointF(rect.Right, rect.Top),
               new PointF(rect.Left, rect.Top)
                    };*/
                var pts = new[] {
               new PointF(rect.Left+(rect.Right-rect.Left)/5, rect.Bottom-(rect.Bottom-rect.Top)/5),
               new PointF(rect.Right-(rect.Right-rect.Left)/5, rect.Bottom-(rect.Bottom-rect.Top)/5),
               new PointF(rect.Right-(rect.Right-rect.Left)/5, rect.Top+(rect.Bottom-rect.Top)/5),
               new PointF(rect.Left+(rect.Right-rect.Left)/5, rect.Top+(rect.Bottom-rect.Top)/5)
                    };
                //根据整个图片的旋转、变形情况，计算出原图中四个顶点转换后的坐标，并画出四边形
                homography.ProjectPoints(pts);
                    area = Getarea(pts);
                double xsum=0;double ysum=0;
                    foreach(var point in pts)
                    {
                        xsum+=point.X;ysum+=point.Y;
                    }
                    center = new Point(Convert.ToInt32(xsum / 4), Convert.ToInt32(ysum / 4));
                    if (area > minarea)
                    {
                        var temp = new Image<Bgr, Byte>(result.Width, result.Height);
                        temp.DrawPolyline(Array.ConvertAll(pts, Point.Round), true, new Bgr(Color.Red), 5);
                    //estimated_dist = GetDist(pts);
                   
                    var a = CountContours(temp.ToBitmap());
                        if (a == 2 )
                    {
                        result.DrawPolyline(Array.ConvertAll(pts, Point.Round), true, new Bgr(Color.Red), 5);
                        //result.Save("D:\\temp\\" + estimated_dist.ToString() + ".jpg");
                    }
                        else
                    {
                        matchTime = 0;
                        area = 0;//dst = estimated_dist;
                        return result;
                    }
                    }
                }
                else area = 0; 
            #endregion

            matchTime = watch.ElapsedMilliseconds;
            //dst = estimated_dist;
            return result;
        }
   }
}

