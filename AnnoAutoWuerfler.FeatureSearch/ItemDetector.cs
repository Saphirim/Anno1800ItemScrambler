using OpenCvSharp;
using OpenCvSharp.Extensions;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace AnnoAutoWuerfler.FeatureSearch
{
    public class ItemDetector
    {
        private const float THRESHOLD = 0.8f;
        public IList<FileInfo> Templates { get; }


        public ItemDetector(IList<FileInfo> templates)
        {
            if (!templates.Any())
            {
                throw new ArgumentException($"{nameof(templates)} may not be empty!");
            }
            Templates = templates;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="source">The input image to check for matches to the Templates</param>
        /// <param name="debugMode">Enables saving of the matching results after processing</param>
        /// <returns></returns>
        public (int, int) DetectMatches(Bitmap source, bool debugMode = false)
        {
            using (Mat matSrc = source.ToMat())
            using (Mat matTo = new Mat(Templates.First().FullName, ImreadModes.Grayscale))
            using (Mat matSrcRet = new Mat())
            using (Mat matToRet = new Mat())
            {

                KeyPoint[] keyPointsSrc, keyPointsTo;
                using (var surf = OpenCvSharp.XFeatures2D.SURF.Create(THRESHOLD, 4, 3, true, true))
                {
                    surf.DetectAndCompute(matSrc, null, out keyPointsSrc, matSrcRet);
                    surf.DetectAndCompute(matTo, null, out keyPointsTo, matToRet);
                }

                using (var flnMatcher = new OpenCvSharp.FlannBasedMatcher())
                {
                    var matches = flnMatcher.Match(matSrcRet, matToRet);
                    //Finding the Minimum and Maximum Distance
                    double minDistance = 1000;//Backward approximation
                    double maxDistance = 0;
                    for (int i = 0; i < matSrcRet.Rows; i++)
                    {
                        double distance = matches[i].Distance;
                        if (distance > maxDistance)
                        {
                            maxDistance = distance;
                        }
                        if (distance < minDistance)
                        {
                            minDistance = distance;
                        }
                    }
                    Console.WriteLine($"max distance : {maxDistance}");
                    Console.WriteLine($"min distance : {minDistance}");

                    var pointsSrc = new List<Point2f>();
                    var pointsDst = new List<Point2f>();
                    //Screening better matching points
                    var goodMatches = new List<DMatch>();
                    for (int i = 0; i < matSrcRet.Rows; i++)
                    {
                        double distance = matches[i].Distance;
                        if (distance < Math.Max(minDistance * 2, 0.02))
                        {
                            pointsSrc.Add(keyPointsSrc[matches[i].QueryIdx].Pt);
                            pointsDst.Add(keyPointsTo[matches[i].TrainIdx].Pt);
                            //Compression of new ones with distances less than ranges DMatch
                            goodMatches.Add(matches[i]);
                        }
                    }

                    var outMat = new Mat();

                    // algorithm RANSAC Filter the matched results
                    var pSrc = pointsSrc.ConvertAll(Point2fToPoint2d);

                    if (debugMode)
                    {
                        var pDst = pointsDst.ConvertAll(Point2fToPoint2d);
                        var outMask = new Mat();
                        // If the original matching result is null, Skip the filtering step
                        if (pSrc.Count > 0 && pDst.Count > 0)
                            Cv2.FindHomography(pSrc, pDst, HomographyMethods.Ransac, mask: outMask);
                        // If passed RANSAC After processing, the matching points are more than 10.,Only filters are used. Otherwise, use the original matching point result(When the matching point is too small, it passes through RANSAC After treatment,It's possible to get the result of 0 matching points.).
                        if (outMask.Rows > 10)
                        {
                            byte[] maskBytes = new byte[outMask.Rows * outMask.Cols];
                            outMask.GetArray(out maskBytes);
                            Cv2.DrawMatches(matSrc, keyPointsSrc, matTo, keyPointsTo, goodMatches, outMat, matchesMask: maskBytes, flags: DrawMatchesFlags.NotDrawSinglePoints);
                        }
                        else

                            Cv2.DrawMatches(matSrc, keyPointsSrc, matTo, keyPointsTo, goodMatches, outMat, flags: DrawMatchesFlags.NotDrawSinglePoints);
                        var bitmap = OpenCvSharp.Extensions.BitmapConverter.ToBitmap(outMat);

                        var outputDirectory = Path.Combine(new FileInfo(Assembly.GetExecutingAssembly().Location).DirectoryName, "MatchResults");
                        Directory.CreateDirectory(outputDirectory);
                        bitmap.Save(Path.Combine(outputDirectory, $"Match_{DateTime.Now.Ticks}.bmp"));

                    }
                    return ((int)Math.Round(pSrc.Select(p => p.X).Average(), 0), (int)Math.Round(pSrc.Select(p => p.Y).Average(), 0));
                }
            }
        }

        private static Point2d Point2fToPoint2d(Point2f pf) => new Point2d(((int)pf.X), ((int)pf.Y));



    }
}
