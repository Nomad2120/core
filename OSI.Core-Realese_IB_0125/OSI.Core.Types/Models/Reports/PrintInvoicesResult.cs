using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OSI.Core.Models.Reports
{
    public class PrintInvoicesResult
    {
        /// <summary>
        /// Содержит либо html-контент, либо путь до готового pdf
        /// </summary>
        public string Data { get; set; }

        /// <summary>
        /// Кол-во инвойсов внутри
        /// </summary>
        public int Count { get; set; }
    }
}
