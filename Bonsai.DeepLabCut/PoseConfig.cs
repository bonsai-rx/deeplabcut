using System.Collections.Generic;

namespace Bonsai.DeepLabCut
{
    public class PoseConfig
    {
        public List<string> JointNames { get; } = new List<string>();

        public List<Link> PartAffinityGraph { get; } = new List<Link>();

        public bool LocationRefinement { get; set; }

        public bool PredictPartAffinityField { get; set; }

        public struct Link
        {
            public int Source;
            public int Target;
        }
    }
}
