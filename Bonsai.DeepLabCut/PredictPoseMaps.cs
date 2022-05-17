using OpenCV.Net;
using System;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Linq;
using TensorFlow;

namespace Bonsai.DeepLabCut
{
    [DefaultProperty(nameof(ModelFileName))]
    [Description("Computes markerless pose estimation maps using a DeepLabCut model on the input image sequence.")]
    public class PredictPoseMaps : Transform<IplImage, PoseEstimation>
    {
        [FileNameFilter("Protocol Buffer Files(*.pb)|*.pb")]
        [Editor("Bonsai.Design.OpenFileNameEditor, Bonsai.Design", DesignTypes.UITypeEditor)]
        [Description("The path to the exported Protocol Buffer file containing the pretrained DeepLabCut model.")]
        public string ModelFileName { get; set; }

        [FileNameFilter("YAML Files(*.yaml)|*.yaml|All Files|*.*")]
        [Editor("Bonsai.Design.OpenFileNameEditor, Bonsai.Design", DesignTypes.UITypeEditor)]
        [Description("The path to the configuration YAML file containing joint labels.")]
        public string PoseConfigFileName { get; set; }

        IObservable<TResult> Process<TSource, TResult>(
            IObservable<TSource> source,
            Func<TSource, IplImage[]> sourceSelector,
            Func<PoseEstimation[], TResult> resultSelector)
        {
            return Observable.Defer(() =>
            {
                TFTensor tensor = null;
                TFSession.Runner runner = null;
                var graph = TensorHelper.ImportModel(ModelFileName, out TFSession session);
                var config = ConfigHelper.LoadPoseConfig(PoseConfigFileName);
                return source.Select(value =>
                {
                    var input = sourceSelector(value);
                    if (input.Length == 0)
                    {
                        throw new InvalidOperationException("Frame batch must have at least one frame.");
                    }

                    var tensorSize = input[0].Size;
                    if (tensor == null ||
                        tensor.Shape[0] != input.Length ||
                        tensor.Shape[1] != tensorSize.Height ||
                        tensor.Shape[2] != tensorSize.Width)
                    {
                        runner = session.GetRunner();
                        tensor = TensorHelper.CreatePlaceholder(graph, runner, tensorSize, input.Length);
                        if (config.LocationRefinement) runner.Fetch("Sigmoid", "pose/locref_pred/block4/BiasAdd");
                        else runner.Fetch("Sigmoid", "pose/part_pred/block4/BiasAdd");
                    }

                    // Run the model
                    TensorHelper.UpdateTensor(tensor, input);
                    var output = runner.Run();

                    // Fetch the results from output
                    var result = new PoseEstimation[input.Length];
                    for (int i = 0; i < result.Length; i++)
                    {
                        var scoreMap = TensorHelper.GetTensorMaps(output[0], i);
                        var locRef = TensorHelper.GetTensorMaps(output[1], i);
                        result[i] = new PoseEstimation(input[0], config, scoreMap, locRef);
                    }

                    return resultSelector(result);
                });
            });
        }

        public override IObservable<PoseEstimation> Process(IObservable<IplImage> source)
        {
            return Process(source, frame => new[] { frame }, result => result[0]);
        }

        public IObservable<PoseEstimation[]> Process(IObservable<IplImage[]> source)
        {
            return Process(source, frame => frame, result => result);
        }
    }
}
