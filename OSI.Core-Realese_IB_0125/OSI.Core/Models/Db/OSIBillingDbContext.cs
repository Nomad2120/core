using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Npgsql.NameTranslation;
using OSI.Core.Helpers.EFCore;
using OSI.Core.Models.Enums;
using System;
using System.Diagnostics;
using System.Linq;

namespace OSI.Core.Models.Db
{
    public class OSIBillingDbContext : DbContext
    {
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            ConfigureOptions(optionsBuilder);
        }

        public static string ConnectionString { get; set; } = "";
        public static bool LogToDebug { get; set; } = true;

        private static readonly IServiceProvider serviceProvider = new ServiceCollection()
            .AddEntityFrameworkNpgsql()
            .AddEntityFrameworkNamingConventions()
            .AddSingleton<IMethodCallTranslatorPlugin, InTranslatorPlugin>()
            .BuildServiceProvider();

        public static OSIBillingDbContext DbContext
        {
            get
            {
                var context = new OSIBillingDbContext();
                context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
                return context;
            }
        }

        internal static void ConfigureOptions(DbContextOptionsBuilder optionsBuilder)
        {
            string cstring = Startup.Configuration.GetConnectionString("OSI_Billing") ?? ConnectionString;
#if DEBUG
            if (LogToDebug)
                optionsBuilder
                    .LogTo(log => Debug.WriteLine(log))
                    .EnableSensitiveDataLogging(true);
#endif

            optionsBuilder
                .UseInternalServiceProvider(serviceProvider)
                .UseNpgsql(cstring)
                .UseSnakeCaseNamingConvention();
        }

        // добавляем enum из базы
        static OSIBillingDbContext()
        {
            NpgsqlConnection.GlobalTypeMapper.MapEnum<AccountReportStateCodes>("account_report_state_codes", new NpgsqlNullNameTranslator());
            NpgsqlConnection.GlobalTypeMapper.MapEnum<AccountTypeCodes>("account_type_codes", new NpgsqlNullNameTranslator());
            NpgsqlConnection.GlobalTypeMapper.MapEnum<AccuralAmountTypeCodes>("accural_amount_type_codes", new NpgsqlNullNameTranslator());
            NpgsqlConnection.GlobalTypeMapper.MapEnum<ActStateCodes>("act_state_codes", new NpgsqlNullNameTranslator());
            NpgsqlConnection.GlobalTypeMapper.MapEnum<AreaTypeCodes>("area_type_codes", new NpgsqlNullNameTranslator());
            NpgsqlConnection.GlobalTypeMapper.MapEnum<ComissionCalcTypes>("comission_calc_types", new NpgsqlNullNameTranslator());
            NpgsqlConnection.GlobalTypeMapper.MapEnum<DataTypeCodes>("data_type_codes", new NpgsqlNullNameTranslator());
            NpgsqlConnection.GlobalTypeMapper.MapEnum<HouseStateCodes>("house_state_codes", new NpgsqlNullNameTranslator());
            NpgsqlConnection.GlobalTypeMapper.MapEnum<OperationTypeCodes>("operation_type_codes", new NpgsqlNullNameTranslator());
            NpgsqlConnection.GlobalTypeMapper.MapEnum<RegistrationStateCodes>("registration_state_codes", new NpgsqlNullNameTranslator());
            NpgsqlConnection.GlobalTypeMapper.MapEnum<ServiceCodes>("service_codes", new NpgsqlNullNameTranslator());
            NpgsqlConnection.GlobalTypeMapper.MapEnum<TransactionTypeCodes>("transaction_type_codes", new NpgsqlNullNameTranslator());
        }

        protected OSIBillingDbContext()
        {
        }

