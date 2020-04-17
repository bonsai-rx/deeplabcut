using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using YamlDotNet.RepresentationModel;

namespace Bonsai.DeepLabCut
{
    static class ConfigHelper
    {
        static YamlMappingNode OpenFile(string fileName)
        {
            var yaml = new YamlStream();
            var reader = new StringReader(File.ReadAllText(fileName));
            yaml.Load(reader);

            var document = yaml.Documents.FirstOrDefault();
            if (document == null)
            {

            }

            var mapping = document.RootNode as YamlMappingNode;
            return mapping;
        }

        public static List<string> PoseConfig(string fileName)
        {
            var mapping = OpenFile(fileName);
            return PoseConfig(mapping);
        }

        public static List<string> PoseConfig(YamlMappingNode mapping)
        {
            var joints = new List<string>();
            foreach (var entry in mapping.Children)
            {
                switch (((string)entry.Key).ToLowerInvariant())
                {
                    case "all_joints_names":
                        var jointNames = (YamlSequenceNode)entry.Value;
                        foreach (var part in jointNames.Children)
                        {
                            joints.Add((string)part);
                        }
                        break;
                }
            }
            return joints;
        }

        public static PoseConfig ConfigFile(YamlMappingNode mapping)
        {
            var config = new PoseConfig();
            foreach (var entry in mapping.Children)
            {
                switch (((string)entry.Key).ToLowerInvariant())
                {
                    case "task": config.Task = (string)entry.Value; break;
                    case "scorer": config.Scorer = (string)entry.Value; break;
                    case "date": config.Date = DateTime.Parse((string)entry.Value); break;
                    case "bodyparts":
                        var bodyparts = (YamlSequenceNode)entry.Value;
                        foreach (var part in bodyparts.Children)
                        {
                            config.Skeleton.BodyParts.Add((string)part);
                        }
                        break;
                    case "skeleton":
                        var skeleton = (YamlSequenceNode)entry.Value;
                        foreach (YamlSequenceNode link in skeleton)
                        {
                            SkeletonDescriptor.Link linkDescriptor;
                            linkDescriptor.Source = config.Skeleton.BodyParts.IndexOf((string)link.Children[0]);
                            linkDescriptor.Target = config.Skeleton.BodyParts.IndexOf((string)link.Children[1]);
                            config.Skeleton.Links.Add(linkDescriptor);
                        }
                        break;
                    case "skeleton_color": config.SkeletonColor = ColorTranslator.FromHtml((string)entry.Value); break;
                    case "pcutoff": config.PartCutoff = float.Parse((string)entry.Value); break;
                    case "dotsize": config.DotSize = float.Parse((string)entry.Value); break;
                    case "alphavalue": config.AlphaValue = float.Parse((string)entry.Value); break;
                    case "colormap": config.ColorMap = (string)entry.Value; break;
                    default:
                        break;
                }
            }
            return config;
        }
    }
}
