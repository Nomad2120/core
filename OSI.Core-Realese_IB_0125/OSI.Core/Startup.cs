using BlazorDownloadFile;
using Blazored.LocalStorage;
using Blazorise;
using Blazorise.Bootstrap;
using Blazorise.Icons.FontAwesome;
using Coravel;
using IbanNet.DependencyInjection.ServiceProvider;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using OSI.Core.Auth;
using OSI.Core.Helpers;
using OSI.Core.Jobs;
using OSI.Core.Logging;
using OSI.Core.Models;
using OSI.Core.Services;
using System;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.Json.Serialization;

namespace OSI.Core
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public static IConfiguration Configuration { get; private set; }

        public static decimal MRP => 3932m;

        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddRazorPages();
            services.AddServerSideBlazor();
            services.AddOptions();

            // добавляем настройки Cors, в Configure будет их применение
            // Надо настроить перед развертыванием в бой, когда уже будет свой домен для проекта, чтобы пропускал не все домены и методы
            services.AddCors(o => o.AddPolicy("OSIPolicy", builder =>
            {
                builder.AllowAnyOrigin()
                       .AllowAnyMethod()
                       .AllowAnyHeader();
            }));

            // компоненты
            services.AddScoped<Radzen.DialogService>();
            services.AddScoped<Radzen.NotificationService>();
            services.AddBlazorise(options =>
            {
                options.ChangeTextOnKeyPress = true; // optional
            }).AddBootstrapProviders()
             .AddFontAwesomeIcons();
            services.AddBlazoredLocalStorage();
            // скачивание файлов
            services.AddBlazorDownloadFile(ServiceLifetime.Scoped);

            // база
            //services.AddDbContext<OSIBillingDbContext>(OSIBillingDbContext.ConfigureOptions,
            //    ServiceLifetime.Transient,
            //    ServiceLifetime.Transient);

            // сервисы
            services.AddAutoMapper(typeof(AppMappingProfile));
            services.AddSingleton(typeof(IModelService<,>), typeof(ModelService<,>));
            services.AddSingleton<IAbonentSvc, AbonentSvc>();
            services.AddSingleton<IAccountReportSvc, AccountReportSvc>();
            services.AddSingleton<IActSvc, ActSvc>();
            services.AddSingleton<IAddressRegistrySvc, AddressRegistrySvc>();
            services.AddSingleton<IBaseRateSvc, BaseRateSvc>();
            services.AddSingleton<IBuhSvc, BuhSvc>();
            services.AddSingleton<ICatalogSvc, CatalogSvc>();
            services.AddSingleton<IContractSvc, ContractSvc>();
            services.AddSingleton<IJobLogic, JobLogic>();
            services.AddSingleton<IJobSvc, JobSvc>();
            services.AddSingleton<IKeyValueSvc, KeyValueSvc>();
            services.AddSingleton<IOsiAccountSvc, OsiAccountSvc>();
            services.AddSingleton<IOsiAccountApplicationSvc, OsiAccountApplicationSvc>();
            services.AddSingleton<IOsiServiceCompanySvc, OsiServiceCompanySvc>();
            services.AddSingleton<IOsiServiceSvc, OsiServiceSvc>();
            services.AddSingleton<IOsiSvc, OsiSvc>();
            services.AddSingleton<IOTPSvc, OTPSvc>();
            services.AddSingleton<IPastDebtSvc, PastDebtSvc>();
            services.AddSingleton<IPaymentOrderSvc, PaymentOrderSvc>();
            services.AddSingleton<IPlanAccuralSvc, PlanAccuralSvc>();
            services.AddSingleton<IPrintInvoiceSvc, PrintInvoiceSvc>();
            services.AddSingleton<IQRCodeSvc, QRCodeSvc>();
            services.AddSingleton<IRegistrationSvc, RegistrationSvc>();
            services.AddSingleton<IRegistrationAccountSvc, RegistrationAccountSvc>();
            services.AddSingleton<IReportsSvc, ReportsSvc>();
            services.AddSingleton<IScanSvc, ScanSvc>();
            services.AddSingleton<ISendEmailSvc, SendEmailSvc>();
            services.AddSingleton<IServiceGroupSaldoSvc, ServiceGroupSaldoSvc>();
            services.AddSingleton<ISmsSvc, SmsSvc>();
            services.AddSingleton<ITariffSvc, TariffSvc>();
            services.AddSingleton<ITelegramBotSvc, TelegramBotSvc>();
            services.AddSingleton<ITelegramNotificationSvc, TelegramNotificationSvc>();
            services.AddSingleton<ITelegramSubscriptionBotSvc, TelegramSubscriptionBotSvc>();
            services.AddSingleton<ITransactionSvc, TransactionSvc>();
            services.AddSingleton<IUserSvc, UserSvc>();
