using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using EasySales.Model;
using EasySales.Object;
using Google.Protobuf.WellKnownTypes;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Org.BouncyCastle.Asn1;
using Quartz;
using Quartz.Logging;

namespace EasySales.Job
{
    [DisallowConcurrentExecution]
    public class JobAPSTransferSO : IJob
    {
        public void Execute()
        {
            this.Run();
        }

        public async Task Execute(IJobExecutionContext context)
        {
            this.Run();
        }

        public string filterOrderUDFbyKey(dynamic udf, string key)
        {
            /* key = "refId" */

            foreach (var item in udf)
            {
                if(item.code == key)
                {
                    return item.value; /* ORDER */
                }
            }
            return string.Empty;
        }

        public Dictionary<string, string> fieldList(string refId, Dictionary<string,string> obj, SQLServer mssql)
        {
            if (refId != string.Empty)
            { 
                ArrayList selRefID = mssql.Select("SELECT intRefCodeID FROM Sal_RefCodeTbl where charRefCode = '" + refId + "'");
                int __cc = 0;

                if (selRefID.Count > 0)
                {
                    Dictionary<string, string> selRefObj = (Dictionary<string, string>)selRefID[0];
                    refId = selRefObj["intRefCodeID"];
                    __cc++;
                }
                refId = __cc > 0 ? refId : string.Empty;
            }

            if (refId == string.Empty)
            {
                string tStaffCode = obj["staff_code"];
                tStaffCode = tStaffCode.ToUpper().Trim();

                ArrayList salesRefId = mssql.Select("SELECT intRefCodeID FROM Sal_RefCodeTbl where upper(rtrim(ltrim(charRefCode))) = '" + tStaffCode + "'");
                int __cc = 0;

                if (salesRefId.Count > 0)
                {
                    Dictionary<string, string> salesRefObj = (Dictionary<string, string>)salesRefId[0];
                    refId = salesRefObj["intRefCodeID"];
                    __cc++;
                }
                string cc = __cc.ToString();
                refId = __cc > 0 ? refId : cc;
            }

            string custIdQuery = "SELECT intCustID, intCustGrpId, intAreaCodeId FROM Sal_CustomerTbl where charRef ='" + obj["cust_code"] + "' AND charCusttype = 'CR' AND blnIsDelete='FALSE'"; //SELECT intCustID, intCustGrpId, intAreaCodeId FROM Sal_CustomerTbl where charRef ='J073' AND charCusttype = 'CR' AND blnIsDelete='FALSE'
            ArrayList custIdList = mssql.Select(custIdQuery);

            string dockey, custId, custGroupId, areaCode, salesmanId, staffId, orderDate, varRemarks, billingAddress1, billingAddress2, billingAddress3, billingAddress4, shippingAddress1, shippingAddress2, shippingAddress3, shippingAddress4, baskedId, custFax, custcompanyName, custInchargePerson, termCode, deliveryDate, checkerName, branchCode, _wareHouseID, deliveryLocation, whCode;

            custId = string.Empty;
            custGroupId = string.Empty;
            areaCode = string.Empty;

            if (custIdList.Count > 0)
            {
                Dictionary<string, string> custObj = (Dictionary<string, string>)custIdList[0];

                custId = custObj["intCustID"];
                custGroupId = custObj["intCustGrpId"];
                areaCode = custObj["intAreaCodeId"];
            }

            string saleIdQuery = "SELECT intUserID FROM Adm_UserTbl WHERE charUserID ='" + obj["staff_code"] + "'";
            ArrayList saleIdList = mssql.Select(saleIdQuery);

            salesmanId = "0";
            if(saleIdList.Count > 0)
            {
                Dictionary<string, string> saleObj = (Dictionary<string, string>)saleIdList[0];
                salesmanId = saleObj["intUserID"];
            }
            
            string staffIdQuery = "SELECT intUserID FROM Adm_UserTbl WHERE charUserID = '" + obj["staff_code"] + "'";
            ArrayList staffIdList = mssql.Select(staffIdQuery);

            staffId = "0";
            if (staffIdList.Count > 0)
            {
                Dictionary<string, string> staffObj = (Dictionary<string, string>)staffIdList[0];
                staffId = staffObj["intUserID"]; //but we dont use the staffId value
            }

            ArrayList dockeys = mssql.Select("(SELECT ISNULL(MAX(intSONo) + 1, 0) as dockey FROM Sal_SOTbl)"); //{[dockey, 61239]}

            dockey = "0";
            if(dockeys.Count > 0)
            {
                Dictionary<string, string> dockeyObj = (Dictionary<string, string>)dockeys[0];
                dockey = dockeyObj["dockey"];
            }

            Encoding utf8 = Encoding.UTF8;

            string conv_cust_company_name = obj["cust_company_name"];
            byte[] ccmBytes = utf8.GetBytes(conv_cust_company_name);
            conv_cust_company_name = utf8.GetString(ccmBytes);

            //orderDate = obj["order_date"];
            orderDate = Convert.ToDateTime(obj["order_date"]).ToString("yyyy-MM-dd");

            billingAddress1 = obj["billing_address1"].Replace("'", "''");
            billingAddress2 = obj["billing_address2"].Replace("'", "''");
            billingAddress3 = obj["billing_address3"].Replace("'", "''");
            billingAddress4 = obj["billing_address4"].Replace("'", "''");
            shippingAddress1 = obj["shipping_address1"].Replace("'", "''");
            shippingAddress2 = obj["shipping_address2"].Replace("'", "''");
            shippingAddress3 = obj["shipping_address3"].Replace("'", "''");
            shippingAddress4 = obj["shipping_address4"].Replace("'", "''");
            custFax = obj["cust_fax"].Replace("'", "''");
            custcompanyName = obj["cust_company_name"].Replace("'", "''");
            custInchargePerson = obj["cust_incharge_person"];
            termCode = obj["termcode"];
            baskedId = obj["basket_id"];
            deliveryDate = obj["delivery_date"];
            branchCode = obj["branch_code"];
            deliveryLocation = obj["delivery_location"];
            whCode = obj["warehouse_code"];

            if (branchCode == "N/A" || branchCode == "")
            {
                branchCode = "NULL";
            }

            if (obj["warehouse_code"] != "")
            {
                _wareHouseID = obj["warehouse_code"];
            }
            else
            {
                _wareHouseID = "1";
            }
            int.TryParse(_wareHouseID, out int warehouseID);

            //checkerName = obj["pack_confirmed_by"];
            checkerName = obj["packed_by"];

            if (checkerName != null)
            {
                checkerName = "|" + checkerName;
            }
            else
            {
                checkerName = "";
            }

            if(baskedId != string.Empty)
            {
                varRemarks = obj["order_delivery_note"] + "|Picked|" + baskedId + "|" + checkerName;
            }
            else
            {
                varRemarks = obj["order_delivery_note"] + "|Picked|" + checkerName;
            }
            
            string stringDate = string.Empty;
            DateTime date = DateTime.Now;
            stringDate = date.ToString("s");
            Console.WriteLine(stringDate);

            Dictionary<string,string> valueField = new Dictionary<string, string>();
            valueField.Add("dockey", dockey);
            valueField.Add("custId", custId);
            valueField.Add("orderDate", orderDate);
            valueField.Add("varRemarks", varRemarks);
            valueField.Add("salesmanId", salesmanId);
            valueField.Add("_warehouseID", _wareHouseID);
            valueField.Add("areaCode", areaCode);
            valueField.Add("refId", refId);
            valueField.Add("custGroupId", custGroupId);
            valueField.Add("stringDate", stringDate);
            valueField.Add("checkerName", checkerName);
            valueField.Add("deliveryLocation", deliveryLocation); 
            valueField.Add("whCode", whCode); 

            return valueField;
        }

