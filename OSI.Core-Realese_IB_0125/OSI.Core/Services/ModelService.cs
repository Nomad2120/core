using Blazorise;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NLog.Filters;
using OSI.Core.Models.Db;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;

namespace OSI.Core.Services
{
    public interface IModelService<TDbContext, TModel> : IModelServiceBase<TDbContext, TModel>
        where TDbContext : DbContext
        where TModel : ModelBase
    {
        Task AddOrUpdateModel(TModel model);
        Task<TModel> GetModelById(int id);
        Task RemoveModelById(int id);
    }

    public interface IModelServiceBase<TDbContext, TModel>
        where TDbContext : DbContext
        where TModel : class
    {
        Task<TModel> GetModelByFunc(Expression<Func<TModel, bool>> func);
        Task<TModel> GetModelByQuery(Func<DbSet<TModel>, IQueryable<TModel>> query);
        Task<TModel> GetModelByQuery(Func<DbSet<TModel>, IQueryable<TModel>> query, Expression<Func<TModel, bool>> func);
        Task<TSelect> GetModelByQuery<TSelect>(Func<DbSet<TModel>, IQueryable<TSelect>> query);
        Task<TSelect> GetModelByQuery<TSelect>(Func<DbSet<TModel>, IQueryable<TSelect>> query, Expression<Func<TSelect, bool>> func);
        Task<IEnumerable<TModel>> GetModels();
        Task<IEnumerable<TModel>> GetModelsByFunc(Expression<Func<TModel, bool>> func);
        Task<IEnumerable<TModel>> GetModelsByQuery(Func<DbSet<TModel>, IQueryable<TModel>> query);
        Task<IEnumerable<TSelect>> GetModelsByQuery<TSelect>(Func<DbSet<TModel>, IQueryable<TSelect>> query);
        Task<bool> HasModels();
        Task<bool> HasModels(Expression<Func<TModel, bool>> func);
        Task<bool> HasModels<TSelect>(Func<DbSet<TModel>, IQueryable<TSelect>> query);
        Task<bool> HasModels<TSelect>(Func<DbSet<TModel>, IQueryable<TSelect>> query, Expression<Func<TSelect, bool>> func);
        Task RemoveModel(TModel model);
        Task RemoveModels(IEnumerable<TModel> models);
    }

    internal static class ModelServiceReflectionHelper
    {
        private static readonly Dictionary<Type, Func<DbContext>> ActivatorCache = new();
        internal static readonly Dictionary<(Type, Type), PropertyInfo> DbSetPropertyCache = new();

        internal static TDbContext CreateDbContext<TDbContext>() where TDbContext : DbContext
        {
            if (ActivatorCache.TryGetValue(typeof(TDbContext), out var func))
            {
                return func() as TDbContext;
            }
            else
            {
                TDbContext dbContext;
                try
                {
                    dbContext = Activator.CreateInstance<TDbContext>();
                    ActivatorCache.Add(typeof(TDbContext), () => Activator.CreateInstance<TDbContext>());
                }
                catch (MissingMethodException)
                {
                    dbContext = Activator.CreateInstance(typeof(TDbContext), nonPublic: true) as TDbContext;
                    ActivatorCache.Add(typeof(TDbContext), () => Activator.CreateInstance(typeof(TDbContext), nonPublic: true) as TDbContext);
                }
                return dbContext;
            }
        }
    }

    public class ModelService<TDbContext, TModel> : ModelServiceBase<TDbContext, TModel>, IModelService<TDbContext, TModel>
        where TDbContext : DbContext
        where TModel : ModelBase
    {

        public ModelService() : base()
        {
        }

        public async Task<TModel> GetModelById(int id)
        {
            using var dbContext = DbContext;
            return await GetDbSet(dbContext).FirstOrDefaultAsync(m => m.Id == id);
        }

        public async Task AddOrUpdateModel(TModel model)
        {
            using var dbContext = DbContext;
            if (model.Id == default)
            {
                GetDbSet(dbContext).Add(model);
            }
            else
            {
                GetDbSet(dbContext).Update(model);
            }
            await dbContext.SaveChangesAsync();
        }

        public async Task RemoveModelById(int id)
        {
            using var dbContext = DbContext;
            DbSet<TModel> dbSet = GetDbSet(dbContext);
            var model = await dbSet.FirstOrDefaultAsync(m => m.Id == id);
            dbSet.Remove(model);
            await dbContext.SaveChangesAsync();
        }
    }

