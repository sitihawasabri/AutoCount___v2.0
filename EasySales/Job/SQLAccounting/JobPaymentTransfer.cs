using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using EasySales.Model;
using EasySales.Object;
using Quartz;
using Quartz.Impl;
using Quartz.Logging;

namespace EasySales.Job
{
    [DisallowConcurrentExecution]
    public class JobPaymentTransfer : IJob
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

                    DpprSyncLog slog = new DpprSyncLog();
                    slog.action_identifier = Constants.Action_Transfer_Payment;
                    slog.action_failure = 0;
                    slog.action_failure_message = string.Empty;
                    slog.action_time = DateTime.Now.ToString();

                    DateTime startTime = DateTime.Now;

                    LocalDB.InsertSyncLog(slog);
                    logger.message = "----------------------------------------------------------------------------";
                    logger.Broadcast();
                    logger.message = "Transfer Payment is running";
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

                    dynamic BizObject, lMainDataSet, lDetailDataSet;
                    string post_date, paymentId;
                    double total;

                    Database _mysql = new Database();

                    dynamic hasUdf = new CheckBackendRule().CheckTablesExist().GetSettingByTableName("cms_payment");

                    int payment_status = 1;
                    payment_status.ToString();
                    //string payment_status = "1";
                    string payment_method = string.Empty;
                    string cash = string.Empty;
                    string bank = string.Empty;
                    int include_ext_no = 0;
                    string payment_method_reference = string.Empty;
                    //dynamic 
                    //hasUdf = "{\"include\":{},\"condition\":{\"payment_status\":\"2\",\"payment_method\":[{\"cash\":[{\"DEFAULT\":\"CASH\"},{\"MIRI\":\"3022\/001\"},{\"SIBU\":\"3022\/000\"},{\"KUCHING\":\"3022\/002\"},{\"BINTULU\":\"3022\/003\"}]},{\"bank\":\"310-000\"}],\"include_ext_no\":\"0\"}}";
                    Dictionary<string, string> cashPaymentMethodList = new Dictionary<string, string>();
                    Dictionary<string, string> cashFinalPaymentMethodList = new Dictionary<string, string>();
                    Dictionary<string, string> bankPaymentMethodList = new Dictionary<string, string>();
                    Dictionary<string, string> bankFinalPaymentMethodList = new Dictionary<string, string>();

                    if (hasUdf.Count > 0)
                    {
                        foreach (var condition in hasUdf)
                        {
                            dynamic _condition = condition.condition;

                            dynamic _payment_status = _condition.payment_status;
                            if (_payment_status != null)
                            {
                                if (_payment_status != "1")
                                {
                                    payment_status = _payment_status;
                                }
                            }

                            //dynamic _payment_method = _condition.payment_method;
                            //if(_payment_method != null)
                            //{
                            //    foreach(var value in _payment_method)
                            //    {
                            //        if(value.cash != null)
                            //        {
                            //            cash = value.cash;
                            //        }
                                    
                            //        if (value.bank != null)
                            //        {
                            //            bank = value.bank;
                            //        }
                            //    }
                            //}

                            dynamic _include_ext_no = _condition.include_ext_no;
                            if (_include_ext_no != null)
                            {
                                if (_include_ext_no != string.Empty)
                                {
                                    include_ext_no = _include_ext_no;
                                }
                            }
                            
                            dynamic _payment_method_reference = _condition.payment_method_reference;
                            if (_payment_method_reference != null)
                            {
                                if (_payment_method_reference != string.Empty)
                                {
                                    payment_method_reference = _payment_method_reference;
                                }
                            }

                            dynamic _payment_method = _condition.payment_method;
                            if (_payment_method != null)
                            {
                                if (_payment_method.Count > 0)
                                {
                                    int methodCount = 0;
                                    foreach (var item in _payment_method)
                                    {
                                        methodCount++;
                                        Console.WriteLine(item);

                                        foreach(var __each in item)
                                        {
                                            Console.WriteLine(__each);

                                            foreach(var __i in __each)
                                            {
                                                Console.WriteLine(__i);

                                                foreach (var __value in __i)
                                                {
                                                    Console.WriteLine(__value);
                                                    if(methodCount == 1)
                                                    {
                                                        cashPaymentMethodList = __value.ToObject<Dictionary<string, string>>();
                                                        cashFinalPaymentMethodList = cashFinalPaymentMethodList.Union(cashPaymentMethodList).ToDictionary(k => k.Key, v => v.Value);
                                                    }
                                                    else
                                                    {
                                                        bankPaymentMethodList = __value.ToObject<Dictionary<string, string>>();
                                                        bankFinalPaymentMethodList = bankFinalPaymentMethodList.Union(bankPaymentMethodList).ToDictionary(k => k.Key, v => v.Value);
                                                    }
                                                    
                                                }
                                            }
                                        }
                                        //paymentMethodList = item.ToObject<Dictionary<string, string>>();
                                        //finalPaymentMethodList = finalPaymentMethodList.Union(paymentMethodList).ToDictionary(k => k.Key, v => v.Value);
                                    }
                                }
                            }

                        }
                    }
                    else
                    {
                        goto ENDJOB;
                    }

