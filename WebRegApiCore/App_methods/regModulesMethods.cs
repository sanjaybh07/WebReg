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
    public class regModulesmethods
    {
        commonMethods globalMethods = new commonMethods();


        public dynamic GetModulesList(String cConStr, string productId)
        {
            dynamic result = new ExpandoObject();
            try
            {

                String cExpr = "";

                if (String.IsNullOrEmpty(productId))
                    cExpr = $"Select a.productId,productName,moduleId,moduleName,moduleAlias,active FROM  rw_modules a JOIN rw_products b on a.productid = b.productid";
                else
                    cExpr = $"Select moduleId, moduleName, moduleAlias, active FROM rw_modules where productid = '" + productId + "' ";

                SqlConnection con = new SqlConnection(cConStr);
                SqlCommand cmd = new SqlCommand(cExpr, con);
                SqlDataAdapter sda = new SqlDataAdapter(cmd);
                DataSet dset = new DataSet();
                sda.Fill(dset, "TDATA");

                result.Modules = dset.Tables["TDATA"];
                result.errMsg = "";
                return result;


            }
            catch (Exception ex)
            {
                result.errMsg = ex.Message;
                return result;
            }


        }



        public String SaveRegmodules(String connStr, Object body, int nUpdatemode, String cProductId = null)
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

                DataSet Dd = (DataSet)Newtonsoft.Json.JsonConvert.DeserializeObject(serializedObject, (typeof(DataSet)));

                if (!Dd.Tables.Contains("Modules"))
                    return "Invalid Json String ";

                DataTable tRegModules = Dd.Tables[0];

                if (nUpdatemode == 2)
                {
                    DataRow dr = tRegModules.Rows[0];
                    if (String.IsNullOrEmpty(tRegModules.Rows[0]["productId"].ToString()))
                    {
                        String cmdText = $"Select top 1 productId from rw_products (nolock) where productId='{cProductId}'";

                        DataTable dtExists = new DataTable();

                        sda = new SqlDataAdapter(cmdText, con);
                        sda.Fill(dtExists);

                        if (dtExists.Rows.Count == 0)
                            return "Invalid Prodct Id parameter";

                        foreach (DataRow drM in tRegModules.Rows)
                        {
                            drM["productId"] = cProductId;
                        }
                    }
                }


                cmd = new SqlCommand("Declare @tblModules as tvRegModules Select * from @tblModules", con);
                sda = new SqlDataAdapter(cmd);
                sda.Fill(dset, "TREGMODULES");



                globalMethods.AddUploadTableData(tRegModules, dset.Tables["TREGMODULES"], ref cErr);
                if (cErr != "")
                    return cErr;

                if (nUpdatemode == 2)
                {
                    cmd = new SqlCommand("Declare @tblEditCols as tv_EditCols Select * from @tblEditCols", con);

                    sda = new SqlDataAdapter(cmd);
                    sda.Fill(dset, "tEditCols");

                    globalMethods.AddDataForEditCols(tRegModules, dset.Tables["tEditCols"], "rw_modules", ref cErr);

                    if (cErr != "")
                        return cErr;

                }

                cErr = "";

                cmd = new SqlCommand("SAVETRAN_REGMODULES", con);
                cmd.CommandType = CommandType.StoredProcedure;



                cmd.Parameters.Clear();
                cmd.Parameters.AddWithValue("@nUpdatemode", nUpdatemode);
                cmd.Parameters.AddWithValue("@tblModules", dset.Tables["TREGMODULES"]);

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


        public dynamic SaveContacts(String connStr, Object body, int nUpdatemode)
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

                Contact newContact = Newtonsoft.Json.JsonConvert.DeserializeObject<Contact>(serializedObject);

                List<Contact> ContactsList = new List<Contact> { newContact };

                DataTable tContact = globalMethods.CreateDataTable<Contact>(ContactsList);

                if (String.IsNullOrEmpty(tContact.Rows[0]["clientId"].ToString()))
                {
                    String cClientId = tContact.Rows[0]["clientId"].ToString();

                    String cmdText = $"Select top 1 client_id from rw_regn_mst (nolock) where client_id='{cClientId}'";

                    DataTable dtExists = new DataTable();

                    sda = new SqlDataAdapter(cmdText, con);
                    sda.Fill(dtExists);

                    if (dtExists.Rows.Count == 0)
                        return "Invalid Client Id provided";

                    tContact.Rows[0]["clientId"] = cClientId;
                }


                cmd = new SqlCommand("SELECT * FROM rw_mapping_cols where tablename='rw_contact_Details';" +
                    "                 Declare @tblContacts as tvContactDetails Select * from @tblContacts", con);
                sda = new SqlDataAdapter(cmd);
                sda.Fill(dset);

                DataTable dtMapTable = new DataTable();
                DataTable dtTargetTable = new DataTable();

                dtMapTable = dset.Tables[0];
                dtTargetTable = dset.Tables[1];

                globalMethods.AddDataInUploadTablewithMapping(dtMapTable, tContact, dtTargetTable, ref cErr);
                if (cErr != "")
                    return cErr;

                if (nUpdatemode == 2)
                {
                    cmd = new SqlCommand("Declare @tblEditCols as tv_EditCols Select * from @tblEditCols", con);

                    sda = new SqlDataAdapter(cmd);
                    sda.Fill(dset, "tEditCols");

                    globalMethods.AddDataForEditCols(tContact, dset.Tables["tEditCols"], "rw_contact_details", ref cErr);

                    if (cErr != "")
                    {
                      result.Message= cErr;
                      return result;
                    }
                }

                cErr = "";

                cmd = new SqlCommand("SAVETRAN_RW_CONTACTS", con);
                cmd.CommandType = CommandType.StoredProcedure;



                cmd.Parameters.Clear();
                cmd.Parameters.AddWithValue("@nUpdatemode", nUpdatemode);
                cmd.Parameters.AddWithValue("@tblContacts", dtTargetTable);

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
                            result.Message = cErr;
                        }
                        else
                        {
                            result.rowId = dset.Tables["TDATA"].Rows[0]["MEMO_ID"];
                            result.Message = "";
                        }
                    }

                }
                else
                {
                    result.Message = "Record Not Updated";
                }

                return result;

            }

            catch (Exception ex)
            {
                return ex.Message.ToString();
            }

        }
    }

}