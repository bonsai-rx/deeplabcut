using System;
using System.Linq;
using System.Reactive.Linq;
using OpenCV.Net;
using TensorFlow;
using System.ComponentModel;

namespace Bonsai.DeepLabCut
{
    [DefaultProperty(nameof(ModelFileName))]
    [Description("Performs markerless pose estimation using a DeepLabCut model on the input image sequence.")]
    public class PredictPose : Transform<IplImage, Pose>
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

        [Description("The optional scale factor used to resize video frames for inference.")]
        public float? ScaleFactor { get; set; }

        [Description("The optional color conversion used to prepare RGB video frames for inference.")]
        public ColorConversion? ColorConversion { get; set; } = OpenCV.Net.ColorConversion.Bgr2Rgb;

        IObservable<Pose> Process<TSource>(IObservable<TSource> source, Func<TSource, (IplImage, Rect)> roiSelector)
        {
            return Observable.Defer(() =>
            {
                IplImage resizeTemp = null;
                IplImage colorTemp = null;
                TFTensor tensor = null;
                TFSession.Runner runner = null;
                var graph = TensorHelper.ImportModel(ModelFileName, out TFSession session);
                var config = ConfigHelper.LoadPoseConfig(PoseConfigFileName);
                return source.Select(value =>
                {
                    var poseScale = 1.0;
                    var (input, roi) = roiSelector(value);
                    var tensorSize = roi.Width > 0 && roi.Height > 0 ? new Size(roi.Width, roi.Height) : input.Size;
                    var scaleFactor = ScaleFactor;
                    if (scaleFactor.HasValue)
                    {
                        poseScale = scaleFactor.Value;
                        tensorSize.Width = (int)(tensorSize.Width * poseScale);
                        tensorSize.Height = (int)(tensorSize.Height * poseScale);
                        poseScale = 1.0 / poseScale;
                    }

                    if (tensor == null || tensor.Shape[1] != tensorSize.Height || tensor.Shape[2] != tensorSize.Width)
                    {
                        tensor?.Dispose();
                        runner = session.GetRunner();
                        tensor = TensorHelper.CreatePlaceholder(graph, runner, tensorSize);
                        runner.Fetch("concat_1");
                    }

                    // Run the model
                    var frame = TensorHelper.GetRegionOfInterest(input, roi, out Point offset);
                    frame = TensorHelper.EnsureFrameSize(frame, tensorSize, ref resizeTemp);
                    frame = TensorHelper.EnsureColorFormat(frame, ColorConversion, ref colorTemp);
                    TensorHelper.UpdateTensor(tensor, frame);
                    var output = runner.Run();

                    // Fetch the results from output
                    var poseTensor = output[0];
                    var pose = new Mat((int)poseTensor.Shape[0], (int)poseTensor.Shape[1], Depth.F32, 1, poseTensor.Data);
                    var result = new Pose(input);
                    var threshold = MinConfidence;
                    for (int i = 0; i < pose.Rows; i++)
                    {
                        BodyPart bodyPart;
                        bodyPart.Name = config.JointNames[i];
                        bodyPart.Confidence = (float)pose.GetReal(i, 2);
                        if (bodyPart.Confidence < threshold)
                        {
                            bodyPart.Position = new Point2f(float.NaN, float.NaN);
                        }
                        else
                        {
                            bodyPart.Position.X = (float)(pose.GetReal(i, 1) * poseScale) + offset.X;
                            bodyPart.Position.Y = (float)(pose.GetReal(i, 0) * poseScale) + offset.Y;
                        }
                        result.Add(bodyPart);
                    }
                    return result;
                });
            });
        }

        public override IObservable<Pose> Process(IObservable<IplImage> source)
        {
            return Process(source, frame => (frame, new Rect(0, 0, 0, 0)));
        }

        public IObservable<Pose> Process(IObservable<Tuple<IplImage, Rect>> source)
        {
            return Process(source, input => (input.Item1, input.Item2));
        }

        public IObservable<Pose> Process(IObservable<PoseEstimation> source)
        {
            return source.Select(input =>
            {
                var config = input.PoseConfig;
                var result = new Pose(input.Image);
                var threshold = MinConfidence;
                var stride = config.Stride;
                for (int i = 0; i < config.JointNames.Count; i++)
                {
                    BodyPart bodyPart;
                    var scoreMap = input.ScoreMaps[i];
                    bodyPart.Name = config.JointNames[i];
                    CV.MinMaxLoc(scoreMap, out double min, out double max, out Point minLoc, out Point maxLoc);
                    bodyPart.Confidence = (float)max;
                    if (bodyPart.Confidence < threshold)
                    {
                        bodyPart.Position = new Point2f(float.NaN, float.NaN);
                    }
                    else
                    {
                        var locRefX = input.LocationRefinement[i * 2 + 0];
                        var locRefY = input.LocationRefinement[i * 2 + 1];
                        var offsetX = locRefX[maxLoc.Y, maxLoc.X].Val0;
                        var offsetY = locRefY[maxLoc.Y, maxLoc.X].Val0;
                        bodyPart.Position.X = (float)(maxLoc.X * stride + 0.5 * stride + offsetX);
                        bodyPart.Position.Y = (float)(maxLoc.Y * stride + 0.5 * stride + offsetY);
                    }
                    result.Add(bodyPart);
                }
                return result;
            });
        }
    }

    [Obsolete]
    public class DetectPose : PredictPose
    {
        public DetectPose()
        {
            ColorConversion = null;
        }
    }
}
