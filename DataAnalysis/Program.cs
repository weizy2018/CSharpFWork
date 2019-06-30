using JiebaNet.Segmenter;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Globalization;
using System.Text.RegularExpressions;
using JiebaNet.Analyser;

namespace DataAnalysis
{
    class Analysis
    {
        private string path;
        private string savePath;
        public Analysis(string datePath, string savePath)
        {
            this.path = datePath;
            this.savePath = savePath;
            if (!Directory.Exists(savePath))
            {
                Directory.CreateDirectory(savePath);
            }
        }
        //获取该日期在当年的第几周
        private int getWeek(DateTime dt)
        {
            GregorianCalendar gc = new GregorianCalendar();
            int weekOfYear = gc.GetWeekOfYear(dt, CalendarWeekRule.FirstDay, DayOfWeek.Monday);
            return weekOfYear;
        }
        //获取每周的通知数量
        public void weekNews()
        {
            //2018-1-1刚好是周一
            DateTime start = Convert.ToDateTime("2018-01-01");

            List<int[]> news = new List<int[]>();
            string[] folders = Directory.GetDirectories(path);

            DateTime end = start.AddDays(6);
            int[] week = new int[7];

            for (int i = 0; i < folders.Length; i++)
            {
                
                DateTime date = Convert.ToDateTime(folders[i].Substring(path.Length + 1));
                string[] files = Directory.GetFiles(folders[i]);

                if (date.CompareTo(end) <= 0)
                {
                    DayOfWeek d = date.DayOfWeek;
                    if (d.Equals(DayOfWeek.Monday))
                    {
                        week[0] += files.Length;
                    }
                    else if (d.Equals(DayOfWeek.Tuesday))
                    {
                        week[1] += files.Length;
                    }
                    else if (d.Equals(DayOfWeek.Wednesday))
                    {
                        week[2] += files.Length;
                    }
                    else if (d.Equals(DayOfWeek.Thursday))
                    {
                        week[3] += files.Length;
                    }
                    else if (d.Equals(DayOfWeek.Friday))
                    {
                        week[4] += files.Length;
                    }
                    else if (d.Equals(DayOfWeek.Saturday))
                    {
                        week[5] += files.Length;
                    }
                    else if (d.Equals(DayOfWeek.Sunday))
                    {
                        week[6] += files.Length;
                    }
                }
                else
                {
                    i--;
                    end = end.AddDays(7);
                    news.Add(week);
                    week = new int[7];
                }
            }
            string filePath = savePath + @"\各周每天的通知.txt";
            string filePath2 = savePath + @"\各周总的通知.txt";
            StreamWriter writer = new StreamWriter(filePath);
            StreamWriter writer2 = new StreamWriter(filePath2);
            foreach (int[] w in news)
            {
                //Console.Write("[{0}", w[0]);
                writer.Write("[{0}", w[0]);
                int sum = w[0];
                for (int i = 1; i < w.Length; i++)
                {
                    //Console.Write(", {0}", w[i]);
                    writer.Write(", {0}", w[i]);
                    sum += w[i];
                }
                //Console.WriteLine("]");
                writer.WriteLine("],");
                writer2.Write("{0}, ", sum);
            }
            writer.Close();
            writer2.Close();
        }
        //统计每个部门的通知数量
        public void departmentNews()
        {
            Dictionary<string, int> department = new Dictionary<string, int>();
            string[] folders = Directory.GetDirectories(path);
            foreach (string folder in folders)
            {
                string[] files = Directory.GetFiles(folder);
                foreach (string file in files)
                {
                    
                    FileStream fs = new FileStream(file, FileMode.Open);
                    StreamReader reader = new StreamReader(fs);
                    reader.ReadLine();
                    reader.ReadLine();
                    //部门都在文件中的第三行的位置
                    string dept = reader.ReadLine();
                    dept = dept.Substring(4).Trim();

                    string[] depts = Regex.Split(dept, @"\s+");
                    foreach (string s in depts)
                    {
                        if (s != "")
                        {
                            if (department.ContainsKey(s))
                            {
                                department[s]++;
                            }
                            else
                            {
                                department.Add(s, 1);
                            }
                        }
                    }
                    reader.Close();
                    fs.Close();
                }
            }
            string filePath = savePath + @"\各部门的通知.txt";
            StreamWriter writer = new StreamWriter(filePath);
            foreach (KeyValuePair<string, int> item in department)
            {
                //Console.WriteLine("{0}: {1}", item.Key, item.Value);
                writer.WriteLine("[\"{0}\", {1}],", item.Key, item.Value);
            }
            writer.Close();
        }

        public void wordCut()
        {
            string[] folders = Directory.GetDirectories(path);
            string filePath = savePath + @"\分词结果.txt";
            StreamWriter writer = new StreamWriter(filePath);

            foreach (string folder in folders)
            {
                int index = folder.LastIndexOf('\\');
                string folderName = folder.Substring(index + 1);
                writer.Write("\"{0}\": [", folderName);
                Console.WriteLine(folderName);
                string[] files = Directory.GetFiles(folder);
                string text = "";
                foreach (string file in files)
                {
                    text += File.ReadAllText(file);
                }
                var extractor = new TfidfExtractor();
                var keywords = extractor.ExtractTagsWithWeight(text);

                foreach (WordWeightPair w in keywords)
                {
                    writer.Write("[\"{0}\", {1}],", w.Word, (int)(w.Weight * 100 * 2));      //将权重按照一定倍数放大并取整，便于后期处理
                    Console.WriteLine("{0}: {1}", w.Word, (int)(w.Weight * 100 * 2));
                }
                writer.WriteLine("],");

            }

            writer.Close();

        }
    }
    class Program
    {
        static void Main(string[] args)
        {
            string dataPath = @"D:\data";
            string savePath = @"D:\dataAnalysis";
            
            Analysis analysis = new Analysis(dataPath, savePath);
            analysis.weekNews();
            analysis.departmentNews();
            analysis.wordCut();

            Console.WriteLine("分析完成， 结果保存在{0}中", savePath);

        }
    }
}
