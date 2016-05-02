using System;
using System.Windows.Forms;

namespace NIDataProcessor
{
    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new RDToNeucomFormatConverter());
        }
    }

    public class ExtractedEEG
    {
        public int TrailNumber { set; get; }
        public string SensorPosition { set; get; }
        public int SampleNumber { set; get; }
        public string SensorOutput { set; get; }
        public int Subject { get; internal set; }
        public string Paradigm { get; internal set; }
        public string Class { get; internal set; }
    }
}
