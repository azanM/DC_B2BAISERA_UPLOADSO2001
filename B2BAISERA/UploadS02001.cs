using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using B2BAISERA.Log;
using B2BAISERA.Models.Providers;
using B2BAISERA.Properties;
using System.Net;
using B2BAISERA.Models;
using System.Security.Principal;
using B2BAISERA.Models.EFServer;
using System.Diagnostics;
using B2BAISERA.wsB2B;

namespace B2BAISERA
{
    public partial class UploadS02001 : Form
    {
        private static string fileType = "S02001";
        private static string clientTag = "B2BAITAG";
        private bool acknowledge;
        private string ticketNo = "";
        private string message = "";
        
        public UploadS02001()
        {
            InitializeComponent();
        }

        //private void UploadS02001_Load(object sender, EventArgs e)
        //{
        //    LoginAuthentication();
        //    if (acknowledge == false || ticketNo == string.Empty)
        //    {
        //        //close
        //    }
        //    else Upload();
        //}

        private void UploadS02001_Load(object sender, EventArgs e)
        {
            LogEvent logEvent = new LogEvent();
            TransactionProvider transactionProvider = new TransactionProvider();
            List<CUSTOM_S02001_TEMP_HS> tempHS = new List<CUSTOM_S02001_TEMP_HS>();
            List<CUSTOM_S02001_TEMP_IS> tempIS = new List<CUSTOM_S02001_TEMP_IS>();
            List<CUSTOM_S02001_TEMP_HS> tempHSISChecked = new List<CUSTOM_S02001_TEMP_HS>();
            List<CUSTOM_S02001_TEMP_IS> tempHSISChecked2 = new List<CUSTOM_S02001_TEMP_IS>();
            try
            {
                transactionProvider.DeleteAllTempHSIS();
                do
                {
                    tempHS = transactionProvider.CreatePOSeraToAI_HS();
                    tempIS = transactionProvider.CreatePOSeraToAI_IS();

                    tempHSISChecked = transactionProvider.CheckingHistoryHSIS(tempHS, tempIS);
                    tempHSISChecked2 = transactionProvider.CheckingHistoryHSIS2(tempHS, tempIS);
                    transactionProvider.DeleteAllTempHSIS();

                    if (tempHSISChecked.Count > 0 && tempHSISChecked2.Count > 0)
                    {
                        LoginAuthentication();
                        if (acknowledge == false || ticketNo == string.Empty)
                        {
                            //close
                        }
                        else Upload(tempHSISChecked, tempHSISChecked2);
                        //Upload(tempHSISChecked, tempHSISChecked2);

                        tempHS = new List<CUSTOM_S02001_TEMP_HS>();
                        tempIS = new List<CUSTOM_S02001_TEMP_IS>();
                        tempHSISChecked = new List<CUSTOM_S02001_TEMP_HS>();
                        tempHSISChecked2 = new List<CUSTOM_S02001_TEMP_IS>();

                        //tempHS = transactionProvider.CreatePOSeraToAI_HS();
                        //tempIS = transactionProvider.CreatePOSeraToAI_IS();

                        tempHSISChecked = transactionProvider.CheckingHistoryHSIS(tempHS, tempIS);
                        tempHSISChecked2 = transactionProvider.CheckingHistoryHSIS2(tempHS, tempIS);

                        transactionProvider.DeleteAllTempHSIS();

                    }
                } while (tempHSISChecked.Count > 0 && tempHSISChecked2.Count > 0);

                logEvent.WriteDBLog("", "UploadS02001_Load", false, "", "Upload Finish.", fileType, "SERA");
                Process.Start("taskkill.exe", "/f /im B2BAISERA_S02001.exe");
            }
            catch (Exception ex)
            {
                LblResult.Text = ex.Message;
                LblAcknowledge.Text = "";
                LblTicketNo.Text = "";
                LblMessage.Text = "";

                //logevent login failed
                logEvent.WriteDBLog("", "UploadS02001_Load", false, "", ex.Message, fileType, "SERA");
                transactionProvider.DeleteAllTempHSIS();

                // start add by fhi 02.06.2014 untuk kill aplikasi bila ada error data upload 
                Process.Start("taskkill.exe", "/f /im B2BAISERA_S02001.exe");
                //end
            }
        }

