using AnnoAutoWuerfler.FeatureSearch;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Forms;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AnnoAutoWuerfler.ConsoleApp
{
    internal class Program
    {
        const string ResourceDirectoy = "Resources";
        [System.Runtime.InteropServices.DllImport("User32.dll")]
        private static extern bool SetForegroundWindow(IntPtr handle);

        //This is a replacement for Cursor.Position in WinForms
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        static extern bool SetCursorPos(int x, int y);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern void mouse_event(int dwFlags, int dx, int dy, int cButtons, int dwExtraInfo);

        public const int MOUSEEVENTF_LEFTDOWN = 0x02;
        public const int MOUSEEVENTF_LEFTUP = 0x04;

        private static (int, int) HandelnButtonCoordinates;
        private static (int, int) ScrambleButtonCoordinates;

        private static int XOffsetLeft;
        private static int XOffsetRight;
        private static int Width;
        private static int YOffsetTop;
        private static int YOffsetBottom;
        private static int Height;


        static async Task Main(string[] args)
        {
            XOffsetLeft = (int)(Screen.PrimaryScreen.Bounds.Width * 0.55f);
            XOffsetRight = (int)(Screen.PrimaryScreen.Bounds.Width * 0.25f);
            Width = Screen.PrimaryScreen.Bounds.Width - XOffsetLeft - XOffsetRight;
            YOffsetTop = (int)(Screen.PrimaryScreen.Bounds.Height * 0.35f);
            YOffsetBottom = (int)(Screen.PrimaryScreen.Bounds.Height * 0.2f);
            Height = Screen.PrimaryScreen.Bounds.Height - YOffsetTop - YOffsetBottom;

            Console.WriteLine("Go to Anno1800 ...");
            await Task.Delay(5000);

            HandelnButtonCoordinates = await FindHandelnButton();
            ScrambleButtonCoordinates = await FindScrableButton();


            await Trade();
        }

        private static async Task Trade()
        {
            using (var kadjahDetector = new ItemDetector(new List<FileInfo> { new FileInfo(Path.Combine(ResourceDirectoy, "Kadjah.png")) }))
            using (var garrickDetector = new ItemDetector(new List<FileInfo> { new FileInfo(Path.Combine(ResourceDirectoy, "Garrick.png")) }))
            using (var livkovskyDetector = new ItemDetector(new List<FileInfo> { new FileInfo(Path.Combine(ResourceDirectoy, "Livkovsky.png")) }))
            using (var kerasDetector = new ItemDetector(new List<FileInfo> { new FileInfo(Path.Combine(ResourceDirectoy, "Keras.png")) }))
            using (var hermannDetector = new ItemDetector(new List<FileInfo> { new FileInfo(Path.Combine(ResourceDirectoy, "Hermann.png")) }))
            using (var sappeurDetector = new ItemDetector(new List<FileInfo> { new FileInfo(Path.Combine(ResourceDirectoy, "Sappeur.png")) }))
            {
                var itemDetectors = new List<ItemDetector> {
                    //hermannDetector,
                    kerasDetector,
                    //kadjahDetector,
                    garrickDetector,
                    livkovskyDetector,
                    sappeurDetector,
                    };


                int i = 0;
                while (i < 6)
                {
                    var matches = DetectFromScreenshot(itemDetectors);
                    if (matches.Any())
                    {
                        foreach (var match in matches.Where(m => m.Item1))
                        {
                            Console.WriteLine("Selecting detected Item");
                            LeftMouseClick(match.Item2, match.Item3);
                            await Task.Delay(800);
                            i++;
                        }
                        Console.WriteLine("Transfering to ship");
                        LeftMouseClick(HandelnButtonCoordinates.Item1, HandelnButtonCoordinates.Item2);
                        await Task.Delay(800);
                    }

                    Console.WriteLine("Scrambling...");
                    LeftMouseClick(ScrambleButtonCoordinates.Item1, ScrambleButtonCoordinates.Item2);
                    await Task.Delay(700);
                }
            }

        }

        private static void LeftMouseClick(int xPos, int yPos)
        {
            SetCursorPos(xPos, yPos);
            mouse_event(MOUSEEVENTF_LEFTDOWN, xPos, yPos, 0, 0);
            mouse_event(MOUSEEVENTF_LEFTUP, xPos, yPos, 0, 0);
        }

        private static async Task<(int, int)> FindHandelnButton()
        {
            using (var handleButtonDetector = new ItemDetector(new List<FileInfo> { new FileInfo(Path.Combine(ResourceDirectoy, "Button_Handeln.png")) }))
            {
                (bool, int, int) handelnButtonCoordinates = DetectFromScreenshot(new List<ItemDetector> { handleButtonDetector }, true).First();

                return (handelnButtonCoordinates.Item2, handelnButtonCoordinates.Item3);
            }
        }

        private static async Task<(int, int)> FindScrableButton()
        {
            using (var scrambleButtonDetector = new ItemDetector(new List<FileInfo> { new FileInfo(Path.Combine(ResourceDirectoy, "Button_Scramble.png")) }))
            {

                (bool, int, int) scrambleButtonCoordinates = DetectFromScreenshot(new List<ItemDetector> { scrambleButtonDetector }, true).First();

                return (scrambleButtonCoordinates.Item2, scrambleButtonCoordinates.Item3);
            }
        }

        private static IList<(bool, int, int)> DetectFromScreenshot(IList<ItemDetector> itemDetectors, bool fullscreen = false)
        {
            BringAnno1800ToForeground();

            /*
            Parallel.ForEach(itemDetectors, itemDetector =>
            {
                var detectionResult = itemDetector.DetectMatches((Bitmap)screenshotBitmap.Clone(), true);
                if (detectionResult.Item1)
                {
                    matches.Add((detectionResult.Item1, detectionResult.Item2 + XOffset, detectionResult.Item3));
                }
            });
            */



            var matches = new List<(bool, int, int)>();

            foreach (var detector in itemDetectors)
            {
                var detectionResult = detector.DetectMatches(TakeScreenshot(fullscreen), true);
                if (detectionResult.Item1)
                {
                    if (!fullscreen)
                    {
                        matches.Add((detectionResult.Item1, detectionResult.Item2 + XOffsetLeft, detectionResult.Item3 + YOffsetTop));
                    }
                    else
                    {
                        matches.Add((detectionResult.Item1, detectionResult.Item2, detectionResult.Item3));
                    }

                }
            }

            return matches;
        }

        private static Bitmap TakeScreenshot(bool fullscreen)
        {
            if (fullscreen)
            {
                var fullscreenshotBitmap = new Bitmap(Screen.PrimaryScreen.Bounds.Width,
                Screen.PrimaryScreen.Bounds.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

                var gfxfullScreenshot = Graphics.FromImage(fullscreenshotBitmap);

                gfxfullScreenshot.CopyFromScreen(Screen.PrimaryScreen.Bounds.X,
                    Screen.PrimaryScreen.Bounds.Y, 0, 0,
                    new Size(Screen.PrimaryScreen.Bounds.Width, Screen.PrimaryScreen.Bounds.Height),
                    CopyPixelOperation.SourceCopy);

                return fullscreenshotBitmap;
            }

            var screenshotBitmap = new Bitmap(Width,
                Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

            var gfxScreenshot = Graphics.FromImage(screenshotBitmap);

            gfxScreenshot.CopyFromScreen(XOffsetLeft,
                YOffsetTop, 0, 0,
                new Size(Width, Height),
                CopyPixelOperation.SourceCopy);

            return screenshotBitmap;
        }

        private static void BringAnno1800ToForeground()
        {
            Process process = Process.GetProcessesByName("Anno1800")[0];
            BringProcessToFront(process);
        }

        public static void BringProcessToFront(Process process)
        {
            IntPtr handle = process.MainWindowHandle;
            SetForegroundWindow(handle);
        }


    }
}
