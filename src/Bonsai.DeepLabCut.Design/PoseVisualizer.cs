using Bonsai;
using Bonsai.DeepLabCut;
using Bonsai.DeepLabCut.Design;
using Bonsai.Vision.Design;
using OpenCV.Net;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;
using Font = System.Drawing.Font;

[assembly: TypeVisualizer(typeof(PoseVisualizer), Target = typeof(Pose))]

namespace Bonsai.DeepLabCut.Design
{
    public class PoseVisualizer : IplImageVisualizer
    {
        Pose pose;
        IplImage labelImage;
        IplImageTexture labelTexture;
        ToolStripButton drawLabelsButton;
        Font labelFont;

        public bool DrawLabels { get; set; } = false;

        public override void Load(IServiceProvider provider)
        {
            base.Load(provider);
            drawLabelsButton = new ToolStripButton("Draw Labels");
            drawLabelsButton.CheckState = CheckState.Checked;
            drawLabelsButton.Checked = DrawLabels;
            drawLabelsButton.CheckOnClick = true;
            drawLabelsButton.CheckedChanged += (sender, e) => DrawLabels = drawLabelsButton.Checked;
            StatusStrip.Items.Add(drawLabelsButton);

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
            var drawLabels = DrawLabels;
            GL.Color4(Color4.White);
            base.RenderFrame();

            if (pose != null)
            {
                GL.PointSize(5 * VisualizerCanvas.Height / 480f);
                GL.Disable(EnableCap.Texture2D);
                GL.Begin(PrimitiveType.Points);
                for (int i = 0; i < pose.Count; i++)
                {

                    var bodyPart = pose[i];
                    var position = bodyPart.Position;
                    GL.Color3(ColorPalette.GetColor(i));
                    GL.Vertex2(NormalizePoint(position, pose.Image.Size));
                }
                GL.End();

                if (drawLabels)
                {
                    if (labelImage == null || labelImage.Size != pose.Image.Size)
                    {
                        const float LabelFontScale = 0.05f;
                        labelImage = new IplImage(pose.Image.Size, IplDepth.U8, 4);
                        var emSize = VisualizerCanvas.Font.SizeInPoints * (labelImage.Height * LabelFontScale) / VisualizerCanvas.Font.Height;
                        labelFont = new Font(VisualizerCanvas.Font.FontFamily, emSize);
                    }

                    labelImage.SetZero();
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
                            var bodyPart = pose[i];
                            var position = bodyPart.Position;
                            graphics.DrawString(bodyPart.Name, labelFont, Brushes.White, position.X, position.Y);
                        }
                    }

                    GL.Color4(Color4.White);
                    GL.Enable(EnableCap.Texture2D);
                    labelTexture.Update(labelImage);
                    labelTexture.Draw();
                }
            }
        }
    }
}
