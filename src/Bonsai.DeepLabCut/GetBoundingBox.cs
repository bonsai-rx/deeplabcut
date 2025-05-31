using OpenCV.Net;
using System;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Linq;

namespace Bonsai.DeepLabCut
{
    [Description("Computes the bounding box according to the smallest and largest position of the pose body parts and the specified margin.")]
    public class GetBoundingBox : Transform<Pose, Rect>
    {
        [Description("Specifies the margin, in pixels, used to set or expand the pose bounding box size.")]
        public Size Margin { get; set; }

        [Description("Specifies the method for extracting the pose bounding box.")]
        public BoundingBoxMethod Method { get; set; }

        public override IObservable<Rect> Process(IObservable<Pose> source)
        {
            return source.Select(pose =>
            {
                var hasBounds = false;
                var boundsMin = new Point2f(float.NaN, float.NaN);
                var boundsMax = new Point2f(float.NaN, float.NaN);
                for (int i = 0; i < pose.Count; i++)
                {
                    var position = pose[i].Position;
                    if (float.IsNaN(position.X) || float.IsNaN(position.Y))
                    {
                        continue;
                    }

                    if (!hasBounds)
                    {
                        boundsMin = boundsMax = position;
                        hasBounds = true;
                    }
                    else
                    {
                        boundsMin.X = Math.Min(boundsMin.X, position.X);
                        boundsMin.Y = Math.Min(boundsMin.Y, position.Y);
                        boundsMax.X = Math.Max(boundsMax.X, position.X);
                        boundsMax.Y = Math.Max(boundsMax.Y, position.Y);
                    }
                }

                if (float.IsNaN(boundsMin.X)) return new Rect(0, 0, 0, 0);
                var method = Method;
                var margin = Margin;
                var imageSize = pose.Image.Size;
                if (method == BoundingBoxMethod.FixedSize)
                {
                    var centerX = (int)((boundsMin.X + boundsMax.X) / 2);
                    var centerY = (int)((boundsMin.Y + boundsMax.Y) / 2);
                    var left = centerX - margin.Width / 2;
                    var top = centerY - margin.Height / 2;
                    var rect = new Rect(left, top, margin.Width, margin.Height);
                    rect.X += rect.X < 0 ? -rect.X : -Math.Max(0, rect.X + rect.Width - imageSize.Width);
                    rect.Y += rect.Y < 0 ? -rect.Y : -Math.Max(0, rect.Y + rect.Height - imageSize.Height);
                    return rect;
                }
                else
                {
                    var left = Math.Max(0, (int)boundsMin.X - margin.Width);
                    var top = Math.Max(0, (int)boundsMin.Y - margin.Height);
                    var right = Math.Min(imageSize.Width - 1, (int)boundsMax.X + margin.Width);
                    var bottom = Math.Min(imageSize.Height - 1, (int)boundsMax.Y + margin.Height);
                    return new Rect(left, top, right - left + 1, bottom - top + 1);
                }
            });
        }
    }

    public enum BoundingBoxMethod
    {
        FixedSize,
        Margin,
    }
}
