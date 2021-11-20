using Darknet.Dataset.Merger.Model;
using SixLabors.ImageSharp;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Darknet.Dataset.Merger.Services
{
    public class BBOXES
    {
        private struct bbox
        {
            public int Class;

            // rel
            public float Cx;
            public float Cy;
            public float Bw;
            public float Bh;

            // abs
            public float RCx;
            public float RCy;
            public float RBw;
            public float RBh;
            public float RBx;
            public float RBy;

            public float RArea => RBw * RBh;
            public float RRight => RBx + RBw;
            public float RBottom => RBy + RBh;
        }

        private readonly List<bbox> _bboxes;
        private readonly float _width;
        private readonly float _height;

        public BBOXES(IEnumerable<Annotation> annotations, float width, float height)
        {
            _width = width;
            _height = height;
            _bboxes = new List<bbox>();
            foreach (var a in annotations)
            {
                _bboxes.Add(new bbox
                {
                    Class = a.Class,

                    Cx = a.Cx,
                    Cy = a.Cy,
                    Bw = a.Width,
                    Bh = a.Height,

                    RCx = a.Cx * _width,
                    RCy = a.Cy * _height,
                    RBw = a.Width * _width,
                    RBh = a.Height * _height,
                    RBx = (a.Cx - a.Width / 2.0f) * _width,
                    RBy = (a.Cy - a.Height / 2.0f) * _height
                });
            }
        }

        public IEnumerable<Rectangle> ToMagikGeometry()
        {
            foreach (var box in _bboxes)
            {
                var rect = new Rectangle((int)box.RBx, (int)box.RBy, (int)box.RBw, (int)box.RBh);
                yield return rect;
            }
        }

        public override string ToString()
        {
            var lbls = new StringBuilder();
            _bboxes.Apply(b =>
            {
                lbls.Append($"{b.Class} {b.Cx.ConvertToString()} {b.Cy.ConvertToString()} {b.Bw.ConvertToString()} {b.Bh.ConvertToString()}");
                lbls.Append("\n");
            });
            return lbls.ToString();
        }
    }
}