#if DEBUG
            TelegramBotSvc.DoNotStart = true;
            TelegramSubscriptionBotSvc.DoNotStart = true;
#endif

            // аутентификация и авторизация
            services
                .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new()
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidIssuer = Configuration["Token:Issuer"],
                        ValidAudience = Configuration["Token:Issuer"],
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Configuration["Token:Secret"])),
                        ValidateLifetime = true,
                        ClockSkew = TimeSpan.FromSeconds(Configuration.GetValue<double>("Token:ClockSkew")),
                    };
                });
            services.AddAuthorization();
            services.AddScoped<AuthenticationStateProvider, TokenServerAuthenticationStateProvider>();
            services.AddScoped<IAuthSvc, AuthSvc>();

            // прочее
            services.AddControllers(options =>
            {
                options.InputFormatters.Add(new RawRequestBodyFormatter());
            }).AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()); // для сериализации enum в swagger и не только
            });

            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "OSI.Core", Version = "v1" });
                var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                c.IncludeXmlComments(xmlPath, true);
                c.CustomSchemaIds(t => t.FullName);
                c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    BearerFormat = "JWT",
                    In = ParameterLocation.Header,
                });
                c.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            }
                        },
                        Array.Empty<string>()
                    }
                });
            });

            services.AddIbanNet();

            services.AddHttpContextAccessor();

            services.AddTransient<HttpLoggingHandler>();
            services
                .AddHttpClient(Options.DefaultName)
                .AddHttpMessageHandler<HttpLoggingHandler>();

            services.AddDistributedMemoryCache();

            services.AddScheduler();
            services.AddSingleton<CreateAccountReportsJob>();
            services.AddSingleton<CreateAccuralsJob>();
            services.AddSingleton<CreateFineJob>();

            ConfigureHttpHelpers();
            ChangeToken.OnChange(() => Configuration.GetReloadToken(), ConfigureHttpHelpers);

            static void ConfigureHttpHelpers()
            {
                HttpHelpers.AlwaysLogBytes = Configuration.GetValue<bool>("HttpHelpers:LogBytes");
            }
        }

        public async void Configure(IApplicationBuilder app, IWebHostEnvironment env, IJobSvc jobSvc,
            // НЕ УДАЛЯТЬ! Нужно чтобы бот стартовал в самом начале
#pragma warning disable IDE0079 // Remove unnecessary suppression
#pragma warning disable IDE0060 // Remove unused parameter
            ITelegramBotSvc telegramBot,
            ITelegramSubscriptionBotSvc telegramSubscriptionBot,
            IServiceProvider serviceProvider)
