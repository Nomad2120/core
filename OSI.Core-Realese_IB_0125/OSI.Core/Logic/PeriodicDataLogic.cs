using Microsoft.EntityFrameworkCore;
using OSI.Core.Models.Db;
using System.Linq;
using System.Threading.Tasks;

namespace OSI.Core.Logic
{
    public static class PeriodicDataLogic
    {
        public static async Task SaveConnectedService(OSIBillingDbContext db, ConnectedService connectedService, bool saveChanges = false)
        {
            //using var db = OSIBillingDbContext.DbContext;
            // ищем последнюю запись аб + серв
            var old = await db.ConnectedServices
                .OrderByDescending(o => o.Dt)
                .FirstOrDefaultAsync(a => a.AbonentId == connectedService.AbonentId && a.OsiServiceId == connectedService.OsiServiceId);
            if (old != null)
            {
                // не совпадают состояния, либо суммы
                if (old.IsActive != connectedService.IsActive)
                {
                    // и даты не совпадают, добавляем
                    if (old.Dt != connectedService.Dt)
                    {
                        db.ConnectedServices.Add(connectedService);
                    }
                    else
                    {
                        old.IsActive = connectedService.IsActive;
                        db.ConnectedServices.Update(old);
                    }
                    if (saveChanges)
                        await db.SaveChangesAsync();
                }
            }
            else
            {
                db.ConnectedServices.Add(connectedService);
                if (saveChanges)
                    await db.SaveChangesAsync();
            }
        }

        public static async Task SaveOsiServiceAmount(OSIBillingDbContext db, OsiServiceAmount osiServiceAmount, bool saveChanges = false)
        {
            //using var db = OSIBillingDbContext.DbContext;
            
            // ищем последнюю запись аб + серв
            var old = await db.OsiServiceAmounts.OrderByDescending(o => o.Dt)
                .FirstOrDefaultAsync(a => a.OsiServiceId == osiServiceAmount.OsiServiceId);
            if (old != null)
            {
                // не совпадают методы, либо суммы
                if (old.Amount != osiServiceAmount.Amount || old.AccuralMethodId != osiServiceAmount.AccuralMethodId)
                {
                    // и даты не совпадают, добавляем
                    if (old.Dt != osiServiceAmount.Dt)
                    {
                        db.OsiServiceAmounts.Add(osiServiceAmount);
                    }
                    else
                    {
                        old.Amount = osiServiceAmount.Amount;
                        old.AccuralMethod = null;
                        old.AccuralMethodId = osiServiceAmount.AccuralMethodId;
                        db.OsiServiceAmounts.Update(old);
                    }
                    if (saveChanges)
                        await db.SaveChangesAsync();
                }
            }
            else
            {
                db.OsiServiceAmounts.Add(osiServiceAmount);
                if (saveChanges)
                    await db.SaveChangesAsync();
            }
        }

        public static async Task SaveParkingPlaces(OSIBillingDbContext db, ParkingPlace parkingPlace, bool saveChanges = false)
        {
            //using var db = OSIBillingDbContext.DbContext;

            // ищем последнюю запись аб + серв
            var old = await db.ParkingPlaces.OrderByDescending(o => o.Dt)
                .FirstOrDefaultAsync(a => a.AbonentId == parkingPlace.AbonentId && a.OsiServiceId == parkingPlace.OsiServiceId);
            if (old != null)
            {
                // не совпадают состояния, либо суммы
                if (old.Places != parkingPlace.Places)
                {
                    // и даты не совпадают, добавляем
                    if (old.Dt != parkingPlace.Dt)
                    {
                        db.ParkingPlaces.Add(parkingPlace);
                    }
                    else
                    {
                        old.Places = parkingPlace.Places;
                        db.ParkingPlaces.Update(old);
                    }
                    if (saveChanges)
                        await db.SaveChangesAsync();
                }
            }
            else
            {
                db.ParkingPlaces.Add(parkingPlace);
                if (saveChanges)
                    await db.SaveChangesAsync();
            }
        }
    }
}
