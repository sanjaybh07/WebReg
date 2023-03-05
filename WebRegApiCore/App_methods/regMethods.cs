using WebRegApiCore.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Dynamic;
using System.Linq;
using System.Web;
using WebRegApiCore.App_methods;
using System.Reflection;
using System.Runtime.ConstrainedExecution;
using System.Security.Cryptography;
using System.Transactions;
using System.Linq.Expressions;
using System.Drawing;
using System.Security.Cryptography.X509Certificates;
using System.Runtime.InteropServices;

namespace WebRegApiCore.App_methods
{
    public class regMethods
    {

        commonMethods globalMethods = new commonMethods();


        public dynamic RegisterMultiPos(String connStr, Object body, String cUserCode)
        {
            dynamic result = new ExpandoObject();

            String cErr = "";
            result.Message = "";

            try
            {


                SqlConnection con = new SqlConnection(connStr);
                SqlCommand cmd = new SqlCommand();
                SqlDataAdapter sda = new SqlDataAdapter();
                DataSet dset = new DataSet();


                string serializedObject = Newtonsoft.Json.JsonConvert.SerializeObject(body, Newtonsoft.Json.Formatting.Indented);

                DataSet Dd = (DataSet)Newtonsoft.Json.JsonConvert.DeserializeObject(serializedObject, (typeof(DataSet)));

                if (!Dd.Tables.Contains("regPosData"))
                {
                    result.Message = "Invalid Json String ";
                    return result;
                }

                DataTable tRegPos = Dd.Tables[0];
                DataTable tMapCols = new DataTable();

                cmd = new SqlCommand("SELECT * FROM rw_mapping_cols where tablename='rw_regn_loc_details';", con);
                sda = new SqlDataAdapter(cmd);
                sda.Fill(tMapCols);

                cmd = new SqlCommand("Declare @tblRegPos as tvClientLocDetails Select * from @tblRegPos", con);
                sda = new SqlDataAdapter(cmd);
                sda.Fill(dset, "TREGPos");



                globalMethods.AddDataInUploadTablewithMapping(tMapCols, tRegPos, dset.Tables["TREGPos"], ref cErr);
                if (cErr != "")
                {
                    result.Message = cErr;
                    return result;
                }

                cErr = "";

                cmd = new SqlCommand("SAVETRAN_RW_REGAUDIT", con);
                cmd.CommandType = CommandType.StoredProcedure;


                DateTime dRegLastModifiedon = DateTime.Now;

                cmd.Parameters.Clear();
                cmd.Parameters.AddWithValue("@tblRegPos", dset.Tables["TREGPos"]);
                cmd.Parameters.AddWithValue("@cUserCode", cUserCode);
                cmd.Parameters.AddWithValue("@dRegDetailsModifiedOn", dRegLastModifiedon);

                SqlParameter outputMsgParam = new SqlParameter("@cErrormsg", SqlDbType.VarChar, -1)
                {
                    Direction = ParameterDirection.Output
                };

                cmd.Parameters.Add(outputMsgParam);


                sda = new SqlDataAdapter(cmd);

                sda.Fill(dset, "TDATA");

                String cErrmsgOutput = outputMsgParam.Value.ToString();

                if (dset.Tables["TDATA"].Rows.Count > 0 || !String.IsNullOrEmpty(cErrmsgOutput))
                {
                    result.Message = cErrmsgOutput;
                    if (dset.Tables["TDATA"].Columns.Contains("client_id"))
                    {
                        String cErrClientId = dset.Tables["TDATA"].Rows[0]["client_id"].ToString();
                        if (!String.IsNullOrEmpty(cErrClientId))
                            result.errData = dset.Tables["TDATA"];
                        else
                        {
                            //cErr = EncryptRegString(dRegLastModifiedon, con);

                            //if (!String.IsNullOrEmpty(cErr))
                            //    return cErr;
                        }
                    }

                }


                return result;

            }

            catch (Exception ex)
            {

                result.Message = ex.Message.ToString();
                return result;
            }

        }