        private void LoginAuthentication()
        {
            LogEvent logEvent = new LogEvent();
            TransactionProvider transactionProvider = new TransactionProvider();
            try
            {
                using (wsB2B.B2BAIWebServiceDMZ wsB2B = new wsB2B.B2BAIWebServiceDMZ())
                {
                    var User = transactionProvider.GetUser("SERA", "SERA", clientTag);
                    if (User != null)
                    {
                        var loginReq = new wsB2B.LoginRequest();
                        loginReq.UserName = User.UserCode;
                        loginReq.Password = User.PassCode;
                        loginReq.ClientTag = User.ClientTag;

                        //WebProxy myProxy = new WebProxy(Resources.WebProxyAddress, true);
                        //myProxy.Credentials = new NetworkCredential(Resources.NetworkCredentialUserName, Resources.NetworkCredentialPassword, Resources.NetworkCredentialProxy);

                        //WebProxy myProxy = new WebProxy();
                        //myProxy.Credentials = new NetworkCredential(Resources.NetworkCredentialUserName, Resources.NetworkCredentialPassword, Resources.NetworkCredentialProxy);
                        //myProxy.Credentials = new NetworkCredential("backup", "serasibackup", "trac.astra.co.id");
                        //myProxy.Credentials = new NetworkCredential("rika009692", "mickey1988", "trac.astra.co.id");
                        //myProxy.Credentials = new NetworkCredential("genrpt", "serasera", "trac.astra.co.id");
                        //wsB2B.Proxy = myProxy;
                        
                        var wsResult = wsB2B.LoginAuthentication(loginReq);
                        acknowledge = wsResult.Acknowledge;
                        ticketNo = wsResult.TicketNo;
                        message = wsResult.Message;
                    }

                    LblResult.Text = "Service Result = ";
                    LblAcknowledge.Text = "Acknowledge : " + acknowledge;
                    LblTicketNo.Text = "TicketNo : " + ticketNo;
                    LblMessage.Text = "Message :" + message;

                    //logevent login succeded
                    logEvent.WriteDBLog("B2BAIWebServiceDMZ", "LoginAuthentication", acknowledge, ticketNo, message, fileType, "SERA");
                    
                    
                }
            }
            catch (Exception ex)
            {
                LblResult.Text = ex.Message;
                LblAcknowledge.Text = "";
                LblTicketNo.Text = "";
                LblMessage.Text = "";

                //logevent login failed
                logEvent.WriteDBLog("B2BAIWebServiceDMZ", "LoginAuthentication", acknowledge, ticketNo, "webservice message : " + message + ". exception message : " + ex.Message, fileType, "SERA");

                // start add by fhi 02.06.2014 : untuk kill aplikasi bila ada error login autentification
                Process.Start("taskkill.exe", "/f /im B2BAISERA_S02001.exe");
                //end
            }            
        }

