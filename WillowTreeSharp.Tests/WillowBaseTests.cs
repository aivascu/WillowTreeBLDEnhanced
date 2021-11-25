using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using WillowTree.Services.DataAccess;

namespace Test
{
    [TestClass]
    public class WillowBaseTests
    {
        private string GetOutputName(string outputDir, string fileName)
        {
            return outputDir + @"\" + fileName.Split('.')[0] + ".txt";
        }

        [TestMethod]
        public void ReadExtended()
        {
            string path = Directory.GetCurrentDirectory() + @"\ReadTest\Extended";

            DirectoryInfo d = new DirectoryInfo(path); //Assuming Test is your Folder
            FileInfo[] files = d.GetFiles("*.sav"); //Getting Text files
            var total = files.Length;
            int count = 0;
            ConsoleOutput consoleOutput;
            DirectoryInfo outputDir = Directory.CreateDirectory(Directory.GetCurrentDirectory() + @"\Logs\Extended");
            foreach (var file in outputDir.GetFiles("*.txt"))
            {
                file.Delete();
            }

            foreach (var collection in files)
            {
                bool success = true;
                consoleOutput = new ConsoleOutput();
                try
                {
                    var ws = WillowSaveGameSerializer.ReadFile(collection.FullName, false);
                    count++;
                    consoleOutput.Dispose();
                }
                catch (Exception e)
                {
                    Console.WriteLine(@"========== Exception ==========");
                    Console.WriteLine(e);

                    File.WriteAllText(GetOutputName(outputDir.FullName, collection.Name), consoleOutput.GetOuput());
                    consoleOutput.Dispose();
                    success = false;
                }

                Console.WriteLine(collection.Name + @" : " + success);
            }

            Console.WriteLine(count + @"/" + total + @"(" + ((float)count / total) * 100f + @"%)");
            Assert.AreEqual(total, count);
        }

        [TestMethod]
        public void ReadOld()
        {
            string path = Directory.GetCurrentDirectory() + @"\ReadTest\Vanilla";

            DirectoryInfo d = new DirectoryInfo(path); //Assuming Test is your Folder
            FileInfo[] files = d.GetFiles("*.sav"); //Getting Text files
            var total = files.Length;
            int count = 0;
            ConsoleOutput consoleOutput;
            DirectoryInfo outputDir = Directory.CreateDirectory(Directory.GetCurrentDirectory() + @"\Logs\Vanilla");
            foreach (var file in outputDir.GetFiles("*.txt"))
            {
                file.Delete();
            }

            foreach (var collection in files)
            {
                bool success = true;
                consoleOutput = new ConsoleOutput();
                try
                {
                    var ws = WillowSaveGameSerializer.ReadFile(collection.FullName, false);
                    count++;
                    consoleOutput.Dispose();
                }
                catch (Exception e)
                {
                    Console.WriteLine(@"========== Exception ==========");
                    Console.WriteLine(e);

                    File.WriteAllText(GetOutputName(outputDir.FullName, collection.Name), consoleOutput.GetOuput());
                    consoleOutput.Dispose();
                    success = false;
                }

                Console.WriteLine(collection.Name + @" : " + success);
            }

            Console.WriteLine(count + @"/" + total + @"(" + ((float)count / total) * 100f + @"%)");
            Assert.AreEqual(total, count);
        }

        [TestMethod]
        public void Write()
        {
            string path = Directory.GetCurrentDirectory() + @"\ReadTest\Extended";

            DirectoryInfo d = new DirectoryInfo(path); //Assuming Test is your Folder
            FileInfo[] files = d.GetFiles("*.sav"); //Getting Text files
            var total = files.Length;
            int count = 0;
            ConsoleOutput consoleOutput;
            DirectoryInfo outputDir = Directory.CreateDirectory(Directory.GetCurrentDirectory() + @"\Logs\Extended");
            foreach (var file in outputDir.GetFiles("*.txt"))
            {
                file.Delete();
            }

            foreach (var collection in files)
            {
                bool success = true;
                consoleOutput = new ConsoleOutput();
                try
                {
                    var ws = WillowSaveGameSerializer.ReadFile(collection.FullName, false);
                    var output = outputDir.FullName + @"\" + collection.Name;
                    WillowSaveGameSerializer.WriteToFile(ws, output);

                    ws = WillowSaveGameSerializer.ReadFile(output, false);
                    count++;
                    consoleOutput.Dispose();
                }
                catch (Exception e)
                {
                    Console.WriteLine(@"========== Exception ==========");
                    Console.WriteLine(e);

                    File.WriteAllText(GetOutputName(outputDir.FullName, collection.Name), consoleOutput.GetOuput());
                    consoleOutput.Dispose();
                    success = false;
                }

                Console.WriteLine(collection.Name + @" : " + success);
            }

            Console.WriteLine(count + @"/" + total + @"(" + ((float)count / total) * 100f + @"%)");
            Assert.AreEqual(total, count);
        }
    }
}
