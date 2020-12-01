using System;
using System.Text.RegularExpressions;
using IronOcr;
using System.IO;
using Google.Cloud.Vision.V1;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Storage.v1;
using Google.Cloud.Language.V1;
using Google.Cloud.Storage.V1;
using System.Linq;

namespace ScanAndHoard
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");
            string path = @"C:\Users\d4gei\Workspace\ScanAndHoardData\TestData\PXL_20201126_130717460_2.jpg";

            //ScanImageAndSaveTxt(path);
            GoogleVisionRequest(path);

            string[] shops = new string[] { "Hofer", "Spar", "Billa", "Merkur" };

            System.IO.StreamReader file = new System.IO.StreamReader(path + ".txt");
            string line;
            string shop_name = null;
            string full_sum = null;
            string date = null;
            bool sumnow = false;
            while((line = file.ReadLine()) != null)
            {
                if(sumnow)
                {
                    Regex rx1 = new Regex(@"(\d+.\d+)");
                    MatchCollection matches1 = rx1.Matches(line);
                    foreach (Match match in matches1)
                    {
                        GroupCollection groups = match.Groups;
                        //Console.WriteLine(groups[0]);
                        full_sum = groups[0].ToString();
                    }
                    sumnow = false;
                }

                // Scan for sum
                if (line.Contains("Summe", StringComparison.OrdinalIgnoreCase))
                {
                    sumnow = true;
                }

                // Scan for date
                Regex rx = new Regex(@"([0-9][0-9]\.[0-9][0-9]\.[0-9][0-9][0-9][0-9])");
                MatchCollection matches = rx.Matches(line);
                foreach (Match match in matches)
                {
                    if (matches.Count > 0)
                    {
                        GroupCollection groups = match.Groups;
                        //Console.WriteLine(groups[0]);
                        date = groups[0].ToString();
                    }
                }

                if(shop_name == null)
                {
                    foreach (string shop in shops)
                    {
                        if (line.Contains(shop, StringComparison.OrdinalIgnoreCase))
                        {
                            //Console.WriteLine(line);
                            shop_name = shop;
                        }
                    }
                }
                
                
            }

            Console.WriteLine($"Receipt: Date: { date } Shop: { shop_name } Sum: { full_sum }");

            /*using (var Input = new OcrInput())
            {
                Input.AddImage(@"C:\Users\d4gei\Workspace\ScanAndHoardData\TestData\PXL_20201126_130717460_2.jpg");
                Input.DeNoise();  //fixes digital noise  
                Input.Deskew();   //fixes rotation and perspective  
                                  // there are dozens more filters, but most users wont need them
                IronOcr.OcrResult Result = Ocr.Read(Input);
                Console.WriteLine(Result.Text);
                Result.SaveAsTextFile(C: \Users\d4gei\Workspace\ScanAndHoardData\TestData\PXL_20201126_130717460_2.txt)
                foreach (var Page in Result.Pages)
                {
                    int PageNumber = Page.PageNumber;

                    foreach (var Paragraph in Page.Paragraphs)
                    {
                        foreach (var Line in Paragraph.Lines)
                        {
                            if (Line.Text.Contains("Summe", StringComparison.OrdinalIgnoreCase))
                            {
                                Console.WriteLine(Line.Text);
                            }

                            Regex rx = new Regex(@"[0-9][0-9].[0-9][0-9].[0-9][0-9][0-9][0-9]");
                            MatchCollection matches = rx.Matches(Line.Text);

                            if (matches.Count > 0)
                            {
                                Console.WriteLine(Line.Text);
                            }

                            //Console.WriteLine(Line.Text);
                        }

                    }
                }
            }
            */

            Console.ReadKey();
        }

        static void GoogleVisionRequest(string path)
        {

            ImageAnnotatorClientBuilder builder = new ImageAnnotatorClientBuilder
            {
                CredentialsPath = @"C:\Users\d4gei\Workspace\ScanAndHoardData\scanandhoard-11700d373751.json"
            };

            ImageAnnotatorClient client = builder.Build();

            var image = Image.FromFile(path);
            TextAnnotation text = client.DetectDocumentText(image);

            string fileName = path + ".txt";
            using (StreamWriter writer = new StreamWriter(fileName))
            {
                writer.Write(text.Text);
            }
            Console.WriteLine($"Text: {text.Text}");
            foreach (var page in text.Pages)
            {
                foreach (var block in page.Blocks)
                {
                    string box = string.Join(" - ", block.BoundingBox.Vertices.Select(v => $"({v.X}, {v.Y})"));
                    Console.WriteLine($"Block {block.BlockType} at {box}");
                    foreach (var paragraph in block.Paragraphs)
                    {
                        box = string.Join(" - ", paragraph.BoundingBox.Vertices.Select(v => $"({v.X}, {v.Y})"));
                        Console.WriteLine($"  Paragraph at {box}");
                        foreach (var word in paragraph.Words)
                        {
                            Console.WriteLine($"    Word: {string.Join("", word.Symbols.Select(s => s.Text))}");
                        }
                    }
                }
            }
        }






        static void ScanImageAndSaveTxt(string path)
        {
            var Ocr = new IronTesseract();

            Ocr.Configuration.BlackListCharacters = "~`$#^*_}{][|\\@-©<>«=";
            Ocr.Configuration.TesseractVersion = TesseractVersion.Tesseract4;
            Ocr.Configuration.RenderSearchablePdfsAndHocr = true;
            Ocr.Configuration.PageSegmentationMode = TesseractPageSegmentationMode.Auto;
           
            Ocr.Language = OcrLanguage.GermanBest;

            // var result = Ocr.Read(@"C:\Users\d4gei\Workspace\ScanAndHoardData\TestData\PXL_20201126_130717460.jpg");

            //Console.WriteLine(result.Text);


            using (var Input = new OcrInput())
            {
                Input.AddImage(path);
                Input.DeNoise();  //fixes digital noise  
                Input.Deskew();   //fixes rotation and perspective  
                                  // there are dozens more filters, but most users wont need them
                IronOcr.OcrResult Result = Ocr.Read(Input);
                Console.WriteLine(Result.Text);
                Result.SaveAsTextFile(path + ".txt");
                Result.SaveAsHocrFile(path + ".html");
                Result.SaveAsSearchablePdf(path + ".pdf");
            }
        }
    }
}
