using Bonsai;
using Bonsai.DeepLabCut;
using Bonsai.DeepLabCut.Design;
using Bonsai.Vision.Design;
using OpenCV.Net;
using System;

[assembly: TypeVisualizer(typeof(PoseEstimationVisualizer), Target = typeof(PoseEstimation))]

namespace Bonsai.DeepLabCut.Design
{
    public class PoseEstimationVisualizer : IplImageVisualizer
    {
        IplImage maps;
        IplImage temp;
        IplImage scoreTemp;

        public override void Show(object value)
        {
            var poseEstimation = (PoseEstimation)value;
            if (poseEstimation?.ScoreMaps?.Length > 0)
            {
                var image = poseEstimation.Image;
                var gridRows = (int)Math.Sqrt(poseEstimation.ScoreMaps.Length);
                var gridCols = (int)Math.Ceiling(poseEstimation.ScoreMaps.Length / (double)gridRows);

                for (int i = 0; i < poseEstimation.ScoreMaps.Length; i++)
                {
                    var scoreMap = poseEstimation.ScoreMaps[i];
                    if (i == 0)
                    {
                        var mapSize = new Size(scoreMap.Width * gridCols, scoreMap.Height * gridRows);
                        if (maps == null || maps.Size != mapSize)
                        {
                            maps = new IplImage(mapSize, image.Depth, image.Channels);
                            temp = new IplImage(scoreMap.Size, image.Depth, image.Channels);
                            scoreTemp = new IplImage(scoreMap.Size, image.Depth, scoreMap.Channels);
                            maps.SetZero();
                        }

                        CV.Resize(image, temp);
                    }

                    var x = i % gridCols;
                    var y = i / gridCols;
                    using (var gridCell = maps.GetSubRect(new Rect(
                        x * scoreMap.Width,
                        y * scoreMap.Height,
                        scoreMap.Width,
                        scoreMap.Height)))
                    {
                        CV.ConvertScale(scoreMap, scoreTemp, byte.MaxValue, 0);
                        CV.CvtColor(scoreTemp, gridCell, ColorConversion.Gray2Bgr);
                        CV.Add(temp, gridCell, gridCell);
                    }
                }

                base.Show(maps);
            }
        }

        public override void Unload()
        {
            if (maps != null)
            {
                maps.Dispose();
                temp.Dispose();
                scoreTemp.Dispose();
                maps = temp = scoreTemp = null;
            }
            base.Unload();
        }
    }
}
