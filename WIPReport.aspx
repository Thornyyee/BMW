<%@ Page Title="" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="WIPReport.aspx.cs" Inherits="VPS_WEB.Report.WIPReport" %>

<%@ Register Assembly="AjaxControlToolkit" Namespace="AjaxControlToolkit" TagPrefix="ajax" %>
<asp:Content ID="Content1" ContentPlaceHolderID="styles" runat="server">
    <script type="text/javascript">
        function ItemSelected(sender, args) {
            __doPostBack(sender.get_element().name, "");
        }
    </script>
    <style type="text/css">
        .AutoExtender {
            /*font-size: 12px;
        color: #000;
        padding: 3px 5px;
        border: 1px solid #999;
        background: #fff;
        width: auto;
        float: left;
        z-index: 9999999999;
        position:absolute;
        margin-left:0px;
        list-style: none;
        font-weight: bold;*/
            visibility: hidden;
            margin: 0px 0px 0px 0px !important;
            background-color: #FFFFFF;
            color: windowtext;
            border: buttonshadow;
            border-width: 1px;
            border-style: solid;
            cursor: default;
            overflow: auto;
            height: 200px;
            text-align: left;
            list-style-type: none;
        }

        .auto-style1 {
            text-align: right;
            height: 30px;
        }

        .auto-style2 {
            text-align: left;
            height: 30px;
        }
    </style>
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="body" runat="server">
    <%--<asp:ScriptManager EnablePartialRendering="true" ID="ScriptManager1" runat="server"></asp:ScriptManager>--%>
    <%--<div class="container">--%>
    <table class="table table-hover">
        <tr>
            <td class="Jaey-page-title">WIP Report</td>
        </tr>
    </table>
    <hr class="Jaey-animated-hr" />
    <center>
        <table class="Jaey-table-cell-padding">
            <tbody>
                <tr>
                    <td class="Jaey-text-align-right">Date: </td>
                    <td class="Jaey-text-align-left">
                        <asp:TextBox ID="txtFrDate" runat="server" CssClass="form-control datepicker" Width="200px" placeholder="Date" />
                        <%--&nbsp; - &nbsp;
                      <asp:TextBox ID="txtToDate" runat="server" CssClass="form-control datepicker" Width="140px" placeholder="To Date"/>--%>
                    </td>
                    <td class="Jaey-text-align-right">Shift:</td>
                    <td>
                        <asp:ListBox ID="ddlShift" runat="server" CssClass="form-control" Width="200px"></asp:ListBox>
                    </td>
                </tr>
                <tr>
                    <td class="Jaey-text-align-right">Lot: </td>
                    <td class="Jaey-text-align-left">
                        <asp:ListBox ID="ddlLotNo" runat="server" class="form-control" SelectionMode="Multiple" Width="200px" AutoPostBack="true" OnSelectedIndexChanged="ddlLot_SelectedIndexChanged"></asp:ListBox>
                    </td>
                    <td class="Jaey-text-align-right">Material Group: </td>
                    <td class="Jaey-text-align-left">
                        <asp:ListBox ID="ddlMatlGrp" runat="server" class="form-control" SelectionMode="Multiple" Width="200px" AutoPostBack="true" OnSelectedIndexChanged="ddlModelCode_SelectedIndexChanged"></asp:ListBox>
                    </td>

                </tr>
                <tr>
                    <td class="Jaey-text-align-right">Color: </td>
                    <td class="Jaey-text-align-left">
                        <asp:ListBox ID="ddlColorCode" runat="server" class="form-control" SelectionMode="Multiple" Width="200px"></asp:ListBox>
                    </td>
                    <td class="Jaey-text-align-right">Model: </td>
                    <td class="Jaey-text-align-left">
                        <asp:ListBox ID="ddlMatlGrpDesc" runat="server" class="form-control" SelectionMode="Multiple" Width="200px"></asp:ListBox>
                    </td>

                </tr>
                <tr>
                    <td class="Jaey-text-align-right">OP. Action: </td>
                    <td class="Jaey-text-align-left">
                        <asp:ListBox ID="ddlOpAction" runat="server" class="form-control" SelectionMode="Multiple" Width="200px"></asp:ListBox>
                    </td>
                    <td class="Jaey-text-align-right">Prod Order: </td>
                    <td class="Jaey-text-align-left">
                        <%--<asp:DropDownList ID="ddlProdOrder" runat="server" class="form-control" Width="140px" ></asp:DropDownList>--%>
                        <asp:ListBox ID="ddlProdOrder" runat="server" class="form-control" SelectionMode="Multiple" Width="200px"></asp:ListBox>
                        <%--10/6/19--%>
                    </td>

                </tr>
                <tr>
                    <td class="Jaey-text-align-right">Chassis No: </td>
                    <td class="Jaey-text-align-left">
                        <%--<asp:DropDownList ID="ddlChassisNo" runat="server" class="form-control" Width="200px" onmousedown="this.size=10" onfocusout="this.size=1" OnSelectedIndexChanged="ddlChassisNo_SelectedIndexChanged" ></asp:DropDownList>--%>
                        <asp:TextBox ID="txtChassisNo" runat="server" class="form-control" Width="200px" ClientIDMode="Static"></asp:TextBox>
                        <ajax:AutoCompleteExtender ServiceMethod="SearchChassis" OnClientItemSelected="ItemSelected" CompletionListCssClass="AutoExtender"
                            MinimumPrefixLength="1"
                            CompletionInterval="100" EnableCaching="false" CompletionSetCount="10"
                            TargetControlID="txtChassisNo"
                            ID="AutoCompleteExtender1" runat="server" FirstRowSelected="false">
                        </ajax:AutoCompleteExtender>
                    </td>
                    <td class="Jaey-text-align-right">Line: </td>
                    <td class="Jaey-text-align-left">
                        <asp:ListBox ID="ddlLine" runat="server" class="form-control" SelectionMode="Multiple" Width="200px" OnSelectedIndexChanged="ddlLine_SelectedIndexChanged" AutoPostBack="true"></asp:ListBox>
                        <%--10/6/19--%>
                    </td>
                </tr>
                <tr>
                    <td class="Jaey-text-align-right">Station: </td>
                    <td class="Jaey-text-align-left">
                        <%--<asp:DropDownList ID="ddlFromStation" runat="server" class="form-control" Width="200px"></asp:DropDownList>--%>
                        <asp:ListBox ID="ddlStation" runat="server" class="form-control" SelectionMode="Multiple" Width="200px"></asp:ListBox>
                        <%--10/6/19--%>
                    </td>
                    <%--                    <td class="Jaey-text-align-right">To Station: </td>
                    <td class="Jaey-text-align-left">
                        <asp:DropDownList ID="ddlToStation" runat="server" class="form-control" Width="200px"></asp:DropDownList>
                    </td>--%>
                    <td class="Jaey-text-align-right">Rework Station: </td>
                    <td class="Jaey-text-align-left">
                        <asp:ListBox ID="ddlReworkStation" runat="server" class="form-control" SelectionMode="Multiple" Width="200px"></asp:ListBox>
                    </td>
                </tr>
                <tr>
                    <td class="Jaey-text-align-right">Engine No: </td>
                    <td class="Jaey-text-align-left">
                        <asp:Panel runat="server" DefaultButton="btnInquiry">
                            <asp:TextBox ID="txtEngine" runat="server" Width="200px" class="form-control" ClientIDMode="Static"></asp:TextBox>
                            <ajax:AutoCompleteExtender ServiceMethod="SearchEngine" OnClientItemSelected="ItemSelected" CompletionListCssClass="AutoExtender"
                                MinimumPrefixLength="1"
                                CompletionInterval="100" EnableCaching="false" CompletionSetCount="10"
                                TargetControlID="txtEngine"
                                ID="AutoCompleteExtender2" runat="server" FirstRowSelected="false">
                            </ajax:AutoCompleteExtender>
                        </asp:Panel>
                    </td>
                    <td class="Jaey-text-align-right">Aging: </td>
                    <td class="Jaey-text-align-left">
                        <asp:ListBox ID="ddlAging" runat="server" CssClass="form-control" Width="200px"></asp:ListBox>
                    </td>
                </tr>
            </tbody>
        </table>
        <br />
        <div>
            <asp:LinkButton ID="btnInquiry" runat="server" CssClass="btn btn-primary" OnClick="btnInquiry_Click">
                <i class="glyphicon glyphicon-search"></i>
                &nbsp;View
            </asp:LinkButton>&nbsp;
            <asp:LinkButton ID="btnExcel" runat="server" CssClass="btn btn-primary" OnClick="btnExcel_Click">
                &nbsp;Excel
            </asp:LinkButton>&nbsp;
        </div>
    </center>
    <br />

    <div style="width: 100%; overflow: auto; box-sizing: border-box;">
        <div style="width: 40%; margin-left: auto; margin-right: auto; align-self: center; box-sizing: border-box">
            <asp:GridView ID="GridViewWIP" runat="server" HorizontalAlign="Center" CssClass="table table-bordered table-condensed table-striped table-hover"
                AutoGenerateColumns="False" EmptyDataText="No records" OnRowCreated="GridViewWIP_RowCreated">
                <Columns>
                </Columns>
                <PagerSettings Mode="NumericFirstLast" />
                <EmptyDataRowStyle CssClass="table table-bordered" />
                <HeaderStyle BackColor="#EEEEEE" HorizontalAlign="Center"></HeaderStyle>
                <PagerStyle HorizontalAlign="Center" CssClass="pagination-ys" />
                <PagerSettings Mode="NumericFirstLast" FirstPageText="First" LastPageText="Last" />
            </asp:GridView>
        </div>
    </div>
    <br />
    <br />
    <asp:GridView ID="gv" runat="server" HorizontalAlign="Justify" CssClass="table table-hover table-bordered"
        AutoGenerateColumns="False" EmptyDataText="No records" PageSize="20" AllowPaging="True" AllowSorting="True"
        OnPageIndexChanging="gridView_PageIndexChanging" OnSorting="gridView_Sorting" CurrentSortDir="ASC" OnRowDataBound="gridView_RowDataBound">
        <Columns>
            <%--            <asp:BoundField DataField="BODY_NO" HeaderText="Body No." SortExpression="BODY_NO" />
            <asp:BoundField DataField="CHASSIS_NO" HeaderText="Chassis No." SortExpression="CHASSIS_NO" />
            <asp:BoundField DataField="PROD_ORDER" HeaderText="Prod. Order" SortExpression="PROD_ORDER" />
            <asp:BoundField DataField="PROD_SEQ" HeaderText="Prod. Seq" SortExpression="PROD_SEQ" />
            <asp:BoundField DataField="MSC" HeaderText="MSC" SortExpression="MSC" />
            <asp:BoundField DataField="MODEL" HeaderText="Model" SortExpression="MODEL" />
            <asp:BoundField DataField="MATL_GROUP" HeaderText="Matl Group" SortExpression="MATL_GROUP" />
            <asp:BoundField DataField="ENGINE_NO" HeaderText="Engine No." SortExpression="ENGINE_NO" />
            <asp:BoundField DataField="LOT" HeaderText="Lot No." SortExpression="LOT" />
            <asp:BoundField DataField="COLOR" HeaderText="Color" SortExpression="COLOR" />
            <asp:BoundField DataField="AG_INTERIOR_COLOR" HeaderText="AG Interior Color" SortExpression="AG_INTERIOR_COLOR" />
            <asp:BoundField DataField="AG_EXTERIOR_COLOR" HeaderText="AG Exterior Color" SortExpression="AG_EXTERIOR_COLOR" />
            <asp:BoundField DataField="Station" HeaderText="Station" SortExpression="Station" />
            <asp:BoundField DataField="REWORK" HeaderText="Rework Station" SortExpression="REWORK" />
            <asp:BoundField DataField="F2_CASCADE" HeaderText="F2 Cascade" SortExpression="F2_CASCADE" />
            <asp:BoundField DataField="SCANNED_DATE_TIME" HeaderText="Scanned Date Time" SortExpression="SCANNED_DATE_TIME" DataFormatString="{0:dd/MM/yyyy hh:mm:ss tt}" />
            <asp:BoundField DataField="OPERATION_ACTION" HeaderText="Operation Action" SortExpression="OPERATION_ACTION" />
            <asp:BoundField DataField="SAP_ACTION_DATETIME" HeaderText="SAP Date Time" SortExpression="SAP_ACTION_DATETIME" DataFormatString="{0:dd/MM/yyyy hh:mm:ss tt}" />
            <asp:BoundField DataField="BLOCK_FLAG" HeaderText="Block Status" SortExpression="BLOCK_FLAG" />--%>
            <asp:TemplateField HeaderText="No." ItemStyle-Width="50">
                <ItemTemplate><%# Container.DataItemIndex + 1 %></ItemTemplate>
                <ItemStyle Width="50px"></ItemStyle>
            </asp:TemplateField>
            <asp:BoundField DataField="VIN_NO" HeaderText="Body No." SortExpression="VIN_NO" />
            <asp:BoundField DataField="CHASSIS_NO" HeaderText="Chassis No." SortExpression="CHASSIS_NO" />
            <asp:BoundField DataField="ORDER" HeaderText="Prod. Order" SortExpression="ORDER" />
            <asp:BoundField DataField="SEQ_IN_LOT" HeaderText="Prod. Seq" SortExpression="SEQ_IN_LOT" />
            <asp:BoundField DataField="MATL_GROUP_DESC" HeaderText="MSC" SortExpression="MATL_GROUP_DESC" />
            <asp:BoundField DataField="MODEL_ID" HeaderText="Model" SortExpression="MODEL_ID" />
            <asp:BoundField DataField="MATL_GROUP" HeaderText="Matl Group" SortExpression="MATL_GROUP" />
            <asp:BoundField DataField="ENGINE_NO" HeaderText="Engine No." SortExpression="ENGINE_NO" />
            <asp:BoundField DataField="WBS_ELEM" HeaderText="Lot No." SortExpression="WBS_ELEM" />
            <asp:BoundField DataField="COLOR_CODE" HeaderText="Color" SortExpression="COLOR_CODE" />
            <asp:BoundField DataField="DESTINATION" HeaderText="Destination" SortExpression="DESTINATION" />
            <asp:BoundField DataField="AG_INTERIOR_COLOR" HeaderText="AG Interior Color" SortExpression="AG_INTERIOR_COLOR" />
            <asp:BoundField DataField="AG_EXTERIOR_COLOR" HeaderText="AG Exterior Color" SortExpression="AG_EXTERIOR_COLOR" />
            <asp:BoundField DataField="STATION_NAME" HeaderText="Station" SortExpression="STATION_NAME" />
            <asp:BoundField DataField="REWORK" HeaderText="Rework Station" SortExpression="REWORK" />
            <asp:BoundField DataField="LINEOFF_DATETIME" HeaderText="Line Off Date Time" SortExpression="LINEOFF_DATETIME" DataFormatString="{0:dd/MM/yyyy hh:mm:ss tt}" />
            <asp:BoundField DataField="F2_CASCADE" HeaderText="F2 Cascade" SortExpression="F2_CASCADE" />
            <asp:BoundField DataField="UPDATE_DATETIME" HeaderText="Scanned Date Time" SortExpression="UPDATE_DATETIME" DataFormatString="{0:dd/MM/yyyy hh:mm:ss tt}" />
            <asp:BoundField DataField="OPERATION_ACTION" HeaderText="Operation Action" SortExpression="OPERATION_ACTION" />
            <asp:BoundField DataField="SAP_DATETIME" HeaderText="SAP Date Time" SortExpression="SAP_DATETIME" DataFormatString="{0:dd/MM/yyyy hh:mm:ss tt}" />
            <asp:BoundField DataField="BLOCK_FLAG" HeaderText="Block Status" SortExpression="BLOCK_FLAG" />
            <asp:BoundField DataField="MATCHED_DATETIME" DataFormatString="{0:dd/MM/yyyy hh:mm:ss tt}" HeaderText="Matching Date Time" SortExpression="MATCHED_DATETIME" />
            <asp:BoundField DataField="AGING" HeaderText="Aging" SortExpression="Aging" />
        </Columns>
        <PagerSettings Mode="NumericFirstLast" />
        <EmptyDataRowStyle CssClass="table table-bordered" />
        <HeaderStyle BackColor="#EEEEEE"></HeaderStyle>
        <PagerStyle HorizontalAlign="Center" />
        <PagerSettings Mode="NumericFirstLast" FirstPageText="First" LastPageText="Last" />
    </asp:GridView>
    <br />
    <br />
    <br />
    <%--</div>--%>
    <%-----------------------------------------------------------Pop Out Message------------------------------------------------------------------------%>
    <asp:LinkButton ID="lbtnFake4" runat="server"></asp:LinkButton>
    <ajax:ModalPopupExtender ID="ModalPopupExtender_Msg" TargetControlID="lbtnFake4" PopupControlID="panel_popup_msg"
        CancelControlID="btn" runat="server" BackgroundCssClass="modalBackground">
    </ajax:ModalPopupExtender>
    <asp:Panel ID="panel_popup_msg" runat="server" CssClass="modalPopup" Style="display: none">
        <asp:UpdatePanel ID="updatepanel_msg" runat="server">
            <ContentTemplate>
                <div class="header">
                    <h3><b>Warning</b></h3>
                    <asp:Label ID="lblCommonMsg" runat="server" Text="Label"></asp:Label>
                </div>
                <br />
                <br />
            </ContentTemplate>
        </asp:UpdatePanel>
        <div align="right" class="footer">
            <asp:LinkButton ID="btn" runat="server" CssClass="btn btn-primary">
                <i class="glyphicon glyphicon-ok"></i>
                &nbsp;OK
            </asp:LinkButton>
        </div>
    </asp:Panel>
    <%-----------------------------------------------------------End Pop Out Message--------------------------------------------------------------------%>
