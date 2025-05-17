using OpenCV.Net;
using System;
using System.IO;
using TensorFlow;

namespace Bonsai.DeepLabCut
{
    static class TensorHelper
    {
        const int TensorChannels = 3;

        public static TFGraph ImportModel(string fileName, out TFSession session)
        {
            using (var options = new TFSessionOptions())
            {
                unsafe
                {

                    byte[] GPUConfig = new byte[] { 0x32, 0x02, 0x20, 0x01 };
                    fixed (void* ptr = &GPUConfig[0])
                    {
                        options.SetConfig(new IntPtr(ptr), GPUConfig.Length);
                    }
                }

                var graph = new TFGraph();
                var bytes = File.ReadAllBytes(fileName);
                session = new TFSession(graph, options, null);
                graph.Import(bytes);
                return graph;
            }
        }

        public static TFTensor CreatePlaceholder(TFGraph graph, TFSession.Runner runner, Size frameSize, int batchSize = 1)
        {
            var tensor = new TFTensor(
                TFDataType.Float,
                new long[] { batchSize, frameSize.Height, frameSize.Width, TensorChannels },
                batchSize * frameSize.Width * frameSize.Height * TensorChannels * sizeof(float));
            runner.AddInput(graph["Placeholder"][0], tensor);
            return tensor;
        }

        public static IplImage GetRegionOfInterest(IplImage frame, Rect rect, out Point offset)
        {
            if (rect.Width > 0 && rect.Height > 0)
            {
                frame = frame.GetSubRect(rect);
                offset = new Point(rect.X, rect.Y);
            }
            else offset = Point.Zero;
            return frame;
        }

        public static IplImage EnsureFrameSize(IplImage frame, Size tensorSize, ref IplImage resizeTemp)
        {
            if (tensorSize != frame.Size)
            {
                if (resizeTemp == null || resizeTemp.Size != tensorSize)
                {
                    resizeTemp = new IplImage(tensorSize, frame.Depth, frame.Channels);
                }

                CV.Resize(frame, resizeTemp);
                frame = resizeTemp;
            }

            return frame;
        }

        public static IplImage EnsureColorFormat(IplImage frame, ColorConversion? colorConversion, ref IplImage colorTemp)
        {
            if (colorConversion != null)
            {
                if (colorTemp == null || colorTemp.Size != frame.Size)
                {
                    colorTemp = new IplImage(frame.Size, frame.Depth, TensorChannels);
                }

                CV.CvtColor(frame, colorTemp, colorConversion.Value);
                frame = colorTemp;
            }

            return frame;
        }

        public static void UpdateTensor(TFTensor tensor, params IplImage[] frames)
        {
            var batchSize = (int)tensor.Shape[0];
            var tensorRows = (int)tensor.Shape[1];
            var tensorCols = (int)tensor.Shape[2];
            if (frames?.Length != batchSize)
            {
                throw new ArgumentException("The number of frames does not match the tensor batch size.", nameof(frames));
            }

            using (var data = new Mat(batchSize * tensorRows, tensorCols, Depth.F32, TensorChannels, tensor.Data))
            {
                if (frames.Length == 1)
                {
                    CV.Convert(frames[0], data);
                }
                else
                {
                    for (int i = 0; i < frames.Length; i++)
                    {
                        var startRow = i * tensorRows;
                        var image = data.GetRows(startRow, startRow + tensorRows);
                        CV.Convert(frames[i], image);
                    }
                }
            }
        }

        public static IplImage[] GetTensorMaps(TFTensor tensor, int batchIndex = 0)
        {
            var batchSize = (int)tensor.Shape[0];
            var tensorRows = (int)tensor.Shape[1];
            var tensorCols = (int)tensor.Shape[2];
            var tensorMaps = (int)tensor.Shape[3];
            using (var data = new Mat(batchSize * tensorRows * tensorCols, tensorMaps, Depth.F32, 1, tensor.Data))
            {
                var frameSize = tensorRows * tensorCols;
                var startRow = batchIndex * frameSize;
                var result = new IplImage[tensorMaps];
                for (int i = 0; i < result.Length; i++)
                {
                    result[i] = data
                        .GetSubRect(new Rect(i, startRow, 1, frameSize))
                        .Clone()
                        .Reshape(1, tensorRows)
                        .GetImage();
                }
                return result;
            }
        }
    }
}