#pragma warning restore IDE0060 // Remove unused parameter
#pragma warning restore IDE0079 // Remove unnecessary suppression
        {
            // загрузка абонентов из файла
            //await TempSvc.ProcessTxtFile(@"c:\aaa\meken5_eOsi (1).txt");
            //var a = 1;

            //
            // ++++++++++++ Тестирование AutoMapper
            //
            //using var db = OSIBillingDbContext.DbContext;
            //var abonSvc = serviceProvider.GetRequiredService<IAbonentSvc>();
            //var mapper = serviceProvider.GetRequiredService<IMapper>();

            //Abonent abonent = abonSvc.GetAbonentById(464).GetAwaiter().GetResult();
            //AbonentRequest request = new()
            //{
            //    OsiId = 36,
            //    Name = "Садыкова А.Е.",
            //    Flat = "108",
            //    Idn = "",
            //    AreaTypeCode = AreaTypeCodes.RESIDENTIAL,
            //    Phone = "7775447252",
            //    Floor = 3,
            //    Square = 48.28m,
            //    LivingJur = 0,
            //    LivingFact = 0,
            //    Owner = "Собственник",
            //    External = false,
            //    EffectiveSquare = 40m
            //};
            //abonent = mapper.Map(request, abonent);

            //var a = 1;
            //
            // ----------- Тестирование AutoMapper
            //

            //using (var db = OSIBillingDbContext.DbContext)
            //{
            //    var comission = (db.Payments
            //        .Include(p => p.Transactions)
            //        .Include(p => p.Contract)
            //        .Where(p => p.OsiId == 110
            //        && p.RegistrationDate >= new DateTime(2023, 1, 1)
            //        && p.RegistrationDate < new DateTime(2023, 2, 1)
            //        && p.Transactions.Any(t => t.GroupId == 7)
            //        && p.Contract.BankCode != "OSI")
            //        .Sum(p => p.Amount)) *0.02m;
            //}

            //           select oss.*, (select g.id from service_groups g
            //                    join services s on s.group_id = g.id
            //                    join osi_services os on os.service_code = s.code
            //                   where oss.osi_service_id = os.id) from osi_service_saldo oss
            //where oss.abonent_id = 5

            // перенос OsiServiceSaldo в ServiceGroupSaldo
            //using (var db = serviceProvider.GetRequiredService<OSIBillingDbContext>())
            //{
            //    var osiServiceSaldos = db.OsiServiceSaldos
            //        .Include(o => o.OsiService).ThenInclude(os => os.Service)
            //        .Include(o => o.Transaction)
            //        //.Where(o => o.AbonentId == 5)
            //        .ToList();

            //    foreach (var abonentGroup in osiServiceSaldos.GroupBy(oss => oss.AbonentId))
            //    {
            //        foreach (var serviceGroup in abonentGroup.GroupBy(abOss => abOss.OsiService.Service.GroupId))
            //        {
            //            var oss = serviceGroup.First();
            //            decimal saldoByGroup = serviceGroup.Sum(oss => oss.Saldo);
            //            var serviceGroupSaldo = new ServiceGroupSaldo
            //            {
            //                GroupId = oss.OsiService.Service.GroupId,
            //                AbonentId = oss.AbonentId,
            //                OsiId = oss.OsiId,
            //                Saldo = saldoByGroup,
            //                Transaction = new Transaction
            //                {
            //                    AbonentId = oss.AbonentId,
            //                    Dt = new DateTime(1, 1, 1),
            //                    Amount = saldoByGroup,
            //                    OsiId = oss.OsiId,
            //                    GroupId = oss.OsiService.Service.GroupId,
            //                    TransactionType = TransactionTypeCodes.SALDO
            //                }
            //            };
            //            db.ServiceGroupSaldos.Add(serviceGroupSaldo);
            //            db.OsiServiceSaldos.RemoveRange(serviceGroup);
            //            db.Transactions.RemoveRange(serviceGroup.Select(o => o.Transaction));
            //            db.SaveChanges();
            //        }
            //    }
            //}
            //var a = 1;

            // исправление актов за декабрь
            //using (var db = serviceProvider.GetRequiredService<OSIBillingDbContext>())
            //{
            //    // osi 62 не трогаем, потому что он уже имеет акты и за декабрь и за январь, непонятно почему
            //    var decemberActs = db.Acts.Where(a => a.OsiId != 62 && a.ActPeriod == new DateTime(2021, 12, 31)).ToListAsync().GetAwaiter().GetResult();
            //    foreach (var act in decemberActs)
            //    {
            //        PlanAccural planDecember = db.PlanAccurals.FirstOrDefaultAsync(p => p.OsiId == act.OsiId && p.BeginDate == new DateTime(2021, 12, 1)).GetAwaiter().GetResult();
            //        act.PlanAccural = planDecember;
            //        db.Acts.Update(act);
            //        db.SaveChanges();
            //    }
            //}
            //var a = 1;

            //исправление актов за январь
            //using (var db = serviceProvider.GetRequiredService<OSIBillingDbContext>())
            //{
            //    IActSvc actSvc = serviceProvider.GetRequiredService<IActSvc>();
            //    // osi 62 не трогаем, потому что он уже имеет акты и за декабрь и за январь, непонятно почему
            //    var januaryActs = db.Acts.Where(a => a.OsiId != 62 && a.ActPeriod == new DateTime(2022, 1, 31)).ToListAsync().GetAwaiter().GetResult();
            //    foreach (var act in januaryActs)
            //    {
            //        if (act.StateCode == ActStateCodes.SIGNED)
            //        {
            //            actSvc.UnsignAct(act).GetAwaiter().GetResult();
            //            act.State = null;
            //            act.StateCode = ActStateCodes.CREATED;
            //        }
            //        act.ActPeriod = new DateTime(2021, 12, 31);
            //        db.Acts.Update(act);
            //        db.SaveChanges();
            //    }
            //}
            //var a = 1;

            //using (var db = serviceProvider.GetRequiredService<OSIBillingDbContext>())
            //{
            //    var decemberActs = db.Acts.Where(a => a.OsiId != 62 && a.ActPeriod == new DateTime(2021, 12, 31)).ToListAsync().GetAwaiter().GetResult();
            //    foreach (var decemberAct in decemberActs)
            //    {
            //        decimal comissionDecember = db.Payments.Where(p => p.OsiId == decemberAct.OsiId
            //        && p.RegistrationDate >= new DateTime(2021, 12, 1)
            //        && p.RegistrationDate < new DateTime(2022, 1, 1))?.SumAsync(p => p.Comission).GetAwaiter().GetResult() ?? 0;

            //        decimal comissionJanuary = db.Payments.Where(p => p.OsiId == decemberAct.OsiId
            //            && p.RegistrationDate >= new DateTime(2022, 1, 1)
            //            && p.RegistrationDate < new DateTime(2022, 2, 1))?.SumAsync(p => p.Comission).GetAwaiter().GetResult() ?? 0;

            //        PlanAccural planDecember = db.PlanAccurals.FirstOrDefaultAsync(p => p.OsiId == decemberAct.OsiId && p.BeginDate == new DateTime(2022, 1, 1)).GetAwaiter().GetResult();
            //        decimal actAmount = planDecember.ApartCount * 100.0m;
            //        Act correctAct = new Act
            //        {
            //            ActNum = planDecember.Id.ToString().PadLeft(10, '0'),
            //            ActPeriod = new DateTime(2022, 1, 31),
            //            CreateDt = DateTime.Now,
            //            OsiId = planDecember.OsiId,
            //            StateCode = ActStateCodes.CREATED,
            //            PlanAccuralId = planDecember.Id,
            //            Amount = actAmount,
            //            Debt = actAmount - comissionDecember - comissionJanuary + decemberAct.Comission
            //        };
            //        correctAct.Comission = correctAct.Amount - correctAct.Debt;
            //        db.Acts.Add(correctAct);
            //        db.SaveChanges();
            //    }
            //}
            //var b = 1;

            // тестируем файлы распределения
            //IPaymentOrderSvc paymentOrderSvc = serviceProvider.GetRequiredService<IPaymentOrderSvc>();
            //var dt = new DateTime(2021, 9, 3);
            //var state1 = paymentOrderSvc.GetNotProcessedPayments("ERC", dt).GetAwaiter().GetResult();
            //var state2 = paymentOrderSvc.ProcessPayments("ERC", dt).GetAwaiter().GetResult();
            //var state3 = paymentOrderSvc.GetPaymentOrders("ERC", dt).GetAwaiter().GetResult();
            //var a = 1;

            // тестируем начисления
            //ITransactionSvc transactionSvc = serviceProvider.GetRequiredService<ITransactionSvc>();
            //var transactions = await transactionSvc.GetListTransactionsToCreateAccuralsByPlanId(73);

            // проверяем ОСВ
            //using (var db = serviceProvider.GetRequiredService<OSIBillingDbContext>())
            //{
            //    DateTime onDate1 = new DateTime(2021, 10, 1);
            //    int osiId = 42;
            //    int abonentId = 752;
            //    DateTime checkDate = onDate1.Date;
            //    var transactions = db.Transactions
            //        .Include(t => t.Group)
            //        .Include(t => t.Abonent)
            //        .Where(t => t.Dt.Date < checkDate && t.OsiId == osiId && t.AbonentId == abonentId)
            //        .GroupBy(t => new { t.Abonent.Id, t.Abonent.AreaTypeCode, t.Group.NameRu })
            //        .Select(g => new
            //        {
            //            AbonentId = g.Key.Id,
            //            AreaTypeCode = g.Key.AreaTypeCode,
            //            ServiceName = g.Key.NameRu,
            //            Amount = Math.Round(g.Sum(t => t.Amount), 2)
            //        }).ToList();
            //    var a = 1;
            //}

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                //app.UseHsts();
            }

            // использование политик Cors, указанных выше
            //app.UseCors("OSIPolicy1");
            app.UseCors(builder =>
            {
                builder.AllowAnyOrigin()
                       .AllowAnyMethod()
                       .AllowAnyHeader();
            });

            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "OSI.Core v1");
                c.EnableDeepLinking();
                c.EnableTryItOutByDefault();
                c.DocExpansion(Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.None);
            });

            //app.UseHttpsRedirection();
            app.UseStaticFiles();

            // отдельная мапа для /api чтобы некорректные адреса с этим префиксом выдавали 404 ошибку, за счет того что отключены обработчики MapBlazorHub и MapFallbackToPage
            // как при обычном варианта ниже
            app.MapWhen(context => context.Request.Path.StartsWithSegments("/api"),
                app2 =>
                {
                    app2.UseRouting();

                    app2.UseMiddleware<RequestResponseLoggingMiddleware>();

                    app2.UseAuthentication();
                    app2.UseAuthorization();

                    app2.UseEndpoints(endpoints =>
                    {
                        endpoints.MapControllers();
                    });
                });

            app.UseRouting();

            app.UseMiddleware<RequestResponseLoggingMiddleware>();

            app.UseAuthentication();

            // из-за мапы /api варнингует здесь, но так и должно быть, поэтому просто убираем плесень (зеленое подчеркивание)
