using Darknet.Dataset.Merger.Model;
using System;
using System.IO;
using System.Linq;
using System.Text;

namespace Darknet.Dataset.Merger.Services
{
    public static class Augmentator
    {
        private const float SIDE_LIMIT = 896.0f;

        public static int Augmentate(ImageInfo imageInfo, AugmentationContext context, AugmentationOptions options)
        {
            var sb = new StringBuilder();
            foreach (var a in imageInfo.Annotations)
            {
                if (context.Classes.ContainsKey(a.Label))
                {
                    sb.Append($"{context.Classes[a.Label]} {a.Cx.ConvertToString()} {a.Cy.ConvertToString()} {a.Width.ConvertToString()} {a.Height.ConvertToString()}");
                    sb.Append("\n");
                }
            }
            var local = new LocalAnnotationContext { SourceAnnotationText = sb.ToString(), Image = imageInfo };

            if (options.HasAugmentation == false && options.Cut == false && options.ResizeOriginal == false)
            {
                // Простое копирование
                var (imgName, lblName) = context.GetNextObjFileNames();
                File.Copy(imageInfo.FilePath, imgName);
                File.WriteAllText(lblName, local.SourceAnnotationText);
            }
            else
            {
                var resize = new Func<AImage, AImage>(img =>
                {
                    if (img.Width > SIDE_LIMIT || img.Height > SIDE_LIMIT)
                    {
                        float scale = 1.0f;
                        if (img.Width < img.Height)
                        {
                            scale = SIDE_LIMIT / img.Width;
                        }
                        else
                        {
                            scale = SIDE_LIMIT / img.Height;
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
                    if (options.ResizeOriginal)
                    {
                        using (var original = resize(factory.Clone()))
                        {
                            // Save resized original image
                            var (imgName, lblName) = context.GetNextObjFileNames();
                            original.Write(imgName);
                            File.WriteAllText(lblName, local.SourceAnnotationText);

                            ApplyAugmentations(original, local, context, options);
                        }
                    }
                    else
                    {
                        using (var original = factory.Clone())
                        {
                            // Save resized original image
                            var (imgName, lblName) = context.GetNextObjFileNames();
                            original.Write(imgName);
                            File.WriteAllText(lblName, local.SourceAnnotationText);

                            ApplyAugmentations(original, local, context, options);
                        }
                    }
                    if ((context.WithoutClass || local.Image.Annotations.Any())
                        && options.Cut
                        && options.CutWidth > 0
                        && options.CutHeight > 0
                        && options.CutWidth < (factory.Width / 1.5)
                        && options.CutHeight < (factory.Height / 1.5))
                    {
                        foreach (var cropped in factory.Crop(local.Image.Annotations, options.CutWidth, options.CutHeight, options.CutOverlaps))
                        {
                            using (cropped.Item1)
                            {
                                var sbCropped = new StringBuilder();
                                foreach (var cropped_annotation in cropped.Item2)
                                {
                                    if (context.Classes.ContainsKey(cropped_annotation.Label))
                                    {
                                        sbCropped.Append($"{context.Classes[cropped_annotation.Label]} {cropped_annotation.Cx.ConvertToString()} {cropped_annotation.Cy.ConvertToString()} {cropped_annotation.Width.ConvertToString()} {cropped_annotation.Height.ConvertToString()}");
                                        sbCropped.Append("\n");
                                    }
                                }
                                if (context.WithoutClass || sbCropped.Length > 0) // no augmentation for empty crops
                                {
                                    var croppedContext = new LocalAnnotationContext { SourceAnnotationText = sbCropped.ToString(), Image = new ImageInfo(imageInfo.FilePath, imageInfo.TrainType, cropped.Item2) };
                                    using (var croppedAndResized = resize(cropped.Item1))
                                    {
                                        store(croppedAndResized, croppedContext.SourceAnnotationText);
                                        ApplyAugmentations(cropped.Item1, croppedContext, context, options);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            return context.Counter;
        }

        private static void ApplyAugmentations(AImage factory, LocalAnnotationContext localContext, AugmentationContext context, AugmentationOptions options)
        {
            var boxes = new BBOXES(localContext.Image.Annotations.Where(a => context.Classes.ContainsKey(a.Label)), factory.Width, factory.Height);

            var store = new Action<AImage, string>((ifac, ann) =>
            {
                var (imgName, lblName) = context.GetNextAugFileNames();
                ifac.Write(imgName);
                File.WriteAllText(lblName, ann);
            });
            var storeWithBB = new Action<AImage, BBOXES>((ifac, bb) =>
            {
                var (imgName, lblName) = context.GetNextAugFileNames();
                ifac.Write(imgName);
                File.WriteAllText(lblName, bb.ToString());
            });
            var app = new Action<AImage, Func<AImage, Action>>((img, gf) =>
            {
                gf(img).Invoke();
                store(img, localContext.SourceAnnotationText);
                img.Reset();
            });

            if (options.Blur) app.Invoke(factory, im => im.Blur);
            if (options.Grayscale) app.Invoke(factory, im => im.Grayscale);
            if (options.Moonlight) app.Invoke(factory, im => im.Moonlight);
            if (options.Noise) app.Invoke(factory, im => im.Noise);
            if (options.Sepia) app.Invoke(factory, im => im.SepiaTone);
            if (options.Charcoal) app.Invoke(factory, im => im.Charcoal);
            if (options.LinesNoise) app.Invoke(factory, im => im.LineNoize);
            if (options.Sin) app.Invoke(factory, im => im.FSin);

            if (options.Mirrors)
            {
                var horizontallAnn = new StringBuilder();
                var verticalAnn = new StringBuilder();
                localContext.Image.Annotations.Apply(a =>
                {
                    if (context.Classes.ContainsKey(a.Label))
                    {
                        horizontallAnn.Append($"{context.Classes[a.Label]} {(1.0f - a.Cx).ConvertToString()} {a.Cy.ConvertToString()} {a.Width.ConvertToString()} {a.Height.ConvertToString()}");
                        horizontallAnn.Append("\n");
                        verticalAnn.Append($"{context.Classes[a.Label]} {a.Cx.ConvertToString()} {(1 - a.Cy).ConvertToString()} {a.Width.ConvertToString()} {a.Height.ConvertToString()}");
                        verticalAnn.Append("\n");
                    }
                });
                factory.Flop();
                store(factory, horizontallAnn.ToString());
                factory.Reset();
                factory.Flip();
                store(factory, verticalAnn.ToString());
                factory.Reset();
            }
            if (options.BBoxBlur)
            {
                factory.BlurBoxes(boxes);
                storeWithBB(factory, boxes);
                factory.Reset();
            }
        }
    }
}