                    //string paymentQuery = "SELECT l.staff_code,pm.payment_id, pm.payment_date, pm.cust_code, IF(pm.description = '','Payment For Account',pm.description) AS 'description', pm.payment_amount, dtl.payment_method, IF(SUBSTRING_INDEX(dtl.payment_by, '-', 2) = 'Select Bank' OR SUBSTRING_INDEX(dtl.payment_by, '-', 2) = '', 'CASH', SUBSTRING_INDEX(dtl.payment_by, '-', 2)) AS bank, dtl.cheque_no FROM cms_payment AS pm LEFT JOIN cms_payment_detail dtl ON dtl.payment_id = pm.payment_id LEFT JOIN cms_login l ON l.login_id = pm.salesperson_id AND dtl.cancel_status = 0 WHERE pm.payment_status = " + payment_status + " AND pm.cancel_status = 0 and payment_fault = 0";
                    string paymentQuery = "SELECT l.staff_code, pm.salesperson_id, pm.payment_id, pm.payment_date, pm.cust_code, c.billing_state, IF(pm.description = '','Payment For Account',pm.description) AS 'description', pm.payment_amount, dtl.payment_method, IF(SUBSTRING_INDEX(dtl.payment_by, '-', 2) = 'Select Bank' OR SUBSTRING_INDEX(dtl.payment_by, '-', 2) = '', 'CASH', SUBSTRING_INDEX(dtl.payment_by, '-', 2)) AS bank, dtl.cheque_no FROM cms_payment AS pm LEFT JOIN cms_payment_detail dtl ON dtl.payment_id = pm.payment_id LEFT JOIN cms_login l ON l.login_id = pm.salesperson_id LEFT JOIN cms_customer AS c ON c.cust_code = pm.cust_code AND dtl.cancel_status = 0 WHERE pm.payment_status = " + payment_status + " AND pm.cancel_status = 0 AND payment_fault = 0";
                    ArrayList payment = _mysql.Select(paymentQuery);

                    logger.Broadcast("Payment(s) to insert: " + payment.Count);

