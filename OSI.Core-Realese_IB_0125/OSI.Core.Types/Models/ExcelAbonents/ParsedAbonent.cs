using OSI.Core.Models.Enums;

namespace OSI.Core.Models.ExcelAbonents
{
    public class ParsedAbonent
    {
        public string Flat { get; set; }
        public decimal Square { get; set; }
        public decimal EffectiveSquare { get; set; }
        public AreaTypeCodes AreaTypeCode { get; set; }
        public decimal Debt { get; set; }
    }
}
