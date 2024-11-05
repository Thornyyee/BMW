using OfficeOpenXml;
using OfficeOpenXml.Style;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Web.UI.WebControls;
using VPS_WEB.Common;
using VPSCLS.DBAccessor;

namespace VPS_WEB.Report
{
    public partial class WIPReport : System.Web.UI.Page
    {
        private SQLWipReport db = new SQLWipReport();
        private DataTable dt = new DataTable();
        private IFormatProvider culture = new System.Globalization.CultureInfo("fr-FR", true);
        private string fromdb = string.Empty;

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                ////dbAccessor.getAllWipInfo(out dt);
                ////gv.DataSource = dt;
                ////gv.DataBind();
                //DataTable qr = db.getAllLot();
                //if (qr.Rows.Count > 0)
                //{
                //    ddlLotNo.Items.Clear();
                //    ddlLotNo.Items.Add("ALL");
                //    for (int i = 0; i < qr.Rows.Count; i++)
                //    {
                //        ListItem listItem = new ListItem();
                //        listItem.Text = qr.Rows[i]["LOT"].ToString();

                //        ddlLotNo.Items.Add(listItem);
                //    }
                //}
                //GenerateddlModelCode();
                //GenerateColorDdl();
                //GenerateddlBasicCode();
                //generateddlProdOrder();
                //generateddlLine();
                //generateddlStation();
                //generateddlOpAction();
                //generateddlShift();
                //generateddlReworkStation();
                ////generateddlChassis(); //Comment by KCC 20170221

