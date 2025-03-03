using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OSI.Core.Helpers
{
    public static class LinqHelpers
    {
        public static IQueryable<T> Conditional<T>(this IQueryable<T> source, bool condition, Func<IQueryable<T>, IQueryable<T>> trueFunc, Func<IQueryable<T>, IQueryable<T>> falseFunc = null)
        {
            Func<IQueryable<T>, IQueryable<T>> defaultFunc = x => x;
            return condition ? (trueFunc ?? defaultFunc)(source) : (falseFunc ?? defaultFunc)(source);
        }

        public static Task<IQueryable<T>> Conditional<T>(this IQueryable<T> source, bool condition, Func<IQueryable<T>, Task<IQueryable<T>>> trueFunc, Func<IQueryable<T>, Task<IQueryable<T>>> falseFunc = null)
        {
            Func<IQueryable<T>, Task<IQueryable<T>>> defaultFunc = x => Task.FromResult(x);
            return condition ? (trueFunc ?? defaultFunc)(source) : (falseFunc ?? defaultFunc)(source);
        }

        public static T Conditional<T>(this IQueryable<T> source, bool condition, Func<IQueryable<T>, T> trueFunc, Func<IQueryable<T>, T> falseFunc = null)
        {
            Func<IQueryable<T>, T> defaultFunc = x => default;
            return condition ? (trueFunc ?? defaultFunc)(source) : (falseFunc ?? defaultFunc)(source);
        }

        public static Task<T> Conditional<T>(this IQueryable<T> source, bool condition, Func<IQueryable<T>, Task<T>> trueFunc, Func<IQueryable<T>, Task<T>> falseFunc = null)
        {
            Func<IQueryable<T>, Task<T>> defaultFunc = x => Task.FromResult(default(T));
            return condition ? (trueFunc ?? defaultFunc)(source) : (falseFunc ?? defaultFunc)(source);
        }
    }
}
