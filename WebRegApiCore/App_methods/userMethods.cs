using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Dynamic;
using System.Data.SqlClient;
using System.Data;
using WebRegApiCore.Models;
using WebRegApiCore.App_methods;

namespace WebRegApiCore.App_methods
{
    public class userMethods
    {
        commonMethods globalMethods = new commonMethods();


        public String VerifyUserCredentials(String cConStr, String userId, String password)
        {
            try
            {

                String cExpr = "";
                cExpr = $" Select username  from rw_users  where  usercode = '" + userId + "' and passwd = '" + password + "' ";

                SqlConnection con = new SqlConnection(cConStr);
                SqlCommand cmd = new SqlCommand(cExpr, con);
                SqlDataAdapter sda = new SqlDataAdapter(cmd);
                DataSet dset = new DataSet();
                sda.Fill(dset, "TDATA");

                if (dset.Tables["TDATA"].Rows.Count > 0)
                {
                    return "";
                }
                else
                {
                    return "Invalid userId/password";
                }


            }
            catch (Exception ex)
            {
                return ex.Message.ToString();
            }
        }

        public object GetUsersList(String cConStr)
        {
            dynamic result = new ExpandoObject();
            try
            {


                String cExpr = "";

                cExpr = $" Select userCode,userName,roleCode,isnull(inactive,0) as inactive,loginId " +
                    $"from rw_users ";

                SqlConnection con = new SqlConnection(cConStr);
                SqlCommand cmd = new SqlCommand(cExpr, con);
                SqlDataAdapter sda = new SqlDataAdapter(cmd);
                DataSet dset = new DataSet();
                sda.Fill(dset, "TDATA");

                result.Users = dset.Tables["TDATA"];

                return result;


            }
            catch (Exception ex)
            {
                result.errMsg = ex.Message;
                return result;
            }


        }

        public String SaveUser(String connStr, Object body, int nUpdatemode, String cUserCode = null)
        {
            try
            {
                dynamic result = new ExpandoObject();

                String cErr = "";

                SqlConnection con = new SqlConnection(connStr);
                SqlCommand cmd = new SqlCommand();
                SqlDataAdapter sda = new SqlDataAdapter();
                DataSet dset = new DataSet();

                string serializedObject = Newtonsoft.Json.JsonConvert.SerializeObject(body, Newtonsoft.Json.Formatting.Indented);

                User regUsers = Newtonsoft.Json.JsonConvert.DeserializeObject<User>(serializedObject);

                List<User> regUsersList = new List<User> { regUsers };

                DataTable tRegUsers = globalMethods.CreateDataTable<User>(regUsersList);

                if (nUpdatemode == 2)
                {
                    if (String.IsNullOrEmpty(tRegUsers.Rows[0]["UserCode"].ToString()))
                    {
                        String cmdText = $"Select top 1 UserCode from rw_Users (nolock) where UserCode='{cUserCode}'";

                        DataTable dtExists = new DataTable();

                        sda = new SqlDataAdapter(cmdText, con);
                        sda.Fill(dtExists);

                        if (dtExists.Rows.Count == 0)
                            return "Invalid User Id parameter";

                        tRegUsers.Rows[0]["UserCode"] = cUserCode;
                    }
                }


                cmd = new SqlCommand("Declare @tblUser as tvUsers Select * from @tblUser", con);
                sda = new SqlDataAdapter(cmd);
                sda.Fill(dset, "TREGUserS");



                globalMethods.AddUploadTableData(tRegUsers, dset.Tables["TREGUserS"], ref cErr);
                if (cErr != "")
                    return cErr;

                if (nUpdatemode == 2)
                {
                    cmd = new SqlCommand("Declare @tblEditCols as tv_EditCols Select * from @tblEditCols", con);

                    sda = new SqlDataAdapter(cmd);
                    sda.Fill(dset, "tEditCols");

                    globalMethods.AddDataForEditCols(tRegUsers, dset.Tables["tEditCols"], "rw_Users", ref cErr);

                    if (cErr != "")
                        return cErr;

                }

                cErr = "";

                cmd = new SqlCommand("SAVETRAN_REGUser", con);
                cmd.CommandType = CommandType.StoredProcedure;



                cmd.Parameters.Clear();
                cmd.Parameters.AddWithValue("@nUpdatemode", nUpdatemode);
                cmd.Parameters.AddWithValue("@tblUser", dset.Tables["TREGUserS"]);

                if (nUpdatemode == 2)
                    cmd.Parameters.AddWithValue("@tblEditCols", dset.Tables["tEditCols"]);

                //  cmd.Parameters.AddWithValue("@tblEditCols", dset.Tables["tEditCols"]);


                sda = new SqlDataAdapter(cmd);

                sda.Fill(dset, "TDATA");
                if (dset.Tables["TDATA"].Rows.Count > 0)
                {

                    if (dset.Tables["TDATA"].Columns.Contains("ERRMSG"))
                    {
                        cErr = Convert.ToString(dset.Tables["TDATA"].Rows[0]["ERRMSG"]);

                        if (!String.IsNullOrEmpty(cErr))
                        {
                            return cErr;
                        }
                        else
                        {

                            return "";
                        }
                    }

                }
                else
                {
                    return "Record Not Updated";
                }

                return "";

            }

            catch (Exception ex)
            {
                return ex.Message.ToString();
            }

        }
    }

}