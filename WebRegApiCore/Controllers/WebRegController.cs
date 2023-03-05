using Microsoft.AspNetCore.Mvc;
using WebRegApiCore.App_methods;
using WebRegApiCore.Models;
using System.Data;
using System.Data.SqlClient;
using System.Dynamic;
using Microsoft.AspNetCore.Authorization;
using TasksApi.Helpers;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using Microsoft.AspNetCore.Diagnostics;

namespace WebRegApiCore.Controllers
{
    [Authorize]
    [ApiController]
    //[Route("api/[controller]")]
    [Route("[controller]")]
    //[Route("[action]")]
    public class WebRegController : ControllerBase
    {

        IConfiguration _config;
        String connStr;
        SqlConnection conn;

        public WebRegController(IConfiguration configuration)
        {
            _config = configuration;

            connStr = configuration["ConnectionStrings:CON_REG"];
            conn = new SqlConnection(connStr);
        }

        commonMethods globalMethods = new commonMethods();

        [HttpGet(Name = "test")]
        public string Index()
        {
            return "sucess!";
        }


        [HttpGet]
        [Route("~/getAccessToken")]
        public IActionResult reIssueAccessToken()
        {
            String cErr = "";
            String cConStr = GetConnection(ref cErr);

            if (string.IsNullOrEmpty(cConStr))
                return BadRequest(new {Message=cErr});

            dynamic result = new ExpandoObject();
            dynamic retResult = new ExpandoObject();

            result.Message = "";

            tokenHelper helperMethod = new tokenHelper();

            retResult = helperMethod.validateRefreshToken(cConStr);
            if (string.IsNullOrEmpty(retResult.Message))
            {
                User _userdata;

                _userdata= new User();
                _userdata.userCode= AppConfigModel.userId;
                _userdata.roleCode=AppConfigModel.roleCode;

                var accessTokenData = tokenHelper.GenerateToken(_config, _userdata, 1);

                result.accessToken = new JwtSecurityTokenHandler().WriteToken(accessTokenData);
            }
            else
            {
                result.Message = retResult.Message;
                result.tokenExpired= retResult.tokenExpired;
            }
                
            return Ok(result);

        }

        
        [HttpPatch]
        [Route("~/clientDetails")]

        public IActionResult updateRegDetails(String clientId, String cUserCode, [FromBody] regDetails body)
        {
            String cErr = "";
            String cConStr = GetConnection(ref cErr);

            if (string.IsNullOrEmpty(cConStr))
                return BadRequest(new {Message=cErr});


            dynamic result = new ExpandoObject();

            regMethods regSaveMethod = new regMethods();

            String cMessage;
            cMessage = regSaveMethod.saveRegDetails(cConStr, body, 2, clientId, cUserCode);

            result.Message = cMessage;
            return Ok(result);

        }

        [HttpGet]
        [Route("~/getRegDocs")]
        public IActionResult getSingleClientDocs(String clientId)
        {
            dynamic result = new ExpandoObject();
            dynamic retResult = new ExpandoObject();

            result.Message = "";
            String cErr = "";

            String cGetConStr = GetConnection(ref cErr);

            if (string.IsNullOrEmpty(cGetConStr) || !String.IsNullOrEmpty(cErr))
                return BadRequest(new {Message=cErr});

            regMethods regMethod = new regMethods();

            result = regMethod.GetMemoDocsData(cGetConStr, clientId);

            if (!String.IsNullOrEmpty(result.Message))
                return Ok(result);

            retResult.data = result.data;

            return Ok(retResult);
        }

        /// <summary>
        /// Get Client details
        /// </summary>        
        /// <remarks>Get a Single Client data with List of Locations</remarks>

        [HttpGet]
        [Route("~/clientDetails")]

        public IActionResult GetClientDetails(String clientId)
        {
            dynamic result = new ExpandoObject();
            String cErr = "";
            String cGetConStr = GetConnection(ref cErr);

            if (string.IsNullOrEmpty(cGetConStr) || !String.IsNullOrEmpty(cErr))
                return BadRequest(new {Message=cErr});

            regMethods myRegMethod = new regMethods();

            result = myRegMethod.GetClientRegDetails(cGetConStr, clientId);

            return Ok(result);
        }


        /// <summary>
        /// Get User List 
        /// </summary>        
        /// <remarks>Get user List</remarks>