        public dynamic GetClientRegDetails(String cConStr, String cClientId)
        {
            dynamic result = new ExpandoObject();

            try
            {

                SqlConnection conn = new SqlConnection(cConStr);

                String cmdStr = $" Select client_id clientId,client_name clientName,address1,address2,city,state,email,mobile as phone,remarks," +
                                $" gst_no gstNo, isnull(b.ac_name,'') acName,a.linked_ledger_ac_code acCode, replace(CONVERT(VARCHAR, regn_valid_till, 106),' ','-') regnValidTill," +
                                $" replace(CONVERT(VARCHAR, amc_valid_till, 106),' ','-')  amcValidTill, ho_amc_amount hoAmcAmount," +
                                $" pos_amc_amount posAmcAmount, cloud_charges_per_user_per_month cloudRate," +
                                $" start_date startDate, wizclip_group_code wizclipGrpCode,wizclip_charges wizclipRate,0 frequency,a.no_users noOfUsers" +
                                $" FROM  rw_regn_mst a (NOLOCK)" +
                                $" left join lm01106 b on a.linked_ledger_ac_code=b.ac_code where client_id ='{cClientId}';" +

                    $" Select firstName,lastName,isnull(firstname,'')+' '+isnull(lastname,'') as contactName,email,mobile,designation,inactive,rowId" +
                    $" FROM  rw_contact_details where client_id='{cClientId}';" +

                    $" SELECT b.productId, a.moduleId,a.modulename,a.moduleAlias," +
                    $" (case when c.client_id is not null then convert(bit,1) else convert(bit,0) end) registered" +
                    $" from rw_modules a JOIN rw_regn_mst b on a.productId = b.productId " +
                    $" LEFT JOIN rw_client_regn_modules c ON c.client_id = b.client_id AND c.moduleId = a.moduleId" +
                    $" WHERE b.client_id = '{cClientId}';" +

                    $" select a.location_id locationId,a.location_name locationName, " +
                    $" locCity city,locState state,replace(CONVERT(VARCHAR, a.min_xn_dt, 106),' ','-') as minXnDate," +
                    $" replace(CONVERT(VARCHAR, a.max_xn_dt, 106),' ','-') as maxXnDate,locledgerAcCode acCode,ISNULL(c.ac_name,'') acName," +
                    $" a.no_users noOfUsers,replace(CONVERT(VARCHAR, a.amc_start_date, 106),' ','-') amcStartDate," +
                    $" a.amc_rate amcAmount, a.wizclip_charges wizclipRate," +
                    $" ISNULL(a.registered,0) registered " +
                    $" from rw_regn_loc_details a " +
                    $" JOIN rw_regn_mst b ON a.client_id = b.client_id " +
                    $" LEFT JOIN lm01106 c ON c.ac_code = a.locledgerAcCode where a.client_id = '{cClientId}'";

                SqlCommand cmd = new SqlCommand(cmdStr, conn);
                SqlDataAdapter sda = new SqlDataAdapter(cmd);
                DataSet ds = new DataSet();

                sda.Fill(ds);

                result.clientMst = ds.Tables[0];
                result.contactDetails = ds.Tables[1];
                result.Modules = ds.Tables[2];
                result.locDetails = ds.Tables[3];
            }

            catch (Exception ex)
            {
                result.Message = ex.Message;
            }

            return result;
        }


        public dynamic GetMemoDocsData(String cConStr, String cClientId)
        {

            dynamic result = new ExpandoObject();

            result.Message = "";

            try
            {
                String cExpr = "";


                SqlConnection con = new SqlConnection(cConStr);

                cExpr = $"SPRW_GetmemoDocs";

                SqlCommand cmd = new SqlCommand(cExpr, con);
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("@cXnType", "inhouseReg");
                cmd.Parameters.AddWithValue("@cMemoId", cClientId);

                SqlDataAdapter sda = new SqlDataAdapter(cmd);
                DataTable dt = new DataTable();

                sda.Fill(dt);
                if (dt.Rows.Count > 0)
                {
                    dt.Columns.Add("docImage", typeof(String));

                    foreach (DataRow dr in dt.Rows)
                    {
                        dr["docImage"] = Convert.ToBase64String((byte[])dr["doc_image"]);
                    }

                    dt.Columns.Remove("doc_image");

                    result.data = dt;
                }
                else
                {
                    result.Message = "No Document attached";
                }

            }
            catch (Exception ex)
            {
                result.Message = ex.Message.ToString();
            }

            return result;


        }



