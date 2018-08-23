using System;
using System.Collections.Generic;
using System.Linq;
//using B2BAISERA.Models.DataAccess;
//using B2BAISERA.Helper;
//using B2BAISERA.Logic;
using Microsoft.Practices.Unity;
using System.Text;
using System.Configuration;
using System.Data.SqlClient;
using System.Data.Common;
using B2BAISERA.Models.EFServer;
using B2BAISERA.Helper;
using System.Data.EntityClient;
using System.Data;
using B2BAISERA.wsB2B;
using System.Globalization;
using B2BAISERA.Log;
using System.Diagnostics;

namespace B2BAISERA.Models.Providers
{
    public class TransactionProvider : DataAccessBase
    {
        public TransactionProvider()
            : base()
        {
        }

        public TransactionProvider(EProcEntities context)
            : base(context)
        {
        }

        #region MAIN
        //B2BAISERAEntities ctx = new B2BAISERAEntities(Repository.ConnectionStringEF);

        //public User GetUser(string userName, string password, string clientTag)
        //{
        //    var User = (from o in ctx.Users
        //                where o.UserName == userName && o.Password == password && o.ClientTag == clientTag
        //                select o).FirstOrDefault();

        //    return User;
        //}

        public CUSTOM_USER GetUser(string userName, string password, string clientTag)
        {
            var user = (from o in entities.CUSTOM_USER
                        where o.UserName == userName && o.Password == password && o.ClientTag == clientTag
                        select o).FirstOrDefault();

            return user;
        }

