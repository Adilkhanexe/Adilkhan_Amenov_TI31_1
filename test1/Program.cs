using System;
using System.Collections.Generic;
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
            string testing = Path.Combine(dir, "Reuters", "Reuters_34", "Testing");
            var stop = File.ReadAllLines(Path.Combine(dir, "stopwords.txt"))
                           .Select(w => w.Trim().ToLower()).ToHashSet();
            var stemmer = new PorterStemmer();
            var words = new SortedSet<string>();
            
            foreach (string folder in new[] { training, testing })
            {
                foreach (string file in Directory.GetFiles(folder, "*.xml", SearchOption.AllDirectories))
                {
                    var root = XDocument.Load(file).Root;
                    string content = root.Element("title")?.Value + " " + root.Element("text")?.Value;
                    foreach (Match m in Regex.Matches(content.ToLower(), "[a-z]+"))
                        if (m.Value.Length > 2 && !stop.Contains(m.Value))
                        {
                            string stemmed = stemmer.StemWord(m.Value);
                            if (stemmed.Length > 2)
                                words.Add(stemmed);
                        }
                }
            }
            File.WriteAllLines(Path.Combine(dir, "words.txt"), words);
            Console.WriteLine($"Nr de cuvinte: {words.Count}");
           
            Clusters(dir, training, testing, stop, stemmer, words);
        }
        static void Clusters(string dir, string training, string testing,
                     HashSet<string> stop, PorterStemmer stemmer, SortedSet<string> words)
        {
            var index = words.Select((w, i) => (w, i)).ToDictionary(x => x.w, x => x.i);
            var lines = words.Select(w => "@attribute " + w).ToList();
            lines.Add("@data");
            
            foreach (var (folder, label) in new[] { (training, "Training"), (testing, "Testing") })
            {
                foreach (string file in Directory.GetFiles(folder, "*.xml", SearchOption.AllDirectories))
                {
                    var root = XDocument.Load(file).Root;
                    string content = root.Element("title")?.Value + " " + root.Element("text")?.Value;
                    var freq = new SortedDictionary<int, int>();
                    foreach (Match m in Regex.Matches(content.ToLower(), "[a-z]+"))
                        if (m.Value.Length > 2 && !stop.Contains(m.Value))
                        {
                            string stemmed = stemmer.StemWord(m.Value);
                            if (stemmed.Length > 2 && index.ContainsKey(stemmed))
                            {
                                int i = index[stemmed];
                                freq[i] = freq.GetValueOrDefault(i) + 1;
                            }
                        }
                   
                    var metadata = root.Element("metadata");
                    string topics = "";
                    if (metadata != null)
                    {
                        topics = string.Join(" ", metadata.Elements("codes")
                            .Where(c => (string)c.Attribute("class") == "bip:topics:1.0")
                            .Elements("code")
                            .Select(c => (string)c.Attribute("code")));
                    }
                    string id = Path.GetFileNameWithoutExtension(file)
                                    .Replace("NEWS", "").Replace("NE", "").Replace("NEW", "");
                    lines.Add($"{id} # {string.Join(" ", freq.Select(p => $"{p.Key}:{p.Value}"))} # {topics} # {label}");
                }
            }
            File.WriteAllLines(Path.Combine(dir, "clusters.txt"), lines);
            Console.WriteLine($"Fisierul clusters.txt a fost creat cu {lines.Count} linii.");
        }
    }
}