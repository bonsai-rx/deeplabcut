using System;
using System.Linq;
using System.Reactive.Linq;
using OpenCV.Net;
using TensorFlow;
using System.IO;
using System.ComponentModel;

namespace Bonsai.DeepLabCut
{
    [DefaultProperty(nameof(ModelFileName))]
    [Description("Performs markerless pose estimation using a DeepLabCut model on the input image sequence.")]
    public class DetectPose : Transform<IplImage, Pose>
    {
        [FileNameFilter("Protocol Buffer Files(*.pb)|*.pb")]
        [Editor("Bonsai.Design.OpenFileNameEditor, Bonsai.Design", DesignTypes.UITypeEditor)]
        [Description("The path to the exported Protocol Buffer file containing the pretrained DeepLabCut model.")]
        public string ModelFileName { get; set; }

        [FileNameFilter("YAML Files(*.yaml)|*.yaml|All Files|*.*")]
        [Editor("Bonsai.Design.OpenFileNameEditor, Bonsai.Design", DesignTypes.UITypeEditor)]
        [Description("The path to the configuration YAML file containing joint labels.")]
        public string PoseConfigFileName { get; set; }

        [Range(0, 1)]
        [Editor(DesignTypes.SliderEditor, DesignTypes.UITypeEditor)]
        [Description("The optional confidence threshold used to discard position values.")]
        public float? MinConfidence { get; set; }

        public override IObservable<Pose> Process(IObservable<IplImage> source)
        {
            return Observable.Defer(() =>
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
                var session = new TFSession(graph, options, null);
                var bytes = File.ReadAllBytes(ModelFileName);
                graph.Import(bytes);

                TFTensor tensor = null;
                var config = ConfigHelper.PoseConfig(PoseConfigFileName);
                return source.Select(input =>
                {
                    if (tensor == null || tensor.GetTensorDimension(1) != input.Height || tensor.GetTensorDimension(2) != input.Width)
                    {
                        tensor = new TFTensor(
                            TFDataType.Float,
                            new long[] { 1, input.Height, input.Width, 3 },
                            input.WidthStep * input.Height * 4);
                    }

                    using (var image = new IplImage(input.Size, IplDepth.F32, 3, tensor.Data))
                    {
                        CV.Convert(input, image);
                    }

                    var runner = session.GetRunner();
                    runner.AddInput(graph["Placeholder"][0], tensor);
                    runner.Fetch(graph["concat_1"][0]);

                    // Run the model
                    var output = runner.Run();

                    // Fetch the results from output:
                    var poseTensor = output[0];
                    var pose = new Mat((int)poseTensor.Shape[0], (int)poseTensor.Shape[1], Depth.F32, 1, poseTensor.Data);
                    var result = new Pose(input);
                    var threshold = MinConfidence;
                    for (int i = 0; i < pose.Rows; i++)
                    {
                        BodyPart bodyPart;
                        bodyPart.Name = config[i];
                        bodyPart.Confidence = (float)pose.GetReal(i, 2);
                        if (bodyPart.Confidence < threshold)
                        {
                            bodyPart.Position = new Point2f(float.NaN, float.NaN);
                        }
                        else
                        {
                            bodyPart.Position.X = (float)pose.GetReal(i, 1);
                            bodyPart.Position.Y = (float)pose.GetReal(i, 0);
                        }
                        result.Add(bodyPart);
                    }
                    return result;
                }).Finally(() =>
                {
                    session.Dispose();
                    graph.Dispose();
                });
            });
        }
    }
}
