using Microsoft.Extensions.DependencyInjection;
using OSI.Core.Models.Db;
using OSI.Core.Services;
using System.Threading.Tasks;
using System;
using Xunit;
using OSI.Core.Models.Enums;
using OSI.Core.Models;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics.Contracts;
using Microsoft.AspNetCore.Hosting;
using System.Reflection;

namespace OSI.Core.Tests
{
    public class TestPaymentOrders : IAsyncLifetime
    {
        private readonly string connectionString = "Host=10.1.1.125;Database=osi_billing;Username=postgres;Password=Aa222111";
        private readonly ServiceProvider serviceProvider;
        private readonly IPaymentOrderSvc paymentOrderSvc;

        public TestPaymentOrders()
        {
            OSIBillingDbContext.ConnectionString = connectionString;

            IServiceCollection services = new ServiceCollection();
            services.AddSingleton(_ => new SimpleMock<IWebHostEnvironment>()
                .MemberReturns(x => x.WebRootPath, "D:\\Temp\\WebRootPath")
                .Create());
            services.AddAutoMapper(typeof(AppMappingProfile));
            services.AddSingleton(typeof(IModelService<,>), typeof(ModelService<,>));
            services.AddSingleton<IAbonentSvc, AbonentSvc>();
            services.AddSingleton<ICatalogSvc, CatalogSvc>();
            services.AddSingleton<IContractSvc, ContractSvc>();
            services.AddSingleton<IScanSvc, ScanSvc>();
            services.AddSingleton<IAddressRegistrySvc, AddressRegistrySvc>();
            services.AddSingleton<ITariffSvc, TariffSvc>();
            services.AddSingleton<IRegistrationSvc, RegistrationSvc>();
            services.AddSingleton<IUserSvc, UserSvc>();
            services.AddSingleton<IOTPSvc, OTPSvc>();
            services.AddSingleton<IOsiSvc, OsiSvc>();
            services.AddSingleton<IOsiAccountSvc, OsiAccountSvc>();
            services.AddSingleton<IOsiServiceSvc, OsiServiceSvc>();
            services.AddSingleton<IOsiServiceCompanySvc, OsiServiceCompanySvc>();
            services.AddSingleton<IServiceGroupSaldoSvc, ServiceGroupSaldoSvc>();
            services.AddSingleton<IPastDebtSvc, PastDebtSvc>();
            services.AddSingleton<IPlanAccuralSvc, PlanAccuralSvc>();
            services.AddSingleton<ITransactionSvc, TransactionSvc>();
            services.AddSingleton<IPrintInvoiceSvc, PrintInvoiceSvc>();
            services.AddSingleton<IReportsSvc, ReportsSvc>();
            services.AddSingleton<IActSvc, ActSvc>();
            services.AddSingleton<IPaymentOrderSvc, PaymentOrderSvc>();
            services.AddSingleton<IBuhSvc, BuhSvc>();
            services.AddSingleton<IBaseRateSvc, BaseRateSvc>();
            services.AddSingleton<IAccountReportSvc, AccountReportSvc>();
            serviceProvider = services.BuildServiceProvider();

            paymentOrderSvc = serviceProvider.GetService<IPaymentOrderSvc>();
        }

        public async Task InitializeAsync()
        {
            var transactionSvc = serviceProvider.GetRequiredService<ITransactionSvc>();
            using var db = OSIBillingDbContext.DbContext;
            foreach (var comissionCalcType in Enum.GetValues<ComissionCalcTypes>())
            {
                var bankCode = "4254_" + (int)comissionCalcType;
                if (!await db.Contracts.AnyAsync(c => c.BankCode == bankCode))
                {
                    db.Contracts.Add(new()
                    {
                        BankCode = bankCode,
                        BankName = "4254_" + comissionCalcType.ToString(),
                        Comission = 0.3m,
                        ComissionCalcType = comissionCalcType,
                    });
                    await db.SaveChangesAsync();
                }
                for (int i = 0; i < 5; i++)
                {
                    await transactionSvc.CreatePayment(bankCode, null, new()
                    {
                        //Date = new DateTime(2024, 01, 30, 12, i, 0),
                        Reference = bankCode + "_" + i,
                        AbonentNum = "727",
                        Services = new()
                        {
                            new()
                            {
                                ServiceId = 2,
                                Sum = 20
                            },
                            new()
                            {
                                ServiceId = 4,
                                Sum = 15
                            },
                        }
                    });
                }
                var payments = await db.Payments
                    .AsTracking()
                    .Where(p => p.Contract.BankCode == bankCode)
                    .OrderBy(p => p.Id)
                    .ToListAsync();
                int minute = 0;
                foreach (var payment in payments)
                {
                    payment.RegistrationDate = new DateTime(2024, 01, 30, 12, minute, 0);
                }
                await db.SaveChangesAsync();
            }
        }