        public String deleteClientData(String cConStr, String cClientId)
        {

            String cMessage = "";

            try
            {
                String cExpr = "", cErr = "";


                SqlConnection con = new SqlConnection(cConStr);

                cExpr = $"Select top 1 client_id from rw_regn_mst where client_id='{cClientId}'";

                SqlCommand cmd = new SqlCommand(cExpr, con);
                DataTable dtExists = new DataTable();
                SqlDataAdapter sda = new SqlDataAdapter(cmd);

                sda.Fill(dtExists);
                if (dtExists.Rows.Count == 0)
                    return "Client Id not found in masters...";


                cExpr = "sprw_deleteClient";
                cmd = new SqlCommand(cExpr, con);
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("@cClientId", cClientId);

                DataSet dsResult = new DataSet();
                sda = new SqlDataAdapter(cmd);

                sda.Fill(dsResult, "TDATA");
                if (dsResult.Tables["TDATA"].Rows.Count > 0)
                {

                    if (dsResult.Tables["TDATA"].Columns.Contains("errmsg"))
                    {
                        cErr = Convert.ToString(dsResult.Tables["TDATA"].Rows[0]["ERRMSG"]);

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
                    return "Record Not deleted";
                }

                return "";


            }
            catch (Exception ex)
            {
                cMessage = ex.Message.ToString();
            }

            return cMessage;


        }


        public String deleteDocsData(String cConStr, String cClientId, String cImageId)
        {

            String cMessage = "";

            try
            {
                String cExpr = "", cErr = "";


                SqlConnection con = new SqlConnection(cConStr);

                cExpr = $"Select top 1 client_id from rw_regn_mst where client_id='{cClientId}'";

                SqlCommand cmd = new SqlCommand(cExpr, con);
                DataTable dtExists = new DataTable();
                SqlDataAdapter sda = new SqlDataAdapter(cmd);

                sda.Fill(dtExists);
                if (dtExists.Rows.Count == 0)
                    return "Invalid Client Id given...";


                cExpr = $"Select top 1 img_id from rw_regdocs (nolock) where clientId='{cClientId}' and img_id='{cImageId}'";

                cmd = new SqlCommand(cExpr, con);
                dtExists = new DataTable();
                sda = new SqlDataAdapter(cmd);

                sda.Fill(dtExists);
                if (dtExists.Rows.Count == 0)
                    return "Invalid Image details provided for Deletion...";

                cExpr = "sprw_detachDocs";
                cmd = new SqlCommand(cExpr, con);
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("@cClientId", cClientId);
                cmd.Parameters.AddWithValue("@cImageId", cImageId);

                DataSet dsResult = new DataSet();
                sda = new SqlDataAdapter(cmd);

                sda.Fill(dsResult, "TDATA");
                if (dsResult.Tables["TDATA"].Rows.Count > 0)
                {

                    if (dsResult.Tables["TDATA"].Columns.Contains("errmsg"))
                    {
                        cErr = Convert.ToString(dsResult.Tables["TDATA"].Rows[0]["ERRMSG"]);

                        if (!String.IsNullOrEmpty(cErr))
                        {
                            return cErr;
                        }
                        else
                        {

                            return "Data deleted successfully";
                        }
                    }

                }
                else
                {
                    return "Record Not deleted";
                }

                return "Data deleted successfully";


            }
            catch (Exception ex)
            {
                cMessage = ex.Message.ToString();
            }

            return cMessage;


        }

        public String saveRegDetails(String connStr, Object body, int nUpdatemode, String cClientId, string cUserCode)
        {
            SqlConnection con = new SqlConnection(connStr);

            try
            {
                dynamic result = new ExpandoObject();

                String cErr = "", cmdStr = "";
                Boolean dataFound, bGenerateRegStr;

                SqlCommand cmd = new SqlCommand();
                SqlDataAdapter sda = new SqlDataAdapter();
                DataSet dset = new DataSet();
                DataSet dset1 = new DataSet();


                string serializedObject = Newtonsoft.Json.JsonConvert.SerializeObject(body, Newtonsoft.Json.Formatting.Indented);

                regDetails newRegn = Newtonsoft.Json.JsonConvert.DeserializeObject<regDetails>(serializedObject);

                List<regDetails> regDet = new List<regDetails> { newRegn };

                DataTable tRegDetails = globalMethods.CreateDataTablewithNull<regDetails>(regDet);

                List<Contact> contactsList = new List<Contact>();
                //DataColumnCollection columnsSet = table.Columns;        

                contactsList = regDet[0].regContacts;
                DataTable tContacts = new DataTable();
                if (contactsList is object)
                    tContacts = globalMethods.CreateDataTable<Contact>(contactsList);

                List<clientModules> modulesList = new List<clientModules>();
                modulesList = regDet[0].regModules;
                DataTable tModules = new DataTable();
                if (modulesList is object)
                    tModules = globalMethods.CreateDataTable<clientModules>(modulesList);

                List<clientLocations> locationsList = new List<clientLocations>();
                DataTable tLocations = new DataTable();
                locationsList = regDet[0].regLocations;
                if (locationsList is object)
                    tLocations = globalMethods.CreateDataTable<clientLocations>(locationsList);

                cmdStr = $"Select top 1 client_id from rw_regn_mst where client_id='{cClientId}'";

                cmd = new SqlCommand(cmdStr, con);
                sda = new SqlDataAdapter(cmd);
                DataTable dtExists = new DataTable();

                sda.Fill(dtExists);


                if (dtExists.Rows.Count == 0)
                    return "Invalid Client Id passed";

                cmd = new SqlCommand("SELECT * FROM rw_mapping_cols where tablename='rw_regn_mst';" +
                    "SELECT * FROM rw_mapping_cols where tablename='rw_contact_Details';" +
                    "SELECT * FROM rw_mapping_cols where tablename='rw_client_regn_modules';" +
                    "SELECT * FROM rw_mapping_cols where tablename='rw_regn_loc_details'", con);
                sda = new SqlDataAdapter(cmd);
                sda.Fill(dset);

                DataTable dtregMstMappings = dset.Tables[0];
                DataTable dtContactsMappings = dset.Tables[1];
                DataTable dtModulesMappings = dset.Tables[2];
                DataTable dtLocMappings = dset.Tables[3];

                cmd = new SqlCommand("Declare @tblRegnMst tvRegnMst select * from @tblRegnMst;" +
                    "Declare @tblRegModules tvClientModules select * from @tblRegModules;" +
                    "Declare @tblRegLocs tvClientLocDetails select * from @tblRegLocs;" +
                    "Declare @tblContacts tvContactDetails select * from @tblContacts", con);
                sda = new SqlDataAdapter(cmd);
                sda.Fill(dset1);

                DataTable dtTvpRegMst = dset1.Tables[0];
                DataTable dtTvpRegModules = dset1.Tables[1];
                DataTable dtTvpRegLocs = dset1.Tables[2];
                DataTable dtTvpContacts = dset1.Tables[3];

                globalMethods.AddDataInUploadTablewithMapping(dtregMstMappings, tRegDetails, dtTvpRegMst, ref cErr);
                if (cErr == "" && contactsList is object)
                    globalMethods.AddDataInUploadTablewithMapping(dtContactsMappings, tContacts, dtTvpContacts, ref cErr);

                if (cErr == "" && modulesList is object)
                    globalMethods.AddDataInUploadTablewithMapping(dtModulesMappings, tModules, dtTvpRegModules, ref cErr);

                if (cErr == "" && locationsList is object)
                    globalMethods.AddDataInUploadTablewithMapping(dtLocMappings, tLocations, dtTvpRegLocs, ref cErr);

                if (cErr != "")
                    return cErr;



                cmd = new SqlCommand("Declare @tblEditCols as tv_EditCols Select * from @tblEditCols", con);

                sda = new SqlDataAdapter(cmd);
                sda.Fill(dset, "tEditCols");

                Boolean bRegDetailsModified = false;

                cErr = MarkLocsforEncryption(con, cClientId, modulesList, dset.Tables["tEditCols"], locationsList,ref dtTvpRegMst, ref dtTvpRegLocs,ref bRegDetailsModified);

                if (!String.IsNullOrEmpty(cErr))
                {
                    return cErr;
                }
                

                globalMethods.AddDataForEditCols(dtTvpRegMst, dset.Tables["tEditCols"], "rw_regn_mst", ref cErr);
                if (cErr == "" && contactsList is object)
                    globalMethods.AddDataForEditCols(dtTvpContacts, dset.Tables["tEditCols"], "rw_contact_details", ref cErr);
                if (cErr == "" && modulesList is object)
                    globalMethods.AddDataForEditCols(dtTvpRegModules, dset.Tables["tEditCols"], "rw_client_regn_modules", ref cErr);
                if (cErr == "" && locationsList is object)
                    globalMethods.AddDataForEditCols(dtTvpRegLocs, dset.Tables["tEditCols"], "rw_regn_loc_details", ref cErr);


                if (cErr != "")
                    return cErr;

                String cDeptIdStr = "";


                cErr = "";

                
                cmd = new SqlCommand("SAVETRAN_RW_RegDetails", con);
                cmd.CommandType = CommandType.StoredProcedure;

                
                cmd.Parameters.Clear();
                cmd.Parameters.AddWithValue("@cClientId", cClientId);
                cmd.Parameters.AddWithValue("@nUpdatemode", nUpdatemode);
                cmd.Parameters.AddWithValue("@cUserCode", cUserCode);
                cmd.Parameters.AddWithValue("@tblRegnMst", dtTvpRegMst);

                if (modulesList is object)
                    cmd.Parameters.AddWithValue("@tblRegModules", dtTvpRegModules);

                if (locationsList is object)
                    cmd.Parameters.AddWithValue("@tblRegLocs", dtTvpRegLocs);

                if (contactsList is object)
                    cmd.Parameters.AddWithValue("@tblContacts", dtTvpContacts);

                cmd.Parameters.AddWithValue("@tblEditCols", dset.Tables["tEditCols"]);

                
                sda = new SqlDataAdapter(cmd);

                sda.Fill(dset, "TDATA");
                if (dset.Tables["TDATA"].Rows.Count > 0)
                {

                    if (dset.Tables["TDATA"].Columns.Contains("errmsg"))
                    {
                        cErr = Convert.ToString(dset.Tables["TDATA"].Rows[0]["ERRMSG"]);

                        if (!String.IsNullOrEmpty(cErr))
                        {
                            return cErr;
                        }
                        else
                        {
                            //if (bRegDetailsModified)
                            //{
                            //    DateTime dRegModifiedOn = Convert.ToDateTime(dtTvpRegMst.Rows[0]["regLastModifiedon"]);

                            //    cErr = EncryptRegString(dRegModifiedOn, con);

                            //    if (!String.IsNullOrEmpty(cErr))
                            //        return cErr;
                            //}

                            return "Data saved successfully";
                        }
                    }
                    
                }
                else
                {
                    return "Record Not Updated";
                }

                return "Data updated successfully";

            }

            catch (Exception ex)
            {
              
                return ex.Message.ToString();
            }

        }

        //public string EncryptRegString(DateTime dRegModifiedOn,SqlConnection con)
        //{
        //    dynamic result = new ExpandoObject();
        //    String cErr="";
        //    SqlDataAdapter sda;
        //    String cClientId="";

        //    WizEncrypt.WizEncrypt W = new WizEncrypt.WizEncrypt();
        //    try
        //    {

        //        String cSsplRegKey, cSsplRegKeyDet;
        //        SqlCommand cmd = new SqlCommand();




        //        String cModulesStr = "";
        //        String cExpr; 

        //        DataSet dsRegData = new DataSet();

        //        cExpr = $" SELECT distinct a.client_id FROM rw_regn_mst a (NOLOCK) JOIN rw_regn_loc_details b (NOLOCK) ON a.client_id=b.client_id" +
        //            $" WHERE ISNULL(b.regdetailsmodified,0)=1 AND format(a.regLastModifiedon,'MM/dd/yyyy hh:mm:ss')='" +dRegModifiedOn.ToString("MM/dd/yyyy hh:mm:ss")+"'";
        //        cmd = new SqlCommand(cExpr, con);

        //        sda = new SqlDataAdapter(cmd);

        //        DataTable tRegClients = new DataTable();

        //        sda.Fill(tRegClients);

        //        for (int nCnt = 0; nCnt < tRegClients.Rows.Count ; nCnt++)
        //        {
        //            cClientId = tRegClients.Rows[nCnt]["client_id"].ToString();

        //            cExpr = $"SELECT a.client_id,b.location_id locId,a.ho_loc_id hoLocId,productName,'BASIC' versionName," +
        //                $"format(amc_valid_till,'yyyyddMM') amcValidTill," +
        //            $" format(regn_valid_till,'yyyyddMM') regnValidTill,b.no_users,isnull(b.registered,0) registered from rw_regn_mst a" +
        //            $" JOIN rw_products c (NOLOCK) ON c.productid=a.productId" +
        //            $" JOIN rw_regn_loc_details b ON a.client_id=b.client_id WHERE ISNULL(b.regdetailsmodified,0)=1;" +
        //            $" SELECT moduleName FROM rw_client_regn_modules a (NOLOCK)" +
        //            $" join rw_modules b on a.moduleId=b.moduleId WHERE client_id='{cClientId}'";


        //            cmd = new SqlCommand(cExpr, con);
        //            sda = new SqlDataAdapter(cmd);
        //            sda.Fill(dsRegData);


        //            for (int i = 0; i < dsRegData.Tables[1].Rows.Count; i++)
        //            {
        //                cModulesStr = cModulesStr + "[" + dsRegData.Tables[1].Rows[i]["moduleName"] + "]";
        //            }

        //            String cLocId = "";

        //            int nRowsUpdated = 0;
        //            for (int i = 0; i < dsRegData.Tables[0].Rows.Count; i++)
        //            {
        //                cLocId = dsRegData.Tables[0].Rows[i]["locId"].ToString();

        //                cSsplRegKey = "[SSPL][" + dsRegData.Tables[0].Rows[i]["hoLocId"].ToString().Trim().ToUpper() + "][" +
        //                    cLocId.Trim().ToUpper() + "][" +
        //                    (Convert.ToBoolean(dsRegData.Tables[0].Rows[i]["registered"]) == true ? "TRUE" : "FALSE") + "]";

        //                cSsplRegKey = W.EncryptStringChar(cSsplRegKey);

        //                String cProduct = "[WizApp3S]";
        //                String cVersion = "[" + Convert.ToString(dsRegData.Tables[0].Rows[i]["versionName"]).ToUpper().Trim() + "]";
        //                String cAmc = "[AMC][" + dsRegData.Tables[0].Rows[i]["amcValidTill"] + "]";
        //                String cValid = "[VALID][" + dsRegData.Tables[0].Rows[i]["regnValidTill"] + "]";
        //                String cUser = "[USER][" + dsRegData.Tables[0].Rows[i]["no_users"].ToString() + "]";


        //                String cNewKey = cProduct + cVersion + cAmc + cValid + cUser + cModulesStr;
        //                cSsplRegKeyDet = W.EncryptStringChar(cNewKey);

        //                cExpr = $"update rw_regn_loc_details SET ssplRegKey='{cSsplRegKey}',ssplRegKeyDet='{cSsplRegKeyDet}' " +
        //                    $"  where client_id='{cClientId}' AND location_id='{cLocId}'";
        //                cmd = new SqlCommand(cExpr, con);

        //                con.Open();
        //                nRowsUpdated =cmd.ExecuteNonQuery();
        //                con.Close();
        //            }

        //            cExpr = $"update a with (rowlock) SET regdetailsmodified=0 from rw_regn_loc_details a " +
        //                $" JOIN rw_regn_mst b (NOLOCK) ON a.client_id=b.client_id" +
        //                $" where a.client_id='{cClientId}' AND regdetailsmodified=1 AND format(b.regLastModifiedon, 'MM/dd/yyyy hh:mm:ss') = '" + dRegModifiedOn.ToString("MM/dd/yyyy hh:mm:ss") + "'";
        //            cmd = new SqlCommand(cExpr, con);

        //            con.Open();
        //            nRowsUpdated = cmd.ExecuteNonQuery();
        //            con.Close();

        //        }
        //    }

        //    catch (Exception ex)
        //    {
        //        cErr = ex.Message.ToString();
        //    }


        //    return cErr;

        //}

        public dynamic SaveDocsData(String connStr, String clientId, String cUserCode, Object body)
        {
            dynamic result = new ExpandoObject();

            String cErr = "", cImageId = "", cUploadedOn = "";

            try
            {

                SqlConnection con = new SqlConnection(connStr);
                SqlCommand cmd = new SqlCommand();
                SqlDataAdapter sda = new SqlDataAdapter();


                string serializedObject = Newtonsoft.Json.JsonConvert.SerializeObject(body, Newtonsoft.Json.Formatting.Indented);

                DataSet Dd = (DataSet)Newtonsoft.Json.JsonConvert.DeserializeObject(serializedObject, (typeof(DataSet)));

                DataTable tRegDocs = Dd.Tables[0];

                if (String.IsNullOrEmpty(tRegDocs.Rows[0]["docImage"].ToString()))
                {
                    cErr = "Blank Image data not allowed..";
                    goto endProc;
                }

                DataTable dtExists = new DataTable();

                string cmdText;

                cmdText = $"Select top 1 client_id from rw_regn_mst (NOLOCK) WHERE client_id='{clientId}'";
                sda = new SqlDataAdapter(cmdText, con);
                sda.Fill(dtExists);

                if (dtExists.Rows.Count == 0)
                {
                    cErr = "Invalid Client Id provided";
                    goto endProc;
                }


                cmdText = $"Select top 1 userCode from rw_users (NOLOCK) WHERE userCode='{cUserCode}'";
                sda = new SqlDataAdapter(cmdText, con);
                sda.Fill(dtExists);

                if (dtExists.Rows.Count == 0)
                {
                    cErr = "Invalid User Id provided";
                    goto endProc;
                }

                DataTable dtMapTable = new DataTable();
                cmd = new SqlCommand("SELECT * FROM rw_mapping_cols where tablename = 'rw_regdocs'", con);
                sda = new SqlDataAdapter(cmd);
                sda.Fill(dtMapTable);

                DataTable dtTvpRegDocs = new DataTable();
                cmd = new SqlCommand("Declare @tblRegDocs as tvpRegDocs Select * from @tblRegDocs", con);
                sda = new SqlDataAdapter(cmd);
                sda.Fill(dtTvpRegDocs);



                globalMethods.AddDataInUploadTablewithMapping(dtMapTable, tRegDocs, dtTvpRegDocs, ref cErr);
                if (cErr != "")
                    goto endProc;

                dtTvpRegDocs.Rows[0]["uploadedbyUserCode"] = cUserCode;
                dtTvpRegDocs.Rows[0]["clientId"] = clientId;

                cErr = "";

                cmd = new SqlCommand("SAVETRAN_Rw_RegDocs", con);
                cmd.CommandType = CommandType.StoredProcedure;



                cmd.Parameters.Clear();
                cmd.Parameters.AddWithValue("@tblRegDocs", dtTvpRegDocs);

                DataSet dset = new DataSet();
                sda = new SqlDataAdapter(cmd);

                sda.Fill(dset, "TDATA");
                if (dset.Tables["TDATA"].Rows.Count > 0)
                {

                    if (dset.Tables["TDATA"].Columns.Contains("ERRMSG"))
                    {
                        cErr = Convert.ToString(dset.Tables["TDATA"].Rows[0]["ERRMSG"]);
                        cImageId = dset.Tables["TDATA"].Rows[0]["imageId"].ToString();
                        cUploadedOn = dset.Tables["TDATA"].Rows[0]["uploadDate"].ToString();
                    }

                }
                else
                {
                    return "Docs attachment Not Updated";
                }


            }

            catch (Exception ex)
            {
                cErr = ex.Message.ToString();
            }

        endProc:

            result.Message = cErr;
            result.imageId = cImageId;
            result.uploadDate = cUploadedOn;
            return result;

        }


        public String MarkLocsforEncryption(SqlConnection con, String cClientId, object modulesList, DataTable tEditCols, object locationsList,ref DataTable tRegnMst, 
            ref DataTable tLocations,ref Boolean bRegDetailsModified)
        {
            String cmdStr, cDeptIdStr = "";
            SqlCommand cmd;
            SqlDataAdapter sda;
            bRegDetailsModified = false; 

            try
            {
                
                if (modulesList is object)
                    bRegDetailsModified = true;

                if (!bRegDetailsModified)
                {
                    for (int nEditCols = 0; nEditCols < tEditCols.Rows.Count - 1; nEditCols++)
                    {
                        if (tEditCols.Rows[nEditCols]["columnName"].ToString().ToUpper() == "REGN_VALID_TILL" ||
                            tEditCols.Rows[nEditCols]["columnName"].ToString().ToUpper() == "AMC_VALID_TILL")
                        {
                            bRegDetailsModified = true;
                            break;
                        }
                    }
                }


                if (bRegDetailsModified)
                {
                    cmdStr = $"declare @cLocStr varchar(1000) select @cLocStr=coalesce(@cLocStr+',','')+''''+location_id+'''' FROM rw_regn_loc_details (NOLOCK) " +
                             $" WHERE client_id='{cClientId}' SELECT @cLocStr deptIdStr";
                    DataTable tLocStr = new DataTable();

                    cmd = new SqlCommand(cmdStr, con);
                    sda = new SqlDataAdapter(cmd);
                    sda.Fill(tLocStr);

                    if (tLocStr.Rows.Count > 0)
                        cDeptIdStr = tLocStr.Rows[0]["deptIdStr"].ToString();

                }
                else if (locationsList is object)
                {
                    bRegDetailsModified = true;
                    for (int i = 0; i < tLocations.Rows.Count; i++)
                    {
                        if (tLocations.Rows[i]["registered"] != null || tLocations.Rows[i]["no_users"] != null)
                            cDeptIdStr = cDeptIdStr + (cDeptIdStr == "" ? "" : ",") + "'"+tLocations.Rows[i]["location_id"].ToString()+"'";
                    }
                }


                if (bRegDetailsModified)
                {
                    string find = "location_id in(" + cDeptIdStr + ")";
                    //find out id  
                    //DataRow[] resultupdate = tLocations.Select(find);
                    //update row  

                    tLocations.Select(find).ToList<DataRow>().ForEach(r => r["regdetailsmodified"] = 1);

                    //resultupdate[0]["regdetailsmodified"] = 1;
                    //Accept Changes  
                    tLocations.AcceptChanges();

                    tRegnMst.Rows[0]["regLastModifiedon"] = DateTime.Now;   
                }
            }

            catch (Exception ex)
            {
                return ex.Message.ToString();
            }

            return "";
        }
    }
}