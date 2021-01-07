using System.Collections.Generic;
using System.Drawing;
using LINQtoCSV;

namespace CTALookup
{
    public class Item
    {
        private const int beginning = 1;

        [CsvColumn(CanBeNull = true, FieldIndex = 0, Name = "Index")]
        public int Index { get; set; }
        [CsvColumn(CanBeNull = true, FieldIndex = beginning, Name = "County")]
        public string County { get; set; }
        [CsvColumn(CanBeNull = true, FieldIndex = beginning + 1, Name = "Map Number")]
        public string MapNumber { get; set; }
        [CsvColumn(CanBeNull = true, FieldIndex = beginning + 2, Name = "Sales Date")]
        public string SalesDate { get; set; }
        [CsvColumn(CanBeNull = true, FieldIndex = beginning + 3, Name = "Interested")]
        public string Interested { get; set; }
        [CsvColumn(CanBeNull = true, FieldIndex = beginning + 4, Name = "Minimum bid")]
        public string MinimumBid { get; set; }
        [CsvColumn(CanBeNull = true, FieldIndex = beginning + 5, Name = "Notes")]
        public string Notes { get; set; }
        [CsvColumn(CanBeNull = true, FieldIndex = beginning + 6, Name = "Legal Description")]
        public string LegalDescription { get; set; }
        [CsvColumn(CanBeNull = true, FieldIndex = beginning + 7, Name = "Description")]
        public string Description { get; set; }

        [CsvColumn(CanBeNull = true, FieldIndex = beginning + 8, Name = "Year to Check")]
        public string PropertyType { get; set; }

        [CsvColumn(CanBeNull = true, FieldIndex = beginning + 9, Name = "Acreage")]
        public string Acreage { get; set; }
        [CsvColumn(CanBeNull = true, FieldIndex = beginning + 10, Name = "Physical Address 1")]
        public string PhysicalAddress1 { get; set; }
        [CsvColumn(CanBeNull = true, FieldIndex = beginning + 11, Name = "Physical Address City")]
        public string PhysicalAddressCity { get; set; }
        [CsvColumn(CanBeNull = true, FieldIndex = beginning + 12, Name = "Physical Address State")]
        public string PhysicalAddressState { get; set; }
        [CsvColumn(CanBeNull = true, FieldIndex = beginning + 13, Name = "Physical Address Zip")]
        public string PhysicalAddressZip { get; set; }
        [CsvColumn(CanBeNull = true, FieldIndex = beginning + 14, Name = "Multiple Results Detected")]
        public string MultipleColumns { get; set; }
        [CsvColumn(CanBeNull = true, FieldIndex = beginning + 15, Name = "Owner Name")]
        public string OwnerName { get; set; }
        [CsvColumn(CanBeNull = true, FieldIndex = beginning + 16, Name = "Owner First Name")]
        public string OwnerFirstName { get; set; }
        [CsvColumn(CanBeNull = true, FieldIndex = beginning + 17, Name = "Owner Middle Initial")]
        public string OwnerMiddleInitial { get; set; }
        [CsvColumn(CanBeNull = true, FieldIndex = beginning + 18, Name = "Owner Last Name")]
        public string OwnerLastName { get; set; }
        [CsvColumn(CanBeNull = true, FieldIndex = beginning + 19, Name = "Company")]
        public string Company { get; set; }
        [CsvColumn(CanBeNull = true, FieldIndex = beginning + 20, Name = "Owner Address")]
        public string OwnerAddress { get; set; }
        [CsvColumn(CanBeNull = true, FieldIndex = beginning + 21, Name = "Owner Address2")]
        public string OwnerAddress2 { get; set; }
        [CsvColumn(CanBeNull = true, FieldIndex = beginning + 22, Name = "Owner City")]
        public string OwnerCity { get; set; }
        [CsvColumn(CanBeNull = true, FieldIndex = beginning + 23, Name = "Owner State")]
        public string OwnerState { get; set; }
        [CsvColumn(CanBeNull = true, FieldIndex = beginning + 24, Name = "Owner Zip")]
        public string OwnerZip { get; set; }
        [CsvColumn(CanBeNull = true, FieldIndex = beginning + 25, Name = "Mailing Address Owner")]
        public string MailingAddressOwner { get; set; }
        [CsvColumn(CanBeNull = true, FieldIndex = beginning + 26, Name = "Mailing Address")]
        public string MailingAddress { get; set; }
        [CsvColumn(CanBeNull = true, FieldIndex = beginning + 27, Name = "Mailing Address2")]
        public string MailingAddress2 { get; set; }
        [CsvColumn(CanBeNull = true, FieldIndex = beginning + 28, Name = "Mailing City")]
        public string MailingCity { get; set; }
        [CsvColumn(CanBeNull = true, FieldIndex = beginning + 29, Name = "Mailing State")]
        public string MailingState { get; set; }
        [CsvColumn(CanBeNull = true, FieldIndex = beginning + 30, Name = "Mailing Zip")]
        public string MailingZip { get; set; }

