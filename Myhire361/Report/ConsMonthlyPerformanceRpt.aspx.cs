﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.IO;
using System.Text;
using System.Configuration;
using System.Data;

public partial class Report_ConsMonthlyPerformanceRpt : BaseClass
{
    DailyWorkSummaryBAL dws;
    Search srch;
    int UserId, URole;
    int MyTimeSpan = 0;
    DataTable dt;
    protected void Page_Load(object sender, EventArgs e)
    {
        UserId = Convert.ToInt32(Session["UserId"]);
        URole = Convert.ToInt32(Session["UserRole"]);
        MyTimeSpan = Convert.ToInt32(ConfigurationManager.AppSettings["TimeSpan"]);
        if (!IsPostBack)
        {
            txtMonth.Text = DateTime.Now.AddMinutes(MyTimeSpan).ToString("MMM-yyyy");
            BindGrid();
        }
    }


    protected void btnSearch_OnClick(object sender, EventArgs e)
    {
        BindGrid();
    }

    protected void BindGrid()
    {
        dws = new DailyWorkSummaryBAL();

        try
        {

            dws.DDate = txtMonth.Text;
            dt = getResultInDt();
            DataView dv = new DataView(dt);
            if (ViewState["SortExpr"] != null)
                dv.Sort = (string)ViewState["SortExpr"] + " " + (string)ViewState["SortDir"];
            gdvDWS.DataSource = dv;
            gdvDWS.DataBind();

        }
        catch (Exception e)
        {
        }
        finally
        {
            dws = null;
        }
    }
    protected DataTable getResultInDt()
    {
        dt = new DataTable();
        dt = SearchMonthlyWorkSum();
        return dt;
    }
    protected void gdvDWS_Sorting(object sender, GridViewSortEventArgs e)
    {
        ViewState["SortExpr"] = e.SortExpression;
        if (ViewState["SortDir"] != null)
            e.SortDirection = (string)ViewState["SortDir"] == "ASC" ? SortDirection.Descending : SortDirection.Ascending;
        ViewState["SortDir"] = e.SortDirection == SortDirection.Ascending ? "ASC" : "DESC";
        BindGrid();
    }


    public DataTable SearchMonthlyWorkSum()
    {
        dws = new DailyWorkSummaryBAL();
        DataTable dt = new DataTable();
        dws.DDate = txtMonth.Text;
        String[] MonthYear = txtMonth.Text.Split('-');
        dws.MMonth = MonthYear[0];
        dws.MYear = MonthYear[1];

        dt = dws.GetCandidateStatusForMWPer();
        string a = "";

        if (dt.Rows.Count > 0)
        {
            for (int i = dt.Rows.Count - 1; i >= 0; i--)
            {
                a = a + (dt.Rows[i]["CandidateStatus"].ToString() + "],[");

            }
            a = a.Substring(0, a.Length - 2);

            srch = new Search();
            StringBuilder sb = new StringBuilder();
            sb.Append("select * from ( select u.USR_Name as Consultant ,r.RRNumber,cd.Client_Name, r.Job_Profile as Role,CandidateStatus,CandidateNo ");
            sb.Append(" from MonthlyWorkSummaryPerformance as dl ");
            sb.Append(" inner join UserDetail as u on u.USR_ID=dl.UserId ");
            sb.Append(" inner join RecruitmentRequest as r on r.Request_Id=dl.RequestId ");
            sb.Append(" inner join ClientDetail as cd on r.Client_Id=cd.Client_Id ");
            sb.Append(" where dl.MMonth ='" + dws.MMonth + "' and   dl.MYear ='" + dws.MYear + "' ");
            sb.Append("   ) as t  PIVOT");
            sb.Append(" (sum(CandidateNo) FOR CandidateStatus IN ([" + a + ")) as pvt");
            string query = sb.ToString();
            return srch.SearchRecord(query).Tables[0];
        }
        else
        {
            return dt;
        }

    }

    protected void lbtnDownload_Click(object sender, EventArgs e)
    {
        try
        {

            try
            {

                dt = getResultInDt();
            }
            catch (Exception ex) { }
            finally { }

            string filename = "ConsultantMonthlyPerformance(" + DateTime.Now.AddMinutes(MyTimeSpan).ToString("dd-MMM-yyyy") + ").xls";
            string attachment = "attachment; filename=" + filename;
            Response.ClearContent();
            Response.AddHeader("content-disposition", attachment);
            Response.ContentType = "application/ms-excel";
            DataGrid dg = new DataGrid();
            dg.DataSource = dt;
            dg.DataBind();
            StringWriter stw = new StringWriter();
            HtmlTextWriter htextw = new HtmlTextWriter(stw);
            dg.RenderControl(htextw);
            Response.Write(stw.ToString());
            Response.End();

        }
        catch (Exception ex)
        {
        }
        finally
        { }
    }


}
