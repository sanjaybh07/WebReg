using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Dynamic;
using System.Linq;
using System.Web;
using WebRegApiCore.App_methods;

namespace WebRegApiCore.App_methods
{
    public class productMethods
    {

        commonMethods globalMethods = new commonMethods();
        public String SaveProducts(String connStr, Object body, int nUpdatemode)
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

                if (!Dd.Tables.Contains("Products"))
                    return "Invalid Json String ";

                DataTable tRegProducts = Dd.Tables[0];

                if (String.IsNullOrEmpty(tRegProducts.Rows[0]["productName"].ToString()))
                    return "Blank Product name not allowed..";

                String cmdText;

                String cProductId = tRegProducts.Rows[0]["productId"].ToString();
                if (cProductId == null)
                    cProductId = "";

                String cProductName = tRegProducts.Rows[0]["productName"].ToString();
                cmdText = $"Select top 1 productName from rw_products (NOLOCK) WHERE productname='{cProductName}' and productId<>'{cProductId}'";
                DataTable dtProdExists = new DataTable();
                cmd = new SqlCommand(cmdText, con);

                sda = new SqlDataAdapter(cmd);
                sda.Fill(dtProdExists);

                if (dtProdExists.Rows.Count > 0)
                    return "Product name already exists..";

                if (nUpdatemode == 2)
                {
                    DataRow dr = tRegProducts.Rows[0];
                    if (String.IsNullOrEmpty(cProductId))
                    {
                        return "Blank ProductId not allowed..";
                    }
                    else
                    {
                        cmdText = $"Select top 1 productId from rw_products (nolock) where productId='{cProductId}'";

                        DataTable dtExists = new DataTable();

                        sda = new SqlDataAdapter(cmdText, con);
                        sda.Fill(dtExists);

                        if (dtExists.Rows.Count == 0)
                            return "Invalid Prodct Id parameter";

                    }
                }


                cmd = new SqlCommand("Declare @tblProducts as tvRegProducts Select * from @tblProducts", con);
                sda = new SqlDataAdapter(cmd);
                sda.Fill(dset, "tRegProducts");



                globalMethods.AddUploadTableData(tRegProducts, dset.Tables["tRegProducts"], ref cErr);
                if (cErr != "")
                    return cErr;

                if (nUpdatemode == 2)
                {
                    cmd = new SqlCommand("Declare @tblEditCols as tv_EditCols Select * from @tblEditCols", con);

                    sda = new SqlDataAdapter(cmd);
                    sda.Fill(dset, "tEditCols");

                    globalMethods.AddDataForEditCols(tRegProducts, dset.Tables["tEditCols"], "rw_products", ref cErr);

                    if (cErr != "")
                        return cErr;

                }

                cErr = "";

                cmd = new SqlCommand("SAVETRAN_RW_PRODUCTS", con);
                cmd.CommandType = CommandType.StoredProcedure;



                cmd.Parameters.Clear();
                cmd.Parameters.AddWithValue("@nUpdatemode", nUpdatemode);
                cmd.Parameters.AddWithValue("@tblProducts", dset.Tables["tRegProducts"]);

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
                    return "Products not Updated";
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