        public bool picked(string orderId, Dictionary<string, string> obj, Dictionary<string, string> valueField, Database mysql, SQLServer mssql)
        {
            GlobalLogger logger = new GlobalLogger();
            string pickedItemQuery = "SELECT * FROM cms_order_item WHERE order_id = '" + orderId + "' AND cancel_status = 0 AND packing_status = 1 AND packed_qty <> 0";
            ArrayList pickedItems = mysql.Select(pickedItemQuery);
            mysql.Message(pickedItemQuery);

            int iCount = 1;
            string dockey, custId, refId, orderDate, varRemarks, salesmanId, stringDate, _warehouseID, custGroupId, areaCode, deliveryLocation, whCode;

            Dictionary<string, string> value = (Dictionary<string, string>)valueField;
            dockey = value["dockey"];

            if (dockey == "0")
            {
                return false;
            }

            custId = value["custId"];
            refId = value["refId"];
            orderDate = value["orderDate"];
            varRemarks = value["varRemarks"];
            salesmanId = value["salesmanId"];
            stringDate = value["stringDate"];
            _warehouseID = value["_warehouseID"];
            custGroupId = value["custGroupId"];
            areaCode = value["areaCode"];
            deliveryLocation = value["deliveryLocation"]; //added to varRefNo
            whCode = value["whCode"]; //for checking +-cloudqty

            int.TryParse(_warehouseID, out int warehouseID);

            string totalAmtPicked = "SELECT SUM(unit_price * packed_qty) AS sub_total, COUNT(*) totalQTY FROM cms_order_item WHERE cancel_status = 0 AND packing_status = 1 AND packed_qty <= quantity AND order_id = '" + orderId + "'";
            ArrayList totalAmtListPicked = mysql.Select(totalAmtPicked);

            string _totalAmt = "0";
            string _totalQty = "0";
            double taxAmt = 0.00;
            double totalTax = 0.00;

            if (totalAmtListPicked.Count > 0)
            {
                Dictionary<string, string> each = (Dictionary<string, string>)totalAmtListPicked[0];

                _totalAmt = each["sub_total"];
                _totalQty = each["totalQTY"];
            }

            int.TryParse(_totalQty, out int totalQty);
            if(totalQty <= 0)
            {
                return false;
            }

            double.TryParse(_totalAmt, out double totalAmt);

            taxAmt = totalAmt * 0.00;
            totalTax = totalAmt + taxAmt;

            if (totalTax == 0)
            {
                totalTax = 0;
            }

            if (taxAmt == 0)
            {
                taxAmt = 0;
            }

            string stmt_maxDelKey;
            string _intSODetailsNo = "0";

            stmt_maxDelKey = "(SELECT ISNULL(MAX(intSODetailsNo)+6,0) as intSODetailsNo FROM Sal_SODetailsTbl)";

            ArrayList delKey = mssql.Select(stmt_maxDelKey);

            if(delKey.Count > 0)
            {
                Dictionary<string, string> delKeyObj = (Dictionary<string, string>)delKey[0];
                _intSODetailsNo = delKeyObj["intSODetailsNo"];
            }
            else
            {
                return false;
            }
            int.TryParse(_intSODetailsNo, out int intSODetailsNo);

            varRemarks = varRemarks.Replace("'", "''");
            deliveryLocation = deliveryLocation.Replace("'", "''");

            //added delivery_location value at varRefNo
            string begin = "begin transaction ";
            string declare = "declare @intSoId int; ";
            string getIntSONo = "set @intSoId = (select top(1) (intsono+1) as soId from Sal_SOTbl order by intSONo desc) ";
            string transferPickedSOQuery = "INSERT INTO Sal_SOTbl(intSONo, varTrxNo, intCustID, intRefCodeID, varRefNo, charOverDue, dtSODate, decGSTAmt, decTotalAmt, decRoundAmt, dtSODateInitial, charCustPONo, varRemarks, intTrxUploadNo, charStatus, intApproveBy, intCurrID, intAutoIncrementNo, varDeleteReason, intModifyBy, dtModifyDate, intCreatedby, dtCreatedDate, intUserWareHouseID, intCustGrpID, intSalManID, intAreaCodeID, blnIsDelete) VALUES "; //added blnIsDelete 15102020
            string pickedValues = string.Format("({0}, '{1}', '{2}', '{3}', '{4}', '{5}', '{6}', '{7}', '{8}', '{9}', '{10}', '{11}', '{12}', '{13}', '{14}', '{15}', '{16}', '{17}', '{18}', '{19}', '{20}', '{21}', '{22}', '{23}', '{24}', '{25}', '{26}', '{27}')", "@intSoId", orderId, custId, refId, deliveryLocation, "", orderDate, 0, totalAmt, 0, orderDate, "", varRemarks, 0, "P", 0, 1, 1, "", salesmanId, stringDate, salesmanId, stringDate, warehouseID, custGroupId, salesmanId, areaCode, "FALSE");
            
            string insertPickedSOQuery = begin + declare + getIntSONo + transferPickedSOQuery + string.Join(", ", pickedValues);
            
            HashSet<string> pickedItemQueryList = new HashSet<string>();
            ArrayList cloudQtyQueryList = new ArrayList();
            string ssSql = ";SET IDENTITY_INSERT Sal_SODetailsTbl ON;INSERT INTO Sal_SODetailsTbl (intSODetailsNo,intSONo,intInvID,varDesc,varModel,decOrderQty,decWriteOffQty,decRecommendedPrice,decMinimumSP,decGSTRate,varStockUnitNm,decUnitPrice,varAssemLinkKey,blnIsDelete,intWarehouseID,charPriority,decAvgCost,decCost,charItemCodePrint,intGSTGrpID,intAutoIncrementNo,decDiscPercent1,decDiscPercent2) VALUES ";
            string ssSql2 = ";SET IDENTITY_INSERT Sal_SODetailsTbl OFF;";
            string commit = "commit;";

            for (int ipick = 0; ipick < pickedItems.Count; ipick++)
            {
                Dictionary<string, string> itemPicked = (Dictionary<string, string>)pickedItems[ipick];
                string productCode, productName, uom, packedQty, unitPrice, _qty, cloudProdCode, gstId, varDesc, intInvId;

                varDesc = string.Empty;
                intInvId = string.Empty;
                gstId = string.Empty;

                productCode = itemPicked["product_code"];
                productCode = productCode.Replace("'", "''");

                string stmtProd = "(SELECT intInvid, varDesc, intSalGSTGrpID, varModel FROM Inv_StockTbl WHERE charItemCode = '" + productCode + "')";//"(SELECT intInvid, varDesc, intSalGSTGrpID, varModel FROM Inv_StockTbl WHERE charItemCode = 'TET-OD04RL-1')"
                ArrayList productList = mssql.Select(stmtProd);

                if (productList.Count > 0)
                {
                    Dictionary<string, string> objProd = (Dictionary<string, string>)productList[0];

                    varDesc = objProd["varDesc"];
                    intInvId = objProd["intInvid"];
                    gstId = objProd["intSalGSTGrpID"];
                    itemPicked["product_name"] = objProd["varModel"];
                }
                else
                {
                    string notExists = "["+ orderId + "] PICKED ----> this product (" + productCode + ") is no longer exists in APS";
                    logger.Broadcast(notExists);
                    mysql.Message(notExists);
                    Database.Sanitize(ref notExists);
                    mysql.Insert("UPDATE cms_order SET order_fault_message = '" + notExists + "' WHERE order_id = '" + orderId + "'");
                    return false;
                }

                varDesc = varDesc.Replace("'", "''");

                productName = itemPicked["product_name"];
                productName = productName.Replace("'", "''");

                packedQty = itemPicked["packed_qty"];
                unitPrice = itemPicked["unit_price"];

                uom = itemPicked["unit_uom"];
                uom = uom.Replace("'", "''");

                _qty = itemPicked["quantity"];
                cloudProdCode = itemPicked["product_code"];

                int.TryParse(_qty, out int qty);
                int.TryParse(packedQty, out int packedQtyNo);

                string pickedItemValues = string.Format("('{0}',{1},'{2}','{3}','{4}','{5}','{6}','{7}','{8}','{9}','{10}','{11}','{12}','{13}','{14}','{15}','{16}','{17}','{18}','{19}','{20}','{21}','{22}')", intSODetailsNo, "@intSoId", intInvId, varDesc, productName, packedQtyNo, 0, 0, 0, 0.00, uom, unitPrice, "", "FALSE", warehouseID, "N", 0, 0, productCode, gstId, iCount, 0, 0);
                pickedItemQueryList.Add(pickedItemValues);

                string cloudQtyQuery = "SELECT * FROM cms_warehouse_stock WHERE product_code = '" + cloudProdCode + "' AND wh_code = " + whCode;
                ArrayList checkCloudQty = mysql.Select(cloudQtyQuery);

                int cloud_qty = -1;
                if (checkCloudQty.Count > 0)
                {
                    Dictionary<string, string> objCloudQty = (Dictionary<string, string>)checkCloudQty[0];
                    string _cloud_qty = objCloudQty["cloud_qty"];
                    int.TryParse(_cloud_qty, out cloud_qty);
                    mysql.Message("cloudQtyQuery [picked]: " + cloudQtyQuery + " --- [" + cloud_qty + "] ");
                }

                if(cloud_qty > 0)
                {
                    string updateWhStock = "UPDATE cms_warehouse_stock SET cloud_qty = cloud_qty - " + packedQtyNo + " WHERE product_code = '" + cloudProdCode + "' AND wh_code = " + whCode; 
                    cloudQtyQueryList.Add(updateWhStock);
                    mysql.Message("updateWhStock: " + updateWhStock + " [ minus packedQtyNo: " + packedQtyNo + "] ");
                }

                iCount++;
                intSODetailsNo++;
            }

            string insertedPicked = insertPickedSOQuery + ssSql + string.Join(", ", pickedItemQueryList) + ssSql2 + commit;
            Console.WriteLine(insertedPicked);

            try
            {
                bool inserted = mssql.Insert(insertedPicked);

                if (!inserted)
                {
                    //do not insert the order fault as 1. Later when its time it will not transfer again if the order fault is 1.
                    mssql.Message("False return by MSSQL : [" + orderId + "] Error inserting this order -----> " + insertedPicked);
                    return false;
                }
                else
                {
                    mssql.Message(insertedPicked);
                    if (cloudQtyQueryList.Count > 0)
                    {
                        for (int i = 0; i < cloudQtyQueryList.Count; i++)
                        {
                            string query = cloudQtyQueryList[i].ToString();
                            mysql.Insert(query);
                        }
                        cloudQtyQueryList.Clear();
                    }
                }
            }
            catch (Exception ex)
            {
                mssql.Message("[" + orderId + "] Error inserting this order -----> " + insertedPicked + " || Exception Msg: " + ex.Message);
                return false;
            }

            return true;
        }

