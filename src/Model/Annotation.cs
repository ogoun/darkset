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
    }
}
