using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Reflection;
using System.Web;
using WebRegApiCore.Models;

namespace WebRegApiCore.App_methods
{
    public class commonMethods
    {

        public string GetSqlConnection(string connStr,ref String cError)
        {
            if (!String.IsNullOrEmpty(AppConfigModel.apiRejectedMsg))
            {
                cError = AppConfigModel.apiRejectedMsg;
                return cError;
            }
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

        public DataTable CreateDataTable<T>(List<T> items)
        {
            DataTable dataTable = new DataTable(typeof(T).Name);
            //Get all the properties
            PropertyInfo[] Props = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);
            foreach (PropertyInfo prop in Props)
            {
                //Setting column names as Property names
                dataTable.Columns.Add(prop.Name);
                dataTable.Columns[prop.Name].AllowDBNull = true;
            }
            foreach (T item in items)
            {

                var values = new object[Props.Length];
                for (int i = 0; i < Props.Length; i++)
                {
                    //inserting property values to datatable rows
                    values[i] = Props[i].GetValue(item, null);
                }

                dataTable.Rows.Add(values);
            }
            //put a breakpoint here and check datatable
            return dataTable;
        }

        public DataTable CreateDataTablewithNull<T>(List<T> items)
        {
            DataTable dataTable = new DataTable(typeof(T).Name);
            //Get all the properties
            PropertyInfo[] Props = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);
            foreach (PropertyInfo prop in Props)
            {
                //Setting column names as Property names
                dataTable.Columns.Add(prop.Name);
                dataTable.Columns[prop.Name].AllowDBNull = true;
            }
            foreach (T item in items)
            {
                DataRow dr = dataTable.NewRow();
                for (int i = 0; i < Props.Length; i++)
                {
                    //inserting property values to datatable rows
                    dr[i] = Props[i].GetValue(item, null);
                }

                dataTable.Rows.Add(dr);
            }
            //put a breakpoint here and check datatable
            return dataTable;
        }
        public void AddUploadTableData(DataTable dtSourceTable, DataTable dtTargetTable, ref String cError)
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




        public void AddDataInUploadTablewithMapping(DataTable dtMapTable, DataTable dtSourceTable, DataTable dtTargetTable, ref String cError)
        {
            try
            {

                Boolean dataFound;

                dataFound = false;

                dtTargetTable.Rows.Clear();
                foreach (DataRow dr in dtSourceTable.Rows)
                {
                    if (dr.RowState != DataRowState.Deleted && dr.RowState != DataRowState.Detached)
                    {

                        DataRow drNew = dtTargetTable.NewRow();
                        foreach (DataColumn dcol in dtSourceTable.Columns)
                        {
                            DataRow[] dF = dtMapTable.Select("DevColumnName = '" + dcol.ColumnName.Trim() + "'", "");
                            if (dF.Length > 0)
                            {
                                String cOrgCol = Convert.ToString(dF[0]["OrgColumnName"]);

                                if (dtTargetTable.Columns.Contains(cOrgCol))
                                {
                                    //   drNew[cOrgCol] = dr[dcol.ColumnName];
                                    Type t = dtTargetTable.Columns[cOrgCol].DataType;

                                    if (Type.GetTypeCode(t) == TypeCode.Object && t == typeof(byte[]))
                                    {
                                        drNew[cOrgCol] = (byte[])Convert.FromBase64String(dr[dcol.ColumnName].ToString());
                                        dataFound = true;
                                        continue;
                                    }

                                    switch (Type.GetTypeCode(t))
                                    {
                                        //case TypeCode.Decimal:
                                        //    drNew[cOrgCol] = ConvertDecimal(dr[dcol.ColumnName]);
                                        //    break;

                                        //case TypeCode.Boolean:
                                        //    drNew[cOrgCol] = ConvertBool(dr[dcol.ColumnName]);
                                        //    break;

                                        default:
                                            drNew[cOrgCol] = dr[dcol.ColumnName];

                                            if (!String.IsNullOrEmpty(dr[dcol.ColumnName].ToString()))
                                                dataFound = true;

                                            break;
                                    }
                                }
                            }
                        }

                        if (dataFound)
                            dtTargetTable.Rows.Add(drNew);
                    }
                }
            }
            catch (Exception ex)
            {
                cError = ex.Message;
            }
        }

        public void AddDataForEditCols(DataTable dtSourceTable, DataTable dtTargetTable, String cSource, ref String cError)
        {
            try
            {
                int iRowCount = dtSourceTable.Rows.Count;
                int iNullRowCount = 0;


                foreach (DataColumn dcol in dtSourceTable.Columns)
                {
                    Type t = dcol.DataType;


                    iNullRowCount = 0;
                    //if (Type.GetTypeCode(t) == TypeCode.String)
                    //    iNullRowCount = dtSourceTable.Select(dcol.ColumnName + "=''").Length;
                    //else
                    //if (Type.GetTypeCode(t) == TypeCode.Int16 || Type.GetTypeCode(t) == TypeCode.Int32)
                    //    iNullRowCount = dtSourceTable.Select(dcol.ColumnName + "=0").Length;
                    //else
                    //if (Type.GetTypeCode(t) == TypeCode.Decimal)
                    //    iNullRowCount = dtSourceTable.Select(dcol.ColumnName + "=0.00").Length;
                    //else
                    iNullRowCount = dtSourceTable.Select(dcol.ColumnName + " IS NULL").Length;

                    if (iNullRowCount != iRowCount) //(!dtSourceTable.Rows.OfType<DataRow>().Any(r => r.IsNull(dcol)))
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

        public double ConvertValue(string cValue)
        {
            double nValue = 0;
            bool bCheck = double.TryParse(cValue, out nValue);

            if (bCheck) return nValue;
            else return 0;
        }
        public DateTime ConvertDateTime(object val)
        {
            string dt = Convert.ToString(val);
            DateTime dtValue = new DateTime(1900, 1, 1);

            if (string.IsNullOrEmpty(dt) == false)
                DateTime.TryParse(dt, out dtValue);

            return dtValue;
        }
        public double ConvertDouble(object ob)
        {
            string cVal = Convert.ToString(ob);
            double nValue = 0;

            if (cVal.Length > 0)
                double.TryParse(cVal, out nValue);

            return nValue;
        }

        public Decimal ConvertDecimal(object ob)
        {
            string cVal = Convert.ToString(ob);
            Decimal nValue = 0M;

            if (cVal.Length > 0) Decimal.TryParse(cVal, out nValue);

            return nValue;
        }

        public bool ConvertBool(object Value)
        {
            bool bValue = true;
            string cValue = Convert.ToString(Value);

            if (cValue == "")
                bValue = false;
            else if (cValue == "0")
                bValue = false;
            else if (cValue.ToUpper() == "FALSE")
                bValue = false;
            else if (cValue == "1")
                bValue = true;
            else if (cValue.ToUpper() == "TRUE")
                bValue = true;

            return bValue;
        }
        public Int32 ConvertInt(object cVal)
        {
            string cValue = Convert.ToString(cVal);

            Int32 nValue = 0;
            bool bCheck = true;
            double dbValue = 0;

            if (string.IsNullOrEmpty(cValue.Trim()) == false)
                bCheck = double.TryParse(cValue, out dbValue);

            if (bCheck)
            {
                dbValue = Math.Floor(dbValue);

                if (dbValue != 0)
                    bCheck = Int32.TryParse(Convert.ToString(dbValue), out nValue);
            }

            return nValue;
        }


    }
}