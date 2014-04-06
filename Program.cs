
namespace SpriteSheetBuilder
{
    using System;
    using System.Drawing;
    using System.Drawing.Imaging;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;

    using Newtonsoft.Json;
    using Newtonsoft.Json.Serialization;
    using System.Collections.Generic;
    
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length < 2)
            {
                Console.WriteLine("USAGE build-sprite-sheets input-folder output-folder");

                return;
            }

            Console.WriteLine("Building sprite sheets...");

            var inputFolder = args[0];

            var outputFolder = args[1];

            var sheets = Directory
                .GetDirectories(inputFolder)
                .Select(folder =>
                {
                    var sheetName = Path.GetFileName(folder);

                    var sheetSequences = default(Sheet.Sequence[]);

                    var sheetBitmap = default(Bitmap);

                    CreateSpriteSheet(folder, out sheetSequences, out sheetBitmap);

                    sheetBitmap.Save(
                        Path.Combine(outputFolder, sheetName + ".png"), 
                        ImageFormat.Png);

                    return new Sheet
                    {
                        Name = sheetName,

                        Sequences = sheetSequences
                    };
                })
                .ToArray();

            File.WriteAllText(
                Path.Combine(outputFolder, "sheets.json"),
                JsonConvert.SerializeObject(
                    sheets,
                    Formatting.Indented,
                    new JsonSerializerSettings
                    {
                        ContractResolver = new CamelCasePropertyNamesContractResolver()
                    }));

            Console.WriteLine("Completed successfully.");
        }

        static void CreateSpriteSheet(
            string folder,
            out Sheet.Sequence[] outputSequences, 
            out Bitmap outputBitmap)
        {
            var pngSequences = Directory
                .GetFiles(folder, "*.png")
                .Select(path => new
                {
                    SequenceName = Regex.Match(Path.GetFileNameWithoutExtension(path), "^[^_]+").Value,

                    FrameNumber = int.Parse(Regex.Match(Path.GetFileNameWithoutExtension(path), "(?<=^.*?_)[0-9]+").Value),

                    Image = Bitmap.FromFile(path)
                })
                .OrderBy(frame => frame.SequenceName)
                .ThenBy(frame => frame.FrameNumber)
                .GroupBy(frame => frame.SequenceName)
                .Select(group => new FileSequence
                {
                    Name = group.Key,

                    Frames = group
                        .Select(f => new FileSequence.FileFrame
                        {
                            FrameNumber = f.FrameNumber,
                            
                            Image = f.Image
                        })
                        .ToArray()
                })
                .ToArray();

            var gifSequences = Directory
                .GetFiles(folder, "*.gif")
                .SelectMany(path => GetGifFrames(Bitmap.FromFile(path))
                    .Select((bitmap, index) => new
                    {
                        SequenceName = Path.GetFileNameWithoutExtension(path),

                        FrameNumber = index,

                        Image = bitmap
                    }))
                .OrderBy(frame => frame.SequenceName)
                .ThenBy(frame => frame.FrameNumber)
                .GroupBy(frame => frame.SequenceName)
                .Select(group => new FileSequence
                {
                    Name = group.Key,

                    Frames = group
                        .Select(f => new FileSequence.FileFrame 
                        { 
                            FrameNumber = f.FrameNumber,

                            Image = f.Image
                        })
                        .ToArray()
                })
                .ToArray();

            var sequences = pngSequences.Concat(gifSequences);    

            var rawOutput = new Bitmap(1024, 1024, PixelFormat.Format32bppArgb);

            var rawGraphics = Graphics.FromImage(rawOutput);

            var xPos = 1;

            var yPos = 1;

            var maxX = 0;

            var maxY = 0;

            var rowHeight = 0;

            outputSequences = pngSequences
                .Select(sequence => new Sheet.Sequence
                {
                    Name = sequence.Name,

                    Frames = sequence
                        .Frames
                        .Select(frame =>
                        {
                            if (xPos + frame.Image.Width + 1 > rawOutput.Width)
                            {
                                xPos = 0;

                                yPos += rowHeight + 1;

                                rowHeight = frame.Image.Height;
                            }
                            else
                            {
                                rowHeight = Math.Max(rowHeight, frame.Image.Height);
                            }

                            rawGraphics.DrawImage(frame.Image, xPos, yPos, frame.Image.Width, frame.Image.Height);

                            var f = new Sheet.Sequence.Frame
                            {
                                X = xPos,

                                Y = yPos,

                                Width = frame.Image.Width,

                                Height = frame.Image.Height
                            };

                            xPos += frame.Image.Width + 1;

                            maxX = Math.Max(maxX, xPos);

                            maxY = Math.Max(maxY, yPos + rowHeight + 1);

                            return f;
                        })
                        .ToArray()
                })
                .ToArray();

            outputBitmap = new Bitmap(maxX, maxY, PixelFormat.Format32bppArgb);

            var croppedGraphics = Graphics.FromImage(outputBitmap);

            croppedGraphics.DrawImage(rawOutput, 0, 0, rawOutput.Width, rawOutput.Height);
        }

        static Image[] GetGifFrames(Image inputImage)
        {
            var outputImages = new List<Image>();

            var frameDimension = new FrameDimension(inputImage.FrameDimensionsList[0]);

            var frameCount = inputImage.GetFrameCount(frameDimension);

            for (var i = 0; i < frameCount; i++)
            {
                inputImage.SelectActiveFrame(frameDimension, i);

                var outputImage = new Bitmap(inputImage.Width, inputImage.Height, PixelFormat.Format32bppArgb);

                var graphics = Graphics.FromImage(outputImage);

                graphics.DrawImage(inputImage, 0, 0, inputImage.Width, inputImage.Height);

                outputImages.Add(outputImage);
            }

            return outputImages.ToArray();
        }

        public class FileSequence
        {
            public string Name { get; set; }

            public FileFrame[] Frames { get; set; }
            
            public class FileFrame
            {
                public int FrameNumber { get; set; }

                public Image Image { get; set; }
            }
        }


        class Sheet
        {
            public string Name { get; set; }

            public Sequence[] Sequences { get; set; }

            public class Sequence
            {
                public string Name { get; set; }

                public Frame[] Frames { get; set; }

                public class Frame
                {
                    public int X { get; set; }

                    public int Y { get; set; }

                    public int Width { get; set; }

                    public int Height { get; set; }
                }
            }
        }
    }
}