        //public async Task<int> NextValueForSequence(SequenceEnum.Sequence sequence)
        //{
        //    SqlParameter result = new SqlParameter("@result", System.Data.SqlDbType.Int)
        //    {
        //        Direction = System.Data.ParameterDirection.Output
        //    };
        //    var sequenceIdentifier = sequence.GetType().GetMember(sequence.ToString()).First().GetCustomAttribute<DescriptionAttribute>().Description;
        //    await Database.ExecuteSqlRawAsync($"SELECT @result = (NEXT VALUE FOR [{sequenceIdentifier}])", result);
        //    return (int)result.Value;
        //}
        public DbSet<Abonent> Abonents { get; set; }
        public DbSet<AbonentHistory> AbonentHistories { get; set; }
        public DbSet<AccountReport> AccountReports { get; set; }
        public DbSet<AccountReportCategory> AccountReportCategories { get; set; }
        public DbSet<AccountReportCategoryOption> AccountReportCategoryOptions { get; set; }
        public DbSet<AccountReportDoc> AccountReportDocs { get; set; }
        public DbSet<AccountReportList> AccountReportLists { get; set; }
        public DbSet<AccountReportListItem> AccountReportListItems { get; set; }
        public DbSet<AccountReportListItemDetail> AccountReportListItemDetails { get; set; }
        public DbSet<AccountReportListRelation> AccountReportListRelations { get; set; }
        public DbSet<AccountType> AccountTypes { get; set; }
        public DbSet<AccuralMethod> AccuralMethods { get; set; }
        public DbSet<Act> Acts { get; set; }
        public DbSet<ActDoc> ActDocs { get; set; }
        public DbSet<ActItem> ActItems { get; set; }
        public DbSet<ActOperation> ActOperations { get; set; }
        public DbSet<ActState> ActStates { get; set; }
        public DbSet<AllowedAccuralMethod> AllowedAccuralMethods { get; set; }
        public DbSet<AreaType> AreaTypes { get; set; }
        public DbSet<Arendator> Arendators { get; set; }
        public DbSet<Bank> Banks { get; set; }
        public DbSet<BaseRate> BaseRates { get; set; }
        public DbSet<ConnectedService> ConnectedServices { get; set; }
        public DbSet<Contract> Contracts { get; set; }
        public DbSet<DocType> DocTypes { get; set; }
        public DbSet<Failure> Failures { get; set; }
        public DbSet<Fine> Fines { get; set; }
        public DbSet<Fix> Fixes { get; set; }
        public DbSet<HouseState> HouseStates { get; set; }
        public DbSet<KeyValue> KeyValues { get; set; }
        public DbSet<Knp> Knps { get; set; }
        public DbSet<OperationType> OperationTypes { get; set; }
        public DbSet<Osi> Osies { get; set; }
        public DbSet<OsiAccount> OsiAccounts { get; set; }
        public DbSet<OsiAccountApplication> OsiAccountApplications { get; set; }
        public DbSet<OsiAccountApplicationDoc> OsiAccountApplicationDocs { get; set; }
        public DbSet<OsiDoc> OsiDocs { get; set; }
        public DbSet<OsiService> OsiServices { get; set; }
        public DbSet<OsiServiceAmount> OsiServiceAmounts { get; set; }
        public DbSet<OsiServiceCompany> OsiServiceCompanies { get; set; }
        public DbSet<OsiTariff> OsiTariffs { get; set; }
        public DbSet<OsiUser> OsiUsers { get; set; }
        public DbSet<ParkingPlace> ParkingPlaces { get; set; }
        public DbSet<PastDebt> PastDebts { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<PaymentOrder> PaymentOrders { get; set; }
        public DbSet<PlanAccural> PlanAccurals { get; set; }
        public DbSet<PromoOperation> PromoOperations { get; set; }
        public DbSet<Registration> Registrations { get; set; }
        public DbSet<RegistrationAccount> RegistrationAccounts { get; set; }
        public DbSet<RegistrationDoc> RegistrationDocs { get; set; }
        public DbSet<RegistrationHistory> RegistrationHistories { get; set; }
        public DbSet<RegistrationState> RegistrationStates { get; set; }
        public DbSet<ReqDoc> ReqDocs { get; set; }
        public DbSet<ReqType> ReqTypes { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<Scan> Scans { get; set; }
        public DbSet<ServiceCompany> ServiceCompanies { get; set; }
        public DbSet<ServiceGroup> ServiceGroups { get; set; }
        public DbSet<ServiceGroupSaldo> ServiceGroupSaldos { get; set; }
        public DbSet<ServiceNameExample> ServiceNameExamples { get; set; }
        public DbSet<SystemInformation> SystemInformations { get; set; }
        public DbSet<Tariff> Tariffs { get; set; }
        public DbSet<TelegramChat> TelegramChats { get; set; }
        public DbSet<TelegramSubscription> TelegramSubscriptions { get; set; }
        public DbSet<Transaction> Transactions { get; set; }
        public DbSet<UnionType> UnionTypes { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<UserRole> UserRoles { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.UseIdentityAlwaysColumns();

            // проверка всех зависимостей и выставление для всех ForeignKey OnDeleteAction = Restrict
            foreach (var relationship in modelBuilder.Model.GetEntityTypes().SelectMany(e => e.GetForeignKeys()))
            {
                if (relationship.PrincipalToDependent != null)
                {
                    relationship.DeleteBehavior = relationship.PrincipalToDependent.Name switch
                    {
                        _ => DeleteBehavior.Restrict,
                    };
                }
                else
                    throw new Exception("Wrong relationship = " + relationship.ToString());
            }

            modelBuilder.Entity<User>().Property(x => x.Code).HasConversion(v => v.ToUpperInvariant(), v => v);
            modelBuilder.Entity<Role>().Property(x => x.Code).HasConversion(v => v.ToUpperInvariant(), v => v);
        }
    }
}
