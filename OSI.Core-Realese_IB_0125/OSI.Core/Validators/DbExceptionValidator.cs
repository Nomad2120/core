using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.EntityFrameworkCore;
using OSI.Core.Helpers;
using OSI.Core.Models.Db;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OSI.Core.Validators
{
    public class DbExceptionValidator : ComponentBase
    {
        private ValidationMessageStore messageStore;

        [CascadingParameter]
        private EditContext CurrentEditContext { get; set; }

        protected override void OnInitialized()
        {
            if (CurrentEditContext == null)
            {
                throw new InvalidOperationException(
                    $"{nameof(DbExceptionValidator)} requires a cascading " +
                    $"parameter of type {nameof(EditContext)}. " +
                    $"For example, you can use {nameof(DbExceptionValidator)} " +
                    $"inside an {nameof(EditForm)}.");
            }

            messageStore = new ValidationMessageStore(CurrentEditContext);

            CurrentEditContext.OnValidationRequested += (s, e) =>
                messageStore.Clear();
            CurrentEditContext.OnFieldChanged += (s, e) =>
                messageStore.Clear(e.FieldIdentifier);
        }

        public void DisplayErrors(Exception ex, bool showTypeException = true)
        {
            string message = CurrentEditContext.Model is ModelBase model 
                ? model.GetExceptionMessage(ex) 
                : ((showTypeException ? ex.GetType().FullName + " : " : "") + ex.GetFullInfo());

            messageStore.Add(CurrentEditContext.Field(string.Empty), message);

            CurrentEditContext.NotifyValidationStateChanged();
        }

        public void ClearErrors()
        {
            messageStore.Clear();
            CurrentEditContext.NotifyValidationStateChanged();
        }
    }
}