    public class ModelServiceBase<TDbContext, TModel> : IModelServiceBase<TDbContext, TModel>
        where TDbContext : DbContext
        where TModel : class
    {
        private readonly PropertyInfo DbSetProperty;
        protected readonly Func<TDbContext, DbSet<TModel>> GetDbSet;

        protected TDbContext DbContext
        {
            get
            {
                var dbContext = ModelServiceReflectionHelper.CreateDbContext<TDbContext>();
                dbContext.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
                return dbContext;
            }
        }

        public ModelServiceBase()
        {
            Type dbContextType = typeof(TDbContext);
            Type modelType = typeof(TModel);
            (Type dbContextType, Type modelType) key = (dbContextType, modelType);
            if (ModelServiceReflectionHelper.DbSetPropertyCache.ContainsKey(key))
            {
                DbSetProperty = ModelServiceReflectionHelper.DbSetPropertyCache[key];
            }
            else
            {
                DbSetProperty = dbContextType.GetProperties().FirstOrDefault(p => p.PropertyType.IsGenericType && p.PropertyType.GetGenericTypeDefinition() == typeof(DbSet<>) && p.PropertyType.GetGenericArguments()[0] == modelType);
                if (DbSetProperty == null)
                    throw new InvalidOperationException($"{dbContextType.FullName} does not have DbSet property for model {modelType.FullName}");
                ModelServiceReflectionHelper.DbSetPropertyCache.Add(key, DbSetProperty);
            }
            GetDbSet = dbContext => DbSetProperty.GetValue(dbContext) as DbSet<TModel>;
        }

        public async Task<IEnumerable<TModel>> GetModels()
        {
            using var dbContext = DbContext;
            return await GetDbSet(dbContext).ToListAsync();
        }

        public async Task<IEnumerable<TModel>> GetModelsByFunc(Expression<Func<TModel, bool>> func)
        {
            using var dbContext = DbContext;
            return await GetDbSet(dbContext).Where(func).ToListAsync();
        }

        public async Task<IEnumerable<TModel>> GetModelsByQuery(Func<DbSet<TModel>, IQueryable<TModel>> query)
        {
            using var dbContext = DbContext;
            return await query(GetDbSet(dbContext)).ToListAsync();
        }

        public async Task<IEnumerable<TSelect>> GetModelsByQuery<TSelect>(Func<DbSet<TModel>, IQueryable<TSelect>> query)
        {
            using var dbContext = DbContext;
            return await query(GetDbSet(dbContext)).ToListAsync();
        }

        public async Task<TModel> GetModelByFunc(Expression<Func<TModel, bool>> func)
        {
            using var dbContext = DbContext;
            return await GetDbSet(dbContext).FirstOrDefaultAsync(func);
        }

        public async Task<TModel> GetModelByQuery(Func<DbSet<TModel>, IQueryable<TModel>> query)
        {
            using var dbContext = DbContext;
            return await query(GetDbSet(dbContext)).FirstOrDefaultAsync();
        }

        public async Task<TModel> GetModelByQuery(Func<DbSet<TModel>, IQueryable<TModel>> query, Expression<Func<TModel, bool>> func)
        {
            using var dbContext = DbContext;
            return await query(GetDbSet(dbContext)).FirstOrDefaultAsync(func);
        }

        public async Task<TSelect> GetModelByQuery<TSelect>(Func<DbSet<TModel>, IQueryable<TSelect>> query)
        {
            using var dbContext = DbContext;
            return await query(GetDbSet(dbContext)).FirstOrDefaultAsync();
        }

        public async Task<TSelect> GetModelByQuery<TSelect>(Func<DbSet<TModel>, IQueryable<TSelect>> query, Expression<Func<TSelect, bool>> func)
        {
            using var dbContext = DbContext;
            return await query(GetDbSet(dbContext)).FirstOrDefaultAsync(func);
        }

        public async Task<bool> HasModels()
        {
            using var dbContext = DbContext;
            return await GetDbSet(dbContext).AnyAsync();
        }

        public async Task<bool> HasModels(Expression<Func<TModel, bool>> func)
        {
            using var dbContext = DbContext;
            return await GetDbSet(dbContext).AnyAsync(func);
        }

        public async Task<bool> HasModels<TSelect>(Func<DbSet<TModel>, IQueryable<TSelect>> query)
        {
            using var dbContext = DbContext;
            return await query(GetDbSet(dbContext)).AnyAsync();
        }

        public async Task<bool> HasModels<TSelect>(Func<DbSet<TModel>, IQueryable<TSelect>> query, Expression<Func<TSelect, bool>> func)
        {
            using var dbContext = DbContext;
            return await query(GetDbSet(dbContext)).AnyAsync(func);
        }

        public async Task RemoveModel(TModel model)
        {
            using var dbContext = DbContext;
            GetDbSet(dbContext).Remove(model);
            await dbContext.SaveChangesAsync();
        }

        public async Task RemoveModels(IEnumerable<TModel> models)
        {
            using var dbContext = DbContext;
            GetDbSet(dbContext).RemoveRange(models);
            await dbContext.SaveChangesAsync();
        }
    }
}
