
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

        private static void CreateSpriteSheet(
            string folder,
            out Sheet.Sequence[] outputSequences, 
            out Bitmap outputBitmap)
        {
            var sequences = Directory
                .GetFiles(folder, "*.png")
                .Select(path => new
                {
                    SequenceName = Regex.Match(Path.GetFileNameWithoutExtension(path), "^[^_]+").Value,

                    FrameNumber = Regex.Match(Path.GetFileNameWithoutExtension(path), "(?<=^.*?_)[0-9]+").Value,

                    Bitmap = Bitmap.FromFile(path)
                })
                .OrderBy(frame => frame.SequenceName)
                .ThenBy(frame => frame.FrameNumber)
                .GroupBy(frame => frame.SequenceName)
                .Select(group => new
                {
                    Name = group.Key,

                    Frames = group.ToArray()
                })
                .ToArray();

            var rawOutput = new Bitmap(1024, 1024, PixelFormat.Format32bppArgb);

            var rawGraphics = Graphics.FromImage(rawOutput);

            var xPos = 1;

            var yPos = 1;

            var maxX = 0;

            var maxY = 0;

            var rowHeight = 0;

            outputSequences = sequences
                .Select(sequence => new Sheet.Sequence
                {
                    Name = sequence.Name,

                    Frames = sequence
                        .Frames
                        .Select(frame =>
                        {
                            if (xPos + frame.Bitmap.Width + 1 > rawOutput.Width)
                            {
                                xPos = 0;

                                yPos += rowHeight + 1;

                                rowHeight = frame.Bitmap.Height;
                            }
                            else
                            {
                                rowHeight = Math.Max(rowHeight, frame.Bitmap.Height);
                            }

                            rawGraphics.DrawImageUnscaled(frame.Bitmap, xPos, yPos);

                            var f = new Sheet.Sequence.Frame
                            {
                                X = xPos,

                                Y = yPos,

                                Width = frame.Bitmap.Width,

                                Height = frame.Bitmap.Height
                            };

                            xPos += frame.Bitmap.Width + 1;

                            maxX = Math.Max(maxX, xPos);

                            maxY = Math.Max(maxY, yPos + rowHeight + 1);

                            return f;
                        })
                        .ToArray()
                })
                .ToArray();

            outputBitmap = new Bitmap(maxX, maxY, PixelFormat.Format32bppArgb);

            var croppedGraphics = Graphics.FromImage(outputBitmap);

            croppedGraphics.DrawImageUnscaled(rawOutput, 0, 0);
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