        public bool noStock(string orderId, Dictionary<string, string> obj, Dictionary<string, string> valueField, Database mysql, SQLServer mssql)
        {
            GlobalLogger logger = new GlobalLogger();
            string dockey, custId, refId, orderDate, salesmanId, stringDate, _warehouseID, custGroupId, areaCode, checkerName, deliveryLocation, whCode;

            Dictionary<string, string> value = (Dictionary<string, string>)valueField;
            dockey = value["dockey"];
            custId = value["custId"];
            refId = value["refId"];
            orderDate = value["orderDate"];
            salesmanId = value["salesmanId"];
            stringDate = value["stringDate"];
            _warehouseID = value["_warehouseID"];
            custGroupId = value["custGroupId"];
            areaCode = value["areaCode"];
            checkerName = value["checkerName"];
            deliveryLocation = value["deliveryLocation"]; //added to varRefNo
            whCode = value["whCode"]; //added to varRefNo

            string noStockDockey = string.Empty;

            ArrayList dockeys = mssql.Select("(SELECT ISNULL(MAX(intSONo) + 1, 0) as dockey FROM Sal_SOTbl)"); //{[dockey, 61239]}

            if(dockeys.Count > 0)
            {
                Dictionary<string, string> dockeyObj = (Dictionary<string, string>)dockeys[0];
                noStockDockey = dockeyObj["dockey"];
            }
            else
            {
                return false;
            }
            
            int.TryParse(_warehouseID, out int warehouseID);

            string noStockItemQuery = "SELECT * FROM cms_order_item WHERE packed_qty <> quantity AND packing_status <> 0 AND cancel_status = 0 AND order_id = '" + orderId + "'";

            ArrayList noStockItems = mysql.Select(noStockItemQuery);
            mysql.Message(noStockItemQuery);

            int noStockCount = 1;
            string _totalAmt = "0";
            string _totalQty = "0";
            double taxAmt = 0.00;
            double totalTax = 0.00;

            string totalAmtNoStock = "SELECT SUM(unit_price * (quantity - packed_qty)) AS sub_total, COUNT(*) totalQTY FROM cms_order_item WHERE cancel_status = 0 AND packed_qty <> quantity AND order_id = '" + orderId + "'";
            ArrayList totalAmtListNoStock = mysql.Select(totalAmtNoStock);

            if(totalAmtListNoStock.Count > 0)
            {
                Dictionary<string, string> eachAmt = (Dictionary<string, string>)totalAmtListNoStock[0];
                
                _totalAmt = eachAmt["sub_total"];
                _totalQty = eachAmt["totalQTY"];
            }
            
            double.TryParse(_totalAmt, out double totalAmt);

            int.TryParse(_totalQty, out int totalQty);
            if (totalQty <= 0)
            {
                return false;
            }

            taxAmt = totalAmt * 0.00;
            totalTax = totalAmt + taxAmt;

            if (totalTax == 0)
            {
                totalTax = 0;
            }

            if (taxAmt == 0)
            {
                taxAmt = 0;
            }

            string stmt_maxDelKey;
            string _intSODetailsNo = "0";

            stmt_maxDelKey = "(SELECT ISNULL(MAX(intSODetailsNo)+6,0) as intSODetailsNo FROM Sal_SODetailsTbl)";

            ArrayList delKey = mssql.Select(stmt_maxDelKey);

            if(delKey.Count > 0)
            {
                Dictionary<string, string> delKeyObj = (Dictionary<string, string>)delKey[0];

                _intSODetailsNo = delKeyObj["intSODetailsNo"];
            }
            else
            {
                return false;
            }

            int.TryParse(_intSODetailsNo, out int intSODetailsNo);
            string noStockOrderId = orderId + "-1";
            string noStockOrderDeliveryNote = "|NO-STOCK!" + checkerName;

            noStockOrderDeliveryNote = noStockOrderDeliveryNote.Replace("'", "''");
            deliveryLocation = deliveryLocation.Replace("'", "''");

            //added deliveryLocation ---> varRefNo
            string begin = "begin transaction ";
            string declare = "declare @intSoId int; ";
            string getIntSONo = "set @intSoId = (select top(1) (intsono+1) as soId from Sal_SOTbl order by intSONo desc) ";
            string transferNoStockSOQuery = "INSERT INTO Sal_SOTbl(intSONo, varTrxNo, intCustID, intRefCodeID, varRefNo, charOverDue, dtSODate, decGSTAmt, decTotalAmt, decRoundAmt, dtSODateInitial, charCustPONo, varRemarks, intTrxUploadNo, charStatus, intApproveBy, intCurrID, intAutoIncrementNo, varDeleteReason, intModifyBy, dtModifyDate, intCreatedby, dtCreatedDate, intUserWareHouseID, intCustGrpID, intSalManID, intAreaCodeID, blnIsDelete) VALUES "; //added blnIsDelete 15102020
            string noStockValues = string.Format("({0}, '{1}', '{2}', '{3}', '{4}', '{5}', '{6}', '{7}', '{8}', '{9}', '{10}', '{11}', '{12}', '{13}', '{14}', '{15}', '{16}', '{17}', '{18}', '{19}', '{20}', '{21}', '{22}', '{23}', '{24}', '{25}', '{26}', '{27}')", "@intSoId", noStockOrderId, custId, refId, deliveryLocation, "", orderDate, 0, totalAmt, 0, orderDate, "", noStockOrderDeliveryNote, 0, "P", 0, 1, 1, "", salesmanId, stringDate, salesmanId, stringDate, warehouseID, custGroupId, salesmanId, areaCode, "FALSE");
            
            string noStockSOQuery = begin + declare + getIntSONo + transferNoStockSOQuery + string.Join(", ", noStockValues);

            HashSet<string> noStockItemQueryList = new HashSet<string>();
            ArrayList cloudQtyQueryList = new ArrayList();
            
            string commit = "commit;";
            string noStockSql = ";SET IDENTITY_INSERT Sal_SODetailsTbl ON;INSERT INTO Sal_SODetailsTbl (intSODetailsNo,intSONo,intInvID,varDesc,varModel,decOrderQty,decWriteOffQty,decRecommendedPrice,decMinimumSP,decGSTRate,varStockUnitNm,decUnitPrice,varAssemLinkKey,blnIsDelete,intWarehouseID,charPriority,decAvgCost,decCost,charItemCodePrint,intGSTGrpID,intAutoIncrementNo,decDiscPercent1,decDiscPercent2) VALUES ";
            string noStockSql2 = ";SET IDENTITY_INSERT Sal_SODetailsTbl OFF;";

            for (int ixx = 0; ixx < noStockItems.Count; ixx++)
            {
                Dictionary<string, string> itemNoStock = (Dictionary<string, string>)noStockItems[ixx];

                string productCode, productName, uom, _packedQty, unitPrice, _qty, cloudProdCodeNoStock, gstId, varDesc, intInvId;
                int nostockQty;

                productCode = itemNoStock["product_code"].Replace("'", "''");

                varDesc = string.Empty;
                intInvId = string.Empty;
                gstId = string.Empty;

                string stmtProd = "(SELECT intInvid, varDesc, intSalGSTGrpID, varModel FROM Inv_StockTbl WHERE charItemCode = '" + productCode + "')";
                ArrayList productList = mssql.Select(stmtProd);

                if (productList.Count > 0)
                {
                    Dictionary<string, string> objProd = (Dictionary<string, string>)productList[0];

                    varDesc = objProd["varDesc"];
                    intInvId = objProd["intInvid"];
                    gstId = objProd["intSalGSTGrpID"];
                    itemNoStock["product_name"] = objProd["varModel"];  //get varModel for productName
                }
                else
                {
                    string notExists = "[" + orderId + "] NO STOCK ----> this product (" + productCode + ") is no longer exists in APS";
                    logger.Broadcast(notExists);
                    mysql.Message(notExists);
                    Database.Sanitize(ref notExists);
                    mysql.Insert("UPDATE cms_order SET order_fault_message = '" + notExists + "' WHERE order_id = '" + orderId + "'");
                    return false;
                }

                productName = itemNoStock["product_name"].Replace("'", "''");

                _qty = itemNoStock["quantity"];
                _packedQty = itemNoStock["packed_qty"];

                int.TryParse(_qty, out int qty);
                int.TryParse(_packedQty, out int packedQtyNo);

                nostockQty = qty - packedQtyNo;     /* formula for no stock qty */

                unitPrice = itemNoStock["unit_price"];

                varDesc = varDesc.Replace("'", "''");

                uom = itemNoStock["unit_uom"].Replace("'", "''");

                cloudProdCodeNoStock = itemNoStock["product_code"];

                string noStockItemValues = string.Format("('{0}',{1},'{2}','{3}','{4}','{5}','{6}','{7}','{8}','{9}','{10}','{11}','{12}','{13}','{14}','{15}','{16}','{17}','{18}','{19}','{20}','{21}','{22}')", intSODetailsNo, "@intSoId", intInvId, varDesc, productName, nostockQty, 0, 0, 0, 0.00, uom, unitPrice, "", "FALSE", warehouseID, "N", 0, 0, productCode, gstId, noStockCount, 0, 0);
                noStockItemQueryList.Add(noStockItemValues);

                string cloudQtyQuery = "SELECT * FROM cms_warehouse_stock WHERE product_code = '" + cloudProdCodeNoStock + "' AND wh_code = " + whCode;
                ArrayList checkCloudQty = mysql.Select(cloudQtyQuery);

                int cloud_qty = -1;
                if (checkCloudQty.Count > 0)
                {
                    Dictionary<string, string> objCloudQty = (Dictionary<string, string>)checkCloudQty[0];
                    string _cloud_qty = objCloudQty["cloud_qty"];
                    int.TryParse(_cloud_qty, out cloud_qty);
                    mysql.Message("cloudQtyQuery [noStock]: " + cloudQtyQuery + " --- [" + cloud_qty + "] ");
                }

                if(cloud_qty > 0)
                {
                    string updateWhStock = "UPDATE cms_warehouse_stock SET cloud_qty = cloud_qty - " + nostockQty + " WHERE product_code = '" + cloudProdCodeNoStock + "' AND wh_code = " + whCode;
                    cloudQtyQueryList.Add(updateWhStock);
                    mysql.Message("updateWhStock: " + updateWhStock + " [ minus nostockQty: " + nostockQty + "] ");
                }

                noStockCount++;
                intSODetailsNo++;
            }

            string insertedNoStock = noStockSOQuery + noStockSql + string.Join(", ", noStockItemQueryList) + noStockSql2 + commit;
            Console.WriteLine("insertedNoStock:" + insertedNoStock);

            try
            {
                bool inserted = mssql.Insert(insertedNoStock);

                if (!inserted)
                {
                    mssql.Message("False return by MSSQL : [" + noStockOrderId + "] Error inserting this order -----> " + insertedNoStock);
                    return false;
                }
                else
                {
                    mssql.Message(insertedNoStock);
                    if (cloudQtyQueryList.Count > 0)
                    {
                        for(int i = 0; i < cloudQtyQueryList.Count; i++)
                        {
                            string query = cloudQtyQueryList[i].ToString();
                            mysql.Insert(query);
                        }
                        cloudQtyQueryList.Clear();
                    }
                }
            }
            catch (Exception ex)
            {
                mssql.Message("[" + noStockOrderId + "] Error inserting this order -----> " + insertedNoStock + " || Exception Msg: " + ex.Message);
                return false;
            }

            return true;
        }

