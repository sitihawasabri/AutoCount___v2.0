using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Reflection;
using System.Net.NetworkInformation;
using EasySales.Object;
using MySql.Data.MySqlClient;
using MySql.Data;

namespace EasySales
{
    public partial class MainActivity : Form
    {
        public MainActivity()
        {
            InitializeComponent();
            
            Version version = Assembly.GetExecutingAssembly().GetName().Version;
            DateTime buildDate = new DateTime(2000, 1, 1)
                                    .AddDays(version.Build).AddSeconds(version.Revision * 2);
            string displayableVersion = $"(v.{version})";
            this.Text = "Easysales " + displayableVersion;

        }
        private void rb_multi_companies_CheckedChanged(object sender, EventArgs e)
        {
            btn_save.Visible = false;
            btn_saveaps.Visible = true;
        }

        private void GoDashboard()
        {
            var dashboard = new DashboardActivity();
            dashboard.Show();
            Hide();
        }


        private void btn_save_Click(object sender, EventArgs e)
        {
            string insertSoftware;
            string insertATCConfig;

            string db_host = txt_db_host.Text.ToString().Trim();
            string db_username = txt_db_username.Text.ToString().Trim();
            string db_password = txt_db_password.Text.ToString().Trim();
            string db_dbname = txt_db_name.Text.ToString().Trim();
            string socket_address = txt_socket_address.Text.ToString().Trim();

            string company_name = txtbox_company_name.Text.ToString().Trim();
            string username = txtbox_username.Text.ToString().Trim();
            string password = txtbox_password.Text.ToString().Trim();

            if (tabControl1.SelectedTab == tabControl1.TabPages["sqlacc"])
            {
                string acc_username = txt_acc_username.Text.ToString().Trim();
                string acc_password = txt_acc_password.Text.ToString().Trim();
                string acc_comp_name = txt_acc_comp.Text.ToString().Trim().ToUpper();
                string acc_db = txt_acc_db.Text.ToString().Trim().ToUpper();
                string acc_dcf = txt_acc_dcf.Text.ToString().Trim();

                if (Valid())
                {
                    //IsConnectedToInternet();
                    insertSoftware = string.Format("INSERT INTO accounting_software (software_name, software_username, software_password, software_link, software_db, software_comp) VALUES ('{0}','{1}','{2}','{3}','{4}','{5}')", tabControl1.SelectedTab.Text, acc_username, acc_password, acc_dcf, acc_db, acc_comp_name);
                    //SQLAcc.BizApp
                }
                else
                {
                    MessageBox.Show("Please fill all the boxes","EasySales", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                    return;
                }
            }
            else if (tabControl1.SelectedTab == tabControl1.TabPages["qne"])
            {
                string qneapi_url = txt_qneapi_url.Text.ToString().Trim();
                string database_name = txt_database_name.Text.ToString().Trim();

                if (Valid())
                {
                    insertSoftware = string.Format("INSERT INTO accounting_software (software_name, software_link, software_db) VALUES ('{0}','{1}','{2}')", tabControl1.SelectedTab.Text, qneapi_url, database_name);
                }
                else
                {
                    MessageBox.Show("Please fill all the boxes", "EasySales", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                    return;
                }
            }
            else if(tabControl1.SelectedTab == tabControl1.TabPages["atc"])
            {
                string atc_server_name = txt_atc_server_name.Text.ToString().Trim();
                string atc_server_instance = txt_atc_server_instance.Text.ToString().Trim();
                string atc_server_port = txt_atc_server_port.Text.ToString().Trim();
                string atc_server_db_name = txt_atc_server_db_name.Text.ToString().Trim();
                string atc_server_username = txt_atc_server_username.Text.ToString().Trim();
                string atc_server_password = txt_atc_server_password.Text.ToString().Trim();

                string atc_user_id = txt_atc_user_id.Text.ToString().Trim();
                string atc_password = txt_atc_password.Text.ToString().Trim();

                string datasource = atc_server_name + "\\" + atc_server_instance;

                if (Valid())
                {
                    insertSoftware = string.Format("INSERT INTO accounting_software (software_name, software_username, software_password) VALUES ('{0}','{1}','{2}')", tabControl1.SelectedTab.Text, atc_user_id, atc_password);
                    insertATCConfig = string.Format("INSERT INTO sql_server(data_source, database_name, user_id, password) VALUES ('{0}', '{1}', '{2}', '{3}')", datasource, atc_server_db_name, atc_server_username, atc_server_password);
                    LocalDB.Add(insertATCConfig);
                }
                else
                {
                    MessageBox.Show("Please fill all the boxes", "EasySales", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                    return;
                }
            }
            else //SAGE UBS
            {
                string path_name = txt_sageubs_pathname.Text.ToString().Trim();

                if (Valid())
                {
                    insertSoftware = string.Format("INSERT INTO accounting_software (software_name, software_link) VALUES ('{0}','{1}')", tabControl1.SelectedTab.Text, path_name);
                }
                else
                {
                    MessageBox.Show("Please fill all the boxes", "EasySales", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                    return;
                }
            }

            try
            {
                LocalDB.Add(insertSoftware);

                string insertFTP = string.Format("INSERT INTO ftp_server (username, password, company_name) VALUES ('{0}','{1}','{2}')", username, password, company_name);
                LocalDB.Add(insertFTP);

                /* if use save and add new button, delete this one */
                string insertDb = string.Format("INSERT INTO configuration (config_name, config_host, config_username, config_password, config_database, accounting_software, socket_address) VALUES ('{0}','{1}','{2}','{3}','{4}','{5}','{6}')", "EasySalesBackend", db_host, db_username, db_password, db_dbname, tabControl1.SelectedTab.Text, socket_address); //"easysales.asia"
                LocalDB.Add(insertDb);

                MessageBox.Show("Configurations saved succesfully", "EasySales", MessageBoxButtons.OK, MessageBoxIcon.Information);

                GoDashboard();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "EasySales", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
        }
        
        private bool Valid()
        {
            string company_name = txtbox_company_name.Text.ToString().Trim();
            string username = txtbox_username.Text.ToString().Trim();
            string password = txtbox_password.Text.ToString().Trim();

            string acc_software_name = tabControl1.SelectedTab.Text;
            string acc_username = txt_acc_username.Text.ToString().Trim();
            string acc_password = txt_acc_password.Text.ToString().Trim();
            string acc_comp_name = txt_acc_comp.Text.ToString().Trim();
            string acc_db = txt_acc_db.Text.ToString().Trim();
            string acc_dcf = txt_acc_dcf.Text.ToString().Trim();

            string db_host = txt_db_host.Text.ToString().Trim();
            string db_username = txt_db_username.Text.ToString().Trim();
            string db_password = txt_db_password.Text.ToString().Trim();
            string db_dbname = txt_db_name.Text.ToString().Trim();

            /* QNE */
            string qneapi_url = txt_qneapi_url.Text.ToString().Trim();
            string database_name = txt_database_name.Text.ToString().Trim();
            /* QNE */

            /* SAGE UBS */
            string sageubs_pathname = txt_sageubs_pathname.Text.ToString().Trim();
            /* SAGE UBS */

            /* ATC */
            string atc_server_name = txt_atc_server_name.Text.ToString().Trim();
            string atc_server_instance = txt_atc_server_name.Text.ToString().Trim();
            string atc_server_port = txt_atc_server_port.Text.ToString().Trim();
            string atc_server_db_name = txt_atc_server_db_name.Text.ToString().Trim();
            string atc_server_username = txt_atc_server_username.Text.ToString().Trim();
            string atc_server_password = txt_atc_server_password.Text.ToString().Trim();

            string atc_user_id = txt_atc_user_id.Text.ToString().Trim();
            string atc_password = txt_atc_password.Text.ToString().Trim();
            /* ATC */

            if (acc_software_name.Length != 0 && acc_username.Length != 0 && acc_password.Length != 0 && acc_comp_name.Length != 0 && acc_db.Length != 0 && acc_dcf.Length != 0 && db_username.Length != 0 && db_password.Length != 0 && db_dbname.Length != 0)
            {
                return true;
            }
            else if (qneapi_url.Length != 0 && database_name.Length != 0 && db_username.Length != 0 && db_password.Length != 0 && db_dbname.Length != 0)
            {
                return true;
            }
            else if (sageubs_pathname.Length != 0 && db_username.Length != 0 && db_password.Length != 0 && db_dbname.Length != 0)
            {
                return true;
            }
            else if (atc_server_name.Length != 0 && atc_server_instance.Length != 0 && atc_server_db_name.Length != 0 && atc_server_username.Length != 0 && atc_server_password.Length != 0 && atc_user_id.Length != 0 && atc_password.Length != 0)
            {
                return true;
            }
            //else if (company_name.Length != 0 && username.Length != 0 && password.Length != 0)
            //{
            //    return true;
            //}
            else
            {
                return false;
            }
                //return false;
        }

        private void btn_saveaps_Click(object sender, EventArgs e)
        {
            btn_go_dashboard.Visible = true;

            string mssql_datasource = txt_datasource.Text.ToString().Trim();                 /* MSSQL */
            string mssql_databasename = txt_databasename.Text.ToString().Trim();
            string mssql_userid = txt_userid.Text.ToString().Trim();
            string mssql_password = txt_mssql_password.Text.ToString().Trim();

            string db_host = txt_db_host.Text.ToString().Trim();
            string db_username = txt_db_username.Text.ToString().Trim();
            string db_password = txt_db_password.Text.ToString().Trim();
            string db_dbname = txt_db_name.Text.ToString().Trim();
            string socket_address = txt_socket_address.Text.ToString().Trim();

            string company_name = txtbox_company_name.Text.ToString().Trim();
            string username = txtbox_username.Text.ToString().Trim();
            string password = txtbox_password.Text.ToString().Trim();

            try
            {
                string insertSoftware = string.Format("INSERT INTO accounting_software (software_name) VALUES ('{0}')", "APS");
                LocalDB.Add(insertSoftware);

                string insertFTP = string.Format("INSERT INTO ftp_server (username, password, company_name) VALUES ('{0}','{1}','{2}')", username, password, company_name);
                LocalDB.Add(insertFTP);

                string insertDb = string.Format("INSERT INTO configuration (config_name, config_host, config_username, config_password, config_database, accounting_software, socket_address) VALUES ('{0}','{1}','{2}','{3}','{4}','{5}','{6}')", "EasySalesBackend", db_host, db_username, db_password, db_dbname, "APS", socket_address); //"easysales.asia"
                LocalDB.Add(insertDb);

                string insertMsSQL = string.Format("INSERT INTO sql_server (data_source, database_name, user_id, password) VALUES ('{0}','{1}','{2}','{3}')", mssql_datasource, mssql_databasename, mssql_userid, mssql_password);                  /* MSSQL */
                LocalDB.Add(insertMsSQL);

                txt_datasource.Text = "LAPTOP-B460KIQK QNEBSS";
                txt_databasename.Text = "";
                txt_userid.Text = "sa";
                txt_mssql_password.Text = "QnE123!@#";

                txtbox_company_name.Text = "staging";
                txtbox_username.Text = "staging@easysales.asia";
                txtbox_password.Text = "staging123@";

                txt_db_host.Text = "easysales.asia";
                txt_db_username.Text = "easysale_julfi";
                txt_db_password.Text = "julfi123@";
                txt_db_name.Text = "easysale_testing";

                MessageBox.Show("Configurations saved succesfully", "EasySales");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "EasySales");
            }

        }

        private void btn_go_dashboard_Click(object sender, EventArgs e)
        {
            GoDashboard();
        }

        private void button_test_connection_Click(object sender, EventArgs e)
        {
            string db_host = txt_db_host.Text.ToString().Trim();
            string db_username = txt_db_username.Text.ToString().Trim();
            string db_password = txt_db_password.Text.ToString().Trim();
            string db_dbname = txt_db_name.Text.ToString().Trim();

            string connectionString = string.Format("Server={0}; database={1}; UID={2}; password={3}; Pooling=false;", txt_db_host.Text, txt_db_name.Text, txt_db_username.Text, txt_db_password.Text);

            MySqlConnection connection = new MySqlConnection(connectionString);
            bool result = false;

            try
            {
                connection.Open();
                result = true;
                MessageBox.Show("Connection successful!", "EasySales", MessageBoxButtons.OK, MessageBoxIcon.Information);
                connection.Close();
            }
            catch
            {
                result = false;
                MessageBox.Show("Connection failed! Please check the configuration details.", "EasySales", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
        }

        //private void button_test_connection_atc_Click(object sender, EventArgs e)
        //{
        //    try
        //    {
        //        bool isConnected = AutoCount.TriggerConnection();
        //        if (isConnected == true)
        //        {
        //            MessageBox.Show("Connection successful!", "EasySales", MessageBoxButtons.OK, MessageBoxIcon.Information);
        //        }
        //        else
        //        {
        //            MessageBox.Show("Connection failed! Please check the configuration details.", "EasySales", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine(ex.Message);
        //        MessageBox.Show("Connection failed! Please check the configuration details.", "EasySales", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
        //    }
        //}
    }
}