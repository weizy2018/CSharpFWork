using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace FinalWork
{
    class Spider
    {
        List<string> itemList = new List<string>();
        string startUrl;
        DateTime startDate;
        DateTime endDate;
        string basicPath;
        public Spider(string startUrl, string startDate, string endDate, string path)
        {
            this.startUrl = startUrl;
            this.startDate = Convert.ToDateTime(startDate);
            this.endDate = Convert.ToDateTime(endDate);
            this.basicPath = path;
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }

        public void startCraw()
        {
            crawlItem();
            //crawlDetail();
            DateTime start = DateTime.Now;
            List<Task> tasks = new List<Task>();
            for (int i = 0; i < 8; i++)
            {
                Task t = new Task(() => crawlDetail());
                tasks.Add(t);
                t.Start();
            }
            //等待线程全部结束
            foreach (Task t in tasks)
            {
                t.Wait();
            }
            DateTime end = DateTime.Now;
            Console.WriteLine("=======================时间花费: {0}==========================", (end - start).ToString());

        }

        public void crawlItem()
        {
            string nextUrl = startUrl;

            bool flag = true;
            while (flag)
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(nextUrl);
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                Stream stream = response.GetResponseStream();
                StreamReader streadReader = new StreamReader(stream, Encoding.GetEncoding("utf-8"));
                string html = streadReader.ReadToEnd();
                streadReader.Close();
                response.Close();
                nextUrl = getNextUrl(html);
                flag = getItems(html);
            }
            foreach (string str in itemList)
            {
                Console.WriteLine(str);
            }

        }
        //获取下一页的url
        private string getNextUrl(string html)
        {
            string nextUrl = "https://www.jlu.edu.cn/index/";
            string pattern = @"<a\s.*?href=[\'|\""]([^\""\']*)[\'|\""][^>]*>下页<\/a>";
            Regex regex = new Regex(pattern);
            Match match = regex.Match(html);
            string str = match.Groups[1].ToString();
            if (str.Contains("tzgg"))
            {
                nextUrl += str;
            } else
            {
                nextUrl += "tzgg/";
                nextUrl += str;
            }

            Console.WriteLine("next url: {0}", nextUrl);
            return nextUrl;
        }
        private bool getItems(string html)
        {
            string basic = "https://www.jlu.edu.cn/";
            string itemPattern = @"<ul\s.*?class=""list fl"">([\w\W\s\s]*)<\/ul>";
            Regex regex = new Regex(itemPattern);
            Match match = regex.Match(html);
            html = match.ToString();

            string pattern = @"<li\s.*>\r\n<a\s.*?href=[\'|\""]([^\""\']*)[\'|\""][^>]*>.*<\/a>\n?<span>(\d{4}-\d{2}-\d{2})<\/span><\/li>";
            Regex r = new Regex(pattern);
            MatchCollection matches = r.Matches(html);
            bool flag = true;
            foreach (Match m in matches)
            {
                string relativePath = m.Groups[1].ToString();
                int index = relativePath.IndexOf("info");
                string str = basic + relativePath.Substring(index);
                string time = m.Groups[2].ToString();
                DateTime date = Convert.ToDateTime(time);
                if (date.CompareTo(startDate) < 0)
                {
                    flag = false;
                    break;
                }
                if (date.CompareTo(startDate) >= 0 && date.CompareTo(endDate) <= 0)
                {
                    //Console.WriteLine("{0}  {1}", str, time);
                    itemList.Add(str);
                }
                
            }
            return flag;
        }

        public void crawlDetail()
        {
            List<string> department = new List<string>();
            while (itemList.Count > 0)
            {
                string url;
                lock (itemList)
                {
                    int lastIndex = itemList.Count - 1;
                    if (lastIndex < 0)
                    {
                        break;
                    }
                    url = itemList[lastIndex];
                    itemList.RemoveAt(lastIndex);
                }
                Console.WriteLine("Id: {0}", Thread.CurrentThread.ManagedThreadId);
                Console.WriteLine(url);
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                Stream stream = response.GetResponseStream();
                StreamReader streamReader = new StreamReader(stream, Encoding.GetEncoding("utf-8"));
                string html = streamReader.ReadToEnd();
                streamReader.Close();
                response.Close();
                //先获取内容部分
                string pattern = @"<form\sname=""_newscontent_fromname"">[\w\W\s\s]*<\/form>";
                Regex regex = new Regex(pattern);
                Match match = regex.Match(html);
                html = match.ToString();

                //标题
                //<div.*>(.*)<br[\s]*\/>
                string titlePattern = @"<div\sclass=""nr_title"".*>(.*?)<br[\s]*\/>";
                Regex titleRegex = new Regex(titlePattern);
                Match titleMatch = titleRegex.Match(html);
                string title = titleMatch.Groups[1].ToString();
                Console.WriteLine("标题：{0}", title);

                //时间  作者
                //<span\s.*class="f-14\sgray-3"\s.*?>作者.*时间：(\d{4}-\d{2}-\d{2}).*<\/span>
                string timePattern = @"<span\s.*class=""f-14\sgray-3""\s.*?>作者：(.*?)\&nbsp;\&nbsp;时间：(\d{4}-\d{2}-\d{2}).*<\/span>";
                Regex timeRegex = new Regex(timePattern);
                Match timeMatch = timeRegex.Match(html);
                string author = timeMatch.Groups[1].ToString();
                string time = timeMatch.Groups[2].ToString();
                Console.WriteLine("时间：{0}", time);
                Console.WriteLine("作者：{0}", author);

                if (!author.Equals(""))
                {
                    department.Add(author);
                }

                //内容
                string contentPattern = @"<div\sid=""((vsb_content)|(vsb_content_2)|(vsb_content_6))"">([\w\W]*)<\/div>";
                Regex contentRegex = new Regex(contentPattern);
                Match contentMatch = contentRegex.Match(html);
                html = contentMatch.Groups[5].ToString();


                //获取发布者  不是我想写那么长的正则表达式 而是这个是真的没有什么规律可言
                if (author.Equals(""))
                {
                    string senderPattern = @"<.*?((text-align:\sright.*?)|(margin-bottom:0;text-align:center.*?>)|" +
                        @"(background:\swhite;\stext-align:\scenter;.*?<)|(text-align:\scenter"">)|" +
                        @"(text-align:\scenter;\sline-height:\s40px;""><.*?>)|(text-align:\scenter;"">" +
                        @"<.*?color.*?)).*?>(.*?)<";
                    Regex senderRegex = new Regex(senderPattern);
                    MatchCollection sendersMatch = senderRegex.Matches(html);
                    foreach (Match item in sendersMatch)
                    {
                        string sender = item.Groups[8].ToString();
                        sender = sender.Replace("&nbsp;", "");
                        //发布者：;
                        if (!sender.Equals(""))
                        {
                            string p = @"[\d年月日]";
                            Regex r = new Regex(p);
                            if (!r.IsMatch(sender))
                            {
                                Console.WriteLine("发布者：{0}", sender);
                                department.Add(sender);
                            }
                        }
                    }
                }

                Console.WriteLine();

                //<.*?>
                //html = html.Replace("</p>", "\n");
                string replacePattern = @"<.*?>";
                Regex replaceRegex = new Regex(replacePattern);
                html = replaceRegex.Replace(html, "");
                html = html.Replace("&nbsp;", " ");


                string path = basicPath + @"\" + time;
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }
                string fileName = path + @"\" + title + ".txt";
                StreamWriter writer = new StreamWriter(fileName);

                writer.WriteLine("标题: " + title);
                writer.WriteLine("时间: " + time);
                writer.Write("部门: ");
                foreach (string dept in department)
                {
                    writer.Write(dept + "  ");
                }
                department.Clear();
                writer.WriteLine("\r\n");

                writer.WriteLine(html);
                writer.Close();

                //Console.WriteLine(html);
                
            }
        }
    }
    class Program
    {
        static void Main(string[] args)
        {
            string url = "https://www.jlu.edu.cn/index/tzgg.htm";
            string startDate = "2018-01-01";
            string endDate = "2019-06-01";
            string path = @"D:\data";
            Spider spider = new Spider(url, startDate, endDate, path);
            spider.startCraw();
        }
    }
}
