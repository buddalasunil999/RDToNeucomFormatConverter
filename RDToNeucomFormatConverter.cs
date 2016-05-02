using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace NIDataProcessor
{
    public partial class RDToNeucomFormatConverter : Form
    {
        public RDToNeucomFormatConverter()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (folderBrowserDialog1.ShowDialog() == DialogResult.OK)
            {
                textBox1.Text = folderBrowserDialog1.SelectedPath;
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (folderBrowserDialog1.ShowDialog() == DialogResult.OK)
            {
                textBox2.Text = folderBrowserDialog1.SelectedPath;
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            Task.Factory.StartNew(() =>
            {
                string source = textBox1.Text;
                string outputFolder = textBox2.Text;

                if (!Directory.Exists(outputFolder))
                    Directory.CreateDirectory(outputFolder);

                string outputPath = Path.Combine(Path.Combine(outputFolder, new DirectoryInfo(source).Name + ".txt"));

                StreamWriter writer = File.CreateText(outputPath);
                LargeDataset(source, writer);

                writer.Close();
            });
        }

        private void SmallDataset(string source, StreamWriter writer)
        {
            foreach (string inputFolder in Directory.GetDirectories(source))
            {
                AddItem(new DirectoryInfo(inputFolder).Name);

                ProcessOneByOne(writer, inputFolder);
            }
        }

        private void AddItem(string message)
        {
            this.Invoke(new Action<string>(AddMessage), message);
        }

        private void AddItem(int message)
        {
            this.Invoke(new Action<string>(AddMessage), message.ToString());
        }

        private void AddMessage(string message)
        {
            message += "\r\n" + textBox3.Text;
            textBox3.Text = message;
        }

        private void LargeDataset(string source, StreamWriter writer)
        {
            ProcessAll(writer, source);
        }

        private void ProcessAll(StreamWriter writer, string inputFolder)
        {
            List<ExtractedEEG> eegs = new List<ExtractedEEG>();

            foreach (string inputPath in Directory.GetFiles(inputFolder))
            {
                string filePath = Path.GetFileName(inputPath);
                string eegClass = Regex.Match(filePath, @"co2[ac]").Value.EndsWith("a") ? "1" : "2";
                AddItem(filePath);

                ReadLines(inputPath, eegs, eegClass);
            }

            IEnumerable<int> subjects = eegs.Select(x => x.Subject).Distinct();
            List<string> paradigms = new List<string> { "S1 obj", "S2 match", "S2 nomatch" };
            IEnumerable<string> features = eegs.Select(x => x.SensorPosition).Distinct();

            foreach (int subject in subjects)
            {
                AddItem(subject);
                foreach (string paradigm in paradigms)
                {
                    IEnumerable<ExtractedEEG> filteredEegs = eegs.Where(x => x.Subject == subject && x.Paradigm == paradigm);

                    List<ExtractedEEG> actualEegs = new List<ExtractedEEG>();
                    string eegClass = filteredEegs.First().Class;

                    foreach (var item in filteredEegs.GroupBy(x => new { x.SensorPosition, x.SampleNumber }))
                    {
                        double value = item.Average(x => Convert.ToDouble(x.SensorOutput));

                        ExtractedEEG first = item.First();
                        actualEegs.Add(new ExtractedEEG
                        {
                            Subject = first.Subject,
                            Paradigm = first.Paradigm,
                            SampleNumber = first.SampleNumber,
                            SensorPosition = first.SensorPosition,
                            SensorOutput = value.ToString(),
                            Class = first.Class
                        });
                    }

                    WriteEegBySample(writer, eegClass, actualEegs);
                }
            }

            AddItem("SUCCESS");
        }

        private void ProcessOneByOne(StreamWriter writer, string inputFolder)
        {
            foreach (string inputPath in Directory.GetFiles(inputFolder))
            {
                string filePath = Path.GetFileName(inputPath);
                string eegClass = Regex.Match(filePath, @"co2[ac]").Value.EndsWith("a") ? "1" : "2";
                AddItem(filePath);

                List<ExtractedEEG> eegs = new List<ExtractedEEG>();
                ReadLines(inputPath, eegs, eegClass);
                WriteEegBySample(writer, eegClass, eegs);
            }

            AddItem("SUCCESS");
        }

        private void WriteEegBySample(StreamWriter writer, string eegClass, List<ExtractedEEG> eegs)
        {
            for (int i = 0; i < 256; i++)
            {
                IEnumerable<ExtractedEEG> filteredBySample = eegs.Where(x => x.SampleNumber == i);

                foreach (ExtractedEEG extractedEeg in filteredBySample)
                {
                    writer.Write(extractedEeg.SensorOutput + " ");
                }

                writer.Write(eegClass);
                writer.WriteLine();
            }
        }

        private void ReadLines(string inputPath, List<ExtractedEEG> eegs, string eegClass)
        {
            string[] lines = File.ReadAllLines(inputPath);

            int subject = Convert.ToInt32((Regex.Match(Path.GetFileNameWithoutExtension(inputPath), @"\d{7}")).Value);

            AddItem(subject);

            string paradigm = string.Empty;

            foreach (var line in lines)
            {
                if (line.StartsWith("#"))
                {
                    Match match = Regex.Match(line, @"\bS[12]\s\w+");
                    if (match.Success)
                    {
                        paradigm = match.Value;
                    }
                }
                else
                {
                    string[] items = line.Split(' ');
                    eegs.Add(new ExtractedEEG()
                    {
                        Subject = subject,
                        Paradigm = paradigm,
                        TrailNumber = Convert.ToInt32(items[0]),
                        SampleNumber = Convert.ToInt32(items[2]),
                        SensorPosition = items[1],
                        SensorOutput = items[3],
                        Class = eegClass
                    });
                }
            }
        }
    }
}
