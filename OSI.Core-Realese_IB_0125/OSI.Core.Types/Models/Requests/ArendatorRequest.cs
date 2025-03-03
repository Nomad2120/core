using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OSI.Core.Models.Requests
{
    public class ArendatorRequest
    {
        public int OsiId { get; set; }

        public string Name { get; set; }

        public string Address { get; set; }

        public string Rca { get; set; }

        public string Phone { get; set; }

        public string Idn { get; set; }

        [Required(ErrorMessage = "Укажите квартиру")]
        public string Flat { get; set; }
    }
}