        public void Run()
        {
            try
            {
                Thread thread = new Thread(p =>
                {
                    Thread.CurrentThread.IsBackground = true;
                    GlobalLogger logger = new GlobalLogger();

                    DpprSyncLog slog = new DpprSyncLog();
                    slog.action_identifier = Constants.Action_APS_Transfer_SO;
                    slog.action_failure = 0;
                    slog.action_details = "Starting";
                    slog.action_failure_message = string.Empty;
                    slog.action_time = DateTime.Now.ToString();

                    DateTime startTime = DateTime.Now;

                    List<DpprSyncLog> list = LocalDB.checkJobRunning();
                    if (list.Count > 0)
                    {
                        DpprSyncLog value = list[0];
                        if (value.action_details == "Starting")
                        {
                            logger.message = "APS Transfer SO already running";
                            logger.Broadcast();
                            goto ENDJOB;
                        }
                    }
                    LocalDB.InsertSyncLog(slog);

                    logger.message = "APS Transfer SO is running";
                    logger.Broadcast();

                    List<DpprMySQLconfig> mysql_list = LocalDB.GetRemoteDatabaseConfig().Distinct().ToList();
                    List<DpprSQLServerconfig> mssql_list = LocalDB.GetRemoteSQLServerConfig();

                    mysql_list.Iterate<DpprMySQLconfig>((mysql_config, index) =>
                    {
                        DpprMySQLconfig mysqlconfig = mysql_list[index];
                        Database mysql = new Database();
                        mysql.Connect(index: index);

                        string targetDBname = string.Empty;

                        CheckBackendRule checkDB = new CheckBackendRule(mysql: mysql);
                        dynamic jsonRule = checkDB.CheckTablesExist().GetSettingByTableName("transfer_so");

                        ArrayList mssql_rule = new ArrayList();
                        if (jsonRule != null)
                        {
                            foreach (var key in jsonRule)
                            {
                                if (key.mssql != null && key.mssql.GetType().ToString() == "Newtonsoft.Json.Linq.JArray")
                                {
                                    var mssql_rules = key.mssql;
                                    foreach (var db in mssql_rules)
                                    {
                                        targetDBname = db.name;
                                    }
                                }
                            }
                        }
                        else
                        {
                            throw new Exception("APS Transfer SO sync requires backend rules");
                        }

                        //string SOQuery = "SELECT cms_order.*, CAST(cms_order.order_udf AS CHAR(10000) CHARACTER SET utf8) AS orderUdfJson, cms_login.staff_code FROM cms_order LEFT JOIN cms_login ON cms_order.salesperson_id = cms_login.login_id WHERE order_status = 1 AND cancel_status = 0 AND pack_confirmed = 1 AND doc_type = 'sales' AND order_fault = 0";
                        string SOQuery = "SELECT cms_order.*, CAST(cms_order.order_udf AS CHAR(10000) CHARACTER SET utf8) AS orderUdfJson, cms_login.staff_code FROM cms_order LEFT JOIN cms_login ON cms_order.salesperson_id = cms_login.login_id WHERE order_status = 1 AND cancel_status = 0 AND packing_status >= 1 AND doc_type = 'sales' AND order_fault = 0"; //changed packing_status >= 1
                        ArrayList queryResult = mysql.Select(SOQuery);

                        //store all order id in 1 arraylist, then check whether those order id exists in aps or not. If not, only then proceed with the transfer. If yes, update the status to 2.
                        //select * from Sal_SOTbl where varTrxNo IN('SO-KH-D2-2280', 'SO-KH-D2-2281', 'SO-YS-D2-1687')
                        //then compare with mysql
                        ArrayList orderListToTransfer = new ArrayList();
                        for(int ixx = 0; ixx < queryResult.Count; ixx++)
                        {
                            Dictionary<string, string> each = (Dictionary<string, string>)queryResult[ixx];

                            string order_id = each["order_id"];
                            orderListToTransfer.Add(order_id);
                        }
                        
                        mysql.Message(SOQuery);

                        if (queryResult.Count == 0)
                        {
                            logger.message = "["+mysqlconfig.config_database+"] " + "No orders to insert";
                            logger.Broadcast();
                        }
                        else
                        {
                            SQLServer mssql = new SQLServer();
                            mssql.Connect(targetDBname);

                            HashSet<string> orders = new HashSet<string>();
                            for (int i = 0; i < orderListToTransfer.Count; i++)
                            {
                                string _order_id = orderListToTransfer[i].ToString();
                                orders.Add(_order_id);
                            }

                            string ordersToCheck = "'" + string.Join("','", orders) + "'";
                            string checkExistingOrdersQuery = "SELECT varTrxNo FROM Sal_SOTbl WHERE varTrxNo IN (" + ordersToCheck + ")";
                            ArrayList checkExistingOrders = mssql.Select(checkExistingOrdersQuery);
                            mssql.Message(checkExistingOrdersQuery);

                            for (int idx = 0; idx < checkExistingOrders.Count; idx++)
                            {
                                Dictionary<string, string> each = (Dictionary<string, string>)checkExistingOrders[idx];
                                string order_id = each["varTrxNo"];

                                if (orders.Contains(order_id))
                                {
                                    logger.Broadcast("[" + mysqlconfig.config_database + "] " + " This order is already transferred to APS: " + order_id);
                                    //update status here
                                    int failCounter = 0;
                                checkUpdateStatus:
                                    string updatedQuery = "UPDATE cms_order SET order_status = 2 WHERE order_id = '" + order_id + "'";
                                    bool updated = mysql.Insert(updatedQuery);
                                    if (!updated)
                                    {
                                        Task.Delay(2000);
                                        failCounter++;
                                        if (failCounter < 4)
                                        {
                                            goto checkUpdateStatus;
                                        }
                                    }
                                    orders.Remove(order_id);
                                }
                            }

                            string ordersToTransfer = "'" + string.Join("','", orders) + "'";
                            string transferOrdersQuery = "SELECT cms_order.*, CAST(cms_order.order_udf AS CHAR(10000) CHARACTER SET utf8) AS orderUdfJson, cms_login.staff_code FROM cms_order LEFT JOIN cms_login ON cms_order.salesperson_id = cms_login.login_id WHERE order_id IN (" + ordersToTransfer + ")";
                            ArrayList transferOrders = mysql.Select(transferOrdersQuery);
                            mysql.Message("transferOrdersQuery: " + transferOrdersQuery);

                            logger.Broadcast("[" + mysqlconfig.config_database + "] " + "Total orders to be transferred: " + transferOrders.Count);

                            for (int i = 0; i < transferOrders.Count; i++)
                            {
                                Dictionary<string, string> obj = (Dictionary<string, string>)transferOrders[i];

                                string orderId = obj["order_id"];

                                ArrayList allOrderItems = mysql.Select("SELECT * FROM cms_order_item WHERE order_id = '" + orderId + "' AND cancel_status = 0"); /* get all items based on orderId */

                                string orderUdf = obj["orderUdfJson"].ToString();

                                JArray remarkJArray = orderUdf.IsJArray() ? (JArray)JToken.Parse(orderUdf) : new JArray();

                                Console.WriteLine(remarkJArray);

                                string refId = filterOrderUDFbyKey(remarkJArray, "refId");

                                Dictionary<string, string> valueField = fieldList(refId, obj, mssql);

                                bool exportSalesOrderToAPS_picked = picked(orderId, obj, valueField, mysql, mssql);

                                bool exportSalesOrderToAPS_nostock = noStock(orderId, obj, valueField, mysql, mssql);

                                int counter = 0;

                                if (exportSalesOrderToAPS_picked == true)
                                {
                                    counter++;
                                }
                                if (exportSalesOrderToAPS_nostock == true)
                                {
                                    counter++;
                                }
                                if (counter > 0)
                                {
                                    //updateStatusAgain if fail
                                    string updateOrderStatus = "UPDATE cms_order SET order_status = 2 WHERE order_id = '" + orderId + "'";
                                    int failCounter = 0;
                                checkUpdateStatus:
                                    bool updated = mysql.Insert(updateOrderStatus);
                                    if (!updated)
                                    {
                                        Task.Delay(2000);
                                        failCounter++;
                                        if (failCounter < 4)
                                        {
                                            goto checkUpdateStatus;
                                        }
                                    }

                                    if (exportSalesOrderToAPS_picked == true)
                                    {
                                        logger.Broadcast(orderId + " created in " + mysqlconfig.config_database);
                                    }

                                    if (exportSalesOrderToAPS_nostock == true)
                                    {
                                        logger.Broadcast(orderId + "-1 created in " + mysqlconfig.config_database);
                                    }
                                }
                            }
                        }
                    });
                    slog.action_identifier = Constants.Action_APS_Transfer_SO;
                    slog.action_failure = 0;
                    slog.action_details = "Finished";
                    slog.action_failure_message = string.Empty;
                    slog.action_time = DateTime.Now.ToString();
                    LocalDB.InsertSyncLog(slog);
                
                    DateTime endTime = DateTime.Now;
                    TimeSpan ts = endTime - startTime;
                    Console.WriteLine("Completed in: " + ts.TotalSeconds.ToString("F"));

                    logger.message = "Transfer SO finished in (" + ts.TotalSeconds.ToString("F") + " seconds)";
                    logger.Broadcast();
                ENDJOB:
                    Console.WriteLine("ENDJOB");
                });

                thread.Start();
                //thread.Join();
            }
            catch (ThreadAbortException e)
            {
                DpprException ex = new DpprException
                {
                    file_name = "JobAPSTransferSO",
                    exception = e.Message,
                    time = DateTime.Now.ToString()
                };
                LocalDB.InsertException(ex);

                Console.WriteLine(Constants.Thread_Exception + e.Message);
            }
        }
    }
}