        private string marketValue;
        [CsvColumn(CanBeNull = true, FieldIndex = beginning + 31, Name = "Market Value")]
        public string MarketValue
        {
            get { return marketValue; }
            set
            {
                if (!string.IsNullOrEmpty(value))
                    value = value.Trim('$', ' ');
                marketValue = value;
            }
        }
        [CsvColumn(CanBeNull = true, FieldIndex = beginning + 32, Name = "Land Value")]
        public string LandValue { get; set; }
        [CsvColumn(CanBeNull = true, FieldIndex = beginning + 33, Name = "Improvement Value")]
        public string ImprovementValue { get; set; }
        [CsvColumn(CanBeNull = true, FieldIndex = beginning + 34, Name = "Transfer Date")]
        public string TransferDate { get; set; }
        [CsvColumn(CanBeNull = true, FieldIndex = beginning + 35, Name = "Transfer Price")]
        public string TransferPrice { get; set; }
        [CsvColumn(CanBeNull = true, FieldIndex = beginning + 36, Name = "Homestead Excemption")]
        public string HomesteadExcemption { get; set; }
        [CsvColumn(CanBeNull = true, FieldIndex = beginning + 37, Name = "Owner Resident")]
        public string OwnerResident { get; set; }
        [CsvColumn(CanBeNull = true, FieldIndex = beginning + 38, Name = "Waterfront Property Type")]
        public string WaterfrontPropertyType { get; set; }
        [CsvColumn(CanBeNull = true, FieldIndex = beginning + 39, Name = "Accessory Value")]
        public string AccessoryValue { get; set; }
        [CsvColumn(CanBeNull = true, FieldIndex = beginning + 40, Name = "Reason To Omit")]
        public string ReasonToOmit { get; set; }

        public Image Image { get; set; }

        public List<ImageInfo> Images { get; set; }

        public string State { get; set; }

        public Item()
        {
            MapNumber = "";
            Company = "";
            LegalDescription = "";
            Description = "";
            Acreage = "";
            PhysicalAddress1 = "";
            PhysicalAddressCity = "";
            PhysicalAddressState = "";
            PhysicalAddressZip = "";
            MultipleColumns = "";
            OwnerFirstName = "";
            OwnerLastName = "";
            OwnerMiddleInitial = "";
            OwnerName = "";
            OwnerAddress = "";
            OwnerAddress2 = "";
            OwnerCity = "";
            OwnerState = "";
            OwnerZip = "";
            MailingAddressOwner = "";
            MailingAddress = "";
            MailingAddress2 = "";
            MailingCity = "";
            MailingState = "";
            MailingZip = "";
            MarketValue = "";
            LandValue = "";
            ImprovementValue = "";
            TransferDate = "";
            TransferPrice = "";
            HomesteadExcemption = "";
            OwnerResident = "";
            WaterfrontPropertyType = "";

            SalesDate = "";
            MinimumBid = "";
            Interested = "Y";
            Notes = "";

        }

        public override string ToString()
        {
            return OwnerName;
        }

        public void ClearNameFields()
        {
            OwnerFirstName = "";
            OwnerLastName = "";
            OwnerMiddleInitial = "";
        }

        public void SetReasonToOmit(string reason)
        {
            Interested = "N";
            ReasonToOmit = reason;
        }

        /// <summary>
        /// For some counties, Care of Fields may come from more than one source, in such scenarios,
        /// this field seperates processed field.
        /// </summary>
        public bool IsCareOfProcessed = false;

        public void AdjustValues()
        {
            MarketValue = TrimCurrency(MarketValue);
            LandValue = TrimCurrency(LandValue);
            ImprovementValue = TrimCurrency(ImprovementValue);
            TransferPrice = TrimCurrency(TransferPrice);
            MinimumBid = TrimCurrency(MinimumBid);
            AccessoryValue = TrimCurrency(AccessoryValue);
        }

        private static string TrimCurrency(string source)
        {
            return source?.Replace("$", "").Replace(",", "").Trim();
        }
    }

    public class ImageInfo
    {
        public Image Image { get; set; }
        public string URL { get; set; }
        public string Name { get; set; }
    }
}