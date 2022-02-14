using Darknet.Dataset.Merger.Model;
using SixLabors.ImageSharp.Processing;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Darknet.Dataset.Merger.Services
{
    public static class Augmentator
    {
        public static int Augmentate(ImageInfo imageInfo, AugmentationContext context, AugmentationOptions options)
        {
            var local = new LocalAnnotationContext { Annotation = imageInfo.Annotations, Image = imageInfo };
            if (options.HasAugmentation == false && options.Cut == false && options.ResizeToInput == false)
            {
                // Простое копирование
                var (imgName, lblName) = context.GetNextObjFileNames();
                File.Copy(imageInfo.FilePath, imgName);
                File.WriteAllText(lblName, AnnotationToText(context, local.Annotation));
            }
            else
            {
                var resizeToInput = new Func<AImage, AImage>(img =>
                {
                    if (img.Width > options.InputWidth || img.Height > options.InputHeight)
                    {
                        float scale = 1.0f;
                        if (img.Width < img.Height)
                        {
                            scale = (float)options.InputWidth / img.Width;
                        }
                        else
                        {
                            scale = (float)options.InputHeight / img.Height;
                        }
                        img.Resize((int)(img.Width * scale), (int)(img.Height * scale));
                        img.SaveAsSource();
                    }
                    return img;
                });
                var store = new Action<AImage, string>((ifac, ann) =>
                {
                    var (imgName, lblName) = context.GetNextAugFileNames();
                    ifac.Write(imgName);
                    File.WriteAllText(lblName, ann);
                });
                using (var factory = new AImage(imageInfo.FilePath))
                {
                    if (options.ResizeToInput)
                    {
                        using (var original = resizeToInput(factory.Clone()))
                        {
                            // Save resized original image
                            var (imgName, lblName) = context.GetNextObjFileNames();
                            original.Write(imgName);
                            File.WriteAllText(lblName, AnnotationToText(context, local.Annotation));

                            ApplyAugmentations(original, local, context, options, false);
                        }
                    }
                    else
                    {
                        using (var original = factory.Clone())
                        {
                            // Save original image
                            var (imgName, lblName) = context.GetNextObjFileNames();
                            original.Write(imgName);
                            File.WriteAllText(lblName, AnnotationToText(context, local.Annotation));

                            ApplyAugmentations(original, local, context, options, false);
                        }
                    }
                    if ((options.WithoutClass || local.Image.Annotations.Any())
                        && options.Cut
                        && options.CutWidth > 0
                        && options.CutHeight > 0
                        && options.CutWidth < (factory.Width / 1.4)
                        && options.CutHeight < (factory.Height / 1.4))
                    {
                        foreach (var cropped in factory.Crop(local.Image.Annotations, options.CutWidth, options.CutHeight, options.CutOverlaps))
                        {
                            using (cropped.Item1)
                            {
                                if (options.WithoutClass == false || cropped.Item2.Count > 0)
                                {
                                    var croppedContext = new LocalAnnotationContext { Annotation = cropped.Item2, Image = new ImageInfo(imageInfo.FilePath, imageInfo.TrainType, cropped.Item2) };

                                    if (options.ResizeToInput)
                                    {
                                        using (var r = resizeToInput(cropped.Item1))
                                        {
                                            store(r, AnnotationToText(context, croppedContext.Annotation));
                                            ApplyAugmentations(r, croppedContext, context, options, true);
                                        }
                                    }
                                    else
                                    {
                                        store(cropped.Item1, AnnotationToText(context, croppedContext.Annotation));
                                        ApplyAugmentations(cropped.Item1, croppedContext, context, options, true);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            return context.Counter;
        }

        private static string AnnotationToText(AugmentationContext context, IEnumerable<Annotation> annotations)
        {
            var text = new StringBuilder();
            foreach (var a in annotations)
            {
                if (context.Classes.ContainsKey(a.Label))
                {
                    text.Append($"{context.Classes[a.Label]} {a.Cx.ConvertToString()} {a.Cy.ConvertToString()} {a.Width.ConvertToString()} {a.Height.ConvertToString()}");
                    text.Append("\n");
                }
            }
            return text.ToString();
        }

        private static IEnumerable<Annotation> ApplyAffineTransformToAnnotations(FlipMode flip, RotateMode rotate, IEnumerable<Annotation> annotations)
        {
            var result = new List<Annotation>(annotations.Select(a => a.Clone()));
            if (flip == FlipMode.Horizontal)
            {
                foreach (var a in result)
                {
                    a.Cx = 1.0f - a.Cx;
                }
            }
            if (flip == FlipMode.Vertical)
            {
                foreach (var a in result)
                {
                    a.Cy = 1.0f - a.Cy;
                }
            }
            float cx = .5f;
            float cy = .5f;
            switch (rotate)
            {
                case RotateMode.Rotate90:
                    foreach (var a in result)
                    {
                        var nx = a.Cx - cx;
                        var ny = a.Cy - cy;

                        var rx = ny;
                        var ry = -nx;

                        a.Cx = rx + cx;
                        a.Cy = ry + cy;

                        var b = a.Width;
                        a.Width = a.Height;
                        a.Height = b;
                    }
                    break;
                case RotateMode.Rotate180:
                    foreach (var a in result)
                    {
                        var nx = a.Cx - cx;
                        var ny = a.Cy - cy;

                        var rx = -nx;
                        var ry = -ny;

                        a.Cx = rx + cx;
                        a.Cy = ry + cy;
                    }
                    break;
                case RotateMode.Rotate270:
                    foreach (var a in result)
                    {
                        var nx = a.Cx - cx;
                        var ny = a.Cy - cy;

                        var rx = -ny;
                        var ry = nx;

                        a.Cx = rx + cx;
                        a.Cy = ry + cy;

                        var b = a.Width;
                        a.Width = a.Height;
                        a.Height = b;
                    }
                    break;
            }
            return result;
        }

        static Random rnd = new Random((int)Environment.TickCount);
        private static void ApplyAugmentations(AImage factory, LocalAnnotationContext localContext,
            AugmentationContext context, AugmentationOptions options, bool isCrop)
        {
            var boxes = new BBOXES(localContext.Image.Annotations.Where(a => context.Classes.ContainsKey(a.Label)), factory.Width, factory.Height);
            var annotations = localContext.Annotation;

            FlipMode flip = FlipMode.None;
            RotateMode rotate = RotateMode.None;
            // если это кроп, то стоит применить отражение и вращение для лучшего обобщения
            if (isCrop)
            {
                flip = rnd.NextDouble() > .5d ? FlipMode.Horizontal : FlipMode.Vertical;
                var r = rnd.NextDouble();
                rotate = r < .33d ? RotateMode.Rotate90 : r < .66d ? RotateMode.Rotate180 : RotateMode.Rotate270;
                annotations = ApplyAffineTransformToAnnotations(flip, rotate, localContext.Annotation);

                factory.FlipAndRotate(rotate, flip);
                factory.SaveAsSource();
            }

            var store = new Action<AImage, string>((ifac, ann) =>
            {
                var (imgName, lblName) = context.GetNextAugFileNames();
                ifac.Write(imgName);
                File.WriteAllText(lblName, ann);
            });

            var app = new Action<AImage, Func<AImage, Action>>((img, gf) =>
            {
                gf(img).Invoke();
                store(img, AnnotationToText(context, annotations));
                img.Reset();
            });

            if (options.BBoxBlur && options.LinesNoise)
            {
                if (factory.BlurBoxes(annotations))
                {
                    factory.LineNoize();
                    store(factory, AnnotationToText(context, annotations));
                    factory.Reset();
                }
                else
                {
                    factory.LineNoize();
                    store(factory, AnnotationToText(context, annotations));
                    factory.Reset();
                }
            }
            else if (options.LinesNoise)
            {
                app.Invoke(factory, im => im.LineNoize);
            }
            else if (options.BBoxBlur)
            {
                if (factory.BlurBoxes(annotations))
                {
                    store(factory, AnnotationToText(context, annotations));
                    factory.Reset();
                }
            }
            if (options.Blur) app.Invoke(factory, im => im.Blur);
            if (options.Grayscale) app.Invoke(factory, im => im.Grayscale);
            if (options.Sepia) app.Invoke(factory, im => im.SepiaTone);


            if (options.PrewittKernel) app.Invoke(factory, im => im.ApplyPrewittKernel);
            if (options.KayyaliKernel) app.Invoke(factory, im => im.ApplyKayyaliKernel);
            if (options.ScharrKernel) app.Invoke(factory, im => im.ApplyScharrKernel);
            if (options.RobertsCrossKernel) app.Invoke(factory, im => im.ApplyRobertsCrossKernel);
            if (options.SobelKernel) app.Invoke(factory, im => im.ApplySobelKernel);
        }
    }
}