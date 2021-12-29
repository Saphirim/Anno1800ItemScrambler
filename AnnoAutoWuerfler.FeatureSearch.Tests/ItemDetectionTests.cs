using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AnnoAutoWuerfler.FeatureSearch.Tests
{
    [TestClass]
    public class ItemDetectionTests
    {
        private const string KerasResourcesPath = "TestResources/Keras/";

        [TestMethod]
        public void TestSimpleKerasDetection()
        {
            var sourceImages = new List<FileInfo>
            {
                new FileInfo(Path.Combine(KerasResourcesPath,"Image_01.jpg")),
                new FileInfo(Path.Combine(KerasResourcesPath,"Image_02.jpg")),
                new FileInfo(Path.Combine(KerasResourcesPath, "Image_03.jpg"))
            };
            var templates = new List<FileInfo>
            {
                new FileInfo(Path.Combine(KerasResourcesPath, "template.jpg"))
            };


            var itemDetector = new ItemDetector(templates);

            foreach(var source in sourceImages)
            {
                var resultCoordinates = itemDetector.DetectMatches(new Bitmap(source.FullName), true);
            }

        }
    }
}