        private void Upload(List<CUSTOM_S02001_TEMP_HS> tempHSISChecked, List<CUSTOM_S02001_TEMP_IS> tempHSISChecked2)
        {
            LogEvent logEvent = new LogEvent();
            TransactionProvider transactionProvider = new TransactionProvider();
            TransactionViewModel transaction = null;
            wsB2B.TransactionData[] transactionDataArray = null;
            List<S02001HSViewModel> transactionDataDetailHS = new List<S02001HSViewModel>();
            List<S02001ISViewModel> transactionDataDetailIS = new List<S02001ISViewModel>();
            List<string> arrHSIS = null;
            try
            {
                ////1.Get HS/IS FROM EPROC+STREAMLINER //2.INSERT INTO TEMP_HS + TEMP_IS //3.GET FROM TEMP_HS + TEMP_IS
                //var tempHS = transactionProvider.CreatePOSeraToAI_HS();
                //var tempIS = transactionProvider.CreatePOSeraToAI_IS();

                ////todo error sequence
                ////CHECK IF IT HAS BEEN SENT TO UPLOAD OR NOT BY PONUMBER
                //var tempHSISChecked = transactionProvider.CheckingHistoryHSIS(tempHS, tempIS);
                //var tempHSISChecked2 = transactionProvider.CheckingHistoryHSIS2(tempHS, tempIS);
                ////var tempHSChecked = transactionProvider.CheckingHistoryHS(tempHS);
                ////var tempISChecked = transactionProvider.CheckingHistoryIS(tempIS);

                //4.INSERT INTO LOG TRANSACTION HEADER DETAIL + DELETE TEMP
                CommonResponse commonResponse = new CommonResponse()
                {
                    Acknowledge = acknowledge,
                    TicketNo = ticketNo,
                    Message = message
                };

                var intResult = transactionProvider.InsertLogTransaction(tempHSISChecked, tempHSISChecked2, commonResponse, clientTag);

                //5.GET DATA FROM LOG TRANSACTION HEADER DETAIL 
                if (intResult != 0)
                {
                    //a.GET TRANSACTION 
                    transaction = transactionProvider.GetTransaction();

                    //b.GET TRANSACTION DATA
                    if (transaction != null)
                    {
                        //transactionData = transactionProvider.GetTransactionData(transaction.ID);
                        transactionDataArray = transactionProvider.GetTransactionDataArray(transaction.ID);

                        //c.GET TRANSACTIONDATA DETAIL / HS-IS
                        for (int i = 0; i < transactionDataArray.Count(); i++)
                        {
                            var DataDetailHS = transactionProvider.GetTransactionDataDetailHS(transactionDataArray[i].ID);
                            var DataDetailIS = transactionProvider.GetTransactionDataDetailIS(transactionDataArray[i].ID);
                            for (int j = 0; j < DataDetailHS.Count; j++)
                            {
                                transactionDataDetailHS.Add(DataDetailHS[j]);
                                //masukan ke array
                                arrHSIS = new List<string>();
                                arrHSIS.Add(transactionProvider.ConcateStringHS(DataDetailHS[j]));

                                for (int k = 0; k < DataDetailIS.Count; k++)
                                {
                                    if (DataDetailHS[j].PONumber == DataDetailIS[k].PONumber)
                                    {
                                        transactionDataDetailIS.Add(DataDetailIS[k]);
                                        //masukan ke array
                                        arrHSIS.Add(transactionProvider.ConcateStringIS(DataDetailIS[k]));
                                    }
                                }
                                //masukan ke transactionDataArray.
                                transactionDataArray[i].Data = arrHSIS.ToArray();
                                transactionDataArray[i].DataLength = arrHSIS.Count;
                            }
                        }
                        //6.SEND TO WEB SERVICE
                        using (wsB2B.B2BAIWebServiceDMZ wsB2B = new wsB2B.B2BAIWebServiceDMZ())
                        {
                            wsB2B.UploadRequest uploadRequest = new wsB2B.UploadRequest();
                            var lastTicketNo = transactionProvider.GetLastTicketNo(fileType);
                            uploadRequest.TicketNo = lastTicketNo; //from session ticketNo login
                            uploadRequest.ClientTag = Resources.ClientTag;
                            uploadRequest.transactionData = transactionDataArray;

                            //WebProxy myProxy = new WebProxy(Resources.WebProxyAddress, true);
                            //myProxy.Credentials = new NetworkCredential(Resources.NetworkCredentialUserName, Resources.NetworkCredentialPassword, Resources.NetworkCredentialProxy);

                            //WebProxy myProxy = new WebProxy();
                            //myProxy.Credentials = new NetworkCredential(Resources.NetworkCredentialUserName, Resources.NetworkCredentialPassword, Resources.NetworkCredentialProxy);
                            //myProxy.Credentials = new NetworkCredential("backup", "serasibackup", "trac.astra.co.id");
                            //myProxy.Credentials = new NetworkCredential("rika009692", "mickey1988", "trac.astra.co.id");
                            //myProxy.Credentials = new NetworkCredential("genrpt", "serasera", "trac.astra.co.id");
                            //wsB2B.Proxy = myProxy;

                            //WebProxy myProxy = new WebProxy(Resources.WebProxyAddress, true);
                            //myProxy.Credentials = new NetworkCredential(Resources.NetworkCredentialUserName, Resources.NetworkCredentialPassword, Resources.NetworkCredentialProxy);

                            //WebProxy myProxy = new WebProxy();
                            //myProxy.Credentials = new NetworkCredential(Resources.NetworkCredentialUserName, Resources.NetworkCredentialPassword, Resources.NetworkCredentialProxy);
                            //myProxy.Credentials = new NetworkCredential("backup", "serasibackup", "trac.astra.co.id");
                            //myProxy.Credentials = new NetworkCredential("rika009692", "mickey1988", "trac.astra.co.id");
                            //myProxy.Credentials = new NetworkCredential("genrpt", "serasera", "trac.astra.co.id");
                            //wsB2B.Proxy = myProxy;

                            var wsResult = wsB2B.UploadDocument(uploadRequest);
                            acknowledge = wsResult.Acknowledge;
                            ticketNo = wsResult.TicketNo;
                            message = wsResult.Message;
                          
                            //TODO 16-08-2013: IF INVALID TICKETNO RE-LOGIN
                            // Get file info
                        }
                    }

                    //add 03.06.2014 by fhi
                    else if (transaction == null)
                    {
                        logEvent.WriteDBLog("", "UploadS02001_Load", false, "", "transaction == null", "S02001", "SERA");
                        Process.Start("taskkill.exe", "/f /im B2BAISERA_S02001.exe");
                    }

                }
                else if (intResult == 0)
                {
                    //delete temp table 
                    transactionProvider.DeleteAllTempHSIS();
                    acknowledge = false;
                    ticketNo = "";
                    message = "No Data Upload.";
                }

                LblResult.Text = "Service Result = ";
                LblAcknowledge.Text = "Acknowledge : " + acknowledge;
                LblTicketNo.Text = "TicketNo : " + ticketNo;
                LblMessage.Text = "Message :" + message;

                //logevent login succeded
                logEvent.WriteDBLog("B2BAIWebServiceDMZ", "UploadDocumentS02001", acknowledge, ticketNo, message, fileType, "SERA");                
            }
            catch (Exception ex)
            {
                //delete temp table 
                transactionProvider.DeleteAllTempHSIS();

                LblResult.Text = ex.Message;
                LblAcknowledge.Text = "";
                LblTicketNo.Text = "";
                LblMessage.Text = "";
                //logevent login failed
                logEvent.WriteDBLog("B2BAIWebServiceDMZ", "UploadDocumentS02001", acknowledge, ticketNo, "webservice message : " + message + ". exception message : " + ex.Message, fileType, "SERA");

                Process.Start("taskkill.exe", "/f /im B2BAISERA_S02001.exe");
            }
        }
    }
}
