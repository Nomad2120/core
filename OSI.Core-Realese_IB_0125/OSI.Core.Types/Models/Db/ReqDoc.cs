namespace OSI.Core.Models.Db
{
    public class ReqDoc: ModelBase
    {
        public string ReqTypeCode { get; set; }

        public string DocTypeCode { get; set; }

        public int? UnionTypeId { get; set; }

        public bool IsRequired { get; set; }

        public virtual ReqType ReqType { get; set; }

        public virtual DocType DocType { get; set; }

        public virtual UnionType UnionType { get; set; }
    }
}
