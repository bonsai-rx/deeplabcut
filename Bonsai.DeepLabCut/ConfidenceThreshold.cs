using OpenCV.Net;
using System;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Linq;

namespace Bonsai.DeepLabCut
{
    [Description("Discards body part positions below a specified confidence threshold.")]
    public class ConfidenceThreshold : Transform<Pose, Pose>
    {
        [Range(0, 1)]
        [Editor(DesignTypes.SliderEditor, DesignTypes.UITypeEditor)]
        [Description("The confidence threshold used to discard position values.")]
        public float MinConfidence { get; set; }

        public override IObservable<Pose> Process(IObservable<Pose> source)
        {
            return source.Select(pose =>
            {
                var result = new Pose(pose.Image);
                foreach (var bodyPart in pose)
                {

                    if (bodyPart.Confidence < MinConfidence)
                    {
                        result.Add(new BodyPart
                        {
                            Name = bodyPart.Name,
                            Position = new Point2f(float.NaN, float.NaN),
                            Confidence = bodyPart.Confidence
                        });
                    }
                    else result.Add(bodyPart);
                }
                return result;
            });
        }
    }
}
