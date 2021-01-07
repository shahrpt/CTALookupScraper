using LINQtoCSV;

namespace CTALookup
{
    class ItemWithMinimumBid : Item
    {
        [CsvColumn(CanBeNull = true, FieldIndex = 67, Name = "Minimum Bid")]
        public string MinimumBid { get; set; }

        public ItemWithMinimumBid() {
            MinimumBid = "";
        }
    }
}