#pragma warning disable ASP0001 // Authorization middleware is incorrectly configured.
            app.UseAuthorization();
#pragma warning restore ASP0001 // Authorization middleware is incorrectly configured.

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapBlazorHub();
                endpoints.MapFallbackToPage("/_Host");
            });

            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance); // дает доступ к кодировкам cp866 и win1251

#if !DEBUG
            jobSvc.StartTimer(); // старый сервис по джобам

            // новый сервис по джобам
            var timeZoneInfo = TimeZoneInfo.FindSystemTimeZoneById("West Asia Standard Time"); // По умолчанию время в UTC
            app.ApplicationServices.UseScheduler(scheduler =>
            {
                scheduler.Schedule<CreateAccountReportsJob>()
                    .Cron("00 01 1 * *") // Каждый месяц 1 числа в 01:00
                    .Zoned(timeZoneInfo)
                    .PreventOverlapping(nameof(CreateAccountReportsJob))
                    //.RunOnceAtStart()
                    ;

                scheduler.Schedule<CreateFineJob>()
                    .Cron("00 01 1 * *") // Каждый месяц 1 числа в 01:00
                    .Zoned(timeZoneInfo)
                    .PreventOverlapping(nameof(CreateFineJob))
                    //.RunOnceAtStart()
                    ;

                scheduler.Schedule<CreateAccuralsJob>()
                   .Cron("0 2 1-10 * *") // В 02:00, с 1 по 10 число месяца
                   .Zoned(timeZoneInfo)
                   .PreventOverlapping(nameof(CreateAccuralsJob))
                   //.RunOnceAtStart()
                   ;
            });
#endif
        }
    }
}
