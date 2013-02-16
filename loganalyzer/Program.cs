using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Text.RegularExpressions;

//keyword: C# regex cheatsheet
//http://www.mikesdotnetting.com/Article/46/CSharp-Regular-Expressions-Cheat-Sheet

namespace loganalyzer
{
    public class LogAnalyzer
    {
        public struct ErrorLogChunk
        {
            public string Message;
            public string Content;
        }

        private string fileName;
        private string outputFileName;
        private FileStream fs;
        private string[] contentStringSplit;
        private List<ErrorLogChunk> ListLog = new List<ErrorLogChunk>();

        private readonly string[] ArrLogTargetMessage = 
            {
                "script not found or unable to stat",
                "attempt to invoke directory as script",
                "client denied by server configuration",
                "File does not exist"
            };

        public LogAnalyzer(string FileName, string OutputFileName)
        {
            this.fileName = FileName;
            this.fs = new FileStream(FileName, FileMode.Open);
            this.outputFileName = OutputFileName;

            StringBuilder sb = new StringBuilder();

            byte[] buf = new byte[4096];
            while (0 != fs.Read(buf, 0, buf.Length))
            {
                sb.Append(Encoding.ASCII.GetString(buf));
            }
            contentStringSplit = sb.ToString().Split('\n');

        }

        private void Analyze()
        {
            for (uint i = 0; i < contentStringSplit.Length; i++)
            {
                var line = contentStringSplit[i];
                var regex = new Regex(@"(\[.*\]\s+){0,}(?<message>.*):\s(?<content>.*)" + "\r");
                var match = regex.Match(line);

                string message = match.Groups["message"].Value,
                    content = match.Groups["content"].Value;

                ListLog.Add(new ErrorLogChunk { Message = message, Content = content });
            }
        }

        private void PrintReport()
        {
            List<string>[] listcontent = new List<string>[ArrLogTargetMessage.Length];
            for ( int i=0; i < listcontent.Length; i++ )
                listcontent[i] = new List<string>();

            for (int i = 0; i < ListLog.Count; i++)
            {
                var message = ListLog[i].Message;
                var content = ListLog[i].Content;

                for ( int icontent=0; icontent < ArrLogTargetMessage.Length; icontent++ )
                {
                    if ( message == ArrLogTargetMessage[icontent] )
                        listcontent[icontent].Add(content);
                }
            }

            using (FileStream fs = new FileStream(outputFileName, FileMode.OpenOrCreate))
            using (StreamWriter writer = new StreamWriter(fs))
            {
                fs.SetLength(0);

                writer.WriteLine("[Detail REPORT]");
                for (int i = 0; i < ArrLogTargetMessage.Length; i++)
                {
                    writer.WriteLine("----------------------------");
                    writer.WriteLine(i + 1 + "." + ArrLogTargetMessage[i]);
                    for (int icontent = 0; icontent < listcontent[i].Count; icontent++)
                        writer.WriteLine(listcontent[i][icontent]);
                }
            }
            
        }

        public void AnalyzeAndReport()
        {
            Analyze();
            PrintReport();
        }
    }

    class Program
    {

        static void Main(string[] args)
        {
            if (args.Length < 2)
            {
                System.Console.WriteLine("<usage> loganalyzer.exe logfilename [outputfilename]");
                return;
            }

            string logfilename = args[0], outputfilename = args[1];
            if (logfilename.Length == 0) System.Console.WriteLine("Log file name is not given.");
            if (outputfilename.Length == 0) outputfilename = "output.txt";

            try
            {
                LogAnalyzer analyzer = new LogAnalyzer(logfilename, outputfilename);
                analyzer.AnalyzeAndReport();
            }
            catch (Exception exception)
            {
                System.Console.WriteLine(exception.Message);
            }
        }
    }
}