        private void AddRecordInUploadTable(DataTable dtSourceTable, DataTable dtTargetTable, ref String cError)
        {
            try
            {

                dtTargetTable.Rows.Clear();
                foreach (DataRow dr in dtSourceTable.Rows)
                {
                    if (dr.RowState != DataRowState.Deleted && dr.RowState != DataRowState.Detached)
                    {

                        DataRow drNew = dtTargetTable.NewRow();
                        foreach (DataColumn dcol in dtSourceTable.Columns)
                        {

                            drNew[dcol.ColumnName] = dr[dcol.ColumnName];

                        }

                        dtTargetTable.Rows.Add(drNew);
                    }
                }
            }
            catch (Exception ex)
            {
                cError = ex.Message;
            }
        }

        private void AddRecordForEditCols(DataTable dtSourceTable, DataTable dtTargetTable, String cSource, ref String cError)
        {
            try
            {
                int iRowCount = dtSourceTable.Rows.Count;
                int iNullRowCount = 0;

                foreach (DataColumn dcol in dtSourceTable.Columns)
                {
                    iNullRowCount = dtSourceTable.Select(dcol.ColumnName + " IS NULL").Length;

                    if (iNullRowCount != iRowCount)
                    {
                        DataRow drNew = dtTargetTable.NewRow();

                        String cOrgCol = dcol.ColumnName.Trim();
                        drNew["TableName"] = cSource;
                        drNew["columnName"] = cOrgCol;
                        dtTargetTable.Rows.Add(drNew);
                    }
                }



            }
            catch (Exception ex)
            {
                cError = ex.Message;
            }
        }





        /// <summary>
        /// Update  User  
        /// </summary>   
        /// <param name="userId">  user Id To Update user  </param>  
        /// <remarks>Update User </remarks>

        /// <summary>
        /// Get all Menu options accessible by Logged in User 
        /// </summary>
        /// <param name="userId"> Login User Id  </param>   
        /// <remarks>API to display all the Software products available</remarks>
        [HttpGet]
        [Route("~/regMenu")]
        public IActionResult regMenu(String userId)
        {
            try
            {
                String cErr = "";
                String cConStr = GetConnection(ref cErr);

                if (string.IsNullOrEmpty(cConStr))
                    return BadRequest(new {Message=cErr});


                dynamic result = new ExpandoObject();
                String cExpr = "";


                cExpr = $"Select menuId, menuTitle from rw_menu_items a JOIN rw_user_auth b ON a.menuid = b.appmodulename \n" +
                         "JOIN rw_users c ON c.rolecode = b.rolecode \n" +
                         "WHERE c.usercode = '" + userId + "' AND authflag = 1";

                SqlConnection con = new SqlConnection(cConStr);
                SqlCommand cmd = new SqlCommand(cExpr, con);
                SqlDataAdapter sda = new SqlDataAdapter(cmd);
                DataSet dset = new DataSet();
                sda.Fill(dset, "TDATA");

                result.Menu = dset.Tables["TDATA"];


                return Ok(result);
            }
            catch (Exception ex)
            {
                return this.BadRequest(ex.Message.ToString());
            }
        }


        /// <summary>
        /// Get List of all Products offered by system
        /// </summary>        
        /// <remarks>API to display all the Software products available</remarks>
        [HttpGet]
        [Route("~/products")]
        public IActionResult products()
        {
            try
            {
                String cErr = "";
                String cConStr = GetConnection(ref cErr);

                if (string.IsNullOrEmpty(cConStr))
                    return BadRequest(new {Message=cErr});


                dynamic result = new ExpandoObject();
                String cExpr = "";


                cExpr = $"Select productId,productName FROM  rw_products";

                SqlConnection con = new SqlConnection(cConStr);
                SqlCommand cmd = new SqlCommand(cExpr, con);
                SqlDataAdapter sda = new SqlDataAdapter(cmd);
                DataSet dset = new DataSet();
                sda.Fill(dset, "TDATA");

                result.Menu = dset.Tables["TDATA"];


                return Ok(result);
            }
            catch (Exception ex)
            {
                return this.BadRequest(ex.Message.ToString());
            }
        }

        [HttpPost]
        [Route("~/registerPos")]

