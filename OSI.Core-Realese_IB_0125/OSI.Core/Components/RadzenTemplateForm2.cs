using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Radzen.Blazor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OSI.Core.Components
{
    public class RadzenTemplateForm2<TItem> : RadzenTemplateForm<TItem>
    {
        [Parameter]
        public new EditContext EditContext
        {
            get
            {
                return base.EditContext;
            }
            set
            {
                base.EditContext = value;
            }
        }
    }
}
