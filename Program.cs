using System;
using System.Collections.Generic;
using DotnetSpider.Core;
using DotnetSpider.Core.Pipeline;
using DotnetSpider.Core.Processor;
using DotnetSpider.Core.Scheduler;
using DotnetSpider.Core.Selector;
using DotnetSpider.HtmlAgilityPack;
using System.Data.SqlClient;

namespace SSQ
{
    class Program
    {
        static void Main(string[] args)
        {
            Site site = new Site { EncodingName = "gb2312", RemoveOutboundLinks = false };
            site.AddStartUrl("http://www.17500.cn/ssq");
            Spider spider = Spider.Create(site,
            new QueueDuplicateRemovedScheduler(),
            new SSQProcessor()).AddPipeline(new SSQPipeline());
            spider.Downloader = new DotnetSpider.Core.Downloader.HttpClientDownloader();
            spider.ThreadNum = 1;
            spider.EmptySleepTime = 3000;
            spider.Run();
            Console.WriteLine("Press any key to continue...");
            Console.ReadKey();
        }
    }

    class SSQPipeline : BasePipeline
    {

        public override void Process(IEnumerable<ResultItems> resultItems, ISpider spider)
        {
            var data = resultItems.GetEnumerator();
            while (data.MoveNext())
            {
                ssqHistory ssq = data.Current.Results["ssqResult"];
                List<SqlParameter> parmList = new List<SqlParameter>();
                parmList.Add(new SqlParameter("@qs",ssq.qs));
                parmList.Add(new SqlParameter("@red",ssq.red));
                parmList.Add(new SqlParameter("@blue",ssq.blue));
                DbHelper.ExecutePrc("prc_save_ssqHistory",parmList.ToArray());
            }
        }
    }

    class DbHelper{
        static string conStr = "Data Source=.;UID=sa;PWd=JL@881103;database=wxservice";
        public static int ExecutePrc(string prcname,SqlParameter[] parms)
        {
            using(SqlConnection con = new SqlConnection(conStr)){
                con.Open();
                var cmd = con.CreateCommand();
                cmd.CommandText = prcname;
                cmd.CommandType = System.Data.CommandType.StoredProcedure;
                cmd.Parameters.AddRange(parms);
                return cmd.ExecuteNonQuery();
            }
        }
    }

    class SSQProcessor : BasePageProcessor
    {
        protected override void Handle(Page page)
        {
            var dataList = page.Selectable.SelectList(Selectors.XPath("//tr[@class='greybg']")).Nodes();
            foreach (var data in dataList)
            {
                ssqHistory ssq = new ssqHistory();
                ssq.qs = data.Select(Selectors.XPath(".//td")).GetValue();
                ssq.red = data.Select(Selectors.XPath(".//b[@class='fred']")).GetValue();
                ssq.blue = data.Select(Selectors.XPath(".//b[@class='fblue']")).GetValue();
                page.AddResultItem("ssqResult", ssq);
                break;
            }
        }
    }

    class ssqHistory
    {
        public string qs { get; set; }

        public string red { get; set; }

        public string blue { get; set; }
    }
}
