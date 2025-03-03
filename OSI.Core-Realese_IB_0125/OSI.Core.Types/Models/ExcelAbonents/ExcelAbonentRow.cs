namespace OSI.Core.Models.ExcelAbonents
{
    public class ExcelAbonentRow
    {
        public string Flat { get; set; }
        public string SquareAll { get; set; }
        public string SquareUseful { get; set; }
        public string IsLiving { get; set; }
        public string Debt { get; set; }

        public ExcelAbonentRow(string flat, string squareAll, string squareUseful, string isLiving, string debt)
        {
            Flat = flat;
            SquareAll = squareAll;
            SquareUseful = squareUseful;
            IsLiving = isLiving;
            Debt = debt;
        }
    }
}
