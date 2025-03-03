using Microsoft.AspNetCore.Authorization;
using OSI.Core.Helpers;
using System;

namespace OSI.Core.Auth
{
    [Flags]
    public enum Roles
    {
        None = 0,
        ADMIN = 1,
        OPERATOR = 2,
        CHAIRMAN = 4,
        ABONENT = 8,
        PAYMENTSERVICE = 16,
        Support = ADMIN | OPERATOR,
        Users = ADMIN | OPERATOR | CHAIRMAN | ABONENT,
        All = Users | PAYMENTSERVICE,
        Default = Users,
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
    public class AuthorizeAttribute : Microsoft.AspNetCore.Authorization.AuthorizeAttribute
#if DEBUG && !ENABLE_AUTH //Выключаем авторизацию, если запускаемся локально и не объявлена константа ENABLE_AUTH
        , IAllowAnonymous
#endif
    {
        private readonly Roles roles;
        private Roles exclude = Auth.Roles.None;
        private Roles include = Auth.Roles.None;

        public Roles Exclude
        {
            get => exclude;
            set
            {
                exclude = value;
                UpdateRoles();
            }
        }
        public Roles Include
        {
            get => include;
            set
            {
                include = value;
                UpdateRoles();
            }
        }
        public AuthorizeAttribute() : this(Auth.Roles.Default) { }

        public AuthorizeAttribute(Roles roles) : base()
        {
            this.roles = roles; 
            Roles = roles.FlagsToString();
        }

        private void UpdateRoles()
        {
            Roles = ((roles | include) & ~exclude).FlagsToString();
        }
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class AllowAnonymousAttribute : Attribute, IAllowAnonymous
    {
    }
}