                    if (payment.Count == 0)
                    {
                        logger.message = "No payment to insert";
                        logger.Broadcast();
                    }
                    else
                    {
                        BizObject = ComServer.BizObjects.Find("AR_PM");
                        lMainDataSet = BizObject.DataSets.Find("MainDataSet");
                        lDetailDataSet = BizObject.DataSets.Find("cdsKnockOff");

                        for(int i = 0; i < payment.Count; i++)
                        {
                            BizObject.New();

                            Dictionary<string, string> paymentObj = (Dictionary<string, string>)payment[i];

                            paymentId = paymentObj["payment_id"];
                            paymentId = paymentId.Replace("CASH", "OR");
                            string _total = paymentObj["payment_amount"];
                            double.TryParse(_total, out double totalAmount);

                            totalAmount = totalAmount * 1.00;

                            post_date = Convert.ToDateTime(paymentObj["payment_date"]).ToString("yyyy-MM-dd");

                            lMainDataSet.FindField("DOCKEY").Value = -1;
                            if (include_ext_no == 1)
                            {
                                lMainDataSet.FindField("DocNo").value = "<<New>>";
                                lMainDataSet.FindField("NOTE").AsString = paymentId;
                            }
                            else
                            {
                                lMainDataSet.FindField("DocNo").AsString = paymentId;
                            }
                           
                            lMainDataSet.FindField("CODE").AsString = paymentObj["cust_code"]; //Customer Account
                            lMainDataSet.FindField("Agent").AsString = paymentObj["staff_code"];
                            lMainDataSet.FindField("Project").AsString = "----";
                            lMainDataSet.FindField("PaymentProject").AsString = "----";
                            lMainDataSet.FindField("DocDate").Value = post_date;
                            lMainDataSet.FindField("PostDate").Value = post_date;
                            lMainDataSet.FindField("Description").AsString = paymentObj["description"];

                            string paymentMethod = paymentObj["bank"];
                            if (paymentMethod == "CASH")
                            {
                                //paymentMethod = cash;
                                string findField = paymentObj[payment_method_reference];
                                string payment_into = string.Empty;
                                if (cashFinalPaymentMethodList.Count > 1)
                                {
                                    if (cashFinalPaymentMethodList.ContainsKey(findField))
                                    {
                                        payment_into = cashFinalPaymentMethodList.Where(pair => pair.Key == findField)
                                                            .Select(pair => pair.Value)
                                                            .FirstOrDefault();
                                        Console.WriteLine(payment_into);
                                        paymentMethod = payment_into;

                                        try
                                        {
                                            lMainDataSet.FindField("PaymentMethod").AsString = paymentMethod; //Bank or Cash Account
                                        }
                                        catch
                                        {
                                            _mysql.Insert("UPDATE cms_payment SET payment_fault = '1', payment_fault_message = '[" + paymentMethod + "] Payment Method is not correct for this customer.' WHERE payment_id = '" + paymentObj["payment_id"] + "'");
                                            logger.Broadcast("[" + paymentObj["order_id"] + "] Payment Method is not correct for this customer.(" + paymentMethod + ")");
                                        }
                                    }
                                    else
                                    {
                                        _mysql.Insert("UPDATE cms_payment SET payment_fault = '1', payment_fault_message = 'Payment Method is not correct for this customer. Kindly add the correct method in cms_payment backend rule.' WHERE payment_id = '" + paymentObj["payment_id"] + "'");
                                        logger.Broadcast("[" + paymentObj["order_id"] + "] Payment Method is not correct for this customer.");
                                        logger.Broadcast("[" + paymentObj["order_id"] + "] Kindly add the correct method in cms_payment backend rule.");
                                        goto nextOrder;
                                    }
                                }
                                else
                                {
                                    if (cashFinalPaymentMethodList.Count > 0)
                                    {
                                        payment_into = cashFinalPaymentMethodList.ElementAt(0).Value; //GET THE FIRST VALUE OF CASH
                                        paymentMethod = payment_into;
                                        Console.WriteLine(payment_into);
                                    }
                                }
                            }
                            else
                            {
                                try
                                {
                                    lMainDataSet.FindField("PaymentMethod").AsString = paymentMethod; //Bank or Cash Account
                                }
                                catch
                                {
                                    _mysql.Insert("UPDATE cms_payment SET payment_fault = '1', payment_fault_message = '["+ paymentMethod + "] Payment Method is not correct for this customer.' WHERE payment_id = '" + paymentObj["payment_id"] + "'");
                                    logger.Broadcast("[" + paymentObj["order_id"] + "] Payment Method is not correct for this customer.("+ paymentMethod + ")");
                                }

                                //paymentMethod = bank;
                            }

                            //lMainDataSet.FindField("PaymentMethod").AsString = paymentMethod; //Bank or Cash Account
                            lMainDataSet.FindField("ChequeNumber").AsString = paymentObj["cheque_no"];
                            lMainDataSet.FindField("BankCharge").AsFloat = 0;
                            lMainDataSet.FindField("DocAmt").AsFloat = totalAmount;
                            lMainDataSet.FindField("Cancelled").AsString = "F";

                            try
                            {
                                //if select cash - , in payment_by column will be 'CASH' but got error, if select bank cheque and no bank selected, payment_by column will be just '-'
                                //IF payment_by [310-001 (maybank)] - ok, no error. If [310-006 (HSBC - S$ ACCOUNT), got error need to insert KO amt]
                                BizObject.Save();
                                int updated_status = 1 + payment_status;
                                string updatedQuery = "UPDATE cms_payment SET payment_status = " + updated_status + " WHERE payment_id = '" + paymentId + "'";
                                _mysql.Insert(updatedQuery);
                                logger.message = paymentObj["payment_id"] + " created";
                                logger.Broadcast();
                            }
                            catch (Exception e)
                            {
                                string msg = string.Empty;
                                msg = e.Message;
                                Database.Sanitize(ref msg);
                                _mysql.Insert("UPDATE cms_payment SET payment_fault = '1', payment_fault_message = '" + msg + "' WHERE payment_id = '" + paymentId + "'");
                            }

                            BizObject.Close();
                            nextOrder:
                            logger.Broadcast("Next Order");
                        }
                    }

                    ENDJOB:
                    //_mysql.Close();

                    slog.action_identifier = Constants.Action_Transfer_Payment;
                    slog.action_failure = 0;
                    slog.action_failure_message = string.Empty;
                    slog.action_time = DateTime.Now.ToString();

                    DateTime endTime = DateTime.Now;
                    TimeSpan ts = endTime - startTime;
                    Console.WriteLine("Completed in: " + ts.TotalSeconds.ToString("F"));

                    LocalDB.InsertSyncLog(slog);

                    logger.message = "Transfer Payment finished in (" + ts.TotalSeconds.ToString("F") + " seconds)";
                    logger.Broadcast();

                });

                thread.Start();
                //thread.Join();

                //await Console.Out.WriteLineAsync("Done POST Sales Orders");
            }
            catch (ThreadAbortException e)
            {
                DpprException ex = new DpprException
                {
                    file_name = "JobTransferPayment",
                    exception = e.Message,
                    time = DateTime.Now.ToString()
                };
                LocalDB.InsertException(ex);

                Console.WriteLine(Constants.Thread_Exception + e.Message);
            }
        }
    }
}