        public string GetLastTicketNo(string fileType)
        {
            var result = "";
            try
            {
                var query = (entities.CUSTOM_LOG
                    .Where(log => (log.Acknowledge == true) && (log.FileType == fileType))
                    .Select(log => new LogViewModel
                    {
                        ID = log.ID,
                        WebServiceName = log.WebServiceName,
                        MethodName = log.MethodName,
                        TicketNo = log.TicketNo,
                        Message = log.Message
                    })
                    ).OrderByDescending(log => log.ID).FirstOrDefault();

                result = query != null ? Convert.ToString(query.TicketNo) : "";
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return result;
        }
        #endregion

        #region UPLOAD

        #region S02001
        public List<CUSTOM_S02001_TEMP_HS> CreatePOSeraToAI_HS()
        {
            LogEvent logEvent = new LogEvent();
            List<CUSTOM_S02001_TEMP_HS> listTempHS = new List<CUSTOM_S02001_TEMP_HS>();
            try
            {
                listTempHS = entities.sp_CreatePOSeraToAI_HS().ToList();
                return listTempHS;
            }
            catch (Exception ex)
            {
                //add task kill : by  fhi 05.06.2014
                //logEvent.WriteDBLog("", "UploadS02001_Load", false, "", ex.Message + "error get data for insert CUSTOM_S020logEvent.WriteDBLog("", "UploadS02001_Load", false, "", ex.Message + "error get data for insert CUSTOM_S02001_TEMP_HS", "S02001", "SERA");01_TEMP_HS", "S02001", "SERA");
                logEvent.WriteDBLog("", "UploadS02001_Load", false, "", ex.StackTrace, "S02001", "SERA");
                logEvent.WriteDBLog("", "UploadS02001_Load", false, "", ex.InnerException.Message, "S02001", "SERA");
                logEvent.WriteDBLog("", "UploadS02001_Load", false, "", ex.InnerException.StackTrace, "S02001", "SERA");
                Process.Start("taskkill.exe", "/f /im B2BAISERA_S02001.exe");
                //end
                throw ex;
            }
        }

        public List<CUSTOM_S02001_TEMP_IS> CreatePOSeraToAI_IS()
        {
            LogEvent logEvent = new LogEvent();
            List<CUSTOM_S02001_TEMP_IS> listTempIS = new List<CUSTOM_S02001_TEMP_IS>();
            try
            {
                listTempIS = entities.sp_CreatePOSeraToAI_IS().ToList();
                return listTempIS;
            }
            catch (Exception ex)
            {
                //add task kill : by  fhi 05.06.2014
                logEvent.WriteDBLog("", "UploadS02001_Load", false, "", ex.Message + "error get data for insert CUSTOM_S02001_TEMP_IS", "S02001", "SERA");
                Process.Start("taskkill.exe", "/f /im B2BAISERA_S02001.exe");
                //end
                throw ex;
            }
        }

        public int InsertLogTransaction(List<CUSTOM_S02001_TEMP_HS> listTempHS, List<CUSTOM_S02001_TEMP_IS> listTempIS, CommonResponse commonResponse, string clientTag)
        {
            LogEvent logEvent = new LogEvent();
            int result = 0;
            try
            {
                if (listTempHS.Count > 0)
                {
                    //insert into CUSTOM_TRANSACTION
                    CUSTOM_TRANSACTION transaction = new CUSTOM_TRANSACTION();

                    transaction.Acknowledge = commonResponse.Acknowledge;
                    transaction.TicketNo = commonResponse.TicketNo;
                    transaction.Message = commonResponse.Message;
                    transaction.ClientTag = clientTag;

                    EntityHelper.SetAuditForInsert(transaction, "SERA");
                    entities.CUSTOM_TRANSACTION.AddObject(transaction);

                    var countListTempHS = listTempHS.Count;
                    var countListTempIS = listTempIS.Count;

                    for (int i = 0; i < listTempHS.Count; i++)
                    {
                        //insert into CUSTOM_TRANSACTIONDATA
                        CUSTOM_TRANSACTIONDATA transactionData = new CUSTOM_TRANSACTIONDATA();
                        transactionData.CUSTOM_TRANSACTION = transaction;
                        transactionData.TransGUID = Guid.NewGuid().ToString();
                        transactionData.DocumentNumber = listTempHS[i].PONumber;
                        transactionData.FileType = "S02001";
                        transactionData.IPAddress = "118.97.80.12"; //IP ADDRESS KOMP SERVER, dan HARUS TERDAFTAR DI DB AI
                        transactionData.DestinationUser = "AI";
                        //transactionData.Key1 = "0002"; //company toyota // untuk rollout dso key1=company code
                        transactionData.Key1 = listTempHS[i].CompanyCodeAI;
                        transactionData.Key2 = listTempHS[i].KodeCabangAI;
                        transactionData.Key3 = "";
                        transactionData.DataLength = null;
                        transactionData.RowStatus = "";
                        EntityHelper.SetAuditForInsert(transactionData, "SERA");
                        entities.CUSTOM_TRANSACTIONDATA.AddObject(transactionData);

                        //CHECK IF DATA HS BY PONUMBER SUDAH ADA, DELETE DULU BY ID, supaya tidak redundant ponumber nya
                        var poNumb = listTempHS[i].PONumber;
                        var query = (from o in entities.CUSTOM_S02001_HS
                                     where o.PONumber == poNumb
                                     select o).ToList();
                        if (query.Count > 0)
                        {
                            for (int d = 0; d < query.Count; d++)
                            {
                                //delete
                                var delID = query[d].ID;
                                CUSTOM_S02001_HS delHS = entities.CUSTOM_S02001_HS.Single(o => o.ID == delID);
                                entities.CUSTOM_S02001_HS.DeleteObject(delHS);
                            }
                        }

                        //insert into CUSTOM_S02001_HS
                        CUSTOM_S02001_HS DataDetailHS = new CUSTOM_S02001_HS();
                        DataDetailHS.CUSTOM_TRANSACTIONDATA = transactionData;
                        DataDetailHS.PONumber = listTempHS[i].PONumber;
                        DataDetailHS.PODate = listTempHS[i].PODate;
                        DataDetailHS.Version = listTempHS[i].Version;
                        DataDetailHS.CustomerNumber = listTempHS[i].CustomerNumber;
                        DataDetailHS.KodeCabangAI = listTempHS[i].KodeCabangAI;
                        DataDetailHS.MaterialNumberSERA = listTempHS[i].MaterialNumberSERA;
                        DataDetailHS.MaterialDescriptionSERA = listTempHS[i].MaterialDescriptionSERA;
                        DataDetailHS.MaterialNumberAI = listTempHS[i].MaterialNumberAI;
                        DataDetailHS.ColorDescSERA = listTempHS[i].ColorDescSERA;
                        DataDetailHS.Quantity = listTempHS[i].Quantity;

                        // add fhi 10.12.2014 : penambahan field karoseri
                        DataDetailHS.NamaKaroseri = listTempHS[i].NamaKaroseri;
                        DataDetailHS.AlamatKaroseri = listTempHS[i].AlamatKaroseri;
                        DataDetailHS.PIC = listTempHS[i].PIC;
                        DataDetailHS.NoTelepon = listTempHS[i].NoTelepon;
                        DataDetailHS.BentukKaroseri = listTempHS[i].BentukKaroseri;
                        DataDetailHS.InfoPromiseDelivery = listTempHS[i].InfoPromiseDelivery;
                        //end

                        DataDetailHS.CustomerSTNKName = listTempHS[i].CustomerSTNKName;
                        DataDetailHS.Title = listTempHS[i].Title;
                        DataDetailHS.Address = listTempHS[i].Address;
                        DataDetailHS.Address2 = listTempHS[i].Address2;
                        DataDetailHS.Address3 = listTempHS[i].Address3;
                        DataDetailHS.Address4 = listTempHS[i].Address4;
                        DataDetailHS.Address5 = listTempHS[i].Address5;
                        DataDetailHS.KTPTDP = listTempHS[i].KTPTDP;
                        DataDetailHS.PostalCode = listTempHS[i].PostalCode;
                        DataDetailHS.PartnerName = listTempHS[i].PartnerName;
                        DataDetailHS.PartnerAddress = listTempHS[i].PartnerAddress;
                        DataDetailHS.Telepon = listTempHS[i].Telepon;
                        DataDetailHS.City = listTempHS[i].City;
                        DataDetailHS.RegionCode = listTempHS[i].RegionCode;
                        DataDetailHS.PartnerPostalCode = listTempHS[i].PartnerPostalCode;
                        DataDetailHS.Diskon = listTempHS[i].Diskon;
                        DataDetailHS.Pricing = listTempHS[i].Pricing;
                        DataDetailHS.CurrencyCode = listTempHS[i].CurrencyCode;

                        //start add identitas penambahan row custom_s02001_hs : by fhi 04.06.2014
                        DataDetailHS.dibuatOleh = "system";
                        DataDetailHS.dibuatTanggal = DateTime.Now;
                        DataDetailHS.diubahOleh = "system";
                        DataDetailHS.diubahTanggal = DateTime.Now;
                        //end
                        entities.CUSTOM_S02001_HS.AddObject(DataDetailHS);

                        //build HS separator
                        var strHS = ConcateStringHS(listTempHS[i]);

                        //insert into CUSTOM_TRANSACTIONDATADETAIL for HS
                        CUSTOM_TRANSACTIONDATADETAIL transactionDataDetail = new CUSTOM_TRANSACTIONDATADETAIL();
                        transactionDataDetail.CUSTOM_TRANSACTIONDATA = transactionData;
                        transactionDataDetail.Data = strHS;
                        //start add identitas penambahan row CUSTOM_TRANSACTIONDATADETAIL : by fhi 04.06.2014
                        transactionDataDetail.dibuatOleh = "system";
                        transactionDataDetail.dibuatTanggal = DateTime.Now;
                        transactionDataDetail.diubahOleh = "system";
                        transactionDataDetail.diubahTanggal = DateTime.Now;
                        //end
                        entities.CUSTOM_TRANSACTIONDATADETAIL.AddObject(transactionDataDetail);

                        if (listTempIS != null)
                        {
                            for (int j = 0; j < countListTempIS; j++)
                            {
                                if (listTempIS[j].PONumber == listTempHS[i].PONumber)
                                {
                                    //CHECK IF DATA IS BY PONUMBER SUDAH ADA, DELETE DULU BY ID
                                    var poNumbIS = listTempIS[j].PONumber;
                                    var queryIS = (from o in entities.CUSTOM_S02001_IS
                                                   where o.PONumber == poNumbIS
                                                   select o).ToList();
                                    if (queryIS.Count > 0)
                                    {
                                        for (int d = 0; d < queryIS.Count; d++)
                                        {
                                            //delete
                                            var delIDIS = queryIS[d].ID;
                                            CUSTOM_S02001_IS delIS = entities.CUSTOM_S02001_IS.Single(o => o.ID == delIDIS);
                                            entities.CUSTOM_S02001_IS.DeleteObject(delIS);
                                        }
                                    }

                                    //insert into CUSTOM_S02001_IS
                                    CUSTOM_S02001_IS DataDetailIS = new CUSTOM_S02001_IS();
                                    DataDetailIS.CUSTOM_TRANSACTIONDATA = transactionData;
                                    DataDetailIS.PONumber = listTempIS[j].PONumber;
                                    DataDetailIS.PODate = listTempIS[j].PODate;
                                    DataDetailIS.DataVersion = listTempIS[j].DataVersion;
                                    DataDetailIS.AccessoriesNumberAI = listTempIS[j].AccessoriesNumberAI;
                                    DataDetailIS.AccessoriesNumberSERA = listTempIS[j].AccessoriesNumberSERA;
                                    DataDetailIS.AccessoriesDescriptionSERA = listTempIS[j].AccessoriesDescriptionSERA;
                                    DataDetailIS.QtyAccessories = listTempIS[j].QtyAccessories;
                                    //start add identitas penambahan row custom_s02001_is : by fhi 04.06.2014
                                    DataDetailIS.dibuatOleh = "system";
                                    DataDetailIS.dibuatTanggal = DateTime.Now;
                                    DataDetailIS.diubahOleh = "system";
                                    DataDetailIS.diubahTanggal = DateTime.Now;
                                    //end
                                    entities.CUSTOM_S02001_IS.AddObject(DataDetailIS);

                                    //build IS separator
                                    var strIS = ConcateStringIS(listTempIS[j]);

                                    //insert into CUSTOM_TRANSACTIONDATADETAIL for IS
                                    transactionDataDetail = new CUSTOM_TRANSACTIONDATADETAIL();
                                    transactionDataDetail.CUSTOM_TRANSACTIONDATA = transactionData;
                                    transactionDataDetail.Data = strIS;

                                    //start add identitas penambahan row CUSTOM_TRANSACTIONDATADETAIL : by fhi 04.06.2014
                                    transactionDataDetail.dibuatOleh = "system";
                                    transactionDataDetail.dibuatTanggal = DateTime.Now;
                                    transactionDataDetail.diubahOleh = "system";
                                    transactionDataDetail.diubahTanggal = DateTime.Now;
                                    //end

                                    entities.CUSTOM_TRANSACTIONDATADETAIL.AddObject(transactionDataDetail);

                                    ////TODO 22-08-2013: CHANGE TO DELETEOBJECT FOR EF IS
                                    //CUSTOM_S02001_TEMP_IS customTempIS = entities.CUSTOM_S02001_TEMP_IS.Single(x => x.PONumber == listTempIS[j].PONumber);
                                    //entities.CUSTOM_S02001_TEMP_IS.DeleteObject(customTempIS);
                                }
                            }
                        }
                        ////TODO 22-08-2013: CHANGE TO DELETEOBJECT FOR EF HS
                        //CUSTOM_S02001_TEMP_HS customTempHS = entities.CUSTOM_S02001_TEMP_HS.Single(x => x.PONumber == listTempHS[i].PONumber);
                        //entities.CUSTOM_S02001_TEMP_HS.DeleteObject(customTempHS);
                    }
                    entities.SaveChanges();
                    //delete temp table 
                    for (int y = 0; y < countListTempHS; y++)
                    {
                        DeleteTempHS(listTempHS[y]);
                    }
                    for (int z = 0; z < countListTempIS; z++)
                    {
                        DeleteTempIS(listTempIS[z]);
                    }
                    result = 1;
                }
            }
            catch (Exception ex)
            {
                //add task kill : by  fhi 05.06.2014
                logEvent.WriteDBLog("", "UploadS02001_Load", false, "", ex.Message, "S02001", "SERA");
                Process.Start("taskkill.exe", "/f /im B2BAISERA_S02001.exe");
                //end
                throw ex;
            }
            return result;
        }

        public TransactionViewModel GetTransaction()
        {
            LogEvent logEvent = new LogEvent();
            TransactionViewModel transaction = null;
            try
            {
                DateTime dateNow = DateTime.Now.Date;
                transaction = (from h in entities.CUSTOM_TRANSACTION
                               join d in entities.CUSTOM_TRANSACTIONDATA
                               on h.ID equals d.TransactionID
                               where d.FileType == "S02001" && h.CreatedWhen >= dateNow
                               select new TransactionViewModel()
                               {
                                   ID = h.ID
                               }).OrderByDescending(z => z.ID).FirstOrDefault();
            }
            catch (Exception ex)
            {
                //add task kill : by  fhi 05.06.2014
                logEvent.WriteDBLog("", "UploadS02001_Load", false, "", ex.Message, "S02001", "SERA");
                Process.Start("taskkill.exe", "/f /im B2BAISERA_S02001.exe");
                //end
                throw ex;
            }
            return transaction;
        }

        public List<TransactionDataViewModel> GetTransactionData(int? transactionID)
        {
            List<TransactionDataViewModel> transactionData = null;
            try
            {
                transactionData = (entities.CUSTOM_TRANSACTIONDATA
                                   .Where(o => o.TransactionID == transactionID)
                                   .Select(o => new TransactionDataViewModel
                                   {
                                       ID = o.ID,
                                       TransactionID = o.TransactionID,
                                       TransGUID = o.TransGUID,
                                       DocumentNumber = o.DocumentNumber,
                                       FileType = o.FileType,
                                       IPAddress = o.IPAddress,
                                       DestinationUser = o.DestinationUser,
                                       Key1 = o.Key1,
                                       Key2 = o.Key2,
                                       Key3 = o.Key3,
                                       DataLength = o.DataLength,
                                       TransStatus = o.RowStatus,
                                       CreatedWho = o.CreatedWho,
                                       CreatedWhen = o.CreatedWhen,
                                       ChangedWho = o.ChangedWho,
                                       ChangedWhen = o.ChangedWhen
                                   }).ToList());
            }
            catch (Exception ex)
            {

                throw ex;
            }
            return transactionData;
        }

        public wsB2B.TransactionData[] GetTransactionDataArray(int? transactionID)
        {
            LogEvent logEvent = new LogEvent();
            wsB2B.TransactionData[] transactionData = null;
            try
            {
                transactionData = (from o in entities.CUSTOM_TRANSACTIONDATA
                                   //join p in entities.CUSTOM_S02001_HS
                                   //on o.ID equals p.TransactionDataID
                                   //join q in entities.CUSTOM_S02001_IS
                                   //on o.ID equals q.TransactionDataID
                                   //join s in entities.CUSTOM_TRANSACTIONDATADETAIL
                                   //on o.ID equals s.TransactionDataID
                                   where o.TransactionID == transactionID
                                   select new wsB2B.TransactionData
                                   {
                                       ID = o.ID,
                                       TransGUID = o.TransGUID,
                                       DocumentNumber = o.DocumentNumber,
                                       FileType = o.FileType,
                                       IPAddress = o.IPAddress,
                                       DestinationUser = o.DestinationUser,
                                       Key1 = o.Key1,
                                       Key2 = o.Key2,
                                       Key3 = o.Key3,
                                       //DataLength = 
                                       //Data = ConcateStringHS()//new ArrayOfString { s.Data, "","" }
                                   }).ToArray();
            }
            catch (Exception ex)
            {
                //add task kill : by  fhi 05.06.2014
                logEvent.WriteDBLog("", "UploadS02001_Load", false, "", ex.Message, "S02001", "SERA");
                Process.Start("taskkill.exe", "/f /im B2BAISERA_S02001.exe");
                //end
                throw ex;
            }
            return transactionData;
        }

        private string[] GetArrayOfStringData()
        {
            string[] arr = new string[100];
            List<string> list = new List<string>(arr);

            string[] arrData1 = new string[] 
            { 
                "HS|F-30C|A001CUA13000999|782725A9-5F3E-4362-A010-3A08BAB1DD11|20130620|20130620|T0A0|20130620|IDR|1|06|A001PQA13000131",
                "IS|01|7006038032||275000|K0||Z000|A001|20130620||||A001PUA13000114|A001PQA13000131/7006038032/Agustari Wira |1030000000||||1080202000||7006038032|||Agustari Wira |Jl KAYA RAYA|JAKARTA UTARA|/AGRAGUST5504//|||||275000|",
                "IS|50|2140322000||25000|K2|||A001||20130620|||A001PUA13000114|A001PUA13000114/7006038032/Agustari Wira |2140322000||||2140322000||||||||/AGRAGUST5504//|||||25000|",
                "IS|50|3000202001||250000|K0|||A001||20130620|||A001PUA13000114|A001PQA13000131/7006038032/Agustari Wira |3000202001||910-A0-001||3000202001|15601-BZ010|7006038032|09|1,00||||/AGRAGUST5504//|||||250000|02"
            };
            return arrData1;
        }

        public List<S02001HSViewModel> GetTransactionDataDetailHS(int? transactionDataID)
        {
            LogEvent logEvent = new LogEvent();
            List<S02001HSViewModel> dataHS = null;
            try
            {
                dataHS = (entities.CUSTOM_S02001_HS
                          .Where(o => o.TransactionDataID == transactionDataID)
                          .Select(o => new S02001HSViewModel
                          {
                              ID = o.ID,
                              TransactionDataID = o.TransactionDataID,
                              PONumber = o.PONumber,
                              PODate = o.PODate,
                              Version = o.Version,
                              CustomerNumber = o.CustomerNumber,
                              KodeCabangAI = o.KodeCabangAI,
                              MaterialNumberSERA = o.MaterialNumberSERA,
                              MaterialDescriptionSERA = o.MaterialDescriptionSERA,
                              MaterialNumberAI = o.MaterialNumberAI,
                              ColorDescSERA = o.ColorDescSERA,
                              Quantity = o.Quantity,

                              // add fhi 10.12.2014 : penambahan field karoseri
                              NamaKaroseri=o.NamaKaroseri,
                              AlamatKaroseri=o.AlamatKaroseri,
                              PIC=o.PIC,
                              NoTelepon=o.NoTelepon,
                              BentukKaroseri=o.BentukKaroseri,
                              InfoPromiseDelivery=o.InfoPromiseDelivery,
                              //end

                              CustomerSTNKName = o.CustomerSTNKName,
                              Title = o.Title,
                              Address = o.Address,
                              Address2 = o.Address2,
                              Address3 = o.Address3,
                              Address4 = o.Address4,
                              Address5 = o.Address5,
                              KTP_TDP = o.KTPTDP,
                              PostalCode = o.PostalCode,
                              PartnerName = o.PartnerName,
                              PartnerAddress = o.PartnerAddress,
                              Telepon = o.Telepon,
                              City = o.City,
                              RegionCode = o.RegionCode,
                              PartnerPostalCode = o.PartnerPostalCode,
                              Diskon = o.Diskon,
                              Pricing = o.Pricing,
                              CurrencyCode = o.CurrencyCode
                          }).ToList());
            }
            catch (Exception ex)
            {
                //add task kill : by  fhi 05.06.2014
                logEvent.WriteDBLog("", "UploadS02001_Load", false, "", ex.Message, "S02001", "SERA");
                Process.Start("taskkill.exe", "/f /im B2BAISERA_S02001.exe");
                //end
                throw ex;
            }
            return dataHS;
        }

        public List<S02001ISViewModel> GetTransactionDataDetailIS(int? transactionDataID)
        {
            LogEvent logEvent = new LogEvent();
            List<S02001ISViewModel> dataIS = null;
            try
            {
                dataIS = (entities.CUSTOM_S02001_IS
                          .Where(o => o.TransactionDataID == transactionDataID)
                          .Select(o => new S02001ISViewModel
                          {
                              ID = o.ID,
                              TransactionDataID = (int)(!o.TransactionDataID.HasValue ? 0 : o.TransactionDataID),
                              PONumber = o.PONumber,
                              PODate = o.PODate,
                              DataVersion = o.DataVersion,
                              AccessoriesNumberAI = o.AccessoriesNumberAI,
                              AccessoriesNumberSERA = o.AccessoriesNumberSERA,
                              AccessoriesDescriptionSERA = o.AccessoriesDescriptionSERA,
                              QtyAccessories = o.QtyAccessories
                          }).ToList());
            }
            catch (Exception ex)
            {
                //add task kill : by  fhi 05.06.2014
                logEvent.WriteDBLog("", "UploadS02001_Load", false, "", ex.Message, "S02001", "SERA");
                Process.Start("taskkill.exe", "/f /im B2BAISERA_S02001.exe");
                //end
                throw ex;
            }
            return dataIS;
        }

        private string ConcateStringHS(CUSTOM_S02001_TEMP_HS tempHS)
        {
            StringBuilder strHS = new StringBuilder(1000);
            strHS.Append("HS|");
            strHS.Append(tempHS.PONumber);
            strHS.Append("|");
            strHS.Append(tempHS.PODate == null ? "19000101" : string.Format("{0:yyyyMMdd}", tempHS.PODate));
            strHS.Append("|");
            strHS.Append(tempHS.Version == null ? "" : tempHS.Version.ToString());
            strHS.Append("|");
            strHS.Append(string.IsNullOrEmpty(tempHS.CustomerNumber) ? "" : tempHS.CustomerNumber);
            strHS.Append("|");
            strHS.Append(string.IsNullOrEmpty(tempHS.KodeCabangAI) ? "" : tempHS.KodeCabangAI);
            strHS.Append("|");
            strHS.Append(string.IsNullOrEmpty(tempHS.MaterialNumberSERA) ? "" : tempHS.MaterialNumberSERA);
            strHS.Append("|");
            strHS.Append(string.IsNullOrEmpty(tempHS.MaterialDescriptionSERA) ? "" : tempHS.MaterialDescriptionSERA);
            strHS.Append("|");
            strHS.Append(string.IsNullOrEmpty(tempHS.MaterialNumberAI) ? "" : tempHS.MaterialNumberAI);
            strHS.Append("|");
            strHS.Append(string.IsNullOrEmpty(tempHS.ColorDescSERA) ? "" : tempHS.ColorDescSERA);
            strHS.Append("|");
            strHS.Append(tempHS.Quantity == null ? "" : tempHS.Quantity.ToString());
            strHS.Append("|");
            strHS.Append(string.IsNullOrEmpty(tempHS.CustomerSTNKName) ? "" : tempHS.CustomerSTNKName);
            strHS.Append("|");
            strHS.Append(tempHS.Title == null ? "" : tempHS.Title.ToString());
            strHS.Append("|");
            strHS.Append(string.IsNullOrEmpty(tempHS.Address) ? "" : tempHS.Address);
            strHS.Append("|");
            strHS.Append(string.IsNullOrEmpty(tempHS.Address2) ? "" : tempHS.Address2);
            strHS.Append("|");
            strHS.Append(string.IsNullOrEmpty(tempHS.Address3) ? "" : tempHS.Address3);
            strHS.Append("|");
            strHS.Append(string.IsNullOrEmpty(tempHS.Address4) ? "" : tempHS.Address4);
            strHS.Append("|");
            strHS.Append(string.IsNullOrEmpty(tempHS.Address5) ? "" : tempHS.Address5);
            strHS.Append("|");
            strHS.Append(string.IsNullOrEmpty(tempHS.KTPTDP) ? "" : tempHS.KTPTDP);
            strHS.Append("|");
            strHS.Append(string.IsNullOrEmpty(tempHS.PostalCode) ? "" : tempHS.PostalCode);
            strHS.Append("|");
            strHS.Append(string.IsNullOrEmpty(tempHS.PartnerName) ? "" : tempHS.PartnerName);
            strHS.Append("|");
            strHS.Append(string.IsNullOrEmpty(tempHS.PartnerAddress) ? "" : tempHS.PartnerAddress);
            strHS.Append("|");
            strHS.Append(string.IsNullOrEmpty(tempHS.Telepon) ? "" : tempHS.Telepon);
            strHS.Append("|");
            strHS.Append(string.IsNullOrEmpty(tempHS.City) ? "" : tempHS.City);
            strHS.Append("|");
            strHS.Append(string.IsNullOrEmpty(tempHS.RegionCode) ? "" : tempHS.RegionCode);
            strHS.Append("|");
            strHS.Append(string.IsNullOrEmpty(tempHS.PartnerPostalCode) ? "" : tempHS.PartnerPostalCode);

            // add fhi 10.12.2014 : penambahan field karoseri
            strHS.Append("|");
            strHS.Append(tempHS.InfoPromiseDelivery == null ? "19000101" : string.Format("{0:yyyyMMdd}", tempHS.InfoPromiseDelivery));
            //end

            strHS.Append("|");
            strHS.Append(tempHS.Diskon == null ? "" : tempHS.Diskon.ToString());
            strHS.Append("|");
            strHS.Append(tempHS.Pricing == null ? "" : tempHS.Pricing.ToString());
            strHS.Append("|");
            strHS.Append(string.IsNullOrEmpty(tempHS.CurrencyCode) ? "" : tempHS.CurrencyCode);

            // add fhi 10.12.2014 : penambahan field karoseri
            strHS.Append("|");
            strHS.Append(string.IsNullOrEmpty(tempHS.NamaKaroseri) ? "" : tempHS.NamaKaroseri);
            strHS.Append("|");
            strHS.Append(string.IsNullOrEmpty(tempHS.AlamatKaroseri) ? "" : tempHS.AlamatKaroseri);
            strHS.Append("|");
            strHS.Append(string.IsNullOrEmpty(tempHS.PIC) ? "" : tempHS.PIC);
            strHS.Append("|");
            strHS.Append(string.IsNullOrEmpty(tempHS.NoTelepon) ? "" : tempHS.NoTelepon);
            strHS.Append("|");
            strHS.Append(string.IsNullOrEmpty(tempHS.BentukKaroseri) ? "" : tempHS.BentukKaroseri);
            //end


            return strHS.ToString();
        }

        public string ConcateStringHS(S02001HSViewModel HS)
        {
            StringBuilder strHS = new StringBuilder(1000);
            strHS.Append("HS|");
            strHS.Append(HS.PONumber);
            strHS.Append("|");
            strHS.Append(HS.PODate == null ? "19000101" : string.Format("{0:yyyyMMdd}", HS.PODate));
            strHS.Append("|");
            strHS.Append(HS.Version == null ? "" : Convert.ToString(HS.Version));
            strHS.Append("|");
            strHS.Append(string.IsNullOrEmpty(HS.CustomerNumber) ? "" : HS.CustomerNumber);
            strHS.Append("|");
            strHS.Append(string.IsNullOrEmpty(HS.KodeCabangAI) ? "" : HS.KodeCabangAI);
            strHS.Append("|");
            strHS.Append(string.IsNullOrEmpty(HS.MaterialNumberSERA) ? "" : HS.MaterialNumberSERA);
            strHS.Append("|");
            strHS.Append(string.IsNullOrEmpty(HS.MaterialDescriptionSERA) ? "" : HS.MaterialDescriptionSERA);
            strHS.Append("|");
            strHS.Append(string.IsNullOrEmpty(HS.MaterialNumberAI) ? "" : HS.MaterialNumberAI);
            strHS.Append("|");
            strHS.Append(string.IsNullOrEmpty(HS.ColorDescSERA) ? "" : HS.ColorDescSERA);
            strHS.Append("|");
            strHS.Append(HS.Quantity == null ? "" : HS.Quantity.ToString());
            strHS.Append("|");
            strHS.Append(string.IsNullOrEmpty(HS.CustomerSTNKName) ? "" : HS.CustomerSTNKName);
            strHS.Append("|");
            strHS.Append(HS.Title == null ? "" : HS.Title.ToString());
            strHS.Append("|");
            strHS.Append(string.IsNullOrEmpty(HS.Address) ? "" : HS.Address);
            strHS.Append("|");
            strHS.Append(string.IsNullOrEmpty(HS.Address2) ? "" : HS.Address2);
            strHS.Append("|");
            strHS.Append(string.IsNullOrEmpty(HS.Address3) ? "" : HS.Address3);
            strHS.Append("|");
            strHS.Append(string.IsNullOrEmpty(HS.Address4) ? "" : HS.Address4);
            strHS.Append("|");
            strHS.Append(string.IsNullOrEmpty(HS.Address5) ? "" : HS.Address5);
            strHS.Append("|");
            strHS.Append(string.IsNullOrEmpty(HS.KTP_TDP) ? "" : HS.KTP_TDP);
            strHS.Append("|");
            strHS.Append(string.IsNullOrEmpty(HS.PostalCode) ? "" : HS.PostalCode);
            strHS.Append("|");
            strHS.Append(string.IsNullOrEmpty(HS.PartnerName) ? "" : HS.PartnerName);
            strHS.Append("|");
            strHS.Append(string.IsNullOrEmpty(HS.PartnerAddress) ? "" : HS.PartnerAddress);
            strHS.Append("|");
            strHS.Append(string.IsNullOrEmpty(HS.Telepon) ? "" : HS.Telepon);
            strHS.Append("|");
            strHS.Append(string.IsNullOrEmpty(HS.City) ? "" : HS.City);
            strHS.Append("|");
            strHS.Append(string.IsNullOrEmpty(HS.RegionCode) ? "" : HS.RegionCode);
            strHS.Append("|");
            strHS.Append(string.IsNullOrEmpty(HS.PartnerPostalCode) ? "" : HS.PartnerPostalCode);

            // add fhi 10.12.2014 : penambahan field karoseri
            strHS.Append("|");
            strHS.Append(HS.InfoPromiseDelivery == null ? "19000101" : string.Format("{0:yyyyMMdd}", HS.InfoPromiseDelivery));
            //end

            strHS.Append("|");
            strHS.Append(HS.Diskon == null ? "" : HS.Diskon.ToString());
            strHS.Append("|");
            strHS.Append(HS.Pricing == null ? "" : HS.Pricing.ToString());
            strHS.Append("|");
            strHS.Append(string.IsNullOrEmpty(HS.CurrencyCode) ? "" : HS.CurrencyCode);

            // add fhi 10.12.2014 : penambahan field karoseri
            strHS.Append("|");
            strHS.Append(string.IsNullOrEmpty(HS.NamaKaroseri) ? "" : HS.NamaKaroseri);
            strHS.Append("|");
            strHS.Append(string.IsNullOrEmpty(HS.AlamatKaroseri) ? "" : HS.AlamatKaroseri);
            strHS.Append("|");
            strHS.Append(string.IsNullOrEmpty(HS.PIC) ? "" : HS.PIC);
            strHS.Append("|");
            strHS.Append(string.IsNullOrEmpty(HS.NoTelepon) ? "" : HS.NoTelepon);
            strHS.Append("|");
            strHS.Append(string.IsNullOrEmpty(HS.BentukKaroseri) ? "" : HS.BentukKaroseri);
            //end


            return strHS.ToString();
        }

        private string ConcateStringIS(CUSTOM_S02001_TEMP_IS tempIS)
        {
            StringBuilder strIS = new StringBuilder(1000);
            strIS.Append("IS|");
            strIS.Append(tempIS.PONumber);
            strIS.Append("|");
            strIS.Append(tempIS.PODate == null ? "19000101" : string.Format("{0:yyyyMMdd}", tempIS.PODate));
            strIS.Append("|");
            strIS.Append(tempIS.DataVersion == null ? "" : Convert.ToString(tempIS.DataVersion));
            strIS.Append("|");
            strIS.Append(tempIS.AccessoriesNumberAI);
            strIS.Append("|");
            strIS.Append(tempIS.AccessoriesNumberSERA);
            strIS.Append("|");
            strIS.Append(tempIS.AccessoriesDescriptionSERA);
            strIS.Append("|");
            strIS.Append(tempIS.QtyAccessories);

            return strIS.ToString();
        }

        public string ConcateStringIS(S02001ISViewModel IS)
        {
            StringBuilder strIS = new StringBuilder(1000);
            strIS.Append("IS|");
            strIS.Append(IS.PONumber);
            strIS.Append("|");
            strIS.Append(IS.PODate == null ? "19000101" : string.Format("{0:yyyyMMdd}", IS.PODate));
            strIS.Append("|");
            strIS.Append(IS.DataVersion == null ? "" : Convert.ToString(IS.DataVersion));
            strIS.Append("|");
            strIS.Append(IS.AccessoriesNumberAI);
            strIS.Append("|");
            strIS.Append(IS.AccessoriesNumberSERA);
            strIS.Append("|");
            strIS.Append(IS.AccessoriesDescriptionSERA);
            strIS.Append("|");
            strIS.Append(IS.QtyAccessories);

            return strIS.ToString();
        }

        public int DeleteTempHS(CUSTOM_S02001_TEMP_HS tempHS)
        {
            int result = 1;
            try
            {
                EntityCommand cmd = new EntityCommand("EProcEntities.sp_DeleteTempHS", entityConnection);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("PONUMBER", DbType.String).Value = tempHS.PONumber;
                OpenConnection();
                cmd.ExecuteNonQuery();

            }
            catch (Exception ex)
            {
                result = 0;

                throw ex;
            }
            finally
            {
                CloseConnection();
            }
            return result;
        }

        private int DeleteTempIS(CUSTOM_S02001_TEMP_IS tempIS)
        {
            int result = 1;
            try
            {
                EntityCommand cmd = new EntityCommand("EProcEntities.sp_DeleteTempIS", entityConnection);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("PONUMBER", DbType.String).Value = tempIS.PONumber;
                OpenConnection();
                cmd.ExecuteNonQuery();

            }
            catch (Exception ex)
            {
                result = 0;

                throw ex;
            }
            finally
            {
                CloseConnection();
            }
            return result;
        }

        public bool EqualS02001HS(S02001HSViewModel item1, S02001HSViewModel item2)
        {
            //jika value sama return true, jika value beda return false
            if (item1 == null && item2 == null)
                return true;
            else if ((item1 != null && item2 == null) || (item1 == null && item2 != null))
                return false;

            var PONumber1 = !string.IsNullOrEmpty(item1.PONumber) ? item1.PONumber : "";
            var PODate1 = item1.PODate != null ? item1.PODate : Convert.ToDateTime("01/01/1900");
            var CustomerNumber1 = !string.IsNullOrEmpty(item1.CustomerNumber) ? item1.CustomerNumber : "";
            var KodeCabangAI1 = !string.IsNullOrEmpty(item1.KodeCabangAI) ? item1.KodeCabangAI : "";
            var MaterialNumberSERA1 = !string.IsNullOrEmpty(item1.MaterialNumberSERA) ? item1.MaterialNumberSERA : "";
            var MaterialDescriptionSERA1 = !string.IsNullOrEmpty(item1.MaterialDescriptionSERA) ? item1.MaterialDescriptionSERA : "";
            var MaterialNumberAI1 = !string.IsNullOrEmpty(item1.MaterialNumberAI) ? item1.MaterialNumberAI : "";
            var ColorDescSERA1 = !string.IsNullOrEmpty(item1.ColorDescSERA) ? item1.ColorDescSERA : "";
            var Quantity1 = item1.Quantity != null ? item1.Quantity : 0;

            // add fhi 01.12.2014 : penambahan field karoseri
            var NamaKaroseri1 = !string.IsNullOrEmpty(item1.NamaKaroseri) ? item1.NamaKaroseri : "";
            var AlamatKaroseri1 = !string.IsNullOrEmpty(item1.NamaKaroseri) ? item1.AlamatKaroseri : "";
            var PIC1 = !string.IsNullOrEmpty(item1.PIC) ? item1.PIC : "";
            var NoTelepon1 = !string.IsNullOrEmpty(item1.NoTelepon) ? item1.NoTelepon : "";
            var BentukKaroseri1 = !string.IsNullOrEmpty(item1.BentukKaroseri) ? item1.BentukKaroseri : "";
            var InfoPromiseDelivery1 = item1.InfoPromiseDelivery != null ? item1.InfoPromiseDelivery : Convert.ToDateTime("01/01/1900");
            //end 

            var CustomerSTNKName1 = !string.IsNullOrEmpty(item1.MaterialNumberSERA) ? item1.MaterialNumberSERA : "";
            var Title1 = item1.Title != null ? item1.Title : 0;
            var Address11 = !string.IsNullOrEmpty(item1.Address) ? item1.Address : "";
            var Address21 = !string.IsNullOrEmpty(item1.Address2) ? item1.Address2 : "";
            var Address31 = !string.IsNullOrEmpty(item1.Address3) ? item1.Address3 : "";
            var Address41 = !string.IsNullOrEmpty(item1.Address4) ? item1.Address4 : "";
            var Address51 = !string.IsNullOrEmpty(item1.Address5) ? item1.Address5 : "";
            var KTP_TDP1 = !string.IsNullOrEmpty(item1.KTP_TDP) ? item1.KTP_TDP : "";
            var PostalCode1 = !string.IsNullOrEmpty(item1.PostalCode) ? item1.PostalCode : "";
            var PartnerName1 = !string.IsNullOrEmpty(item1.PartnerName) ? item1.PartnerName : "";
            var PartnerAddress1 = !string.IsNullOrEmpty(item1.PartnerAddress) ? item1.PartnerAddress : "";
            var Telepon1 = !string.IsNullOrEmpty(item1.Telepon) ? item1.Telepon : "";
            var City1 = !string.IsNullOrEmpty(item1.City) ? item1.City : "";
            var RegionCode1 = !string.IsNullOrEmpty(item1.RegionCode) ? item1.RegionCode : "";
            var PartnerPostalCode1 = !string.IsNullOrEmpty(item1.PartnerPostalCode) ? item1.PartnerPostalCode : "";
            var Diskon1 = item1.Diskon != null ? item1.Diskon : 0;
            var Pricing1 = item1.Pricing != null ? item1.Pricing : 0;
            var CurrencyCode1 = !string.IsNullOrEmpty(item1.CurrencyCode) ? item1.CurrencyCode : "";

            var PONumber2 = !string.IsNullOrEmpty(item2.PONumber) ? item2.PONumber : "";
            var PODate2 = item2.PODate != null ? item2.PODate : Convert.ToDateTime("01/01/1900");
            var CustomerNumber2 = !string.IsNullOrEmpty(item2.CustomerNumber) ? item2.CustomerNumber : "";
            var KodeCabangAI2 = !string.IsNullOrEmpty(item2.KodeCabangAI) ? item2.KodeCabangAI : "";
            var MaterialNumberSERA2 = !string.IsNullOrEmpty(item2.MaterialNumberSERA) ? item2.MaterialNumberSERA : "";
            var MaterialDescriptionSERA2 = !string.IsNullOrEmpty(item2.MaterialDescriptionSERA) ? item2.MaterialDescriptionSERA : "";
            var MaterialNumberAI2 = !string.IsNullOrEmpty(item2.MaterialNumberAI) ? item2.MaterialNumberAI : "";
            var ColorDescSERA2 = !string.IsNullOrEmpty(item2.ColorDescSERA) ? item2.ColorDescSERA : "";
            var Quantity2 = item2.Quantity != null ? item2.Quantity : 0;

            // add fhi 01.12.2014 : penambahan field karoseri
            var NamaKaroseri2 = !string.IsNullOrEmpty(item2.NamaKaroseri) ? item2.NamaKaroseri : "";
            var AlamatKaroseri2 = !string.IsNullOrEmpty(item2.NamaKaroseri) ? item2.AlamatKaroseri : "";
            var PIC2 = !string.IsNullOrEmpty(item2.PIC) ? item2.PIC : "";
            var NoTelepon2 = !string.IsNullOrEmpty(item2.NoTelepon) ? item2.NoTelepon : "";
            var BentukKaroseri2 = !string.IsNullOrEmpty(item2.BentukKaroseri) ? item2.BentukKaroseri : "";
            var InfoPromiseDelivery2 = item2.InfoPromiseDelivery != null ? item2.InfoPromiseDelivery : Convert.ToDateTime("01/01/1900");
            //end

            var CustomerSTNKName2 = !string.IsNullOrEmpty(item2.MaterialNumberSERA) ? item2.MaterialNumberSERA : "";
            var Title2 = item2.Title != null ? item2.Title : 0;
            var Address12 = !string.IsNullOrEmpty(item2.Address) ? item2.Address : "";
            var Address22 = !string.IsNullOrEmpty(item2.Address2) ? item2.Address2 : "";
            var Address32 = !string.IsNullOrEmpty(item2.Address3) ? item2.Address3 : "";
            var Address42 = !string.IsNullOrEmpty(item2.Address4) ? item2.Address4 : "";
            var Address52 = !string.IsNullOrEmpty(item2.Address5) ? item2.Address5 : "";
            var KTP_TDP2 = !string.IsNullOrEmpty(item2.KTP_TDP) ? item2.KTP_TDP : "";
            var PostalCode2 = !string.IsNullOrEmpty(item2.PostalCode) ? item2.PostalCode : "";
            var PartnerName2 = !string.IsNullOrEmpty(item2.PartnerName) ? item2.PartnerName : "";
            var PartnerAddress2 = !string.IsNullOrEmpty(item2.PartnerAddress) ? item2.PartnerAddress : "";
            var Telepon2 = !string.IsNullOrEmpty(item2.Telepon) ? item2.Telepon : "";
            var City2 = !string.IsNullOrEmpty(item2.City) ? item2.City : "";
            var RegionCode2 = !string.IsNullOrEmpty(item2.RegionCode) ? item2.RegionCode : "";
            var PartnerPostalCode2 = !string.IsNullOrEmpty(item2.PartnerPostalCode) ? item2.PartnerPostalCode : "";
            var Diskon2 = item2.Diskon != null ? item2.Diskon : 0;
            var Pricing2 = item2.Pricing != null ? item2.Pricing : 0;
            var CurrencyCode2 = !string.IsNullOrEmpty(item2.CurrencyCode) ? item2.CurrencyCode : "";

            return PONumber1.Equals(PONumber2) &&
                PODate1.Equals(PODate2) &&
                CustomerNumber1.Equals(CustomerNumber2) &&
                KodeCabangAI1.Equals(KodeCabangAI2) &&
                MaterialNumberSERA1.Equals(MaterialNumberSERA2) &&
                MaterialDescriptionSERA1.Equals(MaterialDescriptionSERA2) &&
                MaterialNumberAI1.Equals(MaterialNumberAI2) &&
                ColorDescSERA1.Equals(ColorDescSERA2) &&
                Quantity1.Equals(Quantity2) &&

                // add fhi 01.12.2014 : penambahan field karoseri
                NamaKaroseri1.Equals(NamaKaroseri2) &&
                AlamatKaroseri1.Equals(AlamatKaroseri2) &&
                PIC1.Equals(PIC2) &&
                NoTelepon1.Equals(NoTelepon2) &&
                BentukKaroseri1.Equals(BentukKaroseri2) &&
                InfoPromiseDelivery1.Equals(InfoPromiseDelivery2) &&
                //end

                CustomerSTNKName1.Equals(CustomerSTNKName2) &&
                Title1.Equals(Title2) &&
                Address11.Equals(Address12) &&
                Address21.Equals(Address22) &&
                Address31.Equals(Address32) &&
                Address41.Equals(Address42) &&
                Address51.Equals(Address52) &&
                KTP_TDP1.Equals(KTP_TDP2) &&
                PostalCode1.Equals(PostalCode2) &&
                PartnerName1.Equals(PartnerName2) &&
                PartnerAddress1.Equals(PartnerAddress2) &&
                Telepon1.Equals(Telepon2) &&
                City1.Equals(City2) &&
                RegionCode1.Equals(RegionCode2) &&
                PartnerPostalCode1.Equals(PartnerPostalCode2) &&
                Diskon1.Equals(Diskon2) &&
                Pricing1.Equals(Pricing2) &&
                CurrencyCode1.Equals(CurrencyCode2);
        }

        public bool EqualS02001IS(S02001ISViewModel item1, S02001ISViewModel item2)
        {
            //jika value sama return true, jika value beda return false
            if (item1 == null && item2 == null)
                return true;
            else if ((item1 != null && item2 == null) || (item1 == null && item2 != null))
                return false;

            var PONumber1 = !string.IsNullOrEmpty(item1.PONumber) ? item1.PONumber : "";
            var PODate1 = item1.PODate != null ? item1.PODate : Convert.ToDateTime("01/01/1900");
            var AccessoriesNumberAI1 = !string.IsNullOrEmpty(item1.AccessoriesNumberAI) ? item1.AccessoriesNumberAI : "";
            var AccessoriesNumberSERA1 = !string.IsNullOrEmpty(item1.AccessoriesNumberSERA) ? item1.AccessoriesNumberSERA : "";
            var AccessoriesDescriptionSERA1 = !string.IsNullOrEmpty(item1.AccessoriesDescriptionSERA) ? item1.AccessoriesDescriptionSERA : "";
            var QtyAccessories1 = item1.QtyAccessories != null ? item1.QtyAccessories : 0;

            var PONumber2 = !string.IsNullOrEmpty(item2.PONumber) ? item2.PONumber : "";
            var PODate2 = item2.PODate != null ? item2.PODate : Convert.ToDateTime("01/01/1900");
            var AccessoriesNumberAI2 = !string.IsNullOrEmpty(item2.AccessoriesNumberAI) ? item2.AccessoriesNumberAI : "";
            var AccessoriesNumberSERA2 = !string.IsNullOrEmpty(item2.AccessoriesNumberSERA) ? item2.AccessoriesNumberSERA : "";
            var AccessoriesDescriptionSERA2 = !string.IsNullOrEmpty(item2.AccessoriesDescriptionSERA) ? item2.AccessoriesDescriptionSERA : "";
            var QtyAccessories2 = item2.QtyAccessories != null ? item2.QtyAccessories : 0;

            return PONumber1.Equals(PONumber2) &&
                PODate1.Equals(PODate2) &&
                AccessoriesNumberAI1.Equals(AccessoriesNumberAI2) &&
                AccessoriesNumberSERA1.Equals(AccessoriesNumberSERA2) &&
                AccessoriesDescriptionSERA1.Equals(AccessoriesDescriptionSERA2) &&
                QtyAccessories1.Equals(QtyAccessories2);
        }

        public List<CUSTOM_S02001_TEMP_HS> CheckingHistoryHSIS(List<CUSTOM_S02001_TEMP_HS> tempHS, List<CUSTOM_S02001_TEMP_IS> tempIS)
        {
            LogEvent logEvent = new LogEvent();
            List<CUSTOM_S02001_TEMP_HS> listDataHS = new List<CUSTOM_S02001_TEMP_HS>();
            List<CUSTOM_S02001_TEMP_IS> listDataIS = new List<CUSTOM_S02001_TEMP_IS>();
            List<S02001HSISViewModel> listDataHSIS = new List<S02001HSISViewModel>();
            try
            {
                if (tempHS.Count > 0)
                {
                    listDataHS = tempHS;
                    var existingRowHS = (from o in tempHS
                                         where entities.CUSTOM_S02001_HS.Any(e => o.PONumber == e.PONumber)
                                         select new S02001HSViewModel
                                         {
                                             PONumber = o.PONumber,
                                             PODate = o.PODate,
                                             Version = o.Version,
                                             CustomerNumber = o.CustomerNumber,
                                             KodeCabangAI = o.KodeCabangAI,
                                             MaterialNumberSERA = o.MaterialNumberSERA,
                                             MaterialDescriptionSERA = o.MaterialDescriptionSERA,
                                             MaterialNumberAI = o.MaterialNumberAI,
                                             ColorDescSERA = o.ColorDescSERA,
                                             Quantity = o.Quantity,

                                             // add fhi 10.12.2014 : penambahan field karoseri
                                             NamaKaroseri = o.NamaKaroseri,
                                             AlamatKaroseri = o.AlamatKaroseri,
                                             PIC = o.PIC,
                                             NoTelepon = o.NoTelepon,
                                             BentukKaroseri = o.BentukKaroseri,
                                             InfoPromiseDelivery = o.InfoPromiseDelivery,
                                             //end

                                             CustomerSTNKName = o.CustomerSTNKName,
                                             Title = o.Title,
                                             Address = o.Address,
                                             Address2 = o.Address2,
                                             Address3 = o.Address3,
                                             Address4 = o.Address4,
                                             Address5 = o.Address5,
                                             KTP_TDP = o.KTPTDP,
                                             PostalCode = o.PostalCode,
                                             PartnerName = o.PartnerName,
                                             PartnerAddress = o.PartnerAddress,
                                             Telepon = o.Telepon,
                                             City = o.City,
                                             RegionCode = o.RegionCode,
                                             PartnerPostalCode = o.PartnerPostalCode,
                                             Diskon = o.Diskon,
                                             Pricing = o.Pricing,
                                             CurrencyCode = o.CurrencyCode,
                                             CompanyCodeAI = o.CompanyCodeAI
                                         }).ToList();

                    for (int i = 0; i < existingRowHS.Count; i++)
                    {
                        string existPoNumber = existingRowHS[i].PONumber;
                        var q = (from o in entities.CUSTOM_S02001_HS
                                 where o.PONumber == existPoNumber
                                 select new S02001HSViewModel
                                 {
                                     PONumber = o.PONumber,
                                     PODate = o.PODate,
                                     Version = o.Version,
                                     CustomerNumber = o.CustomerNumber,
                                     KodeCabangAI = o.KodeCabangAI,
                                     MaterialNumberSERA = o.MaterialNumberSERA,
                                     MaterialDescriptionSERA = o.MaterialDescriptionSERA,
                                     MaterialNumberAI = o.MaterialNumberAI,
                                     ColorDescSERA = o.ColorDescSERA,
                                     Quantity = o.Quantity,

                                     // add fhi 10.12.2014 : penambahan field karoseri
                                     NamaKaroseri = o.NamaKaroseri,
                                     AlamatKaroseri = o.AlamatKaroseri,
                                     PIC = o.PIC,
                                     NoTelepon = o.NoTelepon,
                                     BentukKaroseri = o.BentukKaroseri,
                                     InfoPromiseDelivery = o.InfoPromiseDelivery,
                                     //end

                                     CustomerSTNKName = o.CustomerSTNKName,
                                     Title = o.Title,
                                     Address = o.Address,
                                     Address2 = o.Address2,
                                     Address3 = o.Address3,
                                     Address4 = o.Address4,
                                     Address5 = o.Address5,
                                     KTP_TDP = o.KTPTDP,
                                     PostalCode = o.PostalCode,
                                     PartnerName = o.PartnerName,
                                     PartnerAddress = o.PartnerAddress,
                                     Telepon = o.Telepon,
                                     City = o.City,
                                     RegionCode = o.RegionCode,
                                     PartnerPostalCode = o.PartnerPostalCode,
                                     Diskon = o.Diskon,
                                     Pricing = o.Pricing,
                                     CurrencyCode = o.CurrencyCode
                                 }).OrderByDescending(o => o.Version).SingleOrDefault();

                        var compareHS = EqualS02001HS(existingRowHS[i], q);

                        // jika di HS sama persis tiap fieldnya, cek di IS nya
                        if (compareHS)
                        {
                            //compareIS
                            if (tempIS.Count > 0)
                            {
                                listDataIS = tempIS;
                                var existingRowIS = (from o in tempIS
                                                     where entities.CUSTOM_S02001_IS.Any(e => o.PONumber == e.PONumber)
                                                        && o.PONumber == existPoNumber
                                                     select new S02001ISViewModel
                                                     {
                                                         PONumber = o.PONumber,
                                                         PODate = o.PODate,
                                                         DataVersion = o.DataVersion,
                                                         AccessoriesNumberAI = o.AccessoriesNumberAI,
                                                         AccessoriesNumberSERA = o.AccessoriesNumberSERA,
                                                         AccessoriesDescriptionSERA = o.AccessoriesDescriptionSERA,
                                                         QtyAccessories = o.QtyAccessories
                                                     }).ToList();

                                for (int j = 0; j < existingRowIS.Count; j++)
                                {
                                    if (existingRowIS[j].PONumber == existingRowHS[i].PONumber)
                                    {
                                        string existPoNumberIS = existingRowIS[j].PONumber;
                                        var z = (from o in entities.CUSTOM_S02001_IS
                                                 where o.PONumber == existPoNumberIS
                                                 select new S02001ISViewModel
                                                 {
                                                     PONumber = o.PONumber,
                                                     PODate = o.PODate,
                                                     DataVersion = o.DataVersion,
                                                     AccessoriesNumberAI = o.AccessoriesNumberAI,
                                                     AccessoriesNumberSERA = o.AccessoriesNumberSERA,
                                                     AccessoriesDescriptionSERA = o.AccessoriesDescriptionSERA,
                                                     QtyAccessories = o.QtyAccessories
                                                 }).OrderByDescending(o => o.DataVersion).SingleOrDefault();
                                        var compareIS = EqualS02001IS(existingRowIS[j], z);

                                        //jika di HS dan IS sama persis tiap fieldnya, maka delete row di list
                                        if (compareIS)
                                        {
                                            //remove row di HS dan IS
                                            var rowDelHS = (from o in listDataHS
                                                            where o.PONumber == existingRowHS[i].PONumber
                                                            select o).SingleOrDefault();
                                            listDataHS.Remove(rowDelHS);

                                            //DeleteTempHSByPoNumber(existingRowHS[i].PONumber);

                                            var rowDelIS = (from o in listDataIS
                                                            where o.PONumber == existingRowIS[j].PONumber
                                                            select o).SingleOrDefault();
                                            listDataIS.Remove(rowDelIS);

                                            //DeleteTempISByPoNumber(existingRowIS[i].PONumber);
                                        }

                                        //jika di HS sama persis tapi di IS ada yag berubah, maka update row di list IS saja
                                        else if (!compareIS)
                                        {
                                            //NEW SIT
                                            //remove row HS
                                            var rowDel = (from o in listDataHS
                                                          where o.PONumber == existingRowHS[i].PONumber
                                                          select o).SingleOrDefault();
                                            listDataHS.Remove(rowDel);

                                            //add new row HS
                                            CUSTOM_S02001_TEMP_HS row = new CUSTOM_S02001_TEMP_HS();
                                            row.PONumber = existingRowHS[i].PONumber;
                                            row.PODate = existingRowHS[i].PODate;

                                            row.Version = q.Version != null ? q.Version + 1 : existingRowHS[i].Version != null ? existingRowHS[i].Version : 1;
                                            row.CustomerNumber = existingRowHS[i].CustomerNumber;
                                            row.KodeCabangAI = existingRowHS[i].KodeCabangAI;
                                            row.MaterialNumberSERA = existingRowHS[i].MaterialNumberSERA;
                                            row.MaterialDescriptionSERA = existingRowHS[i].MaterialDescriptionSERA;
                                            row.MaterialNumberAI = existingRowHS[i].MaterialNumberAI;
                                            row.ColorDescSERA = existingRowHS[i].ColorDescSERA;
                                            row.Quantity = existingRowHS[i].Quantity;

                                            // add fhi 10.12.2014 : penambahan field karoseri
                                            row.NamaKaroseri = existingRowHS[i].NamaKaroseri;
                                            row.AlamatKaroseri = existingRowHS[i].AlamatKaroseri;
                                            row.PIC = existingRowHS[i].PIC;
                                            row.NoTelepon = existingRowHS[i].NoTelepon;
                                            row.BentukKaroseri = existingRowHS[i].BentukKaroseri;
                                            row.InfoPromiseDelivery = existingRowHS[i].InfoPromiseDelivery;
                                            //end

                                            row.CustomerSTNKName = existingRowHS[i].CustomerSTNKName;
                                            row.Title = existingRowHS[i].Title;
                                            row.Address = existingRowHS[i].Address;
                                            row.Address2 = existingRowHS[i].Address2;
                                            row.Address3 = existingRowHS[i].Address3;
                                            row.Address4 = existingRowHS[i].Address4;
                                            row.Address5 = existingRowHS[i].Address5;
                                            row.KTPTDP = existingRowHS[i].KTP_TDP;
                                            row.PostalCode = existingRowHS[i].PostalCode;
                                            row.PartnerName = existingRowHS[i].PartnerName;
                                            row.PartnerAddress = existingRowHS[i].PartnerAddress;
                                            row.Telepon = existingRowHS[i].Telepon;
                                            row.City = existingRowHS[i].City;
                                            row.RegionCode = existingRowHS[i].RegionCode;
                                            row.PartnerPostalCode = existingRowHS[i].PartnerPostalCode;
                                            row.Diskon = existingRowHS[i].Diskon;
                                            row.Pricing = existingRowHS[i].Pricing;
                                            row.CurrencyCode = existingRowHS[i].CurrencyCode;
                                            row.CompanyCodeAI = existingRowHS[i].CompanyCodeAI;

                                            listDataHS.Add(row);
                                            //END: NEW SIT

                                            //remove row IS
                                            var rowDelIS = (from o in listDataIS
                                                            where o.PONumber == existingRowIS[j].PONumber
                                                            select o).SingleOrDefault();
                                            listDataIS.Remove(rowDelIS);

                                            //add new row IS
                                            CUSTOM_S02001_TEMP_IS rowIS = new CUSTOM_S02001_TEMP_IS();
                                            rowIS.PONumber = existingRowIS[j].PONumber;
                                            rowIS.PODate = existingRowIS[j].PODate;
                                            rowIS.DataVersion = z.DataVersion != null ? z.DataVersion + 1 : existingRowIS[j].DataVersion != null ? existingRowIS[j].DataVersion : 1;
                                            rowIS.AccessoriesNumberAI = existingRowIS[j].AccessoriesNumberAI;
                                            rowIS.AccessoriesNumberSERA = existingRowIS[j].AccessoriesNumberSERA;
                                            rowIS.AccessoriesDescriptionSERA = existingRowIS[j].AccessoriesDescriptionSERA;
                                            rowIS.QtyAccessories = existingRowIS[j].QtyAccessories;
                                            listDataIS.Add(rowIS);
                                        }
                                    }
                                }
                            }
                        }
                        //jika di HS ada yg berubah di salah satu field atau lebih, maka cek di IS nya 
                        else if (!compareHS)
                        {
                            //compareIS
                            if (tempIS.Count > 0)
                            {
                                listDataIS = tempIS;
                                var existingRowIS = (from o in tempIS
                                                     where entities.CUSTOM_S02001_IS.Any(e => o.PONumber == e.PONumber)
                                                        && o.PONumber == existPoNumber
                                                     select new S02001ISViewModel
                                                     {
                                                         PONumber = o.PONumber,
                                                         PODate = o.PODate,
                                                         DataVersion = o.DataVersion,
                                                         AccessoriesNumberAI = o.AccessoriesNumberAI,
                                                         AccessoriesNumberSERA = o.AccessoriesNumberSERA,
                                                         AccessoriesDescriptionSERA = o.AccessoriesDescriptionSERA,
                                                         QtyAccessories = o.QtyAccessories
                                                     }).ToList();

                                for (int j = 0; j < existingRowIS.Count; j++)
                                {
                                    if (existingRowIS[j].PONumber == existingRowHS[i].PONumber)
                                    {
                                        string existPoNumberIS = existingRowIS[j].PONumber;
                                        var z = (from o in entities.CUSTOM_S02001_IS
                                                 where o.PONumber == existPoNumberIS
                                                 select new S02001ISViewModel
                                                 {
                                                     PONumber = o.PONumber,
                                                     PODate = o.PODate,
                                                     DataVersion = o.DataVersion,
                                                     AccessoriesNumberAI = o.AccessoriesNumberAI,
                                                     AccessoriesNumberSERA = o.AccessoriesNumberSERA,
                                                     AccessoriesDescriptionSERA = o.AccessoriesDescriptionSERA,
                                                     QtyAccessories = o.QtyAccessories
                                                 }).OrderByDescending(o => o.DataVersion).SingleOrDefault();
                                        var compareIS = EqualS02001IS(existingRowIS[j], z);

                                        //jika di HS ada yg berubah tapi di IS sama persis tiap fieldnya, maka update row di list HS saja
                                        if (compareIS)
                                        {
                                            //remove row HS
                                            var rowDel = (from o in listDataHS
                                                          where o.PONumber == existingRowHS[i].PONumber
                                                          select o).SingleOrDefault();
                                            listDataHS.Remove(rowDel);

                                            //add new row HS
                                            CUSTOM_S02001_TEMP_HS row = new CUSTOM_S02001_TEMP_HS();
                                            row.PONumber = existingRowHS[i].PONumber;
                                            row.PODate = existingRowHS[i].PODate;
                                            row.Version = q.Version != null ? q.Version + 1 : existingRowHS[i].Version != null ? existingRowHS[i].Version : 1;
                                            row.CustomerNumber = existingRowHS[i].CustomerNumber;
                                            row.KodeCabangAI = existingRowHS[i].KodeCabangAI;
                                            row.MaterialNumberSERA = existingRowHS[i].MaterialNumberSERA;
                                            row.MaterialDescriptionSERA = existingRowHS[i].MaterialDescriptionSERA;
                                            row.MaterialNumberAI = existingRowHS[i].MaterialNumberAI;
                                            row.ColorDescSERA = existingRowHS[i].ColorDescSERA;
                                            row.Quantity = existingRowHS[i].Quantity;

                                            // add fhi 10.12.2014 : penambahan field karoseri
                                            row.NamaKaroseri = existingRowHS[i].NamaKaroseri;
                                            row.AlamatKaroseri = existingRowHS[i].AlamatKaroseri;
                                            row.PIC = existingRowHS[i].PIC;
                                            row.NoTelepon = existingRowHS[i].NoTelepon;
                                            row.BentukKaroseri = existingRowHS[i].BentukKaroseri;
                                            row.InfoPromiseDelivery = existingRowHS[i].InfoPromiseDelivery;
                                            //end

                                            row.CustomerSTNKName = existingRowHS[i].CustomerSTNKName;
                                            row.Title = existingRowHS[i].Title;
                                            row.Address = existingRowHS[i].Address;
                                            row.Address2 = existingRowHS[i].Address2;
                                            row.Address3 = existingRowHS[i].Address3;
                                            row.Address4 = existingRowHS[i].Address4;
                                            row.Address5 = existingRowHS[i].Address5;
                                            row.KTPTDP = existingRowHS[i].KTP_TDP;
                                            row.PostalCode = existingRowHS[i].PostalCode;
                                            row.PartnerName = existingRowHS[i].PartnerName;
                                            row.PartnerAddress = existingRowHS[i].PartnerAddress;
                                            row.Telepon = existingRowHS[i].Telepon;
                                            row.City = existingRowHS[i].City;
                                            row.RegionCode = existingRowHS[i].RegionCode;
                                            row.PartnerPostalCode = existingRowHS[i].PartnerPostalCode;
                                            row.Diskon = existingRowHS[i].Diskon;
                                            row.Pricing = existingRowHS[i].Pricing;
                                            row.CurrencyCode = existingRowHS[i].CurrencyCode;
                                            row.CompanyCodeAI = existingRowHS[i].CompanyCodeAI;
                                            listDataHS.Add(row);

                                            //NEW SIT
                                            //remove row IS
                                            var rowDelIS = (from o in listDataIS
                                                            where o.PONumber == existingRowIS[j].PONumber
                                                            select o).SingleOrDefault();
                                            listDataIS.Remove(rowDelIS);

                                            //add new row IS
                                            CUSTOM_S02001_TEMP_IS rowIS = new CUSTOM_S02001_TEMP_IS();
                                            rowIS.PONumber = existingRowIS[j].PONumber;
                                            rowIS.PODate = existingRowIS[j].PODate;
                                            rowIS.DataVersion = z.DataVersion != null ? z.DataVersion + 1 : existingRowIS[j].DataVersion != null ? existingRowIS[j].DataVersion : 1;
                                            rowIS.AccessoriesNumberAI = existingRowIS[j].AccessoriesNumberAI;
                                            rowIS.AccessoriesNumberSERA = existingRowIS[j].AccessoriesNumberSERA;
                                            rowIS.AccessoriesDescriptionSERA = existingRowIS[j].AccessoriesDescriptionSERA;
                                            rowIS.QtyAccessories = existingRowIS[j].QtyAccessories;
                                            listDataIS.Add(rowIS);
                                            //END: NEW SIT
                                        }

                                        //jika di HS ada yg berubah dan di IS pun ada yag berubah, maka update row di list HS dan IS
                                        else if (!compareIS)
                                        {
                                            //remove row HS
                                            var rowDel = (from o in listDataHS
                                                          where o.PONumber == existingRowHS[i].PONumber
                                                          select o).SingleOrDefault();
                                            listDataHS.Remove(rowDel);

                                            //add new row HS
                                            CUSTOM_S02001_TEMP_HS row = new CUSTOM_S02001_TEMP_HS();
                                            row.PONumber = existingRowHS[i].PONumber;
                                            row.PODate = existingRowHS[i].PODate;
                                            row.Version = q.Version != null ? q.Version + 1 : existingRowHS[i].Version != null ? existingRowHS[i].Version : 1;
                                            row.CustomerNumber = existingRowHS[i].CustomerNumber;
                                            row.KodeCabangAI = existingRowHS[i].KodeCabangAI;
                                            row.MaterialNumberSERA = existingRowHS[i].MaterialNumberSERA;
                                            row.MaterialDescriptionSERA = existingRowHS[i].MaterialDescriptionSERA;
                                            row.MaterialNumberAI = existingRowHS[i].MaterialNumberAI;
                                            row.ColorDescSERA = existingRowHS[i].ColorDescSERA;
                                            row.Quantity = existingRowHS[i].Quantity;

                                            // add fhi 10.12.2014 : penambahan field karoseri
                                            row.NamaKaroseri = existingRowHS[i].NamaKaroseri;
                                            row.AlamatKaroseri = existingRowHS[i].AlamatKaroseri;
                                            row.PIC = existingRowHS[i].PIC;
                                            row.NoTelepon = existingRowHS[i].NoTelepon;
                                            row.BentukKaroseri = existingRowHS[i].BentukKaroseri;
                                            row.InfoPromiseDelivery = existingRowHS[i].InfoPromiseDelivery;
                                            //end

                                            row.CustomerSTNKName = existingRowHS[i].CustomerSTNKName;
                                            row.Title = existingRowHS[i].Title;
                                            row.Address = existingRowHS[i].Address;
                                            row.Address2 = existingRowHS[i].Address2;
                                            row.Address3 = existingRowHS[i].Address3;
                                            row.Address4 = existingRowHS[i].Address4;
                                            row.Address5 = existingRowHS[i].Address5;
                                            row.KTPTDP = existingRowHS[i].KTP_TDP;
                                            row.PostalCode = existingRowHS[i].PostalCode;
                                            row.PartnerName = existingRowHS[i].PartnerName;
                                            row.PartnerAddress = existingRowHS[i].PartnerAddress;
                                            row.Telepon = existingRowHS[i].Telepon;
                                            row.City = existingRowHS[i].City;
                                            row.RegionCode = existingRowHS[i].RegionCode;
                                            row.PartnerPostalCode = existingRowHS[i].PartnerPostalCode;
                                            row.Diskon = existingRowHS[i].Diskon;
                                            row.Pricing = existingRowHS[i].Pricing;
                                            row.CurrencyCode = existingRowHS[i].CurrencyCode;
                                            row.CompanyCodeAI = existingRowHS[i].CompanyCodeAI;
                                            listDataHS.Add(row);

                                            //remove row IS
                                            var rowDelIS = (from o in listDataIS
                                                            where o.PONumber == existingRowIS[j].PONumber
                                                            select o).SingleOrDefault();
                                            listDataIS.Remove(rowDelIS);

                                            //add new row IS
                                            CUSTOM_S02001_TEMP_IS rowIS = new CUSTOM_S02001_TEMP_IS();
                                            rowIS.PONumber = existingRowIS[j].PONumber;
                                            rowIS.PODate = existingRowIS[j].PODate;
                                            rowIS.DataVersion = z.DataVersion != null ? z.DataVersion + 1 : existingRowIS[j].DataVersion != null ? existingRowIS[j].DataVersion : 1;
                                            rowIS.AccessoriesNumberAI = existingRowIS[j].AccessoriesNumberAI;
                                            rowIS.AccessoriesNumberSERA = existingRowIS[j].AccessoriesNumberSERA;
                                            rowIS.AccessoriesDescriptionSERA = existingRowIS[j].AccessoriesDescriptionSERA;
                                            rowIS.QtyAccessories = existingRowIS[j].QtyAccessories;
                                            listDataIS.Add(rowIS);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                //add task kill : by  fhi 05.06.2014
                logEvent.WriteDBLog("", "UploadS02001_Load", false, "", ex.Message, "S02001", "SERA");
                Process.Start("taskkill.exe", "/f /im B2BAISERA_S02001.exe");
                //end
                throw ex;
            }
            return listDataHS;
        }

        public List<CUSTOM_S02001_TEMP_IS> CheckingHistoryHSIS2(List<CUSTOM_S02001_TEMP_HS> tempHS, List<CUSTOM_S02001_TEMP_IS> tempIS)
        {
            LogEvent logEvent = new LogEvent();
            List<CUSTOM_S02001_TEMP_HS> listDataHS = new List<CUSTOM_S02001_TEMP_HS>();
            List<CUSTOM_S02001_TEMP_IS> listDataIS = new List<CUSTOM_S02001_TEMP_IS>();
            List<S02001HSISViewModel> listDataHSIS = new List<S02001HSISViewModel>();
            try
            {
                if (tempHS.Count > 0)
                {
                    listDataHS = tempHS;
                    listDataIS = tempIS;
                    var existingRowHS = (from o in tempHS
                                         where entities.CUSTOM_S02001_HS.Any(e => o.PONumber == e.PONumber)
                                         select new S02001HSViewModel
                                         {
                                             PONumber = o.PONumber,
                                             PODate = o.PODate,
                                             Version = o.Version,
                                             CustomerNumber = o.CustomerNumber,
                                             KodeCabangAI = o.KodeCabangAI,
                                             MaterialNumberSERA = o.MaterialNumberSERA,
                                             MaterialDescriptionSERA = o.MaterialDescriptionSERA,
                                             MaterialNumberAI = o.MaterialNumberAI,
                                             ColorDescSERA = o.ColorDescSERA,
                                             Quantity = o.Quantity,

                                             // add fhi 10.12.2014 : penambahan field karoseri
                                             NamaKaroseri = o.NamaKaroseri,
                                             AlamatKaroseri = o.AlamatKaroseri,
                                             PIC = o.PIC,
                                             NoTelepon = o.NoTelepon,
                                             BentukKaroseri = o.BentukKaroseri,
                                             InfoPromiseDelivery = o.InfoPromiseDelivery,
                                             //end

                                             CustomerSTNKName = o.CustomerSTNKName,
                                             Title = o.Title,
                                             Address = o.Address,
                                             Address2 = o.Address2,
                                             Address3 = o.Address3,
                                             Address4 = o.Address4,
                                             Address5 = o.Address5,
                                             KTP_TDP = o.KTPTDP,
                                             PostalCode = o.PostalCode,
                                             PartnerName = o.PartnerName,
                                             PartnerAddress = o.PartnerAddress,
                                             Telepon = o.Telepon,
                                             City = o.City,
                                             RegionCode = o.RegionCode,
                                             PartnerPostalCode = o.PartnerPostalCode,
                                             Diskon = o.Diskon,
                                             Pricing = o.Pricing,
                                             CurrencyCode = o.CurrencyCode,
                                             CompanyCodeAI = o.CompanyCodeAI
                                         }).ToList();

                    for (int i = 0; i < existingRowHS.Count; i++)
                    {
                        string existPoNumber = existingRowHS[i].PONumber;
                        var q = (from o in entities.CUSTOM_S02001_HS
                                 where o.PONumber == existPoNumber
                                 select new S02001HSViewModel
                                 {
                                     PONumber = o.PONumber,
                                     PODate = o.PODate,
                                     Version = o.Version,
                                     CustomerNumber = o.CustomerNumber,
                                     KodeCabangAI = o.KodeCabangAI,
                                     MaterialNumberSERA = o.MaterialNumberSERA,
                                     MaterialDescriptionSERA = o.MaterialDescriptionSERA,
                                     MaterialNumberAI = o.MaterialNumberAI,
                                     ColorDescSERA = o.ColorDescSERA,
                                     Quantity = o.Quantity,

                                     // add fhi 10.12.2014 : penambahan field karoseri
                                     NamaKaroseri = o.NamaKaroseri,
                                     AlamatKaroseri = o.AlamatKaroseri,
                                     PIC = o.PIC,
                                     NoTelepon = o.NoTelepon,
                                     BentukKaroseri = o.BentukKaroseri,
                                     InfoPromiseDelivery = o.InfoPromiseDelivery,
                                     //end

                                     CustomerSTNKName = o.CustomerSTNKName,
                                     Title = o.Title,
                                     Address = o.Address,
                                     Address2 = o.Address2,
                                     Address3 = o.Address3,
                                     Address4 = o.Address4,
                                     Address5 = o.Address5,
                                     KTP_TDP = o.KTPTDP,
                                     PostalCode = o.PostalCode,
                                     PartnerName = o.PartnerName,
                                     PartnerAddress = o.PartnerAddress,
                                     Telepon = o.Telepon,
                                     City = o.City,
                                     RegionCode = o.RegionCode,
                                     PartnerPostalCode = o.PartnerPostalCode,
                                     Diskon = o.Diskon,
                                     Pricing = o.Pricing,
                                     CurrencyCode = o.CurrencyCode
                                 }).OrderByDescending(o => o.Version).SingleOrDefault();

                        var compareHS = EqualS02001HS(existingRowHS[i], q);

                        // jika di HS sama persis tiap fieldnya, cek di IS nya
                        if (compareHS)
                        {
                            //compareIS
                            if (tempIS.Count > 0)
                            {
                                listDataIS = tempIS;
                                var existingRowIS = (from o in tempIS
                                                     where entities.CUSTOM_S02001_IS.Any(e => o.PONumber == e.PONumber)
                                                        && o.PONumber == existPoNumber
                                                     select new S02001ISViewModel
                                                     {
                                                         PONumber = o.PONumber,
                                                         PODate = o.PODate,
                                                         DataVersion = o.DataVersion,
                                                         AccessoriesNumberAI = o.AccessoriesNumberAI,
                                                         AccessoriesNumberSERA = o.AccessoriesNumberSERA,
                                                         AccessoriesDescriptionSERA = o.AccessoriesDescriptionSERA,
                                                         QtyAccessories = o.QtyAccessories
                                                     }).ToList();

                                for (int j = 0; j < existingRowIS.Count; j++)
                                {
                                    if (existingRowIS[j].PONumber == existingRowHS[i].PONumber)
                                    {
                                        string existPoNumberIS = existingRowIS[j].PONumber;
                                        var z = (from o in entities.CUSTOM_S02001_IS
                                                 where o.PONumber == existPoNumberIS
                                                 select new S02001ISViewModel
                                                 {
                                                     PONumber = o.PONumber,
                                                     PODate = o.PODate,
                                                     DataVersion = o.DataVersion,
                                                     AccessoriesNumberAI = o.AccessoriesNumberAI,
                                                     AccessoriesNumberSERA = o.AccessoriesNumberSERA,
                                                     AccessoriesDescriptionSERA = o.AccessoriesDescriptionSERA,
                                                     QtyAccessories = o.QtyAccessories
                                                 }).OrderByDescending(o => o.DataVersion).SingleOrDefault();
                                        var compareIS = EqualS02001IS(existingRowIS[j], z);

                                        //jika di HS dan IS sama persis tiap fieldnya, maka delete row di list
                                        if (compareIS)
                                        {
                                            //remove row di HS dan IS
                                            var rowDelHS = (from o in listDataHS
                                                            where o.PONumber == existingRowHS[i].PONumber
                                                            select o).SingleOrDefault();
                                            listDataHS.Remove(rowDelHS);

                                            //DeleteTempHSByPoNumber(existingRowHS[i].PONumber);

                                            var rowDelIS = (from o in listDataIS
                                                            where o.PONumber == existingRowIS[j].PONumber
                                                            select o).SingleOrDefault();
                                            listDataIS.Remove(rowDelIS);

                                            //DeleteTempISByPoNumber(existingRowIS[i].PONumber);
                                        }

                                        //jika di HS sama persis tapi di IS ada yag berubah, maka update row di list IS saja
                                        else if (!compareIS)
                                        {
                                            //NEW SIT
                                            //remove row HS
                                            var rowDel = (from o in listDataHS
                                                          where o.PONumber == existingRowHS[i].PONumber
                                                          select o).SingleOrDefault();
                                            listDataHS.Remove(rowDel);

                                            //add new row HS
                                            CUSTOM_S02001_TEMP_HS row = new CUSTOM_S02001_TEMP_HS();
                                            row.PONumber = existingRowHS[i].PONumber;
                                            row.PODate = existingRowHS[i].PODate;
                                            row.Version = q.Version != null ? q.Version + 1 : existingRowHS[i].Version != null ? existingRowHS[i].Version : 1;
                                            row.CustomerNumber = existingRowHS[i].CustomerNumber;
                                            row.KodeCabangAI = existingRowHS[i].KodeCabangAI;
                                            row.MaterialNumberSERA = existingRowHS[i].MaterialNumberSERA;
                                            row.MaterialDescriptionSERA = existingRowHS[i].MaterialDescriptionSERA;
                                            row.MaterialNumberAI = existingRowHS[i].MaterialNumberAI;
                                            row.ColorDescSERA = existingRowHS[i].ColorDescSERA;
                                            row.Quantity = existingRowHS[i].Quantity;

                                            // add fhi 10.12.2014 : penambahan field karoseri
                                            row.NamaKaroseri = existingRowHS[i].NamaKaroseri;
                                            row.AlamatKaroseri = existingRowHS[i].AlamatKaroseri;
                                            row.PIC = existingRowHS[i].PIC;
                                            row.NoTelepon = existingRowHS[i].NoTelepon;
                                            row.BentukKaroseri = existingRowHS[i].BentukKaroseri;
                                            row.InfoPromiseDelivery = existingRowHS[i].InfoPromiseDelivery;
                                            //end

                                            row.CustomerSTNKName = existingRowHS[i].CustomerSTNKName;
                                            row.Title = existingRowHS[i].Title;
                                            row.Address = existingRowHS[i].Address;
                                            row.Address2 = existingRowHS[i].Address2;
                                            row.Address3 = existingRowHS[i].Address3;
                                            row.Address4 = existingRowHS[i].Address4;
                                            row.Address5 = existingRowHS[i].Address5;
                                            row.KTPTDP = existingRowHS[i].KTP_TDP;
                                            row.PostalCode = existingRowHS[i].PostalCode;
                                            row.PartnerName = existingRowHS[i].PartnerName;
                                            row.PartnerAddress = existingRowHS[i].PartnerAddress;
                                            row.Telepon = existingRowHS[i].Telepon;
                                            row.City = existingRowHS[i].City;
                                            row.RegionCode = existingRowHS[i].RegionCode;
                                            row.PartnerPostalCode = existingRowHS[i].PartnerPostalCode;
                                            row.Diskon = existingRowHS[i].Diskon;
                                            row.Pricing = existingRowHS[i].Pricing;
                                            row.CurrencyCode = existingRowHS[i].CurrencyCode;
                                            row.CompanyCodeAI = existingRowHS[i].CompanyCodeAI;
                                            listDataHS.Add(row);
                                            //END: NEW SIT

                                            //remove row IS
                                            var rowDelIS = (from o in listDataIS
                                                            where o.PONumber == existingRowIS[j].PONumber
                                                            select o).SingleOrDefault();
                                            listDataIS.Remove(rowDelIS);

                                            //add new row IS
                                            CUSTOM_S02001_TEMP_IS rowIS = new CUSTOM_S02001_TEMP_IS();
                                            rowIS.PONumber = existingRowIS[j].PONumber;
                                            rowIS.PODate = existingRowIS[j].PODate;
                                            rowIS.DataVersion = z.DataVersion != null ? z.DataVersion + 1 : existingRowIS[j].DataVersion != null ? existingRowIS[j].DataVersion : 1;
                                            rowIS.AccessoriesNumberAI = existingRowIS[j].AccessoriesNumberAI;
                                            rowIS.AccessoriesNumberSERA = existingRowIS[j].AccessoriesNumberSERA;
                                            rowIS.AccessoriesDescriptionSERA = existingRowIS[j].AccessoriesDescriptionSERA;
                                            rowIS.QtyAccessories = existingRowIS[j].QtyAccessories;
                                            listDataIS.Add(rowIS);
                                        }
                                    }
                                }
                            }
                        }
                        //jika di HS ada yg berubah di salah satu field atau lebih, maka cek di IS nya 
                        else if (!compareHS)
                        {
                            //compareIS
                            if (tempIS.Count > 0)
                            {
                                //listDataIS = tempIS;
                                var existingRowIS = (from o in tempIS
                                                     where entities.CUSTOM_S02001_IS.Any(e => o.PONumber == e.PONumber)
                                                        && o.PONumber == existPoNumber
                                                     select new S02001ISViewModel
                                                     {
                                                         PONumber = o.PONumber,
                                                         PODate = o.PODate,
                                                         DataVersion = o.DataVersion,
                                                         AccessoriesNumberAI = o.AccessoriesNumberAI,
                                                         AccessoriesNumberSERA = o.AccessoriesNumberSERA,
                                                         AccessoriesDescriptionSERA = o.AccessoriesDescriptionSERA,
                                                         QtyAccessories = o.QtyAccessories
                                                     }).ToList();

                                for (int j = 0; j < existingRowIS.Count; j++)
                                {
                                    if (existingRowIS[j].PONumber == existingRowHS[i].PONumber)
                                    {
                                        string existPoNumberIS = existingRowIS[j].PONumber;
                                        var z = (from o in entities.CUSTOM_S02001_IS
                                                 where o.PONumber == existPoNumberIS
                                                 select new S02001ISViewModel
                                                 {
                                                     PONumber = o.PONumber,
                                                     PODate = o.PODate,
                                                     DataVersion = o.DataVersion,
                                                     AccessoriesNumberAI = o.AccessoriesNumberAI,
                                                     AccessoriesNumberSERA = o.AccessoriesNumberSERA,
                                                     AccessoriesDescriptionSERA = o.AccessoriesDescriptionSERA,
                                                     QtyAccessories = o.QtyAccessories
                                                 }).OrderByDescending(o => o.DataVersion).SingleOrDefault();
                                        var compareIS = EqualS02001IS(existingRowIS[j], z);

                                        //jika di HS ada yg berubah tapi di IS sama persis tiap fieldnya, maka update row di list HS saja
                                        if (compareIS)
                                        {
                                            //remove row HS
                                            var rowDel = (from o in listDataHS
                                                          where o.PONumber == existingRowHS[i].PONumber
                                                          select o).SingleOrDefault();
                                            listDataHS.Remove(rowDel);

                                            //add new row HS
                                            CUSTOM_S02001_TEMP_HS row = new CUSTOM_S02001_TEMP_HS();
                                            row.PONumber = existingRowHS[i].PONumber;
                                            row.PODate = existingRowHS[i].PODate;
                                            row.Version = q.Version != null ? q.Version + 1 : existingRowHS[i].Version != null ? existingRowHS[i].Version : 1;
                                            row.CustomerNumber = existingRowHS[i].CustomerNumber;
                                            row.KodeCabangAI = existingRowHS[i].KodeCabangAI;
                                            row.MaterialNumberSERA = existingRowHS[i].MaterialNumberSERA;
                                            row.MaterialDescriptionSERA = existingRowHS[i].MaterialDescriptionSERA;
                                            row.MaterialNumberAI = existingRowHS[i].MaterialNumberAI;
                                            row.ColorDescSERA = existingRowHS[i].ColorDescSERA;
                                            row.Quantity = existingRowHS[i].Quantity;

                                            // add fhi 10.12.2014 : penambahan field karoseri
                                            row.NamaKaroseri = existingRowHS[i].NamaKaroseri;
                                            row.AlamatKaroseri = existingRowHS[i].AlamatKaroseri;
                                            row.PIC = existingRowHS[i].PIC;
                                            row.NoTelepon = existingRowHS[i].NoTelepon;
                                            row.BentukKaroseri = existingRowHS[i].BentukKaroseri;
                                            row.InfoPromiseDelivery = existingRowHS[i].InfoPromiseDelivery;
                                            //end

                                            row.CustomerSTNKName = existingRowHS[i].CustomerSTNKName;
                                            row.Title = existingRowHS[i].Title;
                                            row.Address = existingRowHS[i].Address;
                                            row.Address2 = existingRowHS[i].Address2;
                                            row.Address3 = existingRowHS[i].Address3;
                                            row.Address4 = existingRowHS[i].Address4;
                                            row.Address5 = existingRowHS[i].Address5;
                                            row.KTPTDP = existingRowHS[i].KTP_TDP;
                                            row.PostalCode = existingRowHS[i].PostalCode;
                                            row.PartnerName = existingRowHS[i].PartnerName;
                                            row.PartnerAddress = existingRowHS[i].PartnerAddress;
                                            row.Telepon = existingRowHS[i].Telepon;
                                            row.City = existingRowHS[i].City;
                                            row.RegionCode = existingRowHS[i].RegionCode;
                                            row.PartnerPostalCode = existingRowHS[i].PartnerPostalCode;
                                            row.Diskon = existingRowHS[i].Diskon;
                                            row.Pricing = existingRowHS[i].Pricing;
                                            row.CurrencyCode = existingRowHS[i].CurrencyCode;
                                            row.CompanyCodeAI = existingRowHS[i].CompanyCodeAI;
                                            listDataHS.Add(row);

                                            //NEW SIT
                                            //remove row IS
                                            var rowDelIS = (from o in listDataIS
                                                            where o.PONumber == existingRowIS[j].PONumber
                                                            select o).SingleOrDefault();
                                            listDataIS.Remove(rowDelIS);

                                            //add new row IS
                                            CUSTOM_S02001_TEMP_IS rowIS = new CUSTOM_S02001_TEMP_IS();
                                            rowIS.PONumber = existingRowIS[j].PONumber;
                                            rowIS.PODate = existingRowIS[j].PODate;
                                            rowIS.DataVersion = z.DataVersion != null ? z.DataVersion + 1 : existingRowIS[j].DataVersion != null ? existingRowIS[j].DataVersion : 1;
                                            rowIS.AccessoriesNumberAI = existingRowIS[j].AccessoriesNumberAI;
                                            rowIS.AccessoriesNumberSERA = existingRowIS[j].AccessoriesNumberSERA;
                                            rowIS.AccessoriesDescriptionSERA = existingRowIS[j].AccessoriesDescriptionSERA;
                                            rowIS.QtyAccessories = existingRowIS[j].QtyAccessories;
                                            listDataIS.Add(rowIS);
                                            //END: NEW SIT
                                        }

                                        //jika di HS ada yg berubah dan di IS pun ada yag berubah, maka update row di list HS dan IS
                                        else if (!compareIS)
                                        {
                                            //remove row HS
                                            var rowDel = (from o in listDataHS
                                                          where o.PONumber == existingRowHS[i].PONumber
                                                          select o).SingleOrDefault();
                                            listDataHS.Remove(rowDel);

                                            //add new row HS
                                            CUSTOM_S02001_TEMP_HS row = new CUSTOM_S02001_TEMP_HS();
                                            row.PONumber = existingRowHS[i].PONumber;
                                            row.PODate = existingRowHS[i].PODate;
                                            row.Version = q.Version != null ? q.Version + 1 : existingRowHS[i].Version != null ? existingRowHS[i].Version : 1;
                                            row.CustomerNumber = existingRowHS[i].CustomerNumber;
                                            row.KodeCabangAI = existingRowHS[i].KodeCabangAI;
                                            row.MaterialNumberSERA = existingRowHS[i].MaterialNumberSERA;
                                            row.MaterialDescriptionSERA = existingRowHS[i].MaterialDescriptionSERA;
                                            row.MaterialNumberAI = existingRowHS[i].MaterialNumberAI;
                                            row.ColorDescSERA = existingRowHS[i].ColorDescSERA;
                                            row.Quantity = existingRowHS[i].Quantity;

                                            // add fhi 10.12.2014 : penambahan field karoseri
                                            row.NamaKaroseri = existingRowHS[i].NamaKaroseri;
                                            row.AlamatKaroseri = existingRowHS[i].AlamatKaroseri;
                                            row.PIC = existingRowHS[i].PIC;
                                            row.NoTelepon = existingRowHS[i].NoTelepon;
                                            row.BentukKaroseri = existingRowHS[i].BentukKaroseri;
                                            row.InfoPromiseDelivery = existingRowHS[i].InfoPromiseDelivery;
                                            //end

                                            row.CustomerSTNKName = existingRowHS[i].CustomerSTNKName;
                                            row.Title = existingRowHS[i].Title;
                                            row.Address = existingRowHS[i].Address;
                                            row.Address2 = existingRowHS[i].Address2;
                                            row.Address3 = existingRowHS[i].Address3;
                                            row.Address4 = existingRowHS[i].Address4;
                                            row.Address5 = existingRowHS[i].Address5;
                                            row.KTPTDP = existingRowHS[i].KTP_TDP;
                                            row.PostalCode = existingRowHS[i].PostalCode;
                                            row.PartnerName = existingRowHS[i].PartnerName;
                                            row.PartnerAddress = existingRowHS[i].PartnerAddress;
                                            row.Telepon = existingRowHS[i].Telepon;
                                            row.City = existingRowHS[i].City;
                                            row.RegionCode = existingRowHS[i].RegionCode;
                                            row.PartnerPostalCode = existingRowHS[i].PartnerPostalCode;
                                            row.Diskon = existingRowHS[i].Diskon;
                                            row.Pricing = existingRowHS[i].Pricing;
                                            row.CurrencyCode = existingRowHS[i].CurrencyCode;
                                            row.CompanyCodeAI = existingRowHS[i].CompanyCodeAI;
                                            listDataHS.Add(row);

                                            //remove row IS
                                            var rowDelIS = (from o in listDataIS
                                                            where o.PONumber == existingRowIS[j].PONumber
                                                            select o).SingleOrDefault();
                                            listDataIS.Remove(rowDelIS);

                                            //add new row IS
                                            CUSTOM_S02001_TEMP_IS rowIS = new CUSTOM_S02001_TEMP_IS();
                                            rowIS.PONumber = existingRowIS[j].PONumber;
                                            rowIS.PODate = existingRowIS[j].PODate;
                                            rowIS.DataVersion = z.DataVersion != null ? z.DataVersion + 1 : existingRowIS[j].DataVersion != null ? existingRowIS[j].DataVersion : 1;
                                            rowIS.AccessoriesNumberAI = existingRowIS[j].AccessoriesNumberAI;
                                            rowIS.AccessoriesNumberSERA = existingRowIS[j].AccessoriesNumberSERA;
                                            rowIS.AccessoriesDescriptionSERA = existingRowIS[j].AccessoriesDescriptionSERA;
                                            rowIS.QtyAccessories = existingRowIS[j].QtyAccessories;
                                            listDataIS.Add(rowIS);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                //add task kill : by  fhi 05.06.2014
                logEvent.WriteDBLog("", "UploadS02001_Load", false, "", ex.Message, "S02001", "SERA");
                Process.Start("taskkill.exe", "/f /im B2BAISERA_S02001.exe");
                //end
                throw ex;
            }
            return listDataIS;
        }

        public int DeleteAllTempHSIS()
        {
            int result = 1;
            try
            {
                EntityCommand cmd = new EntityCommand("EProcEntities.sp_DeleteAllTempHSISS02001", entityConnection);
                cmd.CommandType = CommandType.StoredProcedure;
                OpenConnection();
                cmd.ExecuteNonQuery();

            }
            catch (Exception ex)
            {
                result = 0;

                throw ex;
            }
            finally
            {
                CloseConnection();
            }
            return result;
        }

        public int UpdateCustomPOStatusPOId(string poNumber, string poStatusId)
        {
            int result;
            try
            {
                EntityCommand cmd = new EntityCommand("EProcEntities.sp_UpdateCustomPOStatusPOId", entityConnection);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("PONUMBER", DbType.String).Value = poNumber;
                cmd.Parameters.Add("POSTATUSID", DbType.String).Value = poStatusId;
                OpenConnection();
                result = Convert.ToInt32(cmd.ExecuteScalar());
            }
            catch (Exception ex)
            {
                result = 0;
                throw ex;
            }
            finally
            {
                CloseConnection();
            }
            return result;
        }
        #endregion

        #endregion

    }
}