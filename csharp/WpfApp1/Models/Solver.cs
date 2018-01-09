using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using OpenCvSharp;
using Microsoft.ProjectOxford.Vision;
using System.Net;

namespace WpfApp1.Models
{
    class Solver
    {
        public Solver()
        {

        }

        private readonly int n = 9;
        private readonly string TempFile = "temp.jpg";

        public string SolveThePuzzle(string filename, string key, string endpoint)
        {
            try
            {
                Mat imgSource = Cv2.ImRead(filename);
                Mat img = new Mat();
                Cv2.CvtColor(imgSource, img, ColorConversionCodes.BGR2GRAY);
                Cv2.Blur(img, img, new Size(3, 3));
                Cv2.AdaptiveThreshold(img, img, 255, AdaptiveThresholdTypes.GaussianC, ThresholdTypes.Binary, 15, 15);
                SegmentPuzzle(img, out img, out Rect puzzleRect);

                SplitToDigits(img, out List<Mat> digits, out List<int> puzzle, new Size(50, 50));
                Mat digitsAll = new Mat();
                List<Mat> blkNonEmpty = new List<Mat>();
                for (int k = 0; k != puzzle.Count; ++k)
                {
                    if (puzzle[k] != 0)
                    {
                        blkNonEmpty.Add(digits[k]);
                    }
                }
                Cv2.HConcat(blkNonEmpty.ToArray(), digitsAll);
                recognizeDigits(digitsAll, key, endpoint);

                using (new Window("Image", WindowMode.Normal, digitsAll))
                {
                    Cv2.WaitKey();
                }
                return "solved";
            }
            catch (Exception e)
            {
                return e.Message;
            }

        }

        private void SegmentPuzzle(Mat img, out Mat roi, out Rect roiRect)
        {
            try
            {
                Point[][] contours;
                HierarchyIndex[] hierarchyIndexes;
                Cv2.FindContours(img, out contours, out hierarchyIndexes, RetrievalModes.Tree, ContourApproximationModes.ApproxSimple);
                if (contours.Length == 0)
                {
                    throw new Exception("No contour found in image");
                }
                double[] area = new double[contours.Length];
                for (int k = 0; k != contours.Length; ++k)
                {
                    Point[] appx = Cv2.ApproxPolyDP(contours[k], 4, true);
                    if (appx.Length != 4 || !Cv2.IsContourConvex(appx))
                    {
                        area[k] = 0;
                    }
                    else
                    {
                        area[k] = Cv2.ContourArea(appx);
                    }
                }
                int kmax = area.Select((value, index) => new { value, index }).OrderByDescending(x => x.value).First().index;
                roiRect = Cv2.BoundingRect(contours[kmax]);
                roi = img[roiRect];
            }
            catch (Exception e)
            {
                throw new Exception(e.Message);
            }
        }


        private void SplitToDigits(Mat img, out List<Mat> digits, out List<int> puzzle, Size outShape)
        {
            digits = new List<Mat>();
            puzzle = new List<int>();
            for (int k = 0; k != n * n; ++k)
            {
                int i = k / n, j = k % n;
                int x = Convert.ToInt32(Convert.ToDouble(img.Width) * j / n);
                int y = Convert.ToInt32(Convert.ToDouble(img.Height) * i / n);
                int w = Convert.ToInt32(Convert.ToDouble(img.Width) / n);
                int h = Convert.ToInt32(Convert.ToDouble(img.Height) / n);
                focusOnDigits(img[new Rect(x, y, w, h)], out Mat rst, out int d, new Size(50, 50));
                digits.Add(rst);
                puzzle.Add(d);
            }
        }

        private void focusOnDigits(Mat img, out Mat result, out int digit, Size outShape)
        {
            Cv2.FindContours(img, out Point[][] contours, out HierarchyIndex[] hind, RetrievalModes.Tree, ContourApproximationModes.ApproxSimple);
            List<Point> validContour = new List<Point>();
            for (int k = 0; k != contours.Length; ++k)
            {
                Rect roiBox = Cv2.BoundingRect(contours[k]);
                if (NormalizedDistance(roiBox, img.BoundingRect()) < 0.1)
                {
                    continue;
                }
                validContour.AddRange(contours[k]);
            }

            digit = 0;
            result = new Mat(outShape, MatType.CV_8U);
            if (validContour.Count == 0)
            {
                return;
            }
            Point[] hull = Cv2.ConvexHull(validContour);
            if (Cv2.ContourArea(hull) / (img.Width * img.Height) < 0.01)
            {
                return;
            }
            digit = -1;
            result = img[Cv2.BoundingRect(hull)];
            Cv2.Resize(result, result, outShape);
        }

        private double NormalizedDistance(Rect roi, Rect boundary)
        {
            double dx = Math.Min(roi.Left - boundary.Left, boundary.Right - roi.Right) * 1.0 / boundary.Width;
            double dy = Math.Min(roi.Top - boundary.Top, boundary.Bottom - roi.Bottom) * 1.0 / boundary.Height;
            return Math.Min(dx, dy);
        }


        private void recognizeDigits(Mat img, string key, string endpoint)
        {
            Cv2.ImWrite(TempFile, img);
            Byte[] imgStream = File.ReadAllBytes(TempFile);
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(String.Format("{0}/{1}?", endpoint, "recognizeText"));
            request.ContentType = "application/octet-stream";
            request.Headers.Add("Ocp-Apim-Subscription-Key", key);
            request.Method = "POST";
            request.ContentLength = imgStream.Length;
            Stream s = request.GetRequestStream();
            s.Write(imgStream, 0, imgStream.Length);
            HttpWebResponse response = null;
            try
            {
                response = request.GetResponse() as HttpWebResponse;
            }
            catch(Exception e)
            {
                throw new Exception(e.ToString());
            }
        }
    }
}