        public IActionResult RegisterPos(String cUserCode, [FromBody] objPosReg body)
        {
            String cErr = "";
            String cConStr = GetConnection(ref cErr);

            if (string.IsNullOrEmpty(cConStr))
                return BadRequest(new {Message=cErr});

            dynamic result = new ExpandoObject();

            regMethods regMethod = new regMethods();

            result = regMethod.RegisterMultiPos(cConStr, body, cUserCode);

            if (String.IsNullOrEmpty(result.Message))
                result.Message = "Location(s) Registered successfully";

            return Ok(result);
        }

        [HttpPost]
        [Route("~/attachDocs")]
        public IActionResult SaveRegDocs(String clientId, String cUserCode, [FromBody] objDocs body)
        {
            String cErr = "";
            String cConStr = GetConnection(ref cErr);



            dynamic result = new ExpandoObject();

            if (string.IsNullOrEmpty(cConStr))
            {
                result.Message = cErr;
                return Ok(result);
            }

            regMethods regMethod = new regMethods();

            result = regMethod.SaveDocsData(cConStr, clientId, cUserCode, body);

            return Ok(result);

        }

        [HttpPatch]
        [Route("~/removeDocs")]
        [Produces("application/json")]
        public IActionResult removeRegDocs(String clientId, String cImageId)
        {
            String cErr = "";
            String cConStr = GetConnection(ref cErr);
            String cMessage = "";

            if (string.IsNullOrEmpty(cConStr))
                return BadRequest(new {Message=cErr});

            regMethods regMethod = new regMethods();

            cMessage = regMethod.deleteDocsData(cConStr, clientId, cImageId);

            if (!String.IsNullOrEmpty(cMessage))
                return BadRequest(new {Message=cMessage});

            return Ok(new { Message = "Document detached successfully" });

        }


        [HttpPatch]
        [Route("~/removeClient")]
        public IActionResult removeClient(String clientId)
        {
            String cErr = "";
            String cConStr = GetConnection(ref cErr);
            String cMessage = "";

            if (string.IsNullOrEmpty(cConStr))
                return BadRequest(new { Message = cErr });

            regMethods regMethod = new regMethods();

            cMessage = regMethod.deleteClientData(cConStr, clientId);

            if (!String.IsNullOrEmpty(cMessage))
                return BadRequest(new { Message = cMessage });

            return  Ok(new { Message = "Client Data deleted successfully" });

        }

        [HttpPost]
        [Route("~/Products")]

        public IActionResult SaveProducts([FromBody] objProducts body)
        {
            String cErr = "";
            String cConStr = GetConnection(ref cErr);

            if (string.IsNullOrEmpty(cConStr))
                return BadRequest(new {Message=cErr});

            dynamic result = new ExpandoObject();

            productMethods prdMethod = new productMethods();

            cErr = prdMethod.SaveProducts(cConStr, body, 1);

            if (!String.IsNullOrEmpty(cErr))
                return BadRequest(new {Message=cErr});

            result.Message = "New Product saved successfully";

            return Ok(result);
        }

        [HttpPatch]
        [Route("~/Products")]
        public IActionResult UpdateProducts([FromBody] objProducts body)
        {
            String cErr = "";
            String cConStr = GetConnection(ref cErr);

            if (string.IsNullOrEmpty(cConStr))
                return BadRequest(new {Message=cErr});

            dynamic result = new ExpandoObject();

            productMethods prdMethod = new productMethods();

            cErr = prdMethod.SaveProducts(cConStr, body, 2);

            if (!String.IsNullOrEmpty(cErr))
                return BadRequest(new {Message=cErr});

            result.Message = "Product updated successfully";

            return Ok(result);
        }

        [HttpPost]
        [Route("~/Users")]
        public IActionResult SaveUsers([FromBody] User Body)
        {
            String cErr = "";
            String cConStr = GetConnection(ref cErr);

            if (string.IsNullOrEmpty(cConStr))
                return BadRequest(new {Message=cErr});

            userMethods regUserMethod = new userMethods();

            dynamic result = new ExpandoObject();

            cErr = regUserMethod.SaveUser(cConStr, Body, 1);

            if (!String.IsNullOrEmpty(cErr))
                return BadRequest(new {Message=cErr});

            result.Message = "Data saved successfully";

            return Ok(result);


        }

