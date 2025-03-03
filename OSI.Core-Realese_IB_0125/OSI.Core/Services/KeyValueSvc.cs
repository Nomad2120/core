using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.Extensions.Options;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Text;
using OSI.Core.Models.Db;
using OSI.Core.Pages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OSI.Core.Services
{
    public interface IKeyValueSvc : IModelServiceBase<OSIBillingDbContext, KeyValue>
    {
        Task<string> Get(string key);
        Task<IReadOnlyDictionary<string, string>> Get(IEnumerable<string> keys);
        Task AddOrUpdate(string key, string value);
        Task AddOrUpdate(IReadOnlyDictionary<string, string> keyValues);
        Task Remove(string key);
        Task Remove(IEnumerable<string> keys);
    }

    public class KeyValueSvc : ModelServiceBase<OSIBillingDbContext, KeyValue>, IKeyValueSvc
    {

        public async Task AddOrUpdate(string key, string value)
        {
            if (string.IsNullOrEmpty(key)) throw new System.ArgumentException($"'{nameof(key)}' cannot be null or empty.", nameof(key));
            if (string.IsNullOrEmpty(value)) throw new System.ArgumentException($"'{nameof(value)}' cannot be null or empty.", nameof(value));

            using var db = OSIBillingDbContext.DbContext;
            var keyValue = await db.KeyValues.AsTracking().FirstOrDefaultAsync(kv => kv.Key == key);
            if (keyValue == null)
            {
                keyValue = new()
                {
                    Key = key,
                };
                db.KeyValues.Add(keyValue);
            }
            keyValue.Value = value;
            await db.SaveChangesAsync();
        }

        public async Task AddOrUpdate(IReadOnlyDictionary<string, string> keyValues)
        {
            if (keyValues is null) throw new ArgumentNullException(nameof(keyValues));

            using var db = OSIBillingDbContext.DbContext;
            var dbKeyValues = await db.KeyValues.AsTracking().Where(kv => keyValues.Keys.Contains(kv.Key)).ToListAsync();
            foreach (var keyValue in keyValues)
            {
                var dbKeyValue = dbKeyValues.FirstOrDefault(kv => kv.Key == keyValue.Key);
                if (dbKeyValue == null)
                {
                    dbKeyValue = new()
                    {
                        Key = keyValue.Key,
                    };
                    db.KeyValues.Add(dbKeyValue);
                }
                dbKeyValue.Value = keyValue.Value;
            }
            await db.SaveChangesAsync();
        }

        public async Task<string> Get(string key)
        {
            if (string.IsNullOrEmpty(key)) throw new System.ArgumentException($"'{nameof(key)}' cannot be null or empty.", nameof(key));

            using var db = OSIBillingDbContext.DbContext;
            var keyValue = await db.KeyValues.FirstOrDefaultAsync(kv => kv.Key == key);
            return keyValue?.Value;
        }

        public async Task<IReadOnlyDictionary<string, string>> Get(IEnumerable<string> keys)
        {
            if (keys is null) throw new ArgumentNullException(nameof(keys));

            using var db = OSIBillingDbContext.DbContext;
            var keyValues = await db.KeyValues.Where(kv => keys.Contains(kv.Key)).ToListAsync();
            return keyValues.ToDictionary(kv => kv.Key, kv => kv.Value);
        }

        public async Task Remove(string key)
        {
            if (string.IsNullOrEmpty(key)) throw new System.ArgumentException($"'{nameof(key)}' cannot be null or empty.", nameof(key));

            using var db = OSIBillingDbContext.DbContext;
            var keyValue = await db.KeyValues.AsTracking().FirstOrDefaultAsync(kv => kv.Key == key);
            if (keyValue != null)
            {
                db.Remove(keyValue);
                await db.SaveChangesAsync();
            }
        }

        public async Task Remove(IEnumerable<string> keys)
        {
            if (keys is null) throw new ArgumentNullException(nameof(keys));

            using var db = OSIBillingDbContext.DbContext;
            var keyValues = await db.KeyValues.AsTracking().Where(kv => keys.Contains(kv.Key)).ToListAsync();
            db.RemoveRange(keyValues);
            await db.SaveChangesAsync();
        }
    }
}
