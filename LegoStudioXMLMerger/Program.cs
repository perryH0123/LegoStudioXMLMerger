using System;
using System.IO;
using System.Xml;
using System.Collections.Generic;
using System.Xml.Linq;
using System.Diagnostics;

namespace LegoStudioXMLMerger
{
    class Program
    {
        static void Main(string[] args)
        {
            if(args.Length == 0)
            {
                Console.WriteLine("Please specify at least one file. Press enter to quit.");
                Console.ReadLine();
                Environment.Exit(2);
            }

            bool quantityIgnored = args.AsSpan().Contains("-nq");
            bool showFullStackTrace = args.AsSpan().Contains("-s");
            int howManyOptions = 0;
            foreach (string arg in args) if (arg.StartsWith("-")) howManyOptions++;

            var currentDirectory = AppDomain.CurrentDomain.BaseDirectory;
            Dictionary<Item, int>[] partsLists = new Dictionary<Item, int>[args.Length - howManyOptions];
            int i = 0;
            string defaultName = "Merged_";
            foreach (string file in args)
            {
                if (file.StartsWith("-")) continue;
                Dictionary<Item, int> partsList = new Dictionary<Item, int>();
                var path = file.StartsWith("/") ? file : $"{currentDirectory}/{file}";
                ValidateFile(currentDirectory, file);
                ProcessFile(path, partsList, showFullStackTrace);
                partsLists[i] = partsList;
                defaultName += $"{ TrimAbsoluteFileNameAndExtension(file)}+";
                i++;
            }
            var mergedPartList = MergeDictionaries(partsLists);
            foreach (Item item in mergedPartList.Keys)
            {
                Console.WriteLine($"{item.ToString()} \t Qty: {mergedPartList[item]}");
            }
            if (quantityIgnored)
            {
                Console.ForegroundColor = ConsoleColor.DarkCyan;
                Console.WriteLine($"Options: You are on no quantity mode. Proceed if you would like quantities to be omitted from the XML.");
                Console.ResetColor();
            }
            Console.WriteLine("\nA preview is shown above. Press y to continue with the operation. Press any other key to cancel.");
            switch (Console.ReadKey(true).Key)
            {
                case ConsoleKey.Y:
                    string fName, fDirectory;
                    Console.Write("File name (don't include an extension or path): ");
                    fName = TrimAbsoluteFileNameAndExtension(Console.ReadLine());
                    if (string.IsNullOrWhiteSpace(fName)) 
                    {
                        fName = defaultName;
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine($"Warning: Invalid file name. Default file name of {fName} used");
                        Console.ResetColor();
                    }
                    Console.WriteLine();

                    Console.Write("Directory (leave blank for current directory): ");
                    fDirectory = Console.ReadLine();
                    if (string.IsNullOrWhiteSpace(fDirectory))
                    {
                        fDirectory = currentDirectory + "/output";
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine($"Warning: Invalid directory name. Default directory [{currentDirectory}] used");
                        Console.ResetColor();
                    }
                    Console.WriteLine();
                    var xmlFile = DictionaryToXML(mergedPartList);
                    try
                    {
                        var fileName = $"{fDirectory}/{fName}.xml";
                        xmlFile.Save(fileName);
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine($"Saved to {fileName}");
                        Console.ResetColor();
                        if (!fileName.StartsWith("/")) fileName = "/" + fileName;
                        try
                        {
                            Process.Start("explorer.exe", $"/select,\"{fileName}\"");
                        }catch(Exception e) { };

                    } catch (Exception e)
                    {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine($"Error: Exception {e.GetType().ToString()} occured during saving with the following message: {e.Message}");
                        if (showFullStackTrace)
                        {
                            Console.WriteLine(e.StackTrace);
                        }
                        else Console.WriteLine("Rerun the program with -s to see the whole stack trace.");
                        Console.ResetColor();
                    }
                    break;
                default:
                    Console.Clear();
                    Console.WriteLine("Operation cancelled by user");
                    break;
            }
        }

        static void ValidateFile(string currentDirectory, string path)
        {    
            if (!File.Exists(path))
            {
                Console.WriteLine($"File {path} not found.  Press enter to quit.");
                Console.ReadLine();
                Environment.Exit(3);
            }
            if(new FileInfo(path).Extension.ToLower() != ".xml")
            {
                Console.WriteLine($"File {path} is not an xml file (type = {new FileInfo(path).Extension.ToLower()}). Press enter to quit.");
                Console.ReadLine();
                Environment.Exit(2);
            }
        }
        