</asp:Content>
<asp:Content ID="Content3" ContentPlaceHolderID="script" runat="server">
    <script type="text/javascript">
        window.onload = function () { pageLoad(); }
        function pageLoad(sender, args) {
            var dp1 = $('#<%=txtFrDate.ClientID%>');
            <%--var dp2 = $('#<%=txtToDate.ClientID%>');--%>

            dp1.datepicker({
                changeMonth: true,
                changeYear: true,
                format: "dd-mm-yyyy",
                language: "tr"
            }).on('changeDate', function (ev) {
                $(this).blur();
                $(this).datepicker('hide');
            });

            //dp2.datepicker({
            //    changeMonth: true,
            //    changeYear: true,
            //    format: "dd-mm-yyyy",
            //    language: "tr"
            //}).on('changeDate', function (ev) {
            //    $(this).blur();
            //    $(this).datepicker('hide');
            //});
        }

        $(document).ready(function () {
            $(<%=ddlLotNo.ClientID%>).SumoSelect({ selectAll: true, search: true, searchText: 'Enter here.' });
            $(<%=ddlMatlGrp.ClientID%>).SumoSelect({ selectAll: true, search: true, searchText: 'Enter here.' });
            $(<%=ddlMatlGrpDesc.ClientID%>).SumoSelect({ selectAll: true, search: true, searchText: 'Enter here.' });
            $(<%=ddlColorCode.ClientID%>).SumoSelect({ selectAll: true, search: true, searchText: 'Enter here.' });
            $(<%=ddlOpAction.ClientID%>).SumoSelect({ selectAll: true, search: true, searchText: 'Enter here.' });
            $(<%=ddlProdOrder.ClientID%>).SumoSelect({ selectAll: true, search: true, searchText: 'Enter here.' });
            $(<%=ddlLine.ClientID%>).SumoSelect({ selectAll: true, search: true, searchText: 'Enter here.' });
            $(<%=ddlStation.ClientID%>).SumoSelect({ selectAll: true, search: true, searchText: 'Enter here.' });
            $(<%=ddlReworkStation.ClientID%>).SumoSelect({ selectAll: true, search: true, searchText: 'Enter here.' });

            $('#<%=ddlShift.ClientID%>').select2({
                placeholder: "Please Select",
                allowClear: false
            });

            $('#<%=ddlAging.ClientID%>').select2({
                placeholder: "Please Select",
                allowClear: false
            });

             <%--   $('#<%=ddlChassis.ClientID%>').select2({
                        placeholder: "Please Select",
                        allowClear: false
                    });--%>

        });
    </script>
</asp:Content>
