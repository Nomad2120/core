using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using Microsoft.EntityFrameworkCore.Query.Internal;

namespace OSI.Core.Helpers.EFCore
{
    public class InTranslatorPlugin : IMethodCallTranslatorPlugin
    {
        private readonly IMethodCallTranslator[] translators;

        public InTranslatorPlugin([NotNull] ISqlExpressionFactory sqlExpressionFactory)
        {
            translators = new IMethodCallTranslator[]
            {
                new InTranslator(sqlExpressionFactory)
            };
        }

        public IEnumerable<IMethodCallTranslator> Translators => translators;
    }

    [SuppressMessage("Usage", "EF1001:Internal EF Core API usage.", Justification = "")]
    public class InTranslator : ContainsTranslator
    {
        private static readonly MethodInfo InParamsMethod = typeof(ESoft.CommonLibrary.GenericExtensions).GetMethods()
            .First(m => m.Name == "In" && m.GetParameters()[1].ParameterType.IsArray);

        private static readonly MethodInfo InEnumerableMethod = typeof(ESoft.CommonLibrary.GenericExtensions).GetMethods()
            .First(m =>
            {
                ParameterInfo valuesParameter = m.GetParameters()[1];
                return m.Name == "In" && valuesParameter.ParameterType.IsGenericType && valuesParameter.ParameterType.GetGenericTypeDefinition() == typeof(IEnumerable<>);
            });

        private static readonly MethodInfo EnumerableContainsMethod = typeof(Enumerable).GetMethods()
            .First(m => m.Name == "Contains" && m.GetParameters().Length == 2);

        public InTranslator([NotNull] ISqlExpressionFactory sqlExpressionFactory) : base(sqlExpressionFactory)
        {
        }
        public override SqlExpression Translate(SqlExpression instance, MethodInfo method, IReadOnlyList<SqlExpression> arguments, IDiagnosticsLogger<DbLoggerCategory.Query> logger)
        {
            Type parameterType = method.GetParameters().FirstOrDefault()?.ParameterType;
            if (parameterType != null &&
                (method.Equals(InParamsMethod.MakeGenericMethod(parameterType)) || method.Equals(InEnumerableMethod.MakeGenericMethod(parameterType))))
            {
                return base.Translate(instance, EnumerableContainsMethod, arguments.Reverse().ToList(), logger);
            }

            return null;
        }
    }
}
