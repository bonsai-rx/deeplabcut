using System;
using System.Drawing;

namespace Bonsai.Dlc
{
    public class DlcConfig
    {
        readonly SkeletonDescriptor skeleton = new SkeletonDescriptor();

        public string Task { get; set; }

        public string Scorer { get; set; }

        public DateTime Date { get; set; }

        public SkeletonDescriptor Skeleton
        {
            get { return skeleton; }
        }

        public Color SkeletonColor { get; set; }

        public float PartCutoff { get; set; }

        public float DotSize { get; set; }

        public float AlphaValue { get; set; }

        public string ColorMap { get; set; }
    }
}
