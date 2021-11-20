using Darknet.Dataset.Merger.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Windows;
using ZeroLevel;

namespace Darknet.Dataset.Merger.Services
{
    public static class DatasetMerger
    {
        public static void Merge(MergeOptions options, Action<int> progress)
        {
            if (PrepareRootFolder(options.TargetFolder) == false) return;
            if (PrepareObjFolder(options.TargetFolder) == false) return;
            if (PrepareAugFolder(options.TargetFolder) == false) return;

            var objFolder = Path.Combine(options.TargetFolder, "obj");

            var files = new List<string>();
            var counter = 0;
            var classIndex = 0;
            var classes = new Dictionary<string, int>();
            var classesNames = new Dictionary<string, int>();
            var lastIndex = -1;
            foreach (var c in options.Classes.OrderBy(c => c.Index))
            {
                if (c.Active)
                {
                    if (lastIndex != -1 && lastIndex != c.Index)
                    {
                        classIndex++;
                    }

                    classes.Add(c.Name, classIndex);
                    if (c.IsPriorityInIndex)
                    {
                        classesNames.Add(c.Name, classIndex);
                    }
                    lastIndex = c.Index;
                }
            }

            var augmentationContext = new AugmentationContext(counter, objFolder, files)
            {
                Classes = classes
            };
            int image_progress = 0;
            foreach (var dataset in options.Datasets)
            {
                foreach (var image in dataset.Images)
                {
                    try
                    {
                        if (options.WithoutClass == false)
                        {
                            if (!image.Annotations.Any(a => classes.ContainsKey(a.Label))) continue;
                        }
                        var imagePath = image.FilePath;
                        if (File.Exists(imagePath) == false)
                        {
                            imagePath = Path.Combine(dataset.DatasetPath, "obj", Path.GetFileNameWithoutExtension(image.FilePath));
                        }
                        if (File.Exists(imagePath) == false) continue;

                        var sb = new StringBuilder();
                        foreach (var a in image.Annotations)
                        {
                            if (classes.ContainsKey(a.Label))
                            {
                                sb.Append($"{classes[a.Label]} {a.Cx.ConvertToString()} {a.Cy.ConvertToString()} {a.Width.ConvertToString()} {a.Height.ConvertToString()}");
                                sb.Append("\n");
                            }
                        }
                        counter = Augmentator.Augmentate(image, augmentationContext, dataset.Augmentations);
                        progress(image_progress);
                        image_progress++;
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "[DatasetMerger.Merge] Augmentations");
                    }
                }
            }
            // Store sets
            int test_step = 10;
            var trainData = new StringBuilder();
            var testData = new StringBuilder();
            for (int i = 0; i < files.Count; i++)
            {
                if (i != 0 && i % test_step == 0)
                {
                    testData.Append(files[i]);
                    testData.Append("\n");
                }
                else
                {
                    trainData.Append(files[i]);
                    trainData.Append("\n");
                }
            }
            File.WriteAllLines(Path.Combine(options.TargetFolder, "obj.names"), classesNames.Select(pair => pair.Key));
            File.WriteAllText(Path.Combine(options.TargetFolder, "train.txt"), trainData.ToString());
            File.WriteAllText(Path.Combine(options.TargetFolder, "test.txt"), testData.ToString());
            // Store data
            var dataInfo = new StringBuilder();
            dataInfo.Append($"classes= {classesNames.Count}");
            dataInfo.Append("\n");
            dataInfo.Append($"train= train.txt");
            dataInfo.Append("\n");
            dataInfo.Append($"valid= test.txt");
            dataInfo.Append("\n");
            dataInfo.Append($"names= obj.names");
            dataInfo.Append("\n");
            dataInfo.Append($"backup= backup/");
            File.WriteAllText(Path.Combine(options.TargetFolder, "obj.data"), dataInfo.ToString());
        }



        private static IEnumerable<string> Shuffle(IEnumerable<string> lines)
        {
            using (var rnd = new RNGCryptoServiceProvider())
            {
                return lines.OrderBy(x => GetNextInt32(rnd));
            }
        }

        private static int GetNextInt32(RNGCryptoServiceProvider rnd)
        {
            byte[] randomInt = new byte[4];
            rnd.GetBytes(randomInt);
            return Convert.ToInt32(randomInt[0]);
        }

        private static bool PrepareRootFolder(string path)
        {
            if (Directory.Exists(path))
            {
                if (Directory.GetFiles(path).Length > 0 || Directory.GetDirectories(path).Length > 0)
                {
                    MessageBox.Show($"Target directory '{path}' already exists and is not empty, please remove, move or rename before merge");
                    return false;
                }
            }
            else
            {
                Directory.CreateDirectory(path);
            }
            return true;
        }

        private static bool PrepareObjFolder(string root)
        {
            string path = Path.Combine(root, "obj");
            if (Directory.Exists(path))
            {
                if (Directory.GetFiles(path).Length > 0 || Directory.GetDirectories(path).Length > 0)
                {
                    MessageBox.Show($"Target directory '{path}' already exists and is not empty, please remove, move or rename before merge");
                    return false;
                }
            }
            else
            {
                Directory.CreateDirectory(path);
            }
            return true;
        }

        private static bool PrepareAugFolder(string root)
        {
            string path = Path.Combine(root, "obj", "aug");
            if (Directory.Exists(path))
            {
                if (Directory.GetFiles(path).Length > 0 || Directory.GetDirectories(path).Length > 0)
                {
                    MessageBox.Show($"Target directory '{path}' already exists and is not empty, please remove, move or rename before merge");
                    return false;
                }
            }
            else
            {
                Directory.CreateDirectory(path);
            }
            return true;
        }
    }
}
