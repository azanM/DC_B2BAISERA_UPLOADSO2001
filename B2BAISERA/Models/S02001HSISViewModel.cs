using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace B2BAISERA.Models
{
    class S02001HSISViewModel
    {
        public string HSIS
        {
            get;
            set;
        }

        public string PONumber
        {
            get;
            set;
        }

        public Nullable<System.DateTime> PODate
        {
            get;
            set;
        }

        public Nullable<decimal> Version
        {
            get;
            set;
        }

        public string CustomerNumber
        {
            get;
            set;
        }

        public string KodeCabangAI
        {
            get;
            set;
        }

        public string MaterialNumberSERA
        {
            get;
            set;
        }

        public string MaterialDescriptionSERA
        {
            get;
            set;
        }

        public string MaterialNumberAI
        {
            get;
            set;
        }

        public string ColorDescSERA
        {
            get;
            set;
        }

        public Nullable<int> Quantity
        {
            get;
            set;
        }

        // add fhi 01.12.2014 : penambahan field karoseri
        public string NamaKaroseri { get; set; }
        public string AlamatKaroseri { get; set; }
        public string PIC { get; set; }
        public string NoTelepon { get; set; }
        public string BentukKaroseri { get; set; }
        public Nullable<System.DateTime> InfoPromiseDelivery { get; set; }
        //end

        public string CustomerSTNKName
        {
            get;
            set;
        }

        public Nullable<int> Title
        {
            get;
            set;
        }

        public string Address
        {
            get;
            set;
        }

        public string Address2
        {
            get;
            set;
        }

        public string Address3
        {
            get;
            set;
        }

        public string Address4
        {
            get;
            set;
        }

        public string Address5
        {
            get;
            set;
        }

        public string KTP_TDP
        {
            get;
            set;
        }

        public string PostalCode
        {
            get;
            set;
        }

        public string PartnerName
        {
            get;
            set;
        }

        public string PartnerAddress
        {
            get;
            set;
        }

        public string Telepon
        {
            get;
            set;
        }

        public string City
        {
            get;
            set;
        }

        public string RegionCode
        {
            get;
            set;
        }

        public string PartnerPostalCode
        {
            get;
            set;
        }

        public Nullable<double> Diskon
        {
            get;
            set;
        }

        public Nullable<double> Pricing
        {
            get;
            set;
        }

        public string CurrencyCode
        {
            get;
            set;
        }

        public Nullable<int> DataVersion
        {
            get;
            set;
        }

        public string AccessoriesNumberAI
        {
            get;
            set;
        }

        public string AccessoriesNumberSERA
        {
            get;
            set;
        }

        public string AccessoriesDescriptionSERA
        {
            get;
            set;
        }

        public Nullable<decimal> QtyAccessories
        {
            get;
            set;
        }
    }
}