                GetFilters();
            }
        }

        private void GetFilters()
        {
            GenerateShiftDdl();
            GenerateLotNoDdl();
            GenerateMatlGrpDdl();
            //GenerateMatlGrpDescDdl();
            GenerateModelDdl();
            GenerateColorCodeDdl();
            GenerateOperationActionDdl();
            GenerateProdOrderDdl();
            GenerateLineDdl();
            GenerateStationDdl();
            GenerateReworkStationDdl();
            GenerateAgingDdl();
        }

        #region Drop down List Values

        private void GenerateLotNoDdl()
        {
            string userId = Request.Cookies["USER_INFO"]["USER_ID"].ToString();
            DataTable dt = db.GetAllLots(userId);

            if (dt != null)
            {
                for (int i = 0; i < dt.Rows.Count; i++)
                {
                    ListItem listItem = new ListItem();
                    listItem.Text = dt.Rows[i]["LOT"].ToString();
                    listItem.Value = dt.Rows[i]["LOT"].ToString();
                    ddlLotNo.Items.Add(listItem);
                }
            }
        }

        private void GenerateMatlGrpDdl()
        {
            ddlMatlGrp.Items.Clear();

            string userId = Request.Cookies["USER_INFO"]["USER_ID"].ToString();

            List<string> selectedLots = GetSelectedListBoxItems(ddlLotNo);
            DataTable dt = db.GetAllMatlGroups(userId, selectedLots);

            if (dt != null)
            {
                for (int i = 0; i < dt.Rows.Count; i++)
                {
                    ListItem listItem = new ListItem();
                    listItem.Text = dt.Rows[i]["MATL_GROUP"].ToString();
                    listItem.Value = dt.Rows[i]["MATL_GROUP"].ToString();

                    ddlMatlGrp.Items.Add(listItem);
                }
            }
        }

        private void GenerateModelDdl()
        {
            ddlMatlGrpDesc.Items.Clear();

            string userId = Request.Cookies["USER_INFO"]["USER_ID"].ToString();
            DataTable dt = db.GetAllModels(userId);

            if (dt != null)
            {
                for (int i = 0; i < dt.Rows.Count; i++)
                {
                    ListItem listItem = new ListItem();
                    listItem.Text = dt.Rows[i]["MODEL_ID"].ToString();
                    listItem.Value = dt.Rows[i]["MODEL_ID"].ToString();

                    ddlMatlGrpDesc.Items.Add(listItem);
                }
            }
        }

        private void GenerateMatlGrpDescDdl()
        {
            ddlMatlGrpDesc.Items.Clear();

            string userId = Request.Cookies["USER_INFO"]["USER_ID"].ToString();

            List<string> selectedLots = GetSelectedListBoxItems(ddlLotNo);
            DataTable dt = db.GetAllMatlGroupsDesc(userId, selectedLots);

            if (dt != null)
            {
                for (int i = 0; i < dt.Rows.Count; i++)
                {
                    ListItem listItem = new ListItem();
                    listItem.Text = dt.Rows[i]["MATL_GROUP_DESC"].ToString();
                    listItem.Value = dt.Rows[i]["MATL_GROUP_DESC"].ToString();

                    ddlMatlGrpDesc.Items.Add(listItem);
                }
            }
        }

        private void GenerateColorCodeDdl()
        {
            ddlColorCode.Items.Clear();

            string userId = Request.Cookies["USER_INFO"]["USER_ID"].ToString();

            List<string> selectedMatlGrps = GetSelectedListBoxItems(ddlMatlGrp);
            DataTable dt = db.GetAllColorCodes(userId, selectedMatlGrps);

            if (dt != null)
            {
                for (int i = 0; i < dt.Rows.Count; i++)
                {
                    ListItem listItem = new ListItem();
                    listItem.Text = dt.Rows[i]["COLOR_CODE"].ToString();
                    listItem.Value = dt.Rows[i]["COLOR_CODE"].ToString();

                    ddlColorCode.Items.Add(listItem);
                }
            }
        }

        private void GenerateLineDdl()
        {
            ddlLine.Items.Clear();

            string userId = Request.Cookies["USER_INFO"]["USER_ID"].ToString();
            DataTable dt = db.GetAllLines(userId);

            if (dt != null)
            {
                for (int i = 0; i < dt.Rows.Count; i++)
                {
                    ListItem listItem = new ListItem();
                    listItem.Text = dt.Rows[i]["LINE_NAME"].ToString();
                    listItem.Value = dt.Rows[i]["LINE_ID"].ToString();

                    ddlLine.Items.Add(listItem);
                }
            }
        }

        private void GenerateShiftDdl()
        {
            ddlShift.Items.Clear();

            string userId = Request.Cookies["USER_INFO"]["USER_ID"].ToString();
            DataTable dt = db.GetAllShifts(userId);

            if (dt != null)
            {
                for (int i = 0; i < dt.Rows.Count; i++)
                {
                    ListItem listItem = new ListItem();
                    listItem.Text = dt.Rows[i]["SHIFT_NAME"].ToString();
                    listItem.Value = dt.Rows[i]["SHIFT_ID"].ToString();

                    ddlShift.Items.Add(listItem);
                }
            }
        }

        private void GenerateOperationActionDdl()
        {
            ddlOpAction.Items.Clear();

            string userId = Request.Cookies["USER_INFO"]["USER_ID"].ToString();
            DataTable dt = db.GetAllOperationActions(userId);

            if (dt != null)
            {
                for (int i = 0; i < dt.Rows.Count; i++)

                {
                    ListItem listItem = new ListItem();
                    listItem.Text = dt.Rows[i]["OPERATION_ACTION"].ToString();
                    listItem.Value = dt.Rows[i]["OPERATION_ACTION"].ToString();

                    ddlOpAction.Items.Add(listItem);
                }
            }
        }

        private void GenerateProdOrderDdl()
        {
            ddlProdOrder.Items.Clear();

            string userId = Request.Cookies["USER_INFO"]["USER_ID"].ToString();
            DataTable dt = db.GetAllProdOrders(userId);

            if (dt != null)
            {
                for (int i = 0; i < dt.Rows.Count; i++)

                {
                    ListItem listItem = new ListItem();
                    listItem.Text = dt.Rows[i]["PROD_ORDER"].ToString();
                    listItem.Value = dt.Rows[i]["PROD_ORDER"].ToString();

                    ddlProdOrder.Items.Add(listItem);
                }
            }
        }

        private void GenerateStationDdl()
        {
            ddlStation.Items.Clear();

            string userId = Request.Cookies["USER_INFO"]["USER_ID"].ToString();

            List<string> selectedLines = GetSelectedListBoxItems(ddlLine);
            DataTable dt = db.GetAllStations(userId, selectedLines);

            if (dt != null)
            {
                for (int i = 0; i < dt.Rows.Count; i++)

                {
                    ListItem listItem = new ListItem();
                    listItem.Text = dt.Rows[i]["STATION_NAME"].ToString();
                    listItem.Value = dt.Rows[i]["STATION_ID"].ToString();

                    ddlStation.Items.Add(listItem);
                }
            }
        }

        private void GenerateReworkStationDdl()
        {
            ddlReworkStation.Items.Clear();

            string userId = Request.Cookies["USER_INFO"]["USER_ID"].ToString();
            DataTable dt = db.GetAllReworkStations(userId);

            if (dt != null)
            {
                for (int i = 0; i < dt.Rows.Count; i++)

                {
                    ListItem listItem = new ListItem();
                    listItem.Text = dt.Rows[i]["STATION_NAME"].ToString();
                    listItem.Value = dt.Rows[i]["STATION_ID"].ToString();

                    ddlReworkStation.Items.Add(listItem);
                }
            }
        }

        private void GenerateAgingDdl()
        {
            ddlAging.Items.Clear();

            string userId = Request.Cookies["USER_INFO"]["USER_ID"].ToString();
            DataTable dt = db.GetAllAging(userId);

            if (dt != null)
            {
                for (int i = 0; i < dt.Rows.Count; i++)

                {
                    ListItem listItem = new ListItem();
                    listItem.Text = dt.Rows[i]["AGING_DESC"].ToString();
                    listItem.Value = dt.Rows[i]["AGING_ID"].ToString();

                    ddlAging.Items.Add(listItem);
                }
            }
        }

        #endregion Drop down List Values

        #region Old ddl

        private void GenerateddlModelCode()
        {
            string Lot = "";
            for (int i = 0; i < ddlLotNo.Items.Count; i++)
            {
                if (ddlLotNo.Items[i].Selected == true)
                {
                    if (Lot.Length == 0)
                    {
                        Lot += "'" + ddlLotNo.Items[i].Value + "'";
                    }
                    else
                    {
                        Lot += ",'" + ddlLotNo.Items[i].Value + "'";
                    }
                }
            }
            DataTable qr2 = db.getAllModelCode(Lot);
            if (qr2.Rows.Count > 0)
            {
                ddlMatlGrp.Items.Clear();
                //ddlMatlGrp.Items.Add("ALL");
                for (int i = 0; i < qr2.Rows.Count; i++)
                {
                    ListItem listItem = new ListItem();
                    listItem.Text = qr2.Rows[i]["MODEL_CODE"].ToString();

                    ddlMatlGrp.Items.Add(listItem);
                }
            }
        }

        private void GenerateColorDdl()
        {
            string model = "";
            for (int i = 0; i < ddlMatlGrp.Items.Count; i++)
            {
                if (ddlMatlGrp.Items[i].Selected == true)
                {
                    if (model.Length == 0)
                    {
                        model += "'" + ddlMatlGrp.Items[i].Value + "'";
                    }
                    else
                    {
                        model += ",'" + ddlMatlGrp.Items[i].Value + "'";
                    }
                }
            }
            DataTable qr3 = db.getColor(model);
            if (qr3.Rows.Count > 0)
            {
                ddlColorCode.Items.Clear();
                //ddlColorCode.Items.Add("ALL");
                for (int i = 0; i < qr3.Rows.Count; i++)
                {
                    ListItem listItem = new ListItem();
                    listItem.Text = qr3.Rows[i]["COLOR"].ToString();

                    ddlColorCode.Items.Add(listItem);
                }
            }
        }

        private void GenerateddlBasicCode()
        {
            string model = "";
            for (int i = 0; i < ddlMatlGrp.Items.Count; i++)
            {
                if (ddlMatlGrp.Items[i].Selected == true)
                {
                    if (model.Length == 0)
                    {
                        model += "'" + ddlMatlGrp.Items[i].Value + "'";
                    }
                    else
                    {
                        model += ",'" + ddlMatlGrp.Items[i].Value + "'";
                    }
                }
            }
            DataTable qr4 = db.getBasicCode(model);
            if (qr4.Rows.Count > 0)
            {
                ddlMatlGrpDesc.Items.Clear();
                //ddlMatlGrpDesc.Items.Add("ALL");
                for (int i = 0; i < qr4.Rows.Count; i++)
                {
                    ListItem listItem = new ListItem();
                    listItem.Text = qr4.Rows[i]["MSC"].ToString();

                    ddlMatlGrpDesc.Items.Add(listItem);
                }
            }
        }

        private void generateddlStation()
        {
            string lineId = MultipleSelectToString(ddlLine);   //ddlLine.SelectedValue.ToString();

            DataTable qr6 = new DataTable();

            if (!string.IsNullOrEmpty(lineId))
            {
                qr6 = db.getStation(lineId);
            }
            else
            {
                qr6 = db.getStation(string.Empty);
            }

            if (qr6.Rows.Count > 0)
            {
                ddlStation.Items.Clear();
                //ddlToStation.Items.Clear();
                for (int i = 0; i < qr6.Rows.Count; i++)
                {
                    ListItem listItem1 = new ListItem();
                    ListItem listItem2 = new ListItem();

                    listItem1.Text = qr6.Rows[i]["STATION_NAME"].ToString();
                    listItem1.Value = qr6.Rows[i]["STATION_ID"].ToString();// + '-' + qr6.Rows[i]["SORTING_SEQ"].ToString();

                    //listItem2.Text = qr6.Rows[i]["STATION_NAME"].ToString();
                    //listItem2.Value = qr6.Rows[i]["STATION_ID"].ToString() + '-' + qr6.Rows[i]["SORTING_SEQ"].ToString();

                    ddlStation.Items.Add(listItem1);
                    //  ddlToStation.Items.Add(listItem2);
                }
            }
        }

        //Added by Xian 21/11/2020
        private void generateddlReworkStation()
        {
            DataTable reworkDt = new DataTable();
            reworkDt = db.getReworkStation();

            if (reworkDt.Rows.Count > 0)
            {
                ddlReworkStation.Items.Clear();
                for (int i = 0; i < reworkDt.Rows.Count; i++)
                {
                    ListItem listItemRework = new ListItem();
                    listItemRework.Text = reworkDt.Rows[i]["STATION_NAME"].ToString();
                    listItemRework.Value = reworkDt.Rows[i]["STATION_ID"].ToString();

                    ddlReworkStation.Items.Add(listItemRework);
                }
            }
        }

        //BO JUN add OP Action Filter
        private void generateddlOpAction()
        {
            DataTable qr6 = db.getOpAction();
            ddlOpAction.Items.Clear();
            //ddlOpAction.Items.Add("ALL");
            if (qr6.Rows.Count > 0)
            {
                for (int i = 0; i < qr6.Rows.Count; i++)
                {
                    ListItem listItem1 = new ListItem();

                    listItem1.Text = qr6.Rows[i]["OPERATION_ACTION"].ToString();
                    listItem1.Value = qr6.Rows[i]["OPERATION_ACTION"].ToString();

                    ddlOpAction.Items.Add(listItem1);
                }
            }
        }

        private void generateddlLine()
        {
            DataTable qr6 = db.getLine();
            ddlLine.Items.Clear();
            if (qr6.Rows.Count > 0)
            {
                //ddlLine.Items.Insert(0, new ListItem("ALL", "0")); (10/6/19)

                for (int i = 0; i < qr6.Rows.Count; i++)
                {
                    ListItem listItem1 = new ListItem();

                    listItem1.Text = qr6.Rows[i]["LINE_NAME"].ToString();
                    listItem1.Value = qr6.Rows[i]["LINE_ID"].ToString();

                    ddlLine.Items.Add(listItem1);
                }
            }
        }

        private void generateddlShift()
        {
            ddlShift.Items.Clear();

            ListItem listItemAll = new ListItem();
            listItemAll.Text = "ALL";
            listItemAll.Value = "ALL";
            ddlShift.Items.Add(listItemAll);

            DataTable shiftDt = db.getShift();
            for (int i = 0; i < shiftDt.Rows.Count; i++)
            {
                ListItem listItem = new ListItem();

                listItem.Text = shiftDt.Rows[i]["SHIFT_NAME"].ToString();
                listItem.Value = shiftDt.Rows[i]["SHIFT_ID"].ToString();
                ddlShift.Items.Add(listItem);
            }
            shiftDt.Dispose();
        }

        //10/6/19
        private void generateddlProdOrder()
        {
            DataTable qr6 = db.getProdOrder();
            ddlProdOrder.Items.Clear();
            //ddlOpAction.Items.Add("ALL");
            if (qr6.Rows.Count > 0)
            {
                for (int i = 0; i < qr6.Rows.Count; i++)
                {
                    ListItem listItem1 = new ListItem();

                    listItem1.Text = qr6.Rows[i]["PROD_ORDER"].ToString();
                    listItem1.Value = qr6.Rows[i]["PROD_ORDER"].ToString();

                    ddlProdOrder.Items.Add(listItem1);
                }
            }
        }

        #endregion Old ddl

        #region Drop down Change Events

        protected void ddlLot_SelectedIndexChanged(object sender, EventArgs e)
        {
            //GenerateMatlGrpDdl();
            GenerateddlModelCode();
        }

        protected void ddlModelCode_SelectedIndexChanged(object sender, EventArgs e)
        {
            //GenerateMatlGrpDescDdl();
            //GenerateColorCodeDdl();

            GenerateddlBasicCode(); // Commented by Toh 20210924
            GenerateColorDdl(); // Commented by Toh 20210924
            //generateddlProdOrder();
            //generateddlChassis();//Comment by KCC 20170221
        }

        protected void ddlLine_SelectedIndexChanged(object sender, EventArgs e)
        {
            //GenerateStationDdl();
            generateddlStation();// Commented by Toh 20210924
        }

        #endregion Drop down Change Events

        private void InquiryData()
        {
            bool SelectedPBSI = false;

            DateTime convertedDatetime = new DateTime();

            DataTable dtPBSIWIPCount = new DataTable();
            DataTable dtPBSIWIPInfo = new DataTable();

            try
            {
                string userId = Request.Cookies["USER_INFO"]["USER_ID"].ToString();
                string chassis = txtChassisNo.Text.Trim();
                string engine = txtEngine.Text.Trim();

                string datetimeAsOf = txtFrDate.Text.Trim().ToString();
                if (!string.IsNullOrEmpty(datetimeAsOf))
                {
                    convertedDatetime = DateTime.Parse(datetimeAsOf, culture, System.Globalization.DateTimeStyles.AssumeLocal);
                    convertedDatetime = convertedDatetime.AddDays(1).AddTicks(-1);
                }

                string selectedShift = GetSingleSelectedListBoxItem(ddlShift);
                List<string> selectedLots = GetSelectedListBoxItems(ddlLotNo);
                List<string> selecteMatlGrps = GetSelectedListBoxItems(ddlMatlGrp);
                List<string> selectedMatlGrpDescs = GetSelectedListBoxItems(ddlMatlGrpDesc);
                List<string> selectedColorCodes = GetSelectedListBoxItems(ddlColorCode);
                List<string> selectedOperationActions = GetSelectedListBoxItems(ddlOpAction);
                List<string> selectedProdOrders = GetSelectedListBoxItems(ddlProdOrder);
                List<string> selectedLines = GetSelectedListBoxItems(ddlLine);
                List<string> selectedStations = GetSelectedListBoxItems(ddlStation);
                List<string> selectedReworkStations = GetSelectedListBoxItems(ddlReworkStation);
                List<string> allReworkStations = GetAllListBoxItems(ddlReworkStation);
                string selectedAging = GetSingleSelectedListBoxItem(ddlAging);

                if (selectedStations.Count() > 0)
                {
                    foreach (var station in selectedStations)
                    {
                        if (station == "Q01")
                        {
                            SelectedPBSI = true;
                        }
                    }
                }

                if (selectedStations.Count() == 0)
                {
                    SelectedPBSI = true;
                }

                if (selectedColorCodes.Count() > 0 || selectedProdOrders.Count() > 0 || selectedOperationActions.Count() > 0 || selectedReworkStations.Count() > 0 || chassis.Length > 0 || engine.Length > 0)
                {
                    SelectedPBSI = false;
                }

                if (SelectedPBSI)
                {
                    dtPBSIWIPCount = db.GetPBSIWipCount(convertedDatetime, selectedLots, selecteMatlGrps, selectedMatlGrpDescs, selectedLines, allReworkStations, selectedShift, selectedAging, userId);
                    dtPBSIWIPInfo = db.GetPBSIWIPInfo(convertedDatetime, selectedLots, selecteMatlGrps, selectedMatlGrpDescs, selectedLines, allReworkStations, selectedShift, selectedAging, userId);
                }

                DataTable dtWIPInfo = db.GetAllWipInfo(userId, chassis, selectedShift, engine, convertedDatetime, selectedLots, selecteMatlGrps, selectedMatlGrpDescs, selectedColorCodes, selectedProdOrders,
                    selectedStations, selectedOperationActions, selectedLines, selectedReworkStations, selectedAging);

                DataTable dtWIPCount = db.GetAllWipCount(userId, chassis, selectedShift, engine, convertedDatetime, selectedLots, selecteMatlGrps, selectedMatlGrpDescs, selectedColorCodes, selectedProdOrders,
                    selectedStations, selectedOperationActions, selectedLines, selectedReworkStations, allReworkStations, selectedAging);

                if (dtWIPInfo != null && dtWIPCount != null && dtPBSIWIPCount != null && dtPBSIWIPInfo != null)
                {
                    var resultWIPCount = dtPBSIWIPCount.AsEnumerable().Union(dtWIPCount.AsEnumerable());
                    if (resultWIPCount.Count() > 0)
                    {
                        dtWIPCount = resultWIPCount.CopyToDataTable();
                    }

                    var resultWIPInfo = dtWIPInfo.AsEnumerable().Union(dtPBSIWIPInfo.AsEnumerable());
                    if (resultWIPInfo.Count() > 0)
                    {
                        dtWIPInfo = resultWIPInfo.CopyToDataTable();
                    }

                    GridViewWIP.Columns.Clear();

                    DataRow dr = dtWIPCount.NewRow();
                    dr["STATION_ID"] = "Total";
                    dr["STATION_NAME"] = "Total";
                    dr["DISPLAY_SEQ"] = 0;
                    dr["IN_LINE"] = 0;

                    for (int i = 0; i < ddlReworkStation.Items.Count; i++)
                    {
                        string rework = ddlReworkStation.Items[i].Value;
                        dr[rework] = 0;
                    }

                    dr["F2_CASCADE"] = 0;
                    dr["TOTAL_WIP"] = 0;

                    dtWIPCount.Rows.Add(dr);

                    DataRow dr1 = dtWIPCount.NewRow();
                    dr1["STATION_ID"] = "Total Without PBSI";
                    dr1["STATION_NAME"] = "Total Without PBSI";
                    dr1["DISPLAY_SEQ"] = 0;
                    dr1["IN_LINE"] = 0;

                    for (int i = 0; i < ddlReworkStation.Items.Count; i++)
                    {
                        string rework = ddlReworkStation.Items[i].Value;
                        dr1[rework] = 0;
                    }

                    dr1["F2_CASCADE"] = 0;
                    dr1["TOTAL_WIP"] = 0;
                    dtWIPCount.Rows.Add(dr1);

                    for (int i = 0; i < dtWIPCount.Columns.Count; i++)
                    {
                        dtWIPCount.Columns[i].ReadOnly = false;
                        string columnName = dtWIPCount.Columns[i].ColumnName.ToString();

                        if (columnName != "STATION_NAME" && columnName != "STATION_ID" && columnName != "DISPLAY_SEQ")
                        {
                            int totalCount = 0;
                            int totalPBSICount = 0;

                            for (int j = 0; j < dtWIPCount.Rows.Count; j++)
                            {
                                string rowName = dtWIPCount.Rows[j]["STATION_NAME"].ToString();

                                if (rowName == "PBSI")
                                {
                                    totalPBSICount = (int)dtWIPCount.Rows[j][i];
                                }

                                if ((j + 1) < dtWIPCount.Rows.Count)
                                {
                                    totalCount += (int)dtWIPCount.Rows[j][i];
                                }
                            }

                            dtWIPCount.Rows[dtWIPCount.Rows.Count - 2][i] = totalCount;
                            dtWIPCount.Rows[dtWIPCount.Rows.Count - 1][i] = totalCount - totalPBSICount;
                        }
                    }
                    dtWIPCount.AcceptChanges();

                    for (int i = 0; i < dtWIPCount.Columns.Count; i++)
                    {
                        if (!dtWIPCount.Columns[i].ColumnName.Equals("STATION_ID") && !dtWIPCount.Columns[i].ColumnName.Equals("DISPLAY_SEQ"))
                        {
                            string columnName = dtWIPCount.Columns[i].ColumnName;
                            switch (columnName)
                            {
                                case "STATION_NAME":
                                    columnName = "Station Name";
                                    break;

                                case "IN_LINE":
                                    columnName = "In Line";
                                    break;

                                case "F2_CASCADE":
                                    columnName = "F2 Cascade";
                                    break;

                                case "TOTAL_WIP":
                                    columnName = "Total WIP";
                                    break;
                            }

                            for (int k = 0; k < ddlReworkStation.Items.Count; k++)
                            {
                                if (columnName == ddlReworkStation.Items[k].Value)
                                {
                                    columnName = ddlReworkStation.Items[k].Text;
                                }
                            }

                            BoundField boundField = new BoundField();
                            boundField.DataField = dtWIPCount.Columns[i].ColumnName.ToString();
                            boundField.HeaderText = columnName;
                            boundField.SortExpression = dtWIPCount.Columns[i].ColumnName.ToString();
                            GridViewWIP.Columns.Add(boundField);
                        }
                    }

                    gv.DataSource = dtWIPInfo;
                    gv.DataBind();

                    GridViewWIP.DataSource = dtWIPCount;
                    GridViewWIP.DataBind();

                    if (GridViewWIP.Rows.Count > 0)
                    {
                        GridViewWIP.Rows[GridViewWIP.Rows.Count - 1].Font.Bold = true;
                        GridViewWIP.Rows[GridViewWIP.Rows.Count - 2].Font.Bold = true;
                    }

                    ViewState["CurrentData"] = dtWIPInfo;
                    ViewState["CurrentWIPCountData"] = dtWIPCount;
                }
                else
                {
                    ViewState["CurrentData"] = null;
                    ViewState["CurrentWIPCountData"] = null;

                    gv.DataSource = null;
                    gv.DataBind();

                    GridViewWIP.DataSource = null;
                    GridViewWIP.DataBind();
                }
            }
            catch (Exception ex)
            {
                CommonMethod common = new CommonMethod();
                common.LogWebError(common.MethodName(), ex.ToString());
            }
        }

        protected void btnInquiry_Click(object sender, EventArgs e)
        {
            string userId = Request.Cookies["USER_INFO"]["USER_ID"].ToString();
            string chassis = txtChassisNo.Text.Trim();
            string datetimeAsOf = txtFrDate.Text.Trim().ToString();

            if (string.IsNullOrEmpty(datetimeAsOf))
            {
                if (chassis.Length == 0)
                {
                    lblCommonMsg.Text = "Please select date!";
                    ModalPopupExtender_Msg.Show();

                    return;
                }
            }

            if (chassis.Length > 0)
            {
                Hashtable sqlParams = new Hashtable();
                sqlParams["@chassisNo"] = chassis;

                string chassisExist = db.CheckVehicleExist(userId, chassis);

                if (chassisExist != "False")
                {
                    if (chassisExist == "Archive")
                    {
                        lblCommonMsg.Text = "The Chassis No already move to archive database.";
                        ModalPopupExtender_Msg.Show();

                        return;
                    }
                }
                else
                {
                    gv.DataSource = null;
                    gv.DataBind();

                    lblCommonMsg.Text = "No Record Found.";
                    ModalPopupExtender_Msg.Show();

                    return;
                }
            }

            InquiryData();
        }

        protected void btnInquiry_Click1(object sender, EventArgs e)
        {
            gv.DataSource = null;
            gv.DataBind();

            GridViewWIP.DataSource = null;
            GridViewWIP.DataBind();
            GridViewWIP.Columns.Clear();

            string chassisNo = txtChassisNo.Text.Trim();
            string engineNo = txtEngine.Text.Trim();

            if (txtChassisNo.Text.Trim().Length > 0)
            {
                Hashtable sqlParams2 = new Hashtable();
                sqlParams2["@chassisNo"] = txtChassisNo.Text.ToString();

                fromdb = db.checkVehicleExist(sqlParams2);

                if (fromdb != "False")
                {
                    if (fromdb == "Archive")
                    {
                        lblCommonMsg.Text = "The Chassis No already move to archive database.";
                        ModalPopupExtender_Msg.Show();

                        return;
                    }
                }
                else
                {
                    gv.DataSource = null;
                    gv.DataBind();

                    lblCommonMsg.Text = "No Record Found.";
                    ModalPopupExtender_Msg.Show();

                    return;
                }
            }

            //if (txtFrDate.Text.Trim() != "" && txtToDate.Text.Trim() != "") //Comment by KCC 20170222
            if (txtFrDate.Text.Trim() != "")
            {
                DateTime from = DateTime.Parse(txtFrDate.Text.Trim(), culture, System.Globalization.DateTimeStyles.AssumeLocal);
                //DateTime to = DateTime.Parse(txtToDate.Text.Trim(), culture, System.Globalization.DateTimeStyles.AssumeLocal);
                //to = to.AddDays(1).AddTicks(-1); //Comment by KCC 20170222
                from = from.AddDays(1).AddTicks(-1);
                //string Lot = ddlLotNo.SelectedValue.ToString();
                //string Model = ddlMatlGrp.SelectedValue.ToString();
                //string Color = ddlColorCode.SelectedValue.ToString();
                //string BasicCode = ddlMatlGrpDesc.SelectedValue.ToString();

                string Lot = "";
                for (int i = 0; i < ddlLotNo.Items.Count; i++)
                {
                    if (ddlLotNo.Items[i].Selected == true)
                    {
                        if (Lot.Length == 0)
                        {
                            Lot += "'" + ddlLotNo.Items[i].Value + "'";
                        }
                        else
                        {
                            Lot += ",'" + ddlLotNo.Items[i].Value + "'";
                        }
                    }
                }

                string Model = "";
                for (int i = 0; i < ddlMatlGrp.Items.Count; i++)
                {
                    if (ddlMatlGrp.Items[i].Selected == true)
                    {
                        if (Model.Length == 0)
                        {
                            Model += "'" + ddlMatlGrp.Items[i].Value + "'";
                        }
                        else
                        {
                            Model += ",'" + ddlMatlGrp.Items[i].Value + "'";
                        }
                    }
                }

                string FromStation = "";
                for (int i = 0; i < ddlStation.Items.Count; i++)
                {
                    if (ddlStation.Items[i].Selected == true)
                    {
                        if (FromStation.Length == 0)
                        {
                            FromStation += "'" + ddlStation.Items[i].Value + "'";
                        }
                        else
                        {
                            FromStation += ",'" + ddlStation.Items[i].Value + "'";
                        }
                    }
                }

                string Color = "";
                for (int i = 0; i < ddlColorCode.Items.Count; i++)
                {
                    if (ddlColorCode.Items[i].Selected == true)
                    {
                        if (Color.Length == 0)
                        {
                            Color += "'" + ddlColorCode.Items[i].Value + "'";
                        }
                        else
                        {
                            Color += ",'" + ddlColorCode.Items[i].Value + "'";
                        }
                    }
                }

                string BasicCode = "";
                for (int i = 0; i < ddlMatlGrpDesc.Items.Count; i++)
                {
                    if (ddlMatlGrpDesc.Items[i].Selected == true)
                    {
                        if (BasicCode.Length == 0)
                        {
                            BasicCode += "'" + ddlMatlGrpDesc.Items[i].Value + "'";
                        }
                        else
                        {
                            BasicCode += ",'" + ddlMatlGrpDesc.Items[i].Value + "'";
                        }
                    }
                }

                string Shift = "";
                for (int i = 0; i < ddlShift.Items.Count; i++)
                {
                    if (ddlShift.Items[i].Selected == true)
                    {
                        Shift = ddlShift.Items[i].Value;
                    }
                }

                string ReworkName = "";
                string ReworkID = "";
                int reworkCount = 0;
                for (int i = 0; i < ddlReworkStation.Items.Count; i++)
                {
                    if (ddlReworkStation.Items[i].Selected == true)
                    {
                        reworkCount++;
                        if (ReworkName.Length == 0)
                        {
                            ReworkName += "'" + ddlReworkStation.Items[i].Text + "'";
                            ReworkID += "'" + ddlReworkStation.Items[i].Value + "'";
                        }
                        else
                        {
                            ReworkName += ",'" + ddlReworkStation.Items[i].Text + "'";
                            ReworkID += "'" + ddlReworkStation.Items[i].Value + "'";
                        }
                    }
                }
                if (reworkCount > 1)
                    ReworkName = "All";

                //string ProdOrder = "%" + ddlProdOrder.SelectedValue.ToString() + "%"; //txtProdOrder.Text.Trim() + "%"; (10/6/19)
                string ProdOrder = MultipleSelectToString(ddlProdOrder);
                string lineId = MultipleSelectToString(ddlLine);

                // string[] splitFromStation = ddlStation.SelectedValue.ToString().Split('-');
                // string[] splitToStation = ddlToStation.SelectedValue.ToString().Split('-');

                // string FromStation = MultipleSelectToString(ddlStation);//splitFromStation[1];
                //string ToStation = splitToStation[1];

                /*DataTable dtCheckTrimStartFromStation = new DataTable();
                DataTable dtCheckTrimStartToStation = new DataTable();

                Hashtable sqlParams = new Hashtable();
                sqlParams["@updateId"] = Request.Cookies["USER_INFO"]["USER_ID"].ToString();
                sqlParams["@systemCategoryName"] = "WIP Report";

                sqlParams["@checkFromStationId"] = FromStation;

                dtCheckTrimStartFromStation = dbAccessor.checkTrimStartStation(sqlParams);

                sqlParams["@checkToStationId"] = ToStation;

                dtCheckTrimStartToStation = dbAccessor.checkTrimStartStation(sqlParams);

                if(dtCheckTrimStartFromStation.Rows.Count > 0 && dtCheckTrimStartToStation.Rows.Count > 0)
                {
                    lineId = dtCheckTrimStartFromStation.Rows[0]["STATION_GROUP"]
                }*/

                string chassisno = "%" + txtChassisNo.Text.ToString() + "%";
                string engineno = "%" + txtEngine.Text.ToString() + "%";
                //string OpAction = ddlOpAction.SelectedValue.ToString(); //BO JUN add OpAction
                string OpAction = "";
                for (int i = 0; i < ddlOpAction.Items.Count; i++)
                {
                    if (ddlOpAction.Items[i].Selected == true)
                    {
                        if (OpAction.Length == 0)
                        {
                            OpAction += "'" + ddlOpAction.Items[i].Value + "'";
                        }
                        else
                        {
                            OpAction += ",'" + ddlOpAction.Items[i].Value + "'";
                        }
                    }
                }

                //Session["from"] = from;
                Session["from"] = from.ToString("dd-MM-yyyy");
                //Session["to"] = to;
                Session["Lot"] = Lot;
                Session["Model"] = Model;
                Session["Color"] = Color;
                Session["BasicCode"] = BasicCode;
                Session["ProdOrder"] = ProdOrder;
                Session["LineId"] = lineId;
                Session["FromStation"] = FromStation;
                // Session["ToStation"] = ToStation;
                Session["ChassisNo"] = chassisno;
                Session["EngineNo"] = engineno;
                Session["ReworkStation"] = ReworkName;
                Session["OpAction"] = OpAction; //BO JUN add OpAction
                Session["Shift"] = Shift;

                //if (from < to) //Comment by KCC 20170222
                //{
                //dt = dbAccessor.getAllWipInfo(from, to, Lot, Model, Color, BasicCode, ProdOrder, FromStation, ToStation,chassisno); //Comment by KCC 20170222
                //dt = dbAccessor.getAllWipInfo(from, Lot, Model, Color, BasicCode, ProdOrder, FromStation, ToStation, chassisno); //Comment by BO JUN add OpAction
                dt = db.getAllWipInfo(from, Lot, Model, Color, BasicCode, ProdOrder, FromStation, chassisno, OpAction, lineId, Shift, engineno, ReworkName);

                if (dt.Rows.Count > 0)
                {
                    gv.DataSource = dt;
                    gv.DataBind();
                    ViewState["CurrentData"] = dt;
                }

                DataTable dtWIPCount = db.getAllWIPCount(Lot, Model, Color, BasicCode, ProdOrder, FromStation, chassisno, OpAction, lineId, Shift, engineno, ReworkName);
                var groups = dtWIPCount.AsEnumerable();
                var groupList = from g in groups

                                group g by g.Field<string>("STATION_NAME") into Group1

                                select new
                                {
                                    STATION_NAME = Group1.Key,
                                    IN_LINE = Group1.Sum(x => x.Field<int>("IN_LINE")),
                                    F1_REWORK = Group1.Sum(x => x.Field<int>("F1_REWORK")),
                                    F2_REWORK = Group1.Sum(x => x.Field<int>("F2_REWORK")),
                                    TOTAL_WIP = Group1.Sum(x => x.Field<int>("TOTAL_WIP")),
                                    F2_CASCADE = Group1.Sum(x => x.Field<int>("F2_CASCADE"))
                                };
                var sum = groupList.AsEnumerable();
                var sumList = from g in sum

                              group g by 1 into Group1

                              select new
                              {
                                  STATION_NAME = "Total",
                                  IN_LINE = Group1.Sum(x => x.IN_LINE),
                                  F1_REWORK = Group1.Sum(x => x.F1_REWORK),
                                  F2_REWORK = Group1.Sum(x => x.F2_REWORK),
                                  TOTAL_WIP = Group1.Sum(x => x.TOTAL_WIP),
                                  F2_CASCADE = Group1.Sum(x => x.F2_CASCADE)
                              };
                var resultSum = groupList.Union(sumList);

                var sum1 = groupList.AsEnumerable();
                var sumList1 = from g in sum1

                               group g by 1 into Group1

                               select new
                               {
                                   STATION_NAME = "Total Without PBSI",
                                   IN_LINE = Group1.Sum(x => x.IN_LINE) - Group1.Where(s => s.STATION_NAME.Equals("PBSI")).Sum(a => a.IN_LINE),
                                   F1_REWORK = Group1.Sum(x => x.F1_REWORK) - Group1.Where(s => s.STATION_NAME.Equals("PBSI")).Sum(a => a.F1_REWORK),
                                   F2_REWORK = Group1.Sum(x => x.F2_REWORK) - Group1.Where(s => s.STATION_NAME.Equals("PBSI")).Sum(a => a.F2_REWORK),
                                   TOTAL_WIP = Group1.Sum(x => x.TOTAL_WIP) - Group1.Where(s => s.STATION_NAME.Equals("PBSI")).Sum(a => a.TOTAL_WIP),
                                   F2_CASCADE = Group1.Sum(x => x.F2_CASCADE) - Group1.Where(s => s.STATION_NAME.Equals("PBSI")).Sum(a => a.F2_CASCADE)
                               };

                var resultSum1 = resultSum.Union(sumList1);

                for (int i = 0; i < dtWIPCount.Columns.Count; i++)
                {
                    if (!dtWIPCount.Columns[i].ColumnName.Equals("STATION_ID") && !dtWIPCount.Columns[i].ColumnName.Equals("DISPLAY_SEQ"))
                    {
                        BoundField boundField = new BoundField();
                        boundField.DataField = dtWIPCount.Columns[i].ColumnName.ToString();
                        boundField.HeaderText = dtWIPCount.Columns[i].ColumnName.ToString();
                        boundField.SortExpression = dtWIPCount.Columns[i].ColumnName.ToString();
                        GridViewWIP.Columns.Add(boundField);
                    }
                }
                ViewState["CurrentWIPCountData"] = dtWIPCount;
                GridViewWIP.DataSource = resultSum1.ToList();
                GridViewWIP.DataBind();
                if (GridViewWIP.Rows.Count > 0)
                {
                    GridViewWIP.Rows[GridViewWIP.Rows.Count - 1].Font.Bold = true;
                    GridViewWIP.Rows[GridViewWIP.Rows.Count - 2].Font.Bold = true;
                }
                //}
                //else
                //{
                //    lblCommonMsg.Text = "To date cannot be earlier than from date";
                //    ModalPopupExtender_Msg.Show();
                //    return;
                //}
            }
            else if (txtFrDate.Text.Trim() == "" && (chassisNo.Length > 0 || engineNo.Length > 0))
            {
                //string Lot = ddlLotNo.SelectedValue.ToString();
                //string Model = ddlMatlGrp.SelectedValue.ToString();
                //string Color = ddlColorCode.SelectedValue.ToString();
                //string BasicCode = ddlMatlGrpDesc.SelectedValue.ToString();

                string Lot = "";
                for (int i = 0; i < ddlLotNo.Items.Count; i++)
                {
                    if (ddlLotNo.Items[i].Selected == true)
                    {
                        if (Lot.Length == 0)
                        {
                            Lot += "'" + ddlLotNo.Items[i].Value + "'";
                        }
                        else
                        {
                            Lot += ",'" + ddlLotNo.Items[i].Value + "'";
                        }
                    }
                }

                string Model = "";
                for (int i = 0; i < ddlMatlGrp.Items.Count; i++)
                {
                    if (ddlMatlGrp.Items[i].Selected == true)
                    {
                        if (Model.Length == 0)
                        {
                            Model += "'" + ddlMatlGrp.Items[i].Value + "'";
                        }
                        else
                        {
                            Model += ",'" + ddlMatlGrp.Items[i].Value + "'";
                        }
                    }
                }

                string Color = "";
                for (int i = 0; i < ddlColorCode.Items.Count; i++)
                {
                    if (ddlColorCode.Items[i].Selected == true)
                    {
                        if (Color.Length == 0)
                        {
                            Color += "'" + ddlColorCode.Items[i].Value + "'";
                        }
                        else
                        {
                            Color += ",'" + ddlColorCode.Items[i].Value + "'";
                        }
                    }
                }

                string BasicCode = "";
                for (int i = 0; i < ddlMatlGrpDesc.Items.Count; i++)
                {
                    if (ddlMatlGrpDesc.Items[i].Selected == true)
                    {
                        if (BasicCode.Length == 0)
                        {
                            BasicCode += "'" + ddlMatlGrpDesc.Items[i].Value + "'";
                        }
                        else
                        {
                            BasicCode += ",'" + ddlMatlGrpDesc.Items[i].Value + "'";
                        }
                    }
                }

                string ReworkName = "";
                string ReworkID = "";
                int reworkCount = 0;
                for (int i = 0; i < ddlReworkStation.Items.Count; i++)
                {
                    if (ddlReworkStation.Items[i].Selected == true)
                    {
                        reworkCount++;
                        if (ReworkName.Length == 0)
                        {
                            ReworkName += "'" + ddlReworkStation.Items[i].Text + "'";
                            ReworkID += "'" + ddlReworkStation.Items[i].Value + "'";
                        }
                        else
                        {
                            ReworkName += ",'" + ddlReworkStation.Items[i].Text + "'";
                            ReworkID += "'" + ddlReworkStation.Items[i].Value + "'";
                        }
                    }
                }
                if (reworkCount > 1)
                    ReworkName = "All";

                string FromStation = "";
                for (int i = 0; i < ddlStation.Items.Count; i++)
                {
                    if (ddlStation.Items[i].Selected == true)
                    {
                        if (FromStation.Length == 0)
                        {
                            FromStation += "'" + ddlStation.Items[i].Value + "'";
                        }
                        else
                        {
                            FromStation += ",'" + ddlStation.Items[i].Value + "'";
                        }
                    }
                }
                string ProdOrder = MultipleSelectToString(ddlProdOrder); //(10 / 6 / 19)
                string lineId = MultipleSelectToString(ddlLine); //(10 / 6 / 19)
                                                                 //string[] splitFromStation = ddlStation.SelectedValue.ToString().Split('-');
                                                                 //string[] splitToStation = ddlToStation.SelectedValue.ToString().Split('-');

                // string FromStation =  MultipleSelectToString(ddlStation);//splitFromStation[1];
                //    string ToStation = splitToStation[1];
                string chassisno = "%" + txtChassisNo.Text.ToString() + "%";
                string engineno = "%" + txtEngine.Text.ToString() + "%";
                //string OpAction = ddlOpAction.SelectedValue.ToString(); //BO JUN add OpAction
                string OpAction = "";
                for (int i = 0; i < ddlOpAction.Items.Count; i++)
                {
                    if (ddlOpAction.Items[i].Selected == true)
                    {
                        if (OpAction.Length == 0)
                        {
                            OpAction += "'" + ddlOpAction.Items[i].Value + "'";
                        }
                        else
                        {
                            OpAction += ",'" + ddlOpAction.Items[i].Value + "'";
                        }
                    }
                }

                string Shift = "";
                for (int i = 0; i < ddlShift.Items.Count; i++)
                {
                    if (ddlShift.Items[i].Selected == true)
                    {
                        Shift = ddlShift.Items[i].Value;
                    }
                }

                Session["Lot"] = Lot;
                Session["Model"] = Model;
                Session["Color"] = Color;
                Session["BasicCode"] = BasicCode;
                Session["ProdOrder"] = ProdOrder;
                Session["LineId"] = lineId;
                Session["FromStation"] = FromStation;
                // Session["ToStation"] = ToStation;
                Session["ChassisNo"] = chassisno;
                Session["EngineNo"] = engineNo;
                Session["ReworkStation"] = ReworkID;
                Session["OpAction"] = OpAction; //BO JUN add OpAction
                Session["Shift"] = Shift;

                dt = db.getAllWipInfoNoDate(Lot, Model, Color, BasicCode, ProdOrder, FromStation, chassisno, OpAction, lineId, Shift, engineNo, ReworkName);

                if (dt.Rows.Count > 0)
                {
                    gv.DataSource = dt;
                    gv.DataBind();
                    ViewState["CurrentData"] = dt;
                }

                DataTable dtWIPCount = db.getAllWIPCount(Lot, Model, Color, BasicCode, ProdOrder, FromStation, chassisno, OpAction, lineId, Shift, engineno, ReworkName);
                var groups = dtWIPCount.AsEnumerable();
                var groupList = from g in groups

                                group g by g.Field<string>("STATION_NAME") into Group1

                                select new
                                {
                                    STATION_NAME = Group1.Key,
                                    IN_LINE = Group1.Sum(x => x.Field<int>("IN_LINE")),
                                    F1_REWORK = Group1.Sum(x => x.Field<int>("F1_REWORK")),
                                    F2_REWORK = Group1.Sum(x => x.Field<int>("F2_REWORK")),
                                    TOTAL_WIP = Group1.Sum(x => x.Field<int>("TOTAL_WIP")),
                                    F2_CASCADE = Group1.Sum(x => x.Field<int>("F2_CASCADE"))
                                };
                var sum = groupList.AsEnumerable();
                var sumList = from g in sum

                              group g by 1 into Group1

                              select new
                              {
                                  STATION_NAME = "Total",
                                  IN_LINE = Group1.Sum(x => x.IN_LINE),
                                  F1_REWORK = Group1.Sum(x => x.F1_REWORK),
                                  F2_REWORK = Group1.Sum(x => x.F2_REWORK),
                                  TOTAL_WIP = Group1.Sum(x => x.TOTAL_WIP),
                                  F2_CASCADE = Group1.Sum(x => x.F2_CASCADE)
                              };
                var resultSum = groupList.Union(sumList);

                var sum1 = groupList.AsEnumerable();
                var sumList1 = from g in sum1

                               group g by 1 into Group1

                               select new
                               {
                                   STATION_NAME = "Total Without PBSI",
                                   IN_LINE = Group1.Sum(x => x.IN_LINE) - Group1.Where(s => s.STATION_NAME.Equals("PBSI")).Sum(a => a.IN_LINE),
                                   F1_REWORK = Group1.Sum(x => x.F1_REWORK) - Group1.Where(s => s.STATION_NAME.Equals("PBSI")).Sum(a => a.F1_REWORK),
                                   F2_REWORK = Group1.Sum(x => x.F2_REWORK) - Group1.Where(s => s.STATION_NAME.Equals("PBSI")).Sum(a => a.F2_REWORK),
                                   TOTAL_WIP = Group1.Sum(x => x.TOTAL_WIP) - Group1.Where(s => s.STATION_NAME.Equals("PBSI")).Sum(a => a.TOTAL_WIP),
                                   F2_CASCADE = Group1.Sum(x => x.F2_CASCADE) - Group1.Where(s => s.STATION_NAME.Equals("PBSI")).Sum(a => a.F2_CASCADE)
                               };

                var resultSum1 = resultSum.Union(sumList1);

                for (int i = 0; i < dtWIPCount.Columns.Count; i++)
                {
                    if (!dtWIPCount.Columns[i].ColumnName.Equals("STATION_ID") && !dtWIPCount.Columns[i].ColumnName.Equals("DISPLAY_SEQ"))
                    {
                        BoundField boundField = new BoundField();
                        boundField.DataField = dtWIPCount.Columns[i].ColumnName.ToString();
                        boundField.HeaderText = dtWIPCount.Columns[i].ColumnName.ToString();
                        boundField.SortExpression = dtWIPCount.Columns[i].ColumnName.ToString();
                        GridViewWIP.Columns.Add(boundField);
                    }
                }
                ViewState["CurrentWIPCountData"] = dtWIPCount;
                GridViewWIP.DataSource = resultSum1.ToList();
                GridViewWIP.DataBind();
                if (GridViewWIP.Rows.Count > 0)
                    GridViewWIP.Rows[GridViewWIP.Rows.Count - 1].Font.Bold = true;
            }
            else
            {
                lblCommonMsg.Text = "Please select date!";
                ModalPopupExtender_Msg.Show();
                return;
            }
        }

        protected void btnExcel_Click(object sender, EventArgs e)
        {
            string userId = Request.Cookies["USER_INFO"]["USER_ID"].ToString();
            string chassis = txtChassisNo.Text.Trim();
            string datetimeAsOf = txtFrDate.Text.Trim().ToString();

            if (string.IsNullOrEmpty(datetimeAsOf))
            {
                if (chassis.Length == 0)
                {
                    lblCommonMsg.Text = "Please select date!";
                    ModalPopupExtender_Msg.Show();

                    return;
                }
            }

            if (chassis.Length > 0)
            {
                Hashtable sqlParams = new Hashtable();
                sqlParams["@chassisNo"] = chassis;

                string chassisExist = db.CheckVehicleExist(userId, chassis);

                if (chassisExist != "False")
                {
                    if (chassisExist == "Archive")
                    {
                        lblCommonMsg.Text = "The Chassis No already move to archive database.";
                        ModalPopupExtender_Msg.Show();

                        return;
                    }
                }
                else
                {
                    gv.DataSource = null;
                    gv.DataBind();

                    lblCommonMsg.Text = "No Record Found.";
                    ModalPopupExtender_Msg.Show();

                    return;
                }
            }

            DataTable dtWIPInfo = new DataTable();
            DataTable dtWIPCount = new DataTable();

            if (ViewState["CurrentData"] == null || ViewState["CurrentWIPCountData"] == null)
            {
                InquiryData();
            }

            try
            {
                dtWIPInfo = (DataTable)ViewState["CurrentData"];
                dtWIPCount = (DataTable)ViewState["CurrentWIPCountData"];
            }
            catch (Exception ex)
            {
                InquiryData();
                dtWIPInfo = (DataTable)ViewState["CurrentData"];
                dtWIPCount = (DataTable)ViewState["CurrentWIPCountData"];
            }

            if (dtWIPInfo != null)
            {
                dtWIPInfo.Columns.Remove("STATION_ID");
                //dtWIPInfo.Columns.Remove("AGING");

                // Assuming dtWIPInfo is your DataTable// Move the "AGING" column to the last position
                int lastColumnIndex = dtWIPInfo.Columns.Count - 1;                
                dtWIPInfo.Columns["AGING"].SetOrdinal(lastColumnIndex);

                dtWIPInfo.Columns["VIN_NO"].ColumnName = "Body No.";
                dtWIPInfo.Columns["CHASSIS_NO"].ColumnName = "Chassis No.";
                dtWIPInfo.Columns["ORDER"].ColumnName = "Prod. Order";
                dtWIPInfo.Columns["SEQ_IN_LOT"].ColumnName = "Prod. Seq.";
                dtWIPInfo.Columns["MATL_GROUP_DESC"].ColumnName = "MSC";
                dtWIPInfo.Columns["MODEL_ID"].ColumnName = "Model";
                dtWIPInfo.Columns["MATL_GROUP"].ColumnName = "Material Group";
                dtWIPInfo.Columns["WBS_ELEM"].ColumnName = "Lot No.";
                dtWIPInfo.Columns["COLOR_CODE"].ColumnName = "Color";
                dtWIPInfo.Columns["DESTINATION"].ColumnName = "Destination";
                dtWIPInfo.Columns["AG_INTERIOR_COLOR"].ColumnName = "AG Interior Color";
                dtWIPInfo.Columns["AG_EXTERIOR_COLOR"].ColumnName = "AG Exterior Color";
                dtWIPInfo.Columns["STATION_NAME"].ColumnName = "Station";
                dtWIPInfo.Columns["LINEOFF_DATETIME"].ColumnName = "Line Off Date Time";
                dtWIPInfo.Columns["F2_CASCADE"].ColumnName = "F2 Cascade";
                dtWIPInfo.Columns["UPDATE_DATETIME"].ColumnName = "Scanned Date Time";
                dtWIPInfo.Columns["OPERATION_ACTION"].ColumnName = "Operation Action";
                dtWIPInfo.Columns["SAP_DATETIME"].ColumnName = "SAP Date Time";
                dtWIPInfo.Columns["REWORK"].ReadOnly = false;
                dtWIPInfo.Columns["BLOCK_FLAG"].ReadOnly = false;
                dtWIPInfo.Columns["BLOCK_FLAG"].MaxLength = 30;

                dtWIPInfo.Columns["MATCHED_DATETIME"].ColumnName = "Matching Date Time";

                for (int i = 0; i < dtWIPInfo.Rows.Count; i++)
                {
                    string value = dtWIPInfo.Rows[i]["REWORK"].ToString();
                    for (int k = 0; k < ddlReworkStation.Items.Count; k++)
                    {
                        if (value == ddlReworkStation.Items[k].Value)
                        {
                            value = ddlReworkStation.Items[k].Text;
                        }
                    }

                    dtWIPInfo.Rows[i]["REWORK"] = value;
                }


                for (int i = 0; i < dtWIPInfo.Rows.Count; i++)
                {
                    if (dtWIPInfo.Rows[i]["BLOCK_FLAG"].ToString() == "Y")
                    {
                        dtWIPInfo.Rows[i]["BLOCK_FLAG"] = "Blocked";
                    }
                    else
                    {
                        dtWIPInfo.Rows[i]["BLOCK_FLAG"] = "-";
                    }

                    if (string.IsNullOrEmpty(dtWIPInfo.Rows[i]["REWORK"].ToString()))
                    {
                        dtWIPInfo.Rows[i]["REWORK"] = "-";
                    }

                    if (string.IsNullOrEmpty(dtWIPInfo.Rows[i]["ENGINE_NO"].ToString()))
                    {
                        dtWIPInfo.Rows[i]["ENGINE_NO"] = "-";
                    }
                }

                dtWIPInfo.Columns["ENGINE_NO"].ColumnName = "Engine No.";
                dtWIPInfo.Columns["REWORK"].ColumnName = "Rework Station";
                dtWIPInfo.Columns["BLOCK_FLAG"].ColumnName = "Block Status";
                dtWIPInfo.Columns["AGING"].ColumnName = "Aging";

                dtWIPCount.Columns.Remove("STATION_ID");
                dtWIPCount.Columns.Remove("DISPLAY_SEQ");

                for (int i = 0; i < dtWIPCount.Columns.Count; i++)
                {
                    dtWIPCount.Columns[i].ReadOnly = false;
                    string columnName = dtWIPCount.Columns[i].ColumnName;
                    switch (columnName)
                    {
                        case "STATION_NAME":
                            columnName = "Station Name";
                            break;

                        case "IN_LINE":
                            columnName = "In Line";
                            break;

                        case "F2_CASCADE":
                            columnName = "F2 Cascade";
                            break;

                        case "TOTAL_WIP":
                            columnName = "Total WIP";
                            break;
                    }

                    for (int k = 0; k < ddlReworkStation.Items.Count; k++)
                    {
                        if (columnName == ddlReworkStation.Items[k].Value)
                        {
                            columnName = ddlReworkStation.Items[k].Text;
                        }
                    }

                    dtWIPCount.Columns[i].ColumnName = columnName;
                }
                dtWIPCount.AcceptChanges();

                using (var memoryStream = new MemoryStream())
                using (var excelPackage = new ExcelPackage(memoryStream))
                {
                    var excelWorksheet = excelPackage.Workbook.Worksheets.Add("WIP Report");

                    excelWorksheet.Cells[1, 1, 1, 7].Merge = true;
                    excelWorksheet.Cells[1, 1].Style.Font.Bold = true;
                    excelWorksheet.Cells[2, 1, 2, 7].Style.Font.Bold = true;
                    excelWorksheet.Cells[2, 1, 2, 7].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    excelWorksheet.Cells[2, 1, 2, 7].Style.Fill.BackgroundColor.SetColor(Color.LightGreen);
                    excelWorksheet.Cells[dtWIPCount.Rows.Count + 1, 1, dtWIPCount.Rows.Count + 2, 7].Style.Font.Bold = true;
                    excelWorksheet.Cells[1, 1, dtWIPCount.Rows.Count + 2, 7].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    excelWorksheet.Cells[1, 1, dtWIPCount.Rows.Count + 2, 7].Style.Border.Top.Style = ExcelBorderStyle.Thin;
                    excelWorksheet.Cells[1, 1, dtWIPCount.Rows.Count + 2, 7].Style.Border.Right.Style = ExcelBorderStyle.Thin;
                    excelWorksheet.Cells[1, 1, dtWIPCount.Rows.Count + 2, 7].Style.Border.Left.Style = ExcelBorderStyle.Thin;
                    excelWorksheet.Cells[1, 1, dtWIPCount.Rows.Count + 2, 7].Style.Border.Bottom.Style = ExcelBorderStyle.Thin;

                    excelWorksheet.Cells[1, 1].Value = "WIP Count";

                    int dqRowCount = dtWIPCount.Rows.Count;

                    excelWorksheet.Cells[2, 1].LoadFromDataTable(dtWIPCount, true);
                    excelWorksheet.Cells[dqRowCount + 5, 1].LoadFromDataTable(dtWIPInfo, true);

                    excelWorksheet.Cells[dqRowCount + 5, 1, dqRowCount + 5, dtWIPInfo.Columns.Count].Style.Font.Bold = true;
                    excelWorksheet.Cells[dqRowCount + 5, 1, dqRowCount + 5, dtWIPInfo.Columns.Count].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    excelWorksheet.Cells[dqRowCount + 5, 1, dqRowCount + 5, dtWIPInfo.Columns.Count].Style.Fill.BackgroundColor.SetColor(Color.LightGreen);
                    excelWorksheet.Cells[dqRowCount + 5, 1, dqRowCount + 5 + dtWIPInfo.Rows.Count, dtWIPInfo.Columns.Count].Style.Border.Top.Style = ExcelBorderStyle.Thin;
                    excelWorksheet.Cells[dqRowCount + 5, 1, dqRowCount + 5 + dtWIPInfo.Rows.Count, dtWIPInfo.Columns.Count].Style.Border.Right.Style = ExcelBorderStyle.Thin;
                    excelWorksheet.Cells[dqRowCount + 5, 1, dqRowCount + 5 + dtWIPInfo.Rows.Count, dtWIPInfo.Columns.Count].Style.Border.Left.Style = ExcelBorderStyle.Thin;
                    excelWorksheet.Cells[dqRowCount + 5, 1, dqRowCount + 5 + dtWIPInfo.Rows.Count, dtWIPInfo.Columns.Count].Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                    excelWorksheet.Cells[dqRowCount + 5, 1, dqRowCount + 5 + dtWIPInfo.Rows.Count, dtWIPInfo.Columns.Count].Style.Border.Bottom.Style = ExcelBorderStyle.Thin;

                    //for (int i = 0; i <= dtWIPInfo.Rows.Count; i++)
                    //{
                    //    excelWorksheet.Cells[dqRowCount + 5 + i, dtWIPInfo.Columns.Count].Style.Numberformat.Format = "dd/mm/yyyy hh:mm:ss am/pm";
                    //    excelWorksheet.Cells[dqRowCount + 5 + i, dtWIPInfo.Columns.Count - 2].Style.Numberformat.Format = "dd/mm/yyyy hh:mm:ss am/pm";
                    //    excelWorksheet.Cells[dqRowCount + 5 + i, dtWIPInfo.Columns.Count - 3].Style.Numberformat.Format = "dd/mm/yyyy hh:mm:ss am/pm";
                    //}

                    for (int i = 0; i <= dtWIPInfo.Rows.Count; i++)
                    {
                        excelWorksheet.Cells[dqRowCount + 5 + i, dtWIPInfo.Columns.Count - 1].Style.Numberformat.Format = "dd/mm/yyyy hh:mm:ss am/pm";
                        excelWorksheet.Cells[dqRowCount + 5 + i, dtWIPInfo.Columns.Count - 2].Style.Numberformat.Format = "dd/mm/yyyy hh:mm:ss am/pm";
                        excelWorksheet.Cells[dqRowCount + 5 + i, dtWIPInfo.Columns.Count - 4].Style.Numberformat.Format = "dd/mm/yyyy hh:mm:ss am/pm";
                        excelWorksheet.Cells[dqRowCount + 5 + i, dtWIPInfo.Columns.Count - 5].Style.Numberformat.Format = "dd/mm/yyyy hh:mm:ss am/pm";                        
                    }

                    excelWorksheet.Cells[excelWorksheet.Dimension.Address].AutoFitColumns();

                    excelPackage.Save();

                    var excelFileContent = memoryStream.ToArray();
                    Response.ContentType = "application/vnd.ms-excel";
                    Response.Headers["Content-Disposition"] = "inline; filename=\"WIP Report_" + DateTime.Now.ToString("ddMMyyyy_hhmmss") + ".xlsx\"";
                    Response.BinaryWrite(excelFileContent);
                    Response.End();
                }
            }
        }

        protected void btnExcel_Click1(object sender, EventArgs e)
        {
            if (ViewState["CurrentData"] != null)
            {
                DataTable localData = (DataTable)ViewState["CurrentData"];
                DataTable localWIPCount = (DataTable)ViewState["CurrentWIPCountData"];

                localData.Columns.Remove("STATION_ID");
                localWIPCount.Columns.Remove("STATION_ID");
                localWIPCount.Columns.Remove("DISPLAY_SEQ");

                var groups = localWIPCount.AsEnumerable();
                var groupList = from g in groups

                                group g by g.Field<string>("STATION_NAME") into Group1

                                select new
                                {
                                    STATION_NAME = Group1.Key,
                                    IN_LINE = Group1.Sum(x => x.Field<int>("IN_LINE")),
                                    F1_REWORK = Group1.Sum(x => x.Field<int>("F1_REWORK")),
                                    F2_REWORK = Group1.Sum(x => x.Field<int>("F2_REWORK")),
                                    TOTAL_WIP = Group1.Sum(x => x.Field<int>("TOTAL_WIP")),
                                    F2_CASCADE = Group1.Sum(x => x.Field<int>("F2_CASCADE"))
                                };
                var sum = groupList.AsEnumerable();
                var sumList = from g in sum

                              group g by 1 into Group1

                              select new
                              {
                                  STATION_NAME = "Total",
                                  IN_LINE = Group1.Sum(x => x.IN_LINE),
                                  F1_REWORK = Group1.Sum(x => x.F1_REWORK),
                                  F2_REWORK = Group1.Sum(x => x.F2_REWORK),
                                  TOTAL_WIP = Group1.Sum(x => x.TOTAL_WIP),
                                  F2_CASCADE = Group1.Sum(x => x.F2_CASCADE)
                              };
                var resultSum = groupList.Union(sumList);

                var sum1 = groupList.AsEnumerable();
                var sumList1 = from g in sum1

                               group g by 1 into Group1

                               select new
                               {
                                   STATION_NAME = "Total Without PBSI",
                                   IN_LINE = Group1.Sum(x => x.IN_LINE) - Group1.Where(s => s.STATION_NAME.Equals("PBSI")).Sum(a => a.IN_LINE),
                                   F1_REWORK = Group1.Sum(x => x.F1_REWORK) - Group1.Where(s => s.STATION_NAME.Equals("PBSI")).Sum(a => a.F1_REWORK),
                                   F2_REWORK = Group1.Sum(x => x.F2_REWORK) - Group1.Where(s => s.STATION_NAME.Equals("PBSI")).Sum(a => a.F2_REWORK),
                                   TOTAL_WIP = Group1.Sum(x => x.TOTAL_WIP) - Group1.Where(s => s.STATION_NAME.Equals("PBSI")).Sum(a => a.TOTAL_WIP),
                                   F2_CASCADE = Group1.Sum(x => x.F2_CASCADE) - Group1.Where(s => s.STATION_NAME.Equals("PBSI")).Sum(a => a.F2_CASCADE)
                               };

                var resultSum1 = resultSum.Union(sumList1);
                int k = resultSum1.Count();

                using (var memoryStream = new MemoryStream())
                using (var excelPackage = new ExcelPackage(memoryStream))
                {
                    var excelWorksheet = excelPackage.Workbook.Worksheets.Add("WIP Report");

                    excelWorksheet.Cells[1, 1, 1, 7].Merge = true;
                    excelWorksheet.Cells[1, 1].Style.Font.Bold = true;
                    excelWorksheet.Cells[2, 1, 2, 6].Style.Font.Bold = true;
                    excelWorksheet.Cells[2, 1, 2, 6].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    excelWorksheet.Cells[2, 1, 2, 6].Style.Fill.BackgroundColor.SetColor(Color.LightGreen);
                    excelWorksheet.Cells[resultSum1.Count() + 1, 1, resultSum1.Count() + 2, 6].Style.Font.Bold = true;
                    excelWorksheet.Cells[1, 1, resultSum1.Count() + 2, 6].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    excelWorksheet.Cells[1, 1, resultSum1.Count() + 2, 6].Style.Border.Top.Style = ExcelBorderStyle.Thin;
                    excelWorksheet.Cells[1, 1, resultSum1.Count() + 2, 6].Style.Border.Right.Style = ExcelBorderStyle.Thin;
                    excelWorksheet.Cells[1, 1, resultSum1.Count() + 2, 6].Style.Border.Left.Style = ExcelBorderStyle.Thin;
                    excelWorksheet.Cells[1, 1, resultSum1.Count() + 2, 6].Style.Border.Bottom.Style = ExcelBorderStyle.Thin;

                    excelWorksheet.Cells[1, 1].Value = "WIP Count";

                    int dqRowCount = resultSum.Count();

                    excelWorksheet.Cells[2, 1].LoadFromCollection(resultSum1, true);
                    excelWorksheet.Cells[dqRowCount + 5, 1].LoadFromDataTable(localData, true);

                    excelWorksheet.Cells[dqRowCount + 5, 1, dqRowCount + 5, localData.Columns.Count].Style.Font.Bold = true;
                    excelWorksheet.Cells[dqRowCount + 5, 1, dqRowCount + 5, localData.Columns.Count].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    excelWorksheet.Cells[dqRowCount + 5, 1, dqRowCount + 5, localData.Columns.Count].Style.Fill.BackgroundColor.SetColor(Color.LightGreen);
                    excelWorksheet.Cells[dqRowCount + 5, 1, dqRowCount + 5 + localData.Rows.Count, localData.Columns.Count].Style.Border.Top.Style = ExcelBorderStyle.Thin;
                    excelWorksheet.Cells[dqRowCount + 5, 1, dqRowCount + 5 + localData.Rows.Count, localData.Columns.Count].Style.Border.Right.Style = ExcelBorderStyle.Thin;
                    excelWorksheet.Cells[dqRowCount + 5, 1, dqRowCount + 5 + localData.Rows.Count, localData.Columns.Count].Style.Border.Left.Style = ExcelBorderStyle.Thin;
                    excelWorksheet.Cells[dqRowCount + 5, 1, dqRowCount + 5 + localData.Rows.Count, localData.Columns.Count].Style.Border.Bottom.Style = ExcelBorderStyle.Thin;

                    for (int i = 0; i <= localData.Rows.Count; i++)
                    {
                        excelWorksheet.Cells[dqRowCount + 5 + i, localData.Columns.Count - 1].Style.Numberformat.Format = "dd/mm/yyyy hh:mm:ss am/pm";
                        excelWorksheet.Cells[dqRowCount + 5 + i, localData.Columns.Count - 3].Style.Numberformat.Format = "dd/mm/yyyy hh:mm:ss am/pm";
                    }
                    excelWorksheet.Cells[excelWorksheet.Dimension.Address].AutoFitColumns();

                    excelPackage.Save();

                    var excelFileContent = memoryStream.ToArray();
                    Response.ContentType = "application/vnd.ms-excel";
                    Response.Headers["Content-Disposition"] = "inline; filename=\"WIP Report_" + DateTime.Now.ToString("ddMMyyyy hhmm") + ".xlsx\"";
                    Response.BinaryWrite(excelFileContent);
                    Response.End();

                    localData.Dispose();
                    localWIPCount.Dispose();
                }

                //localData.Columns.Remove("STATION_ID");
                /*DataTable currData = (DataTable)ViewState["CurrentData"];
                currData.Columns.Remove("GROUP_ID");
                currData.Columns.Remove("DEFECT_ID");
                currData.Columns.Remove("CREATOR_ID");
                currData.Columns.Remove("SOURCE_STATION_ID");

                for (int i = 0; i < localData.Rows.Count; i++)
                {
                    string id = localData.Rows[i]["ISSUE_ID"].ToString();
                    currData.Rows[i]["AGING"] = CalculateAging(id);
                }*/

                //ExportExcelCls.ExportDataTableToExcel(localData, "WIP Report_" + DateTime.Now, "WIP Report", false);
            }
            else
            {
                lblCommonMsg.Text = "Please inquiry data!";
                ModalPopupExtender_Msg.Show();
                return;
            }
        }

        #region Grid View

        protected void gridView_PageIndexChanging(object sender, GridViewPageEventArgs e)
        {
            DataTable dataTable = new DataTable();
            try
            {
                dataTable = (DataTable)ViewState["CurrentData"];
            }
            catch (Exception ex)
            {
                InquiryData();
                dataTable = (DataTable)ViewState["CurrentData"];
            }

            if (dataTable != null && dataTable.Rows.Count > 0)
            {
                DataTable sortedDT = null;
                try
                {
                    if (ViewState["SortExpression"] != null || ViewState["SortDirection"] != null)
                    {
                        DataView dv = new DataView(dataTable);
                        dv.Sort = ViewState["SortExpression"].ToString() + " " + convertSort(ViewState["SortDirection"].ToString());
                        sortedDT = dv.ToTable();
                    }
                    else
                    {
                        sortedDT = dataTable;
                    }
                }
                catch
                {
                    ViewState["SortExpression"] = null;
                    ViewState["SortDirection"] = null;
                    sortedDT = dataTable;
                }
                gv.DataSource = sortedDT;
            }
            else
            {
                gv.DataSource = null;
            }

            gv.PageIndex = e.NewPageIndex;
            gv.DataBind();
        }

        protected void gridView_Sorting(object sender, GridViewSortEventArgs e)
        {
            DataTable dataTable = (DataTable)ViewState["CurrentData"];

            if (dataTable == null)
            {
                InquiryData();
                dataTable = (DataTable)ViewState["CurrentData"];
            }

            if (dataTable != null)
            {
                if (ViewState["SortExpression"] == null || ViewState["SortDirection"] == null)
                {
                    ViewState["SortExpression"] = e.SortExpression.ToString();
                    ViewState["SortDirection"] = e.SortDirection.ToString();
                }
                else
                {
                    String currentSortDirection = ViewState["SortDirection"].ToString();
                    if (ViewState["SortExpression"].ToString().Equals(e.SortExpression.ToString()) && ViewState["SortDirection"].ToString().Equals(e.SortDirection.ToString()))
                    {
                        ViewState["SortExpression"] = e.SortExpression.ToString();
                        ViewState["SortDirection"] = "Descending";
                    }
                    else
                    {
                        ViewState["SortExpression"] = e.SortExpression.ToString();
                        ViewState["SortDirection"] = "Ascending";
                    }
                }

                DataView dv = new DataView(dataTable);
                dv.Sort = e.SortExpression + " " + convertSort(ViewState["SortDirection"].ToString());
                DataTable sortedDT = dv.ToTable();
                gv.DataSource = sortedDT;
                gv.DataBind();
            }
        }

        protected string convertSort(string sortType)
        {
            if (sortType.Equals("Ascending"))
                return "asc";
            else
                return "desc";
        }

        private int GetColumnIndexByName(GridViewRow row, string columnName)
        {
            int columnIndex = 0;
            foreach (DataControlFieldCell cell in row.Cells)
            {
                if (cell.ContainingField is BoundField)
                    if (((BoundField)cell.ContainingField).DataField.Equals(columnName))
                        break;
                columnIndex++; // keep adding 1 while we don't have the correct name
            }
            return columnIndex;
        }

        protected void gridView_RowDataBound(object sender, GridViewRowEventArgs e)
        {
            if (e.Row.RowType == DataControlRowType.DataRow)
            {
                int index_rework = GetColumnIndexByName(e.Row, "REWORK");
                int index_blockFlag = GetColumnIndexByName(e.Row, "BLOCK_FLAG");
                int index_engineNo = GetColumnIndexByName(e.Row, "ENGINE_NO");

                string reworkStationId = e.Row.Cells[index_rework].Text;

                for (int i = 0; i < ddlReworkStation.Items.Count; i++)
                {
                    if (ddlReworkStation.Items[i].Value == reworkStationId)
                    {
                        e.Row.Cells[index_rework].Text = ddlReworkStation.Items[i].Text;
                    }
                }

                e.Row.Cells[index_rework].Text = (e.Row.Cells[index_rework].Text.Trim() != "&nbsp;") ? e.Row.Cells[index_rework].Text.Trim() : "-";
                e.Row.Cells[index_blockFlag].Text = (e.Row.Cells[index_blockFlag].Text.Trim() == "Y") ? "Blocked" : "-";
                e.Row.Cells[index_engineNo].Text = (e.Row.Cells[index_engineNo].Text.Trim() == "&nbsp;") ? "-" : e.Row.Cells[index_engineNo].Text;
            }
        }

        protected void GridViewWIP_RowCreated(object sender, GridViewRowEventArgs e)
        {
            if (e.Row.RowType == DataControlRowType.Header)
            {
                GridView ProductGrid = (GridView)sender;
                GridViewRow HeaderRow = new GridViewRow(0, 0, DataControlRowType.Header, DataControlRowState.Insert);
                TableCell HeaderCell = new TableCell();
                HeaderCell.Text = "WIP Count";
                HeaderCell.HorizontalAlign = HorizontalAlign.Center;
                HeaderCell.Font.Bold = true;
                HeaderCell.ColumnSpan = 7;
                HeaderCell.CssClass = "HeaderStyle";
                HeaderRow.Cells.Add(HeaderCell);
                ProductGrid.Controls[0].Controls.AddAt(0, HeaderRow);
            }
        }


        private string ConvertSortDirectionToSql(SortDirection sortDirection)
        {
            string newSortDirection = String.Empty;

            switch (sortDirection)
            {
                case SortDirection.Ascending:
                    newSortDirection = "ASC";
                    break;

                case SortDirection.Descending:
                    newSortDirection = "DESC";
                    break;
            }

            return newSortDirection;
        }

        protected void gridView_PageIndexChanging1(object sender, GridViewPageEventArgs e)
        {
            gv.PageIndex = e.NewPageIndex;
            DateTime from = DateTime.Now;//DateTime.Parse(Session["from"].ToString(), culture, System.Globalization.DateTimeStyles.AssumeLocal);//DateTime.Parse("" + Session["from"]);
            string Lot = "";
            string Model = "";
            string Color = "";
            string BasicCode = "";
            string ProdOrder = "";
            string LineId = "";
            string FromStation = "";
            // string ToStation = "";
            string chassisno = "";
            string engineno = "";
            string OpAction = "";
            string Shift = "";
            string ReworkName = "";

            #region //selection

            try
            {
                from = DateTime.Parse(Session["from"].ToString(), culture, System.Globalization.DateTimeStyles.AssumeLocal);//DateTime.Parse("" + Session["from"]);
                from = from.AddDays(1).AddTicks(-1);

                Lot = "" + Session["Lot"];
                Model = "" + Session["Model"];
                Color = "" + Session["Color"];
                BasicCode = "" + Session["BasicCode"];
                ProdOrder = "" + Session["ProdOrder"];
                LineId = "" + Session["LineId"];
                FromStation = "" + Session["FromStation"];
                // ToStation = "" + Session["ToStation"];
                chassisno = "" + Session["ChassisNo"] + "";
                engineno = "" + Session["EngineNo"] + "";
                ReworkName = "" + Session["ReworkStation"] + "";
                OpAction = "" + Session["OpAction"] + ""; //Bo Jun add OpAction
                Shift = "" + Session["Shift"] + "";
            }
            catch (Exception ex)
            {
                from = DateTime.Parse(txtFrDate.Text.Trim(), culture, System.Globalization.DateTimeStyles.AssumeLocal);
                from = from.AddDays(1).AddTicks(-1);

                Lot = "";
                for (int i = 0; i < ddlLotNo.Items.Count; i++)
                {
                    if (ddlLotNo.Items[i].Selected == true)
                    {
                        if (Lot.Length == 0)
                        {
                            Lot += "'" + ddlLotNo.Items[i].Value + "'";
                        }
                        else
                        {
                            Lot += ",'" + ddlLotNo.Items[i].Value + "'";
                        }
                    }
                }

                Model = "";
                for (int i = 0; i < ddlMatlGrp.Items.Count; i++)
                {
                    if (ddlMatlGrp.Items[i].Selected == true)
                    {
                        if (Model.Length == 0)
                        {
                            Model += "'" + ddlMatlGrp.Items[i].Value + "'";
                        }
                        else
                        {
                            Model += ",'" + ddlMatlGrp.Items[i].Value + "'";
                        }
                    }
                }

                Color = "";
                for (int i = 0; i < ddlColorCode.Items.Count; i++)
                {
                    if (ddlColorCode.Items[i].Selected == true)
                    {
                        if (Color.Length == 0)
                        {
                            Color += "'" + ddlColorCode.Items[i].Value + "'";
                        }
                        else
                        {
                            Color += ",'" + ddlColorCode.Items[i].Value + "'";
                        }
                    }
                }

                BasicCode = "";
                for (int i = 0; i < ddlMatlGrpDesc.Items.Count; i++)
                {
                    if (ddlMatlGrpDesc.Items[i].Selected == true)
                    {
                        if (BasicCode.Length == 0)
                        {
                            BasicCode += "'" + ddlMatlGrpDesc.Items[i].Value + "'";
                        }
                        else
                        {
                            BasicCode += ",'" + ddlMatlGrpDesc.Items[i].Value + "'";
                        }
                    }
                }

                Shift = "";
                for (int i = 0; i < ddlShift.Items.Count; i++)
                {
                    if (ddlShift.Items[i].Selected == true)
                    {
                        Shift = ddlShift.Items[i].Value;
                    }
                }

                ReworkName = "";
                for (int i = 0; i < ddlReworkStation.Items.Count; i++)
                {
                    if (ddlReworkStation.Items[i].Selected == true)
                    {
                        if (ReworkName.Length == 0)
                        {
                            ReworkName += "'" + ddlReworkStation.Items[i].Text + "'";
                        }
                        else
                        {
                            ReworkName += ",'" + ddlReworkStation.Items[i].Text + "'";
                        }
                    }
                }

                FromStation = "";
                for (int i = 0; i < ddlStation.Items.Count; i++)
                {
                    if (ddlStation.Items[i].Selected == true)
                    {
                        if (FromStation.Length == 0)
                        {
                            FromStation += "'" + ddlStation.Items[i].Value + "'";
                        }
                        else
                        {
                            FromStation += ",'" + ddlStation.Items[i].Value + "'";
                        }
                    }
                }

                ProdOrder = MultipleSelectToString(ddlProdOrder); //(10/6/19)
                LineId = MultipleSelectToString(ddlLine); // (10/6/19)

                //string[] splitFromStation = ddlStation.SelectedValue.ToString().Split('-');
                //string[] splitToStation = ddlToStation.SelectedValue.ToString().Split('-');

                // FromStation = MultipleSelectToString(ddlStation);// splitFromStation[1];
                // ToStation = splitToStation[1];
                chassisno = "%" + txtChassisNo.Text.ToString() + "%";
                engineno = "%" + txtEngine.Text.ToString() + "%";
                OpAction = "";
                for (int i = 0; i < ddlOpAction.Items.Count; i++)
                {
                    if (ddlOpAction.Items[i].Selected == true)
                    {
                        if (OpAction.Length == 0)
                        {
                            OpAction += "'" + ddlOpAction.Items[i].Value + "'";
                        }
                        else
                        {
                            OpAction += ",'" + ddlOpAction.Items[i].Value + "'";
                        }
                    }
                }
            }

            #endregion //selection

            if (txtChassisNo.Text.Trim().Length > 0)
            {
                Hashtable sqlParams2 = new Hashtable();
                sqlParams2["@chassisNo"] = txtChassisNo.Text.ToString();

                fromdb = db.checkVehicleExist(sqlParams2);

                if (fromdb != "False")
                {
                    if (fromdb == "Archive")
                    {
                        lblCommonMsg.Text = "The Chassis No already move to archive database.";
                        ModalPopupExtender_Msg.Show();

                        return;
                    }
                }
                else
                {
                    gv.DataSource = null;
                    gv.DataBind();

                    lblCommonMsg.Text = "No Record Found.";
                    ModalPopupExtender_Msg.Show();

                    return;
                }
            }

            DataTable dt = db.getAllWipInfo(from, Lot, Model, Color, BasicCode, ProdOrder, FromStation, chassisno, OpAction, LineId, Shift, engineno, ReworkName);//BO Jun add OpAction

            if (dt.Rows.Count > 0)
            {
                gv.DataSource = dt;
                gv.DataBind();
            }
        }

        //protected void gridView_Sorting(object sender, GridViewSortEventArgs e)
        //{
        //    DataTable dataTable = gv.DataSource as DataTable;

        //    if (dataTable != null)
        //    {
        //        DataView dataView = new DataView(dataTable);
        //        dataView.Sort = e.SortExpression + " " + ConvertSortDirectionToSql(e.SortDirection);

        //        gv.DataSource = dataView;
        //        gv.DataBind();
        //    }
        //}

        protected void gridView_Sorting1(object sender, GridViewSortEventArgs e)
        {
            DataTable dtresult = new DataTable();

            DateTime from = DateTime.Now;//DateTime.Parse(Session["from"].ToString(), culture, System.Globalization.DateTimeStyles.AssumeLocal);//DateTime.Parse("" + Session["from"]);
            string Lot = "";
            string Model = "";
            string Color = "";
            string BasicCode = "";
            string ProdOrder = "";
            string LineId = "";
            string FromStation = "";
            string ChassisNo = "";
            string EngineNo = "";
            string OpAction = "";
            string Shift = "";
            string ReworkName = "";

            #region //selection

            try
            {
                from = DateTime.Parse(Session["from"].ToString(), culture, System.Globalization.DateTimeStyles.AssumeLocal);//DateTime.Parse("" + Session["from"]);
                from = from.AddDays(1).AddTicks(-1);

                Lot = "" + Session["Lot"];
                Model = "" + Session["Model"];
                Color = "" + Session["Color"];
                BasicCode = "" + Session["BasicCode"];
                ProdOrder = "" + Session["ProdOrder"];
                LineId = "" + Session["LineId"];
                FromStation = "" + Session["FromStation"];
                ChassisNo = "" + Session["ChassisNo"] + "";
                EngineNo = "" + Session["EngineNo"] + "";
                ReworkName = "" + Session["ReworkStation"] + "";
                OpAction = "" + Session["OpAction"] + ""; //Bo Jun add OpAction
                Shift = "" + Session["Shift"] + "";
            }
            catch (Exception ex)
            {
                from = DateTime.Parse(txtFrDate.Text.Trim(), culture, System.Globalization.DateTimeStyles.AssumeLocal);
                from = from.AddDays(1).AddTicks(-1);

                Lot = "";
                for (int i = 0; i < ddlLotNo.Items.Count; i++)
                {
                    if (ddlLotNo.Items[i].Selected == true)
                    {
                        if (Lot.Length == 0)
                        {
                            Lot += "'" + ddlLotNo.Items[i].Value + "'";
                        }
                        else
                        {
                            Lot += ",'" + ddlLotNo.Items[i].Value + "'";
                        }
                    }
                }

                Model = "";
                for (int i = 0; i < ddlMatlGrp.Items.Count; i++)
                {
                    if (ddlMatlGrp.Items[i].Selected == true)
                    {
                        if (Model.Length == 0)
                        {
                            Model += "'" + ddlMatlGrp.Items[i].Value + "'";
                        }
                        else
                        {
                            Model += ",'" + ddlMatlGrp.Items[i].Value + "'";
                        }
                    }
                }

                Color = "";
                for (int i = 0; i < ddlColorCode.Items.Count; i++)
                {
                    if (ddlColorCode.Items[i].Selected == true)
                    {
                        if (Color.Length == 0)
                        {
                            Color += "'" + ddlColorCode.Items[i].Value + "'";
                        }
                        else
                        {
                            Color += ",'" + ddlColorCode.Items[i].Value + "'";
                        }
                    }
                }

                BasicCode = "";
                for (int i = 0; i < ddlMatlGrpDesc.Items.Count; i++)
                {
                    if (ddlMatlGrpDesc.Items[i].Selected == true)
                    {
                        if (BasicCode.Length == 0)
                        {
                            BasicCode += "'" + ddlMatlGrpDesc.Items[i].Value + "'";
                        }
                        else
                        {
                            BasicCode += ",'" + ddlMatlGrpDesc.Items[i].Value + "'";
                        }
                    }
                }

                Shift = "";
                for (int i = 0; i < ddlShift.Items.Count; i++)
                {
                    if (ddlShift.Items[i].Selected == true)
                    {
                        Shift = ddlShift.Items[i].Value;
                    }
                }

                ReworkName = "";
                for (int i = 0; i < ddlReworkStation.Items.Count; i++)
                {
                    if (ddlReworkStation.Items[i].Selected == true)
                    {
                        if (ReworkName.Length == 0)
                        {
                            ReworkName += "'" + ddlReworkStation.Items[i].Text + "'";
                        }
                        else
                        {
                            ReworkName += ",'" + ddlReworkStation.Items[i].Text + "'";
                        }
                    }
                }

                FromStation = "";
                for (int i = 0; i < ddlStation.Items.Count; i++)
                {
                    if (ddlStation.Items[i].Selected == true)
                    {
                        if (FromStation.Length == 0)
                        {
                            FromStation += "'" + ddlStation.Items[i].Value + "'";
                        }
                        else
                        {
                            FromStation += ",'" + ddlStation.Items[i].Value + "'";
                        }
                    }
                }

                //ProdOrder = "%" + ddlProdOrder.SelectedValue.ToString() + "%"; //txtProdOrder.Text.Trim() + "%"; (10/6/19)
                ProdOrder = MultipleSelectToString(ddlProdOrder);
                LineId = MultipleSelectToString(ddlLine);

                LineId = ddlLine.SelectedValue.ToString();

                //string[] splitFromStation = ddlStation.SelectedValue.ToString().Split('-');
                //string[] splitToStation = ddlToStation.SelectedValue.ToString().Split('-');

                //FromStation = MultipleSelectToString(ddlStation);//splitFromStation[1];
                // ToStation = splitToStation[1];
                ChassisNo = "%" + txtChassisNo.Text.ToString() + "%";
                EngineNo = "%" + txtEngine.Text.ToString() + "%";
                OpAction = "";
                for (int i = 0; i < ddlOpAction.Items.Count; i++)
                {
                    if (ddlOpAction.Items[i].Selected == true)
                    {
                        if (OpAction.Length == 0)
                        {
                            OpAction += "'" + ddlOpAction.Items[i].Value + "'";
                        }
                        else
                        {
                            OpAction += ",'" + ddlOpAction.Items[i].Value + "'";
                        }
                    }
                }
            }

            #endregion //selection

            if (txtChassisNo.Text.Trim().Length > 0)
            {
                Hashtable sqlParams2 = new Hashtable();
                sqlParams2["@chassisNo"] = txtChassisNo.Text.ToString();

                fromdb = db.checkVehicleExist(sqlParams2);

                if (fromdb != "False")
                {
                    if (fromdb == "Archive")
                    {
                        lblCommonMsg.Text = "The Chassis No already move to archive database.";
                        ModalPopupExtender_Msg.Show();

                        return;
                    }
                }
                else
                {
                    gv.DataSource = null;
                    gv.DataBind();

                    lblCommonMsg.Text = "No Record Found.";
                    ModalPopupExtender_Msg.Show();

                    return;
                }
            }

            dtresult = db.getAllWipInfo(from, Lot, Model, Color, BasicCode, ProdOrder, FromStation, ChassisNo, OpAction, LineId, Shift, EngineNo, ReworkName);
            //DataView dataTable = gv.DataSource as DataView;
            //DataTable dtSortTable = dataTable.Table;
            if (dtresult != null)
            {
                DataView dataView = new DataView(dtresult);
                string m = gv.Attributes["CurrentSortDir"];
                dataView.Sort = e.SortExpression + " " + STRConvertSortDirectionToSql(m);

                gv.DataSource = dataView;
                gv.DataBind();
            }
        }

        private string STRConvertSortDirectionToSql(string sortDirection)
        {
            string newSortDirection = String.Empty;

            switch (sortDirection)
            {
                case "ASC":
                    newSortDirection = "DESC";
                    gv.Attributes["CurrentSortDir"] = newSortDirection;
                    break;

                case "DESC":
                    newSortDirection = "ASC";
                    gv.Attributes["CurrentSortDir"] = newSortDirection;
                    break;
            }

            return newSortDirection;
        }



        #endregion Grid View

        protected void ddlChassisNo_SelectedIndexChanged(object sender, EventArgs e)
        {
        }



        //Comment by KCC 20170221
        /*protected void ddlProdOrder_SelectedIndexChanged(object sender, EventArgs e)
        {
            generateddlChassis();
        }*/

        [System.Web.Script.Services.ScriptMethod()]
        [System.Web.Services.WebMethod]
        public static IEnumerable<string> SearchChassis(string prefixText, int count)
        {
            DataTable dt = new DataTable();
            SQLWipReport dbAccessor = new SQLWipReport();

            //Hashtable sqlParams = new Hashtable();
            //sqlParams["@chassis"] = prefixText + "%";

            dt = dbAccessor.getAllChassisForAutoComplete(prefixText);

            return dt.AsEnumerable().Select(x => x["CHASSIS_NO"].ToString());
        }

        [System.Web.Script.Services.ScriptMethod()]
        [System.Web.Services.WebMethod]
        public static IEnumerable<string> SearchEngine(string prefixText, int count)
        {
            DataTable dt = new DataTable();
            SQLWipReport sqldb = new SQLWipReport();

            dt = sqldb.getAllEngineForAutoComplete(prefixText);

            return dt.AsEnumerable().Select(x => x["ENGINE_NO"].ToString());
        }

        protected String MultipleSelectToString(ListBox listBox)
        {
            string result = "";
            for (int i = 0; i < listBox.Items.Count; i++)
            {
                if (listBox.Items[i].Selected == true)
                {
                    if (result.Length == 0)
                    {
                        result += "'" + listBox.Items[i].Value + "'";
                    }
                    else
                    {
                        result += ",'" + listBox.Items[i].Value + "'";
                    }
                }
            }
            return result;
        }

        private List<string> GetAllListBoxItems(ListBox listBox)
        {
            List<string> items = new List<string>();

            for (int i = 0; i < listBox.Items.Count; i++)
            {
                items.Add(listBox.Items[i].Value);
            }

            return items;
        }

        private List<string> GetSelectedListBoxItems(ListBox listBox)
        {
            List<string> items = new List<string>();

            for (int i = 0; i < listBox.Items.Count; i++)
            {
                if (listBox.Items[i].Selected == true)
                {
                    items.Add(listBox.Items[i].Value);
                }
            }

            return items;
        }

        private string GetSingleSelectedListBoxItem(ListBox listBox)
        {
            string item = string.Empty;

            for (int i = 0; i < listBox.Items.Count; i++)
            {
                if (listBox.Items[i].Selected == true)
                {
                    item = listBox.Items[i].Value;
                }
            }

            return item;
        }
    }
}