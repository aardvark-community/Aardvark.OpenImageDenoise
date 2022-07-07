using Aardvark.Base;
using Aardvark.OpenImageDenoise;
using System;
using System.Diagnostics;
using System.IO;

namespace DenoiseTest
{
    class Program
    {
        static void Main(string[] args)
        {
            Aardvark.Base.Aardvark.Init(); // unpack native

            //Test();
            Batch();
            //Benchmark();
        }

        static void Batch()
        {
            var device = new Device();
            Report.Line("Using {0} Threads", device.NumThreads);
            Report.Line("OIDN Version: {0}", device.Version);

            var files = Directory.GetFiles("C:\\Debug", "*.exr");

            foreach(var f in files)
            {
                Denoise(device, f);
            }

            device.Dispose();
        }

        static void Benchmark()
        {
            var device = new Device();
            Report.Line("Using {0} Threads", device.NumThreads);

            var img = new PixImage<float>(1, 1, 3);
            var avg = new AverageWindow(100);
            var sw = new Stopwatch();
            for (int i = 0; i < 100; i++)
            {
                sw.Restart();
                device.Denoise(img);
                avg.Insert(sw.Elapsed.TotalMilliseconds);
            }

            Report.Line("Avg Time: {0:0.000}ms", avg.Value);

            device.Dispose();
        }

        static void Denoise(Device device, string file)
        {
            // load input texture
            var img4Chan = (PixImage<float>)PixImage.Create(file);
            // OIDN only supports RGB images -> 
            var img = img4Chan.ToPixImage<float>(Col.Format.RGB);

            Report.BeginTimed("Denoise");
            var resultImg = device.Denoise(img, 10);
            Report.End();

            // save image with "_dn" postfix to same directory
            var outFile = Path.Combine(Path.GetDirectoryName(file), Path.GetFileNameWithoutExtension(file) + "_dn.exr");
            resultImg.SaveAsImage(outFile);
        }

        static void Test()
        {
            var dnDevice = new Device();
            Report.Line("Using {0} Threads", dnDevice.NumThreads);

            Denoise(dnDevice, "C:\\Debug\\pt0016.exr");

            dnDevice.Dispose();

            Console.ReadKey();
        }
    }
}
