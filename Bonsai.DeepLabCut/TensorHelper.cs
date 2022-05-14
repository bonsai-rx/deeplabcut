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
            TFSessionOptions options = new TFSessionOptions();
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

        public static TFTensor CreatePlaceholder(TFGraph graph, TFSession.Runner runner, Size tensorSize)
        {
            var tensor = new TFTensor(
                TFDataType.Float,
                new long[] { 1, tensorSize.Height, tensorSize.Width, TensorChannels },
                tensorSize.Width * tensorSize.Height * TensorChannels * sizeof(float));
            runner.AddInput(graph["Placeholder"][0], tensor);
            return tensor;
        }

        public static void UpdateTensor(TFTensor tensor, Size tensorSize, IplImage frame, ref IplImage temp)
        {
            if (tensorSize != frame.Size)
            {
                if (temp == null || temp.Size != tensorSize)
                {
                    temp = new IplImage(tensorSize, frame.Depth, frame.Channels);
                }

                CV.Resize(frame, temp);
                frame = temp;
            }

            using (var image = new IplImage(tensorSize, IplDepth.F32, TensorChannels, tensor.Data))
            {
                CV.Convert(frame, image);
            }
        }

        public static IplImage[] GetImageArray(TFTensor tensor)
        {
            using (var data = new Mat((int)(tensor.Shape[1] * tensor.Shape[2]), (int)tensor.Shape[3], Depth.F32, 1, tensor.Data))
            {
                var result = new IplImage[data.Cols];
                for (int i = 0; i < result.Length; i++)
                {
                    result[i] = data.GetCol(i).Clone().Reshape(1, (int)tensor.Shape[1]).GetImage();
                }
                return result;
            }
        }
    }
}
