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

        public override IObservable<PoseEstimation> Process(IObservable<IplImage> source)
        {
            return Observable.Defer(() =>
            {
                TFTensor tensor = null;
                TFSession.Runner runner = null;
                var graph = TensorHelper.ImportModel(ModelFileName, out TFSession session);
                var config = ConfigHelper.LoadPoseConfig(PoseConfigFileName);
                return source.Select(input =>
                {
                    var tensorSize = input.Size;
                    if (tensor == null || tensor.Shape[1] != tensorSize.Height || tensor.Shape[2] != tensorSize.Width)
                    {
                        runner = session.GetRunner();
                        tensor = TensorHelper.CreatePlaceholder(graph, runner, tensorSize);
                        if (config.LocationRefinement) runner.Fetch("Sigmoid", "pose/locref_pred/block4/BiasAdd");
                        else runner.Fetch("Sigmoid", "pose/part_pred/block4/BiasAdd");
                    }

                    // Run the model
                    TensorHelper.UpdateTensor(tensor, input);
                    var output = runner.Run();

                    // Fetch the results from output
                    var scoreMap = TensorHelper.GetTensorMaps(output[0]);
                    var locRef = TensorHelper.GetTensorMaps(output[1]);
                    return new PoseEstimation(input, config, scoreMap, locRef);
                });
            });
        }
    }
}
