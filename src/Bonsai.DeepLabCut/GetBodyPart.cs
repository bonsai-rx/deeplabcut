using System;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Linq;

namespace Bonsai.DeepLabCut
{
    [Description("Selects the body part with the specified name from each input pose.")]
    public class GetBodyPart : Transform<Pose, BodyPart>
    {
        [Description("The name of the body part.")]
        public string Name { get; set; }

        public override IObservable<BodyPart> Process(IObservable<Pose> source)
        {
            return source.Select(pose => pose[Name]);
        }
    }
}
