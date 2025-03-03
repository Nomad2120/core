using Microsoft.EntityFrameworkCore.Query;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace OSI.Core.Helpers
{
    public interface IIfQueryable<T> : IQueryable<T>, IOrderedQueryable<T>
    {
    }

    internal class IfQueryable<T> : IIfQueryable<T>, IQueryProvider, IAsyncQueryProvider
    {
        private IQueryable<T> queryable;
        private bool condition;
        private bool conditionMet = false;
        private bool elseCalled = false;

        public IfQueryable(IQueryable<T> queryable, bool condition)
        {
            this.queryable = queryable;
            this.condition = condition;
        }

        public Type ElementType => queryable.ElementType;

        public Expression Expression => queryable.Expression;

        public IQueryProvider Provider => this;

        public IIfQueryable<T> ElseIf(bool condition)
        {
            if (elseCalled)
            {
                throw new InvalidOperationException("Else has already been called");
            }

            if (!conditionMet && this.condition)
            {
                conditionMet = true;
            }
            this.condition = condition;
            return this;
        }

        public IIfQueryable<T> Else()
        {
            if (elseCalled)
            {
                throw new InvalidOperationException("Else has already been called");
            }

            if (!conditionMet && condition)
            {
                conditionMet = true;
            }
            condition = !condition;
            elseCalled = true;
            return this;
        }

        public IQueryable<T> EndIf()
        {
            if (!conditionMet && condition)
            {
                conditionMet = true;
            }
            return queryable;
        }

        public IQueryable CreateQuery(Expression expression)
        {
            throw new InvalidOperationException("Not allowed to change queryable type in If/ElseIf/Else");
        }

        public IQueryable<TElement> CreateQuery<TElement>(Expression expression)
        {
            if (typeof(TElement) != typeof(T))
                throw new InvalidOperationException("Not allowed to change queryable type in If/ElseIf/Else");

            queryable = (condition && !conditionMet) ? queryable.Provider.CreateQuery<TElement>(expression) as IQueryable<T> : queryable;

            return this as IQueryable<TElement>;
        }

        public object Execute(Expression expression)
        {
            throw new NotSupportedException("Not supported query execution in If/ElseIf/Else");
        }

        public TResult Execute<TResult>(Expression expression)
        {
            throw new NotSupportedException("Not supported query execution in If/ElseIf/Else");
        }

        public TResult ExecuteAsync<TResult>(Expression expression, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException("Not supported query execution in If/ElseIf/Else");
        }

        public IEnumerator<T> GetEnumerator()
        {
            throw new NotSupportedException("Not supported enumeration in If/ElseIf/Else");
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            throw new NotSupportedException("Not supported enumeration in If/ElseIf/Else");
        }
    }

    public static class IIfQueryableExtensions
    {
        public static IQueryable<T> If<T>(this IQueryable<T> source, bool condition)
        {
            if (source is null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            if (source is IfQueryable<T> ifQueryable)
            {
                throw new InvalidOperationException("If has already been called");
            }

            return new IfQueryable<T>(source, condition);
        }

        public static IQueryable<T> ElseIf<T>(this IQueryable<T> source, bool condition)
        {
            if (source is null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            if (source is not IfQueryable<T> ifQueryable)
            {
                throw new InvalidOperationException("If call must be done before");
            }

            return ifQueryable.ElseIf(condition);
        }

        public static IQueryable<T> Else<T>(this IQueryable<T> source)
        {
            if (source is null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            if (source is not IfQueryable<T> ifQueryable)
            {
                throw new InvalidOperationException("If call must be done before");
            }

            return ifQueryable.Else();
        }

        public static IQueryable<T> EndIf<T>(this IQueryable<T> source)
        {
            if (source is null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            if (source is not IfQueryable<T> ifQueryable)
            {
                throw new InvalidOperationException("If call must be done before");
            }

            return ifQueryable.EndIf();
        }
    }
}