        static void ProcessFile(string filePath, Dictionary<Item, int> partsList, bool showFullStackTrace = false)
        {
            //quick fix for windows
            filePath = filePath.Replace("\"", "");
            filePath = filePath.Replace("\'", "");
            XmlDocument doc = new XmlDocument();
            try
            {
                Stream s = new FileStream(filePath,FileMode.Open);
                doc.Load(s);
                s.Close();
            } catch(Exception e)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"Error: Exception {e.GetType().ToString()} occured during loading file {filePath} with the following message: {e.Message}");
                if (showFullStackTrace)
                {
                    Console.WriteLine(e.StackTrace);
                }
                else Console.WriteLine("Rerun the program with -s to see the whole stack trace.");
                Console.ResetColor();
            }

            XmlElement root = doc.DocumentElement;
            if(root.Name != "INVENTORY")
            {
                Console.WriteLine($"File {filePath} is not an LEGO parts list xml file. Make sure you got this from LEGO Studio. Press enter to quit.");
                Console.ReadLine();
                Environment.Exit(2);
            }
            foreach (XmlNode part in root.ChildNodes)
            {
                Item i = new Item();
                foreach (XmlNode partAttribute in part.ChildNodes)
                {
                    if (partAttribute.Name == "ITEMID") i.Id = partAttribute.InnerText;
                    if (partAttribute.Name == "ITEMKEY") {
                        int parseRes;
                        if (int.TryParse(partAttribute.InnerText, out parseRes))
                        {
                            i.Key = parseRes;
                        }
                    };
                    if (partAttribute.Name == "COLOR")
                    {
                        int parseRes;
                        if (int.TryParse(partAttribute.InnerText, out parseRes)) i.Color = parseRes;
                    }
                    if (partAttribute.Name == "MINQTY")
                    {
                        int parseRes;
                        if(int.TryParse(partAttribute.InnerText, out parseRes))
                        {
                            partsList.Add(i, parseRes);
                        }
                    }
                }
            }
        }

        /*
         * Merge two similar dictionaries toegther, adding up values for duplicate keys.
         * Runtime: O(n)
         */
        static Dictionary<Item, int> MergeDictionaries(params Dictionary<Item, int>[] dicts)
        {
            Dictionary<Item, int> newDict = new Dictionary<Item, int>();
            foreach(var dict in dicts)
            {
                foreach(Item key in dict.Keys){
                    if (newDict.ContainsKey(key))
                    {
                        newDict[key] += dict[key];
                    }
                    else newDict[key] = dict[key];
                }
            }
            return newDict;
        }

        static string TrimAbsoluteFileNameAndExtension(string path)
        {
            var pathSplit = path.Split("/");
            var fileNameWithExt = pathSplit[pathSplit.Length - 1];
            if (!fileNameWithExt.Contains(".")) return fileNameWithExt;
            var fileSplitByPeriods = fileNameWithExt.Split(".");
            //slice off the content after the last period (the extension)
            return String.Join(".",new ReadOnlySpan<String>(fileSplitByPeriods, 0, fileSplitByPeriods.Length - 1).ToArray());
        }

        static XmlDocument DictionaryToXML(Dictionary<Item, int> partsList, bool quantityIgnored = false)
        {
            XmlDocument XMLDoc = new XmlDocument();
            XmlElement root = XMLDoc.CreateElement("INVENTORY");
            XMLDoc.AppendChild(root);
            foreach (Item part in partsList.Keys)
            {
                XmlElement item = XMLDoc.CreateElement("ITEM");

                XmlElement itemType = XMLDoc.CreateElement("ITEMTYPE");
                itemType.InnerText = Item.itemType;
                item.AppendChild(itemType);

                XmlElement itemId = XMLDoc.CreateElement("ITEMID");
                itemId.InnerText = part.Id;
                item.AppendChild(itemId);

                XmlElement itemKey = XMLDoc.CreateElement("ITEMKEY");
                itemKey.InnerText = part.Key.ToString();
                item.AppendChild(itemKey);

                XmlElement color = XMLDoc.CreateElement("COLOR");
                color.InnerText = part.Color.ToString();
                item.AppendChild(color);

                if (!quantityIgnored)
                {
                    XmlElement minqty = XMLDoc.CreateElement("MINQTY");
                    minqty.InnerText = partsList[part].ToString();
                    item.AppendChild(minqty);
                }
                root.AppendChild(item);
                
            }
            return XMLDoc;
        }
    }
}