        public async Task DisposeAsync()
        {
            using var db = OSIBillingDbContext.DbContext;
            foreach (var comissionCalcType in Enum.GetValues<ComissionCalcTypes>())
            {
                var bankCode = "4254_" + (int)comissionCalcType;
                var payments = await db.Payments
                    .AsTracking()
                    .Include(p => p.Transactions)
                    .Where(p => p.Contract.BankCode == bankCode)
                    .ToListAsync();
                var paymentOrders = await db.PaymentOrders
                    .AsTracking()
                    .Include(po => po.ActOperations)
                    .ThenInclude(ao => ao.Act)
                    .Where(po => po.Contract.BankCode == bankCode)
                    .ToListAsync();
                foreach (var payment in payments)
                {
                    foreach (var transaction in payment.Transactions)
                    {
                        db.Remove(transaction);
                    }
                    payment.Transactions.Clear();
                    db.Remove(payment);
                }
                foreach (var paymentOrder in paymentOrders)
                {
                    foreach (var actOperation in paymentOrder.ActOperations)
                    {
                        actOperation.Act.Debt += actOperation.Amount;
                        db.Remove(actOperation);
                    }
                    paymentOrder.ActOperations.Clear();
                    db.Remove(paymentOrder);
                }
                await db.SaveChangesAsync();
            }
        }

        [Fact]
        public async Task TestProcessPaymentsComissionCount()
        {
            foreach (var comissionCalcType in Enum.GetValues<ComissionCalcTypes>())
            {
                var bankCode = "4254_" + (int)comissionCalcType;
                var apiReponse = await paymentOrderSvc.ProcessPayments(bankCode, new DateTime(2024, 01, 30));
                Assert.Equal(0, apiReponse.Code);
                using var db = OSIBillingDbContext.DbContext;
                var paymentOrders = db.PaymentOrders.Where(po => po.Contract.BankCode == bankCode).ToList();
                Assert.Equal(2, paymentOrders.Count);

                var paymentOrderGroup2 = paymentOrders.First(po => po.ServiceGroupId == 2);
                Assert.Equal(20 * 5, paymentOrderGroup2.Amount);
                Assert.Equal(
                    comissionCalcType switch
                    {
                        ComissionCalcTypes.EachPaymentToEven => 0.3m,
                        ComissionCalcTypes.EachPaymentAwayFromZero => 0.3m,
                        ComissionCalcTypes.TotalAmountToEven => 0.3m,
                        ComissionCalcTypes.TotalAmountAwayFromZero => 0.3m,
                        _ => throw new InvalidOperationException("Неизвестный тип вычисления комиссии"),
                    },
                    paymentOrderGroup2.ComisBank);

                var paymentOrderGroup4 = paymentOrders.First(po => po.ServiceGroupId == 4);
                Assert.Equal(15 * 5, paymentOrderGroup4.Amount);
                Assert.Equal(
                    comissionCalcType switch
                    {
                        ComissionCalcTypes.EachPaymentToEven => 0.20m,
                        ComissionCalcTypes.EachPaymentAwayFromZero => 0.25m,
                        ComissionCalcTypes.TotalAmountToEven => 0.22m,
                        ComissionCalcTypes.TotalAmountAwayFromZero => 0.23m,
                        _ => throw new InvalidOperationException("Неизвестный тип вычисления комиссии"),
                    },
                    paymentOrderGroup4.ComisBank);

                Assert.Equal(35 * 5, paymentOrders.Sum(po => po.Amount));
                Assert.Equal(
                    comissionCalcType switch
                    {
                        ComissionCalcTypes.EachPaymentToEven => 0.5m,
                        ComissionCalcTypes.EachPaymentAwayFromZero => 0.55m,
                        ComissionCalcTypes.TotalAmountToEven => 0.52m,
                        ComissionCalcTypes.TotalAmountAwayFromZero => 0.53m,
                        _ => throw new InvalidOperationException("Неизвестный тип вычисления комиссии"),
                    },
                    paymentOrders.Sum(po => po.ComisBank));
            }
        }
    }
}
