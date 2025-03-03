using System;

namespace OSI.Core.Models.Requests
{
    public class EsfCreateRequest
    {
        /// <summary>
        /// Исходящий номер ЭСФ в бухгалтерии отправителя (A 1)
        /// </summary>
        public string Num { get; set; } = string.Empty;

        public string ActNumber { get; set; } = string.Empty;

        public DateTime ActDate { get; set; }

        /// <summary>
        /// Дата подписанного SIGNED_CONTRACT
        /// </summary>
        public DateTime ContractDate { get; set; }

        /// <summary>
        /// Дата совершения оборота, ставим дату act_period из ОСИ
        /// </summary>
        public DateTime TurnoverDate { get; set; }

        public string CustomerAddress { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerBin { get; set; } = string.Empty;

        public string ProductDescription { get; set; } = string.Empty;
        public decimal ProductQuantity { get; set; }
        public decimal ProductPrice { get; set; }

        public decimal Amount { get; set; }

        public string OperatorFullName { get; set; } = string.Empty;
    }
}