using SixLabors.ImageSharp;

namespace Darknet.Dataset.Merger.Model
{
    public class Annotation
    {
        public int Class;
        public string Label;
        public float Cx;
        public float Cy;
        public float Width;
        public float Height;

        public float Left => Cx - Width / 2.0f;
        public float Right => Cx + Width / 2.0f;
        public float Top => Cy - Height / 2.0f;
        public float Bottom => Cy + Height / 2.0f;

        public string Description { get { return $"{Class} {Label} [{Cx}; {Cy}; {Width}; {Height}]"; } }

        public Annotation Clone()
        {
            return new Annotation
            {
                Class = this.Class,
                Cx = this.Cx,
                Cy = this.Cy,
                Height = this.Height,
                Label = this.Label,
                Width = this.Width
            };
        }

        public Rectangle ToMagikGeometry(float width, float height)
        {
            return new Rectangle((int)((Cx - Width / 2.0f) * width), (int)((Cy - Height / 2.0f) * height), (int)(Width * width), (int)(Height * height));
        }
    }
}
