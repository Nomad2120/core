using OSI.Core.Models.Db;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OSI.Core.Models.Responses
{
    public class ServiceGroupResponse
    {
        public int Id { get; set; }

        public string GroupNameRu { get; set; }

        public string GroupNameKz { get; set; }

        public bool CanChangeName { get; set; }

        public bool JustOne { get; set; }

        public bool CanEditAbonents { get; set; }

        public bool CanCreateFixes { get; set; }

        public IEnumerable<AccuralMethod> AccuralMethods { get; set; }

        public IEnumerable<ServiceNameExample> ServiceNameExamples { get; set; }

        public List<OsiServiceResponse> Services { get; set; }
    }
}
