using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using EasySales.Model;
using EasySales.Object;
using Newtonsoft.Json.Linq;
using Quartz;
using Quartz.Impl;
using Quartz.Logging;

namespace EasySales.Job
{
    [DisallowConcurrentExecution]
    public class JobProductGroupsSync : IJob
    {
        public void Execute()
        {
            this.Run();
        }
        public async Task Execute(IJobExecutionContext context)
        {
            this.Run();
        }

        public void Run()
        {
            try
            {
                Thread thread = new Thread(p =>
                {
                    Thread.CurrentThread.IsBackground = true;

                    int RecordCount = 0;
                    GlobalLogger logger = new GlobalLogger();

                    /**
                     * Here we will run SQLAccounting Codes
                     * */

                    DpprSyncLog slog = new DpprSyncLog
                    {
                        action_identifier = Constants.Action_ProductGroupsSync,     
                        action_details = Constants.Tbl_cms_product_group,           
                        action_failure = 0,
                        action_failure_message = "Product groups sync is running",
                        action_time = DateTime.Now.ToLongDateString()
                    };

                    DateTime startTime = DateTime.Now;

                    LocalDB.InsertSyncLog(slog);
                    logger.message = "----------------------------------------------------------------------------";
                    logger.Broadcast();
                    logger.message = "Product groups sync is running";
                    logger.Broadcast();
                    logger.message = "----------------------------------------------------------------------------";
                    logger.Broadcast();

                CHECKAGAIN:

                    SQLAccApi instance = SQLAccApi.getInstance();

                    dynamic ComServer = instance.GetComServer();

                    if (!instance.RPCAvailable())
                    {
                        goto CHECKAGAIN;
                    }

                    Database mysql = new Database();

                    dynamic lDataSet;
                    string lSQL, query, updateQuery;
                    string Code, Name, Description, Member;

                    query = "INSERT INTO cms_product_group (name, description, product_code) VALUES ";

                    updateQuery = " ON DUPLICATE KEY UPDATE name = VALUES(name), description = VALUES(description);";

                    HashSet<string> queryList = new HashSet<string>();

                    lSQL = "SELECT ST_ITEM.*,REPLACE(CAST(SUBSTRING(UDF_PACKAGE FROM 1 FOR 8191) AS VARCHAR(8191)),'.','') AS mblob FROM ST_ITEM WHERE UDF_ISPACKAGE = 'T';";
                    lDataSet = ComServer.DBManager.NewDataSet(lSQL);

                    lDataSet.First();

                    ArrayList memberList = new ArrayList();

                    ArrayList leaderList = new ArrayList();
                    ArrayList leaderListSpecial = new ArrayList();
                    Dictionary<string, string> leaderMember = new Dictionary<string, string>();

                    while (!lDataSet.eof)
                    {
                        RecordCount++;

                        Code = lDataSet.FindField("CODE").AsString;
                        Name = lDataSet.FindField("DESCRIPTION").AsString;
                        Description = lDataSet.FindField("DESCRIPTION2").AsString;
                        Member = lDataSet.FindField("mblob").AsString;


                        string[] MemberArray = Member.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries); 

                        string MemberComma = string.Join(",", MemberArray);       

                        Database.Sanitize(ref Code);
                        Database.Sanitize(ref Name);
                        Database.Sanitize(ref Description);
                        Database.Sanitize(ref MemberComma);                                     

                        leaderMember.Add(Code, MemberComma);

                        memberList.Add(leaderMember);

                        leaderList.Add(Code);

                        Database.Sanitize(ref Name);
                        Database.Sanitize(ref Description);
                        Database.Sanitize(ref Code);

                        leaderListSpecial.Add(string.Format("'{0}'", Code));

                        string Values = string.Format("('{0}','{1}','{2}')", Name, Description, Code);

                         queryList.Add(Values);

                        if (queryList.Count % 2000 == 0)
                        {
                            string tmp_query = query;
                            tmp_query += string.Join(", ", queryList);
                            tmp_query += updateQuery;

                            mysql.Insert(tmp_query);
                            //mysql.Close();

                            queryList.Clear();
                            tmp_query = string.Empty;

                            logger.message = string.Format("{0} product group records is inserted", RecordCount);
                            logger.Broadcast();
                        }

                        lDataSet.Next();
                    }

                    if (queryList.Count > 0)
                    {
                        query = query + string.Join(", ", queryList) + updateQuery;

                        mysql.Insert(query);
                        // mysql.Close();

                        logger.message = string.Format("{0} product groups records is inserted", RecordCount);
                        logger.Broadcast();
                    }

                    int leaderCount;

                    var leaderJoin = leaderListSpecial.Cast<string>();

                    var theLeaders = string.Join(", ", leaderJoin);

                    leaderCount = leaderList.Count;

                    if (leaderCount > 0)
                    {
                        Database _mysql = new Database();

                        Console.WriteLine(theLeaders);
                        Dictionary<string, string> leaderIds = new Dictionary<string, string>();
                        ArrayList group = _mysql.Select("SELECT product_code, id FROM cms_product_group WHERE product_code IN(" + theLeaders + ")");

                        for (int i = 0; i < group.Count; i++)
                        {
                            Dictionary<string, string> each = (Dictionary<string, string>)group[i];
                            leaderIds.Add(each["id"], each["product_code"]);
                        }

                        for (int im = 0; im < leaderMember.Count; im++)
                        {
                            string leader = leaderMember.ElementAt(im).Key;
                            string memberValue = leaderMember.ElementAt(im).Value;

                            string currentLeaderId = leaderIds.ElementAt(im).Key;

                            if (currentLeaderId != null)
                            {
                                // we dont know if any member was deleted or not. let's reset member ids
                                _mysql.Insert("UPDATE cms_product SET product_group_id = 0 WHERE product_group_id = '" + currentLeaderId + "'");

                                string[] memberInfo = memberValue.Split(',');

                                ArrayList memberAddList = new ArrayList();

                                for (int x = 0; x < memberInfo.Length; x++)
                                {
                                    string mem = memberInfo[x].ToString();
                                    memberAddList.Add(string.Format("'{0}'", mem));
                                }

                                var memJoin = memberAddList.Cast<string>();
                                var theMembers = string.Join(",", memJoin);

                                string updateGroup = "UPDATE cms_product SET product_group_id = " + currentLeaderId + " WHERE product_code IN (" + theMembers + ")";

                                Database newmysql = new Database();

                                newmysql.Insert(updateGroup);
                                Console.WriteLine(updateGroup);

                                //newmysql.Close();
                            }
                        }
                    }

                    slog.action_identifier = Constants.Action_ProductGroupsSync; 
                    slog.action_details = Constants.Tbl_cms_product_group + Constants.Is_Finished + string.Format(" ({0}) records", RecordCount);
                    slog.action_failure = 0;
                    slog.action_failure_message = string.Empty;
                    slog.action_time = DateTime.Now.ToString();

                    DateTime endTime = DateTime.Now;
                    TimeSpan ts = endTime - startTime;
                    Console.WriteLine("Completed in: " + ts.TotalSeconds.ToString("F"));

                    LocalDB.InsertSyncLog(slog);

                    logger.message = "Product groups sync finished in (" + ts.TotalSeconds.ToString("F") + " seconds)";
                    logger.Broadcast();
                });

                thread.Start();
                //thread.Join();

                //await Console.Out.WriteLineAsync("Product Group Sync");
            }
            catch (ThreadAbortException e)
            {
                DpprException ex = new DpprException
                {
                    file_name = "JobProductGroupSync",
                    exception = e.Message,
                    time = DateTime.Now.ToLongTimeString()
                };
                LocalDB.InsertException(ex);

                Console.WriteLine(Constants.Thread_Exception + e.Message);
            }
        }
    }
}