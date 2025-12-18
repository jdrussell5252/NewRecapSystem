namespace NewRecap.Model
{
    public class StoreGroup
    {
        public int? StoreLocationID { get; set; }
        public int? StoreNumber { get; set; }
        public List<RecapView> Items { get; set; } = new();
        public string StoreLabel { get; set; } = "";
    }// End of 'StoreGroup' Class.
}// End of 'namespace'.
