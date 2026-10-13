using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace WordFrequency
{
    class Program
    {
        static void Main()
        {
            string dir = @"D:\retete_anul3\sisteme de recunoaste\test1\test1\test1";
            string training = Path.Combine(dir, "Reuters", "Reuters_34", "Training");

            var stop = File.ReadAllLines(Path.Combine(dir, "stopwords.txt"))
                           .Select(w => w.Trim().ToLower()).ToHashSet();
            var stemmer = new PorterStemmer();
            var words = new SortedSet<string>();

            foreach (string file in Directory.GetFiles(training, "*.xml", SearchOption.AllDirectories))
            {
                var root = XDocument.Load(file).Root;
                string content = root.Element("title")?.Value + " " + root.Element("text")?.Value;

                foreach (Match m in Regex.Matches(content.ToLower(), "[a-z]+"))
                    if (m.Value.Length > 2 && !stop.Contains(m.Value))
                        words.Add(stemmer.StemWord(m.Value));
            }

            File.WriteAllLines(Path.Combine(dir, "words.txt"), words);
            Console.WriteLine($"Nr de cuvinte: {words.Count}");
        }
    }
}