﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Features2D;
using Emgu.CV.Structure;
using Emgu.CV.Util;

namespace Auto_Guide
{
    class SpecificItemMatcher
    {
    }
    interface MatchFilter_interface
    {
        int CountContours(System.Drawing.Bitmap temp);
 
        double Getarea(PointF[] pts);
    }
    class SurfProcessor : MatchFilter_interface
    {
        public int CountContours(System.Drawing.Bitmap temp)
        {
            int ContourNumber = 0;
            Image<Gray, Byte> gray = new Image<Gray, byte>(temp);
            gray.ThresholdBinary(new Gray(149), new Gray(255));
            gray._Dilate(1);
            gray._Erode(1);
            Image<Gray, Byte> canny = gray.Canny(150, 50);
            Image<Bgr, Byte> clr = new Image<Bgr, byte>(canny.Width, canny.Height);
            MemStorage stor = new MemStorage();
            Contour<System.Drawing.Point> contours = canny.FindContours(
                Emgu.CV.CvEnum.CHAIN_APPROX_METHOD.CV_CHAIN_APPROX_SIMPLE,
                Emgu.CV.CvEnum.RETR_TYPE.CV_RETR_CCOMP, stor);
            if (contours == null) return 0;
            for (; contours!=null; contours = contours.HNext)
            {
                ContourNumber++;
            }

            return ContourNumber;
        }
        public  double Getarea( PointF[] pts)
        {
            PointF homolt, homort, homorb, homolb;
            PointF zero = new PointF(0, 0); int zerocount = 0;
            //过滤掉明显不合理的坐标点，以及全为0的点
            foreach (PointF points in pts)
            {
                if ( points.X < -10 || points.Y < -10) { return 0; }
                if (points == zero) zerocount++;
            }
            if (zerocount == 4) return 0;
            homolt = pts[3]; homort = pts[2]; homorb = pts[1]; homolb = pts[0];
            //海伦公式求两个三角形面积之和，即求出四边形面积
            double top = Math.Sqrt(Math.Pow((homort.X - homolt.X), 2) + Math.Pow((homort.Y - homolt.Y), 2));
            double right = Math.Sqrt(Math.Pow((homort.X - homorb.X), 2) + Math.Pow((homort.Y - homorb.Y), 2));
            double left = Math.Sqrt(Math.Pow((homolb.X - homolt.X), 2) + Math.Pow((homolb.Y - homolt.Y), 2));
            double bottom = Math.Sqrt(Math.Pow((homolb.X - homorb.X), 2) + Math.Pow((homolb.Y - homorb.Y), 2));
            double middle = Math.Sqrt(Math.Pow((homorb.X - homolt.X), 2) + Math.Pow((homorb.Y - homolt.Y), 2));
            double p_t=(top+right+middle)/2;double p_b=(left+bottom+middle)/2;
            //边长明显过长，返回面积为0，即不会勾画单应矩阵
            if (top > 800 || right > 800 || left > 800 || right > 800) return 0;

            double obarea = Math.Sqrt(p_t * (p_t - top) * (p_t - right) * (p_t - middle)) + Math.Sqrt(p_b * (p_b - left) * (p_b - bottom) * (p_b - middle));
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
            Stopwatch watch;
            area = 0;
            //modelImage.Save("D:\\temp\\modelimage.jpg");
            //observedImage.Save("D:\\temp\\observedimage.jpg");

           
            //单应矩阵
            HomographyMatrix homography = null;

            //surf算法检测器
            SURFDetector surfCPU = new SURFDetector(500, false);

            //原图与实际图中的关键点
            VectorOfKeyPoint modelKeyPoints;
            VectorOfKeyPoint observedKeyPoints;

            Matrix<int> indices;
            Matrix<byte> mask;

            //knn匹配的系数
            int k = 2;
            //滤波系数
            double uniquenessThreshold = 0.8;

             
            //从标记图中，提取surf特征点与描述子
                modelKeyPoints = surfCPU.DetectKeyPointsRaw(modelImage, null);
                Matrix<float> modelDescriptors = surfCPU.ComputeDescriptorsRaw(modelImage, null, modelKeyPoints);

                watch = Stopwatch.StartNew();

                // 从实际图片提取surf特征点与描述子
                observedKeyPoints = surfCPU.DetectKeyPointsRaw(observedImage, null);
                Matrix<float> observedDescriptors = surfCPU.ComputeDescriptorsRaw(observedImage, null, observedKeyPoints);
                if (observedDescriptors == null) {
                    watch.Stop(); matchTime = watch.ElapsedMilliseconds; //dst = estimated_dist;
                    return null; }
            
            //使用BF匹配算法，匹配特征向量    
            BruteForceMatcher<float> matcher = new BruteForceMatcher<float>(DistanceType.L2);
                matcher.Add(modelDescriptors);
                indices = new Matrix<int>(observedDescriptors.Rows, k);
            //通过特征向量筛选匹配对
                using (Matrix<float> dist = new Matrix<float>(observedDescriptors.Rows, k))
                {
                    //最近邻2点特征向量匹配
                    matcher.KnnMatch(observedDescriptors, indices, dist, k, null);
                    //匹配成功的，将特征点存入mask
                    mask = new Matrix<byte>(dist.Rows, 1);
                    mask.SetValue(255);
                    //通过滤波系数，过滤非特征点，剩余特征点存入mask
                    Features2DToolbox.VoteForUniqueness(dist, uniquenessThreshold, mask);
                }

                int nonZeroCount = CvInvoke.cvCountNonZero(mask);
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
                Image<Bgr, byte> result = null;
                System.Drawing.Bitmap bm = observedImage.ToBitmap();
                result = new Image<Bgr, byte>(bm);
            #region draw the projected region on the Image
            //画出单应矩阵
                if (homography != null)
                {
                    Rectangle rect = modelImage.ROI;
                    /*PointF[] pts = new PointF[] { 
               new PointF(rect.Left, rect.Bottom),
               new PointF(rect.Right, rect.Bottom),
               new PointF(rect.Right, rect.Top),
               new PointF(rect.Left, rect.Top)
                    };*/
                PointF[] pts = new PointF[] {
               new PointF(rect.Left+(rect.Right-rect.Left)/5, rect.Bottom-(rect.Bottom-rect.Top)/5),
               new PointF(rect.Right-(rect.Right-rect.Left)/5, rect.Bottom-(rect.Bottom-rect.Top)/5),
               new PointF(rect.Right-(rect.Right-rect.Left)/5, rect.Top+(rect.Bottom-rect.Top)/5),
               new PointF(rect.Left+(rect.Right-rect.Left)/5, rect.Top+(rect.Bottom-rect.Top)/5)
                    };
                //根据整个图片的旋转、变形情况，计算出原图中四个顶点转换后的坐标，并画出四边形
                homography.ProjectPoints(pts);
                    area = Getarea(pts);
                double xsum=0;double ysum=0;
                    foreach(PointF point in pts)
                    {
                        xsum+=point.X;ysum+=point.Y;
                    }
                    center = new Point(Convert.ToInt32(xsum / 4), Convert.ToInt32(ysum / 4));
                    if (area > minarea)
                    {
                        Image<Bgr, byte> temp = new Image<Bgr, Byte>(result.Width, result.Height);
                        temp.DrawPolyline(Array.ConvertAll<PointF, Point>(pts, Point.Round), true, new Bgr(Color.Red), 5);
                    //estimated_dist = GetDist(pts);
                   
                    int a = CountContours(temp.ToBitmap());
                        if (a == 2 )
                    {
                        result.DrawPolyline(Array.ConvertAll<PointF, Point>(pts, Point.Round), true, new Bgr(Color.Red), 5);
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

