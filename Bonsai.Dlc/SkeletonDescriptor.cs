using System.Collections.ObjectModel;

namespace Bonsai.Dlc
{
    public class SkeletonDescriptor
    {
        private readonly Collection<string> bodyParts = new Collection<string>();
        private readonly Collection<Link> links = new Collection<Link>();

        public Collection<string> BodyParts
        {
            get { return bodyParts; }
        }

        public Collection<Link> Links
        {
            get { return links; }
        }

        public struct Link
        {
            public int Source;
            public int Target;
        }
    }
}
