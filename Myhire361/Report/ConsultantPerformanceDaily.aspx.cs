﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Data;
using System.Text;
using System.Configuration;
using System.IO;

public partial class Report_ConsultantPerformanceDaily : BaseClass
{
    ClientBAL clientbal;
    
    LoginBAL loginbal;
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
            txtMonth.Text = DateTime.Now.AddMinutes(MyTimeSpan).ToString("dd-MMM-yyyy");
            BindGrid();
            BindClient();
            BindConsultant();
        }
    }


    protected void btnSearch_OnClick(object sender, EventArgs e)
    {
        BindGrid();
    }
    protected void gdvDWS_RowCreated(object sender, GridViewRowEventArgs e)
    {

        e.Row.Cells[0].Visible = false;
        e.Row.Cells[1].Visible = false;
        e.Row.Cells[2].Visible = false;
      //  e.Row.Cells[3].Visible = false;
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


    private void BindClient()
    {
        clientbal = new ClientBAL();
        try
        {
            ListItem li = new ListItem("All", "0");
            ddlClientName.Items.Clear();
            ddlClientName.Items.Add(li);
            ddlClientName.DataSource = clientbal.FillClient();
            ddlClientName.DataTextField = "Client_Name";
            ddlClientName.DataValueField = "Client_Id";
            ddlClientName.DataBind();
        }
        finally
        {
            clientbal = null;
        }
    }
    protected void BindConsultant()
    {
        LoginBAL loginbal = new LoginBAL();
        try
        {
            ListItem li = new ListItem("Select", "0");
            ddlConsultant.Items.Clear();
            ddlConsultant.Items.Add(li);
            if (URole == 1)
            {
                ddlConsultant.DataSource = loginbal.FillUser();
            }
            else if (URole == 9)
            {
                ddlConsultant.DataSource = loginbal.FillUser();
            }
            else if (URole == 3 || URole == 8)
            {
                loginbal.Usr_Id = UserId;
                ddlConsultant.DataSource = loginbal.FillUserByUserId();
            }
            ddlConsultant.DataTextField = "USR_Name";
            ddlConsultant.DataValueField = "USR_ID";
            ddlConsultant.DataBind();
        }
        finally
        {
            loginbal = null;
        }
    }
    protected DataTable SearchMonthlyWorkSum()
    {
        Search srch = new Search();
        string query = "", subquery = "";
   
        query = " select * from (select * from (Select rr.Consultant_Id,cd.Client_Id, " +
                "cast(dl.CreationDateNew as date) AS CreationDateNew," +
               " u.USR_Name as Consultant ,cd.Client_Name,dl.RRCandStatusReportId, " +
                " case  dl.Candidate_Status " +
                " when 'CV Shared With Client' then 'CV Shared' " +
                 " when 'Interview Done' then 'Interviews' " +
                 " when 'Shortlisted' then 'Shortlisted' " +
                "  when 'Offered' then 'Offered' " +
                "  when 'Offer Accepted' then 'Accepted' " +
                "  when 'Joined' then 'Joined' " +
                "  Else dl.Candidate_Status  " +
                "  end as Candidate_Status " +
                 " from RRCandidateStatusReport as dl  " +
                 "  left join RecruitmentRequest as r on r.Request_Id=dl.Request_Id " +
               "   left join RRCandidateRelation as rr on rr.RRCandidate_Id=dl.RRCandidate_Id " +
               "   left join UserDetail as u on u.USR_ID=rr.Consultant_Id " +
               "   inner join ClientDetail as cd on r.Client_Id=cd.Client_Id " +

               "   ) as t  PIVOT " +
               "   (count(RRCandStatusReportId) FOR Candidate_Status IN ([CV Shared],[Interviews],[Shortlisted],[Offered],[Accepted],[Joined])) as pvt) as z " +
               "   where ([CV Shared]> 0 or [Interviews] > 0  or [Shortlisted]> 0 or [Offered] > 0 or [Accepted] > 0 or [Joined] > 0 )";



        if (ddlClientName.SelectedIndex > 0)
        {
            subquery = " and Client_Id= " + Convert.ToInt32(ddlClientName.SelectedValue);
        }
        if (ddlConsultant.SelectedIndex > 0)
        {
            subquery += " and Consultant_Id = " + Convert.ToInt32(ddlConsultant.SelectedValue);
        }

        if (txtMonth.Text.Trim() != "")
        {
            subquery = subquery + " and  CreationDateNew=Cast('" + txtMonth.Text + "' as Date) ";
         
        }

      //  +" Order by Client_Name desc";

        query = query + subquery;
      

        return srch.SearchRecord(query).Tables[0];
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

            string filename = "ConsultantMonthlyStatus(" + DateTime.Now.AddMinutes(MyTimeSpan).ToString("dd-MMM-yyyy") + ").xls";
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
