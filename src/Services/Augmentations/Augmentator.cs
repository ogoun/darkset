using Darknet.Dataset.Merger.Model;
using System;
using System.IO;
using System.Linq;
using System.Text;

namespace Darknet.Dataset.Merger.Services
{
    public static class Augmentator
    {
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

            if (options.HasAugmentation == false && options.Cut == false && options.ResizeToInput == false)
            {
                // Простое копирование
                var (imgName, lblName) = context.GetNextObjFileNames();
                File.Copy(imageInfo.FilePath, imgName);
                File.WriteAllText(lblName, local.SourceAnnotationText);
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
                            File.WriteAllText(lblName, local.SourceAnnotationText);

                            ApplyAugmentations(original, local, context, options);
                        }
                    }
                    else
                    {
                        using (var original = factory.Clone())
                        {
                            // Save original image
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

                                    if (options.ResizeToInput)
                                    {
                                        using (var r = resizeToInput(cropped.Item1))
                                        {
                                            store(r, croppedContext.SourceAnnotationText);
                                            ApplyAugmentations(r, croppedContext, context, options);
                                        }
                                    }
                                    else
                                    {
                                        store(cropped.Item1, croppedContext.SourceAnnotationText);
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

        private static void ApplyAugmentations(AImage factory, LocalAnnotationContext localContext,
            AugmentationContext context, AugmentationOptions options)
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

            if (options.BBoxBlur && options.LinesNoise)
            {
                if (factory.BlurBoxes(boxes))
                {
                    factory.LineNoize();
                    storeWithBB(factory, boxes);
                    factory.Reset();
                }
                else
                {
                    factory.LineNoize();
                    store(factory, localContext.SourceAnnotationText);
                    factory.Reset();
                }
            }
            else if (options.LinesNoise)
            {
                app.Invoke(factory, im => im.LineNoize);
            }
            else if (options.BBoxBlur)
            {
                if (factory.BlurBoxes(boxes))
                {
                    storeWithBB(factory, boxes);
                    factory.Reset();
                }
            }
            if (options.Blur) app.Invoke(factory, im => im.Blur);
            if (options.Grayscale) app.Invoke(factory, im => im.Grayscale);
            if (options.Sepia) app.Invoke(factory, im => im.SepiaTone);
        }
    }
}