        [HttpPatch]
        [Route("~/users")]
        public IActionResult updateUser(String userId, [FromBody] User Body)
        {
            String cErr = "";
            String cConStr = GetConnection(ref cErr);

            if (string.IsNullOrEmpty(cConStr))
                return BadRequest(new {Message=cErr});

            userMethods regUserMethod = new userMethods();

            dynamic result = new ExpandoObject();

            cErr = regUserMethod.SaveUser(cConStr, Body, 2, userId);

            if (!String.IsNullOrEmpty(cErr))
                return BadRequest(new {Message=cErr});

            result.Message = "Data modified successfully";

            return Ok(result);


        }



        /// <summary>
        ///Get List of Modules Product wise offered by system
        /// </summary>        
        /// <remarks>Get List of Modules Product wise offered by system</remarks>

        [HttpGet]
        [Route("~/regModules")]

        public IActionResult getModules(string productId = null)
        {
            dynamic result = new ExpandoObject();
            regModulesmethods modulesmethods = new regModulesmethods();

            String cErr = "";
            String cConStr = GetConnection(ref cErr);

            if (string.IsNullOrEmpty(cConStr))
                return BadRequest(new {Message=cErr});

            result = modulesmethods.GetModulesList(cConStr, productId);

            return Ok(result);
        }

        /// <summary>
        /// Insert the Registration modules 
        /// </summary>         
        /// <remarks>Insert the Registration modules </remarks>
        [HttpPost]
        [Route("~/regModules")]
        public IActionResult regModules([FromBody] objRegM Body)
        {
            String cErr = "";
            String cConStr = GetConnection(ref cErr);

            if (string.IsNullOrEmpty(cConStr))
                return BadRequest(new { Message = cErr });

            regModulesmethods modulesMethod = new regModulesmethods();

            dynamic result = new ExpandoObject();

            cErr = modulesMethod.SaveRegmodules(cConStr, Body, 1);

            if (!String.IsNullOrEmpty(cErr))
                return BadRequest(new { Message = cErr });

            result.Message = "Data saved successfully";

            return Ok(result);


        }


        /// <summary>
        /// Update the Registration modules 
        /// </summary>     
        /// <param name="moduleId"> Module Id to Update  </param>   
        /// <remarks>User can rename the Modules details e.g. Name,Alias and Active status etc. </remarks>
        [HttpPatch]
        [Route("~/regModules")]
        public IActionResult regModules(string productId, [FromBody] objRegM Body)
        {
            try
            {

                String cErr = "";
                String cConStr = GetConnection(ref cErr);

                if (string.IsNullOrEmpty(cConStr))
                    return BadRequest(new {Message=cErr});

                regModulesmethods modulesMethod = new regModulesmethods();

                dynamic result = new ExpandoObject();

                cErr = modulesMethod.SaveRegmodules(cConStr, Body, 2, productId);

                if (!String.IsNullOrEmpty(cErr))
                    return BadRequest(new {Message=cErr});

                result.Message = "Data modified successfully";

                return Ok(result);
            }
            catch (Exception ex)
            {
                return this.BadRequest(ex.Message.ToString());
            }
        }

        [HttpPost]
        [Route("~/ContactDetails")]
        public IActionResult SaveContacts([FromBody] Contact Body)
        {
            String cErr = "";
            String cConStr = GetConnection(ref cErr);

            if (string.IsNullOrEmpty(cConStr))
                return BadRequest(new {Message=cErr});

            regModulesmethods contactMethod = new regModulesmethods();

            dynamic result = new ExpandoObject();

            result = contactMethod.SaveContacts(cConStr, Body, 1);

            if (!String.IsNullOrEmpty(result.Message))
                return BadRequest(new {Message= result.Message});

            result.Message = "Contacts data saved successfully";

            return Ok(result);


        }

        /// <summary>
        ///Get List of all Ledger accounts under Sundry Debtors
        /// </summary>        
        /// <remarks>Get List of all Ledger accounts under Sundry Debtors</remarks>
        [HttpGet]
        [Route("~/listOfDebtors")]
        public IActionResult listOfDebtors()
        {
            try
            {
                String cErr = "";
                String cConStr = GetConnection(ref cErr);

                if (string.IsNullOrEmpty(cConStr))
                    return BadRequest(new {Message=cErr});


                dynamic result = new ExpandoObject();
                String cExpr = "";

                cExpr = "declare @cDebtorheads varchar(max) \n" +
                        "select @cDebtorheads = dbo.fn_act_travtree('0000000018') \n" +
                        "select ac_name as acName, a.ac_code as acCode from  lm01106 a \n" +
                        "join hd01106 b on a.HEAD_CODE = b.HEAD_CODE \n" +
                        "JOIN lmp01106 lmp on lmp.ac_code = a.ac_code \n" +
                        "JOIN area c on c.area_code = lmp.area_code \n" +
                        "JOIN city d ON d.city_code = c.city_code \n" +
                        "JOIN state e on e.state_code = d.state_code  \n" +
                        "where charindex(a.head_code, @cDebtorheads)> 0 and ac_name<>'' order by ac_name ";

                SqlConnection con = new SqlConnection(cConStr);
                SqlCommand cmd = new SqlCommand(cExpr, con);
                SqlDataAdapter sda = new SqlDataAdapter(cmd);
                DataSet dset = new DataSet();
                sda.Fill(dset, "TDATA");

                result.Debtors = dset.Tables["TDATA"];


                return Ok(result);
            }
            catch (Exception ex)
            {
                return this.BadRequest(ex.Message.ToString());
            }
        }


        /// <summary>
        ///Get List of Companies for viewing Ledger transactions in a Single Company
        /// </summary>        
        /// <remarks>Get List of Companies for viewing Ledger transactions in a Single Company</remarks>
        [HttpGet]
        [Route("~/listOfCompanies")]
        public IActionResult listOfCompanies()
        {
            try
            {
                String cErr = "";
                String cConStr = GetConnection(ref cErr);

                if (string.IsNullOrEmpty(cConStr))
                    return BadRequest(new {Message=cErr});


                dynamic result = new ExpandoObject();
                String cExpr = "";

                cExpr = $"Select pan_no as panNo,company_name as companyName from loc_accounting_company";

                SqlConnection con = new SqlConnection(cConStr);
                SqlCommand cmd = new SqlCommand(cExpr, con);
                SqlDataAdapter sda = new SqlDataAdapter(cmd);
                DataSet dset = new DataSet();
                sda.Fill(dset, "TDATA");

                result.Company = dset.Tables["TDATA"];


                return Ok(result);
            }
            catch (Exception ex)
            {
                return this.BadRequest(ex.Message.ToString());
            }
        }

