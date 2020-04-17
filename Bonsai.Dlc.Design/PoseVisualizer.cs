using Bonsai;
using Bonsai.Dlc;
using Bonsai.Dlc.Design;
using Bonsai.Vision.Design;
using OpenCV.Net;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using Font = System.Drawing.Font;

[assembly: TypeVisualizer(typeof(PoseVisualizer), Target = typeof(Pose))]

namespace Bonsai.Dlc.Design
{
    public class PoseVisualizer : IplImageVisualizer
    {
        const float LabelFontScale = 0.05f;

        Pose pose;
        Mat convertBuffer;
        IplImage labelImage;
        IplImageTexture labelTexture;
        Font labelFont;

        public override void Load(IServiceProvider provider)
        {
            base.Load(provider);
            convertBuffer = new Mat(1, 1, Depth.U8, 3);
            VisualizerCanvas.Load += (sender, e) =>
            {
                labelTexture = new IplImageTexture();
                GL.Enable(EnableCap.Blend);
                GL.Enable(EnableCap.PointSmooth);
                GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
            };
        }

        public override void Show(object value)
        {
            pose = (Pose)value;
            if (pose != null)
            {
                base.Show(pose.Image);
            }
        }

        public static Vector2 NormalizePoint(Point2f point, OpenCV.Net.Size imageSize)
        {
            return new Vector2(
                (point.X * 2f / imageSize.Width) - 1,
                -((point.Y * 2f / imageSize.Height) - 1));
        }

        protected override void RenderFrame()
        {
            GL.Color4(Color4.White);
            base.RenderFrame();

            if (pose != null)
            {
                GL.PointSize(5 * VisualizerCanvas.Height / 640f);
                if (labelImage == null || labelImage.Size != pose.Image.Size)
                {
                    labelImage = new IplImage(pose.Image.Size, IplDepth.U8, 4);
                    var emSize = VisualizerCanvas.Font.SizeInPoints * (labelImage.Height * LabelFontScale) / VisualizerCanvas.Font.Height;
                    labelFont = new Font(VisualizerCanvas.Font.FontFamily, emSize);
                }

                labelImage.SetZero();
                GL.Disable(EnableCap.Texture2D);
                GL.Begin(PrimitiveType.Points);
                using (var labelBitmap = new Bitmap(labelImage.Width, labelImage.Height, labelImage.WidthStep, System.Drawing.Imaging.PixelFormat.Format32bppArgb, labelImage.ImageData))
                using (var graphics = Graphics.FromImage(labelBitmap))
                using (var format = new StringFormat())
                {
                    graphics.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
                    graphics.SmoothingMode = SmoothingMode.AntiAlias;
                    format.Alignment = StringAlignment.Center;
                    format.LineAlignment = StringAlignment.Center;
                    for (int i = 0; i < pose.Count; i++)
                    {
                        var hue = 180f - (i * 180f / pose.Count);
                        convertBuffer[0] = new Scalar(hue, 255, 255);
                        CV.CvtColor(convertBuffer, convertBuffer, ColorConversion.Hsv2Bgr);
                        var color = convertBuffer[0];

                        var bodyPart = pose[i];
                        var position = bodyPart.Position;
                        GL.Color3(color.Val2 / 255, color.Val1 / 255, color.Val0 / 255);
                        GL.Vertex2(NormalizePoint(position, pose.Image.Size));
                        //graphics.DrawString(bodyPart.Name, labelFont, Brushes.White, position.X, position.Y);
                    }
                }
                GL.End();
                
                GL.Color4(Color4.White);
                GL.Enable(EnableCap.Texture2D);
                labelTexture.Update(labelImage);
                labelTexture.Draw();
            }
        }
    }
}
