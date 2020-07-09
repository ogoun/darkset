using Darknet.Dataset.Merger.Model;
using ImageMagick;
using System;
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
        //private readonly float[] _iou;
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
            /*_iou = new float[_bboxes.Count];
            for (int i = 0; i < _bboxes.Count - 1; i++)
            {
                for (int j = i + 1; j < _bboxes.Count; j++)
                {
                    var iou = IOU(_bboxes[i], _bboxes[j]);
                    if (_iou[i] < iou)
                    {
                        _iou[i] = iou;
                    }
                    if (_iou[j] < iou)
                    {
                        _iou[j] = iou;
                    }
                }
            }*/
        }

        /*
        private static void BoxRecalculate(float width, float height, ref bbox box)
        {
            if (box.RBx < 0)
            {
                box.RBw += box.RBx;
                box.RBx = 0;
                box.RCx = box.RBw / 2.0f;
            }
            if (box.RBy < 0)
            {
                box.RBh += box.RBy;
                box.RBy = 0;
                box.RCy = box.RBh / 2.0f;
            }
            if ((box.RBx + box.RBw) > width)
            {
                box.RBw = width - box.RBx;
                box.RBx = width - box.RBw;
                box.RCx = width - box.RBw / 2.0f;
            }
            if ((box.RBy + box.RBh) > height)
            {
                box.RBh = height - box.RBy;
                box.RBy = height - box.RBh;
                box.RCy = height - box.RBh / 2.0f;
            }
            box.RCx = box.RBx + box.RBw / 2.0f;
            box.RCy = box.RBy + box.RBh / 2.0f;
            box.Cx = box.RCx / width;
            box.Cy = box.RCy / height;
            box.Bw = box.RBw / width;
            box.Bh = box.RBh / width;
        }

        private static float IOU(bbox box1, bbox box2)
        {
            var left = (float)Math.Max(box1.RBx, box2.RBx);
            var right = (float)Math.Min(box1.RRight, box2.RRight);

            var top = (float)Math.Max(box1.RBy, box2.RBy);
            var bottom = (float)Math.Min(box1.RBottom, box2.RBottom);

            var width = (float)(right - left);
            var height = (float)(bottom - top);

            var intersectionArea = (float)(width * height);
            return intersectionArea / (float)(box1.RArea + box2.RArea - intersectionArea);
        }
        */

        public IEnumerable<MagickGeometry> ToMagikGeometry()
        {
            //int i = 0;
            //var ignore_rect = new MagickGeometry(-1, -1, -1, -1);
            foreach (var box in _bboxes)
            {
                /*
                if (_iou[i] < float.Epsilon) yield return ignore_rect;
                */
                var rect = new MagickGeometry((int)box.RBx, (int)box.RBy, (int)box.RBw, (int)box.RBh);
                rect.IgnoreAspectRatio = true;
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