        private void ChangeNull(DataTable Dt)
        {
            try
            {
                foreach (DataColumn Dc in Dt.Columns)
                {
                    foreach (DataRow Dr in Dt.Rows)
                    {
                        if (Dr.RowState != DataRowState.Deleted)
                        {
                            if (Convert.IsDBNull(Dr[Dc]) == true)
                            {
                                if (Dc.DataType.ToString().ToLower().Contains("string"))
                                    Dr[Dc] = "";
                                else if (Dc.DataType.ToString().ToLower().Contains("datetime"))
                                    Dr[Dc] = "01 jan 1990";
                                else if (Dc.DataType.ToString().ToLower().Contains("int"))
                                    Dr[Dc] = 0;
                                else if (Dc.DataType.ToString().ToLower().Contains("decimal"))
                                    Dr[Dc] = 0.0;
                                else if (Dc.DataType.ToString().ToLower().Contains("boolean"))
                                    Dr[Dc] = false;
                                else if (Dc.DataType.ToString().ToLower().Contains("byte"))
                                    Dr[Dc] = 0;
                                else
                                    Dr[Dc] = "";
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {

            }
        }

        private Int32 TableColumnWidth(String cCol, DataTable dt)
        {
            try
            {
                int maxLength = 0;
                int cellLength = 0;
                foreach (DataRow row in dt.Rows)
                {
                    if (row[cCol] != DBNull.Value && !string.IsNullOrEmpty(row[cCol].ToString()))
                    {
                        cellLength = row[cCol].ToString().Length; //   .Split('\n').Max(x => x.Length);
                        if (maxLength < cellLength)
                        {
                            maxLength = cellLength;
                        }
                    }
                }
                return maxLength;
            }
            catch (Exception)
            {

                return 25;
            }

        }


        /// <summary>
        ///Get transaction data of a single Ledger for a given Period
        /// </summary>      
        /// <param name="acCode"> Ledger a/c code against which Ledger is viewed  </param>
        /// <param name="FromDt"> Start Date from which transaction data is required [yyyy-MM-dd]  </param>
        /// <param name="ToDt"> End Date upto which transaction data is required [yyyy-MM-dd] </param>
        /// <param name="Mode"> Format of Ledger with 3 values [1.Detailed 2.Month wise 3.Daily]  </param>
        /// <param name="viewtype"> Group Ledger format with 2 values [1.Date wise 2.Sub Ledger wise]  </param>
        /// <param name="panNo"> Pan No. of Company against which Ledger transactions are done  </param>
        /// <remarks>Get transaction data of a single Ledger for a given Period</remarks>
        [HttpGet]
        [Route("~/viewLedger")]
        public IActionResult viewLedger(String FromDt, String ToDt, Int32 Mode, Int32 viewtype, string acCode, String panNo = null)
        {
            try
            {
                String cErr = "";
                String cConStr = GetConnection(ref cErr);

                if (string.IsNullOrEmpty(cConStr))
                    return BadRequest(new {Message=cErr});


                dynamic result = new ExpandoObject();

                DataTable Dt = new DataTable("tDataType");
                DataTable DtW = new DataTable("tDataWidth");

                DataTable Dt1 = new DataTable("tDataType1");
                DataTable DtW1 = new DataTable("tDataWidth1");

                String cExpr = "";

                if (String.IsNullOrEmpty(acCode))
                    acCode = "";

                if (String.IsNullOrEmpty(panNo))
                    panNo = "";



                cExpr = $"Exec SPwow_MULTILEDGER @dFromDt= '" + FromDt + "',@dToDt= '" + ToDt + "',@cAcCodePara='" + acCode + "',@nMode=" + Mode + ", @cPanno='" + panNo + "', @nViewType=" + viewtype + " ";

                SqlConnection con = new SqlConnection(cConStr);
                SqlCommand cmd = new SqlCommand(cExpr, con);
                SqlDataAdapter sda = new SqlDataAdapter(cmd);
                DataSet dset = new DataSet();
                sda.Fill(dset, "TDATA");

                if (dset.Tables.Contains("TDATA"))
                    dset.Tables["TDATA"].Columns.Remove("TableName");

                if (dset.Tables.Contains("TDATA1"))
                    dset.Tables["TDATA1"].Columns.Remove("TableName");


                //  ChangeNull(dset.Tables["TDATA"]);

                result.Master = dset.Tables["TDATA"];

                if (dset.Tables.Contains("TDATA1"))
                {
                    //    ChangeNull(dset.Tables["TDATA1"]);

                    result.Detail = dset.Tables["TDATA1"];
                }




                //DataType Master

                foreach (DataColumn dc in dset.Tables["TDATA"].Columns)
                {
                    Dt.Columns.Add(dc.ColumnName);
                }

                Dt.Rows.Add();

                foreach (DataColumn dc in dset.Tables["TDATA"].Columns)
                {
                    Dt.Rows[0][dc.ColumnName] = dc.DataType.Name;
                }

                result.MasterDataTypes = Dt;

                //Width

                foreach (DataColumn dc in dset.Tables["TDATA"].Columns)
                {
                    DtW.Columns.Add(dc.ColumnName, typeof(System.Int32));
                }

                DtW.Rows.Add();
                foreach (DataColumn dc in dset.Tables["TDATA"].Columns)
                {

                    if (dc.DataType.Name.ToUpper().Contains("DATE"))
                        DtW.Rows[0][dc.ColumnName] = 11;
                    else if (dc.DataType.Name.ToUpper().Contains("STRING"))
                        DtW.Rows[0][dc.ColumnName] = TableColumnWidth(dc.ColumnName, dset.Tables["TDATA"]);
                    else if (dc.DataType.Name.ToUpper().Contains("BYTE"))
                        DtW.Rows[0][dc.ColumnName] = dc.MaxLength;
                    else
                        DtW.Rows[0][dc.ColumnName] = 16;

                }

                result.MasterDataWidth = DtW;



                //DataType Detail

                foreach (DataColumn dc in dset.Tables["TDATA1"].Columns)
                {
                    Dt1.Columns.Add(dc.ColumnName);
                }

                Dt1.Rows.Add();

                foreach (DataColumn dc in dset.Tables["TDATA1"].Columns)
                {
                    Dt1.Rows[0][dc.ColumnName] = dc.DataType.Name;
                }

                result.DetailDataTypes = Dt1;

                //Width

                foreach (DataColumn dc in dset.Tables["TDATA1"].Columns)
                {
                    DtW1.Columns.Add(dc.ColumnName, typeof(System.Int32));
                }

                DtW1.Rows.Add();
                foreach (DataColumn dc in dset.Tables["TDATA1"].Columns)
                {

                    if (dc.DataType.Name.ToUpper().Contains("DATE"))
                        DtW1.Rows[0][dc.ColumnName] = 11;
                    else if (dc.DataType.Name.ToUpper().Contains("STRING"))
                        DtW1.Rows[0][dc.ColumnName] = TableColumnWidth(dc.ColumnName, dset.Tables["TDATA1"]);
                    else if (dc.DataType.Name.ToUpper().Contains("BYTE"))
                        DtW1.Rows[0][dc.ColumnName] = dc.MaxLength;
                    else
                        DtW1.Rows[0][dc.ColumnName] = 16;

                }

                result.DetailDataWidth = DtW1;




                return Ok(result);
            }
            catch (Exception ex)
            {
                return this.BadRequest(ex.Message.ToString());
            }
        }


        /// <summary>
        ///Get List of New Installations Registration status
        /// </summary>      
        /// <param name="regStatus"> Status of Registration (0-Pending,1-Registered)  </param>        
        /// <remarks>Get List of New Installations Registration status</remarks>
        [HttpGet]
        [Route("~/clientsList")]
        public IActionResult ClientsList(Int32 regStatus)
        {
            try
            {
                String cErr = "";
                String cConStr = GetConnection(ref cErr);
                ;
                if (string.IsNullOrEmpty(cConStr))
                    return BadRequest(new {Message=cErr});


                dynamic result = new ExpandoObject();
                String cExpr = "";

                if (regStatus == 1)
                {
                    cExpr = $"Select replace(CONVERT(VARCHAR, reg_submitted_on, 106),' ','-') [Date],client_id,client_name,city,state, email,mobile, \n" +
                            "b.productName,ho_loc_id from  rw_regn_mst a(NOLOCK) \n" +
                            "JOIN rw_products b ON a.productId = b.productId \n" +
                        "WHERE ISNULL(regn_valid_till,'')<> ''";
                }
                else
                {
                    cExpr = $"Select replace(CONVERT(VARCHAR, reg_submitted_on, 106),' ','-') [Date],client_id,client_name,city,state, email,mobile, \n" +
                            "b.productName,ho_loc_id from  rw_regn_mst a(NOLOCK) \n" +
                            "JOIN rw_products b ON a.productId = b.productId \n" +
                            "WHERE ISNULL(regn_valid_till,'') = '' ";
                }

                SqlConnection con = new SqlConnection(cConStr);
                SqlCommand cmd = new SqlCommand(cExpr, con);
                SqlDataAdapter sda = new SqlDataAdapter(cmd);
                DataSet dset = new DataSet();
                sda.Fill(dset, "TDATA");

                result.ClientsList = dset.Tables["TDATA"];


                return Ok(result);
            }
            catch (Exception ex)
            {
                return this.BadRequest(ex.Message.ToString());
            }
        }


        /// <summary>
        ///Get List of Registrations for Location data
        /// </summary>      
        /// <param name="regStatus"> Status of Registration (0-Pending,1-Registered)  </param>        
        /// <remarks>Get List of Registrations for Location data</remarks>
        [HttpGet]
        [Route("~/locsRegnsList")]
        public IActionResult locsRegnsList(Int32 regStatus)
        {
            try
            {
                String cErr = "";
                String cConStr = GetConnection(ref cErr);

                if (string.IsNullOrEmpty(cConStr))
                    return BadRequest(new {Message=cErr});


                dynamic result = new ExpandoObject();
                String cExpr = "";

                cExpr = $"select a.client_id clientId,client_name clientName,a.location_id locationId, a.location_name locationName,ISNULL(c.ac_name,'') ledgerName, \n" +
                         "b.city,b.state,replace(CONVERT(VARCHAR, a.min_xn_dt, 106),' ','-') [Min Xn Date]," +
                         "replace(CONVERT(VARCHAR, a.max_xn_dt, 106),' ','-') [Max Xn Date],a.no_users noOfUsers," +
                         " replace(CONVERT(VARCHAR, a.amc_start_date, 106),' ','-') [AMC start date],\n" +
                         "a.amc_rate[AMC amount], a.wizclip_charges[Wizclip Rate] from rw_regn_loc_details a \n" +
                         "JOIN rw_regn_mst b ON a.client_id = b.client_id \n" +
                         "LEFT JOIN lm01106 c ON c.ac_code = b.linked_ledger_ac_code where registered = " + regStatus + "";


                SqlConnection con = new SqlConnection(cConStr);
                SqlCommand cmd = new SqlCommand(cExpr, con);
                SqlDataAdapter sda = new SqlDataAdapter(cmd);
                DataSet dset = new DataSet();
                sda.Fill(dset, "TDATA");

                result.LocsRegData = dset.Tables["TDATA"];


                return Ok(result);
            }
            catch (Exception ex)
            {
                return this.BadRequest(ex.Message.ToString());
            }
        }






        private object GenArray(DataTable Dt)
        {

            object[,] Tablero = new object[Dt.Rows.Count + 1, Dt.Columns.Count];

            int iR = 0;
            Int32 iC = 0;

            foreach (DataColumn dc in Dt.Columns)
            {
                Tablero[iR, iC] = dc.ColumnName;
                iC = iC + 1;
            }

            iR = iR + 1;
            iC = 0;

            foreach (DataRow D in Dt.Rows)
            {
                foreach (DataColumn dc in Dt.Columns)
                {
                    if (dc.DataType.Name.ToUpper().Contains("DATE"))
                    {

                        if (dc.ColumnName.ToUpper().Contains("DATETIME"))
                            Tablero[iR, iC] = ConvertDateTime(D[dc.ColumnName]).ToString("dd-MMM-yyyy hh:mm tt");
                        else
                            Tablero[iR, iC] = ConvertDateTime(D[dc.ColumnName]).ToString("dd-MMM-yyyy");
                    }
                    else
                    {

                        Tablero[iR, iC] = D[dc.ColumnName];
                    }
                    iC = iC + 1;
                }
                iR = iR + 1;
                iC = 0;
            }
            return Tablero;

        }


        private DateTime ConvertDateTime(object val)
        {
            string dt = Convert.ToString(val);
            DateTime dtValue = new DateTime(1900, 1, 1);

            if (string.IsNullOrEmpty(dt) == false)
                DateTime.TryParse(dt, out dtValue);

            return dtValue;
        }

        private double ConvertDouble(object ob)
        {
            string cVal = Convert.ToString(ob);
            double nValue = 0;

            if (cVal.Length > 0)
                double.TryParse(cVal, out nValue);

            return nValue;
        }

        string Encrypt(string cString)
        {
            System.Text.StringBuilder cEncStr = new System.Text.StringBuilder();
            Int16 i;
            foreach (char c in cString)
            {
                i = (Int16)c;
                cEncStr.Append((char)(i > 250 ? 250 : 250 - i));
            }
            return cEncStr.ToString();
        }

        private string GetConnection(ref String cError)
        {
            SqlConnection sqlCon = new SqlConnection();

            try
            {

                String cConStr = connStr;

                if (cConStr.Trim() == "")
                    cConStr = AppConfigModel.DefaultConnectionString;

                if (string.IsNullOrEmpty(cConStr))
                {
                    cError = "Invalid Connection String";
                    return "";
                }

                sqlCon = new SqlConnection(cConStr);
                SqlCommand sqlCmd = new SqlCommand();

                sqlCon.Open();
                if (sqlCon.State != ConnectionState.Open)
                {
                    cError = "Unable To Connect Master database";
                    return "";
                }

                return cConStr;

            }
            catch (Exception ex)
            {
                cError = ex.Message;
                return "";
            }
            finally
            {
                sqlCon.Close();

            }
        }

        private void GetMapNAme(String OrgTable, ref DataTable DtMap)
        {
            try
            {
                DataTable Dt = new DataTable("TMAP");
                Dt.Columns.Add("UserDefine", typeof(System.String));
                Dt.Columns.Add("OrgColName", typeof(System.String));

                switch (OrgTable.ToUpper())
                {
                    case "CAMPAIGNTITLEMST":


                        Dt.Rows.Add();
                        Dt.Rows[Dt.Rows.Count - 1]["UserDefine"] = "campaignCode";
                        Dt.Rows[Dt.Rows.Count - 1]["OrgColName"] = "CampaignCode";


                        Dt.Rows.Add();
                        Dt.Rows[Dt.Rows.Count - 1]["UserDefine"] = "campaignTitle";
                        Dt.Rows[Dt.Rows.Count - 1]["OrgColName"] = "campaignTitle";

                        break;

                }

                DtMap = Dt;

            }
            catch (Exception)
            {

            }
        }

    }
}
