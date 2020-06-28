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

        public string Description { get { return $"{Class} {Label} [{Cx}; {Cy}; {Width}; {Height}]"; } }
    }
}
