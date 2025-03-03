using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

namespace OSI.Core.Tests
{
    public class SimpleMock<IInterface>
        where IInterface : class
    {
        private readonly Dictionary<MethodInfo, object> methodValues = new();

        public SimpleMock<IInterface> MemberReturns<TProperty>(Expression<Func<IInterface, TProperty>> propertySelector, TProperty value)
        {
            if (propertySelector.Body is MemberExpression memberExpression)
            {
                if (memberExpression.Member is PropertyInfo property)
                {
                    var getter = property.GetGetMethod();
                    if (getter != null)
                    {
                        methodValues.Add(getter, value);
                        return this;
                    }
                }
            }
            else if (propertySelector.Body is MethodCallExpression methodCallExpression)
            {
                methodValues.Add(methodCallExpression.Method, value);
                return this;
            }
            throw new NotSupportedException("Not supported expression");
        }

        public IInterface Create()
        {
            var obj = DispatchProxy.Create<IInterface, SimpleMockProxy>();
            var proxy = obj as SimpleMockProxy;
            proxy.SetMethodValues(methodValues);
            return obj;
        }

        public class SimpleMockProxy : DispatchProxy
        {
            private Dictionary<MethodInfo, object> methodValues = null;

            internal void SetMethodValues(Dictionary<MethodInfo, object> methodValues)
            {
                this.methodValues = methodValues;
            }

            protected override object Invoke(MethodInfo targetMethod, object[] args) =>
                methodValues?.TryGetValue(targetMethod, out object value) == true
                ? value
                : throw new NotImplementedException();
        }
    }
}
