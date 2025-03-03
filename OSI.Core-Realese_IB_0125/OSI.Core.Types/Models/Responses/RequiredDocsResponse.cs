namespace OSI.Core.Models.Responses
{
    public class RequiredDocsResponse
    {
        public string Code { get; set; }

        public string NameRu { get; set; }

        public string NameKz { get; set; }

        public int MaxSize { get; set; }

        public bool IsRequired { get; set; }
    }
}
