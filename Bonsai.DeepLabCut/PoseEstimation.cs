using OpenCV.Net;

namespace Bonsai.DeepLabCut
{
    public class PoseEstimation
    {
        internal PoseEstimation(IplImage image, PoseConfig poseConfig, IplImage[] scoreMaps, IplImage[] locationRefinement)
        {
            Image = image;
            PoseConfig = poseConfig;
            ScoreMaps = scoreMaps;
            LocationRefinement = locationRefinement;
        }

        public IplImage Image { get; }

        public PoseConfig PoseConfig { get; }

        public IplImage[] ScoreMaps { get; }

        public IplImage[] LocationRefinement { get; }
    }
}
