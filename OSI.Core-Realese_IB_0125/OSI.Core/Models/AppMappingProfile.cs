using AutoMapper;
using OSI.Core.Models.Db;
using OSI.Core.Models.Requests;
using System;

namespace OSI.Core.Models
{
    public class AppMappingProfile : Profile
    {
        public AppMappingProfile()
        {
            CreateMap<AbonentRequest, Abonent>()
                .ForMember(dest => dest.ErcAccount, config => config.UseDestinationValue());

            CreateMap<AbonentRequest, AbonentHistory>()
                .ForMember(dest => dest.Id, config => config.UseDestinationValue())
                .ForMember(dest => dest.AbonentId, config => config.UseDestinationValue())
                .ForMember(dest => dest.Dt, config => config.UseDestinationValue());

            CreateMap<RegistrationRequest, Registration>()
                .ForMember(dest => dest.Id, config => config.UseDestinationValue())
                .ForMember(dest => dest.CreateDt, config => config.UseDestinationValue())
                .ForMember(dest => dest.Tariff, config => config.UseDestinationValue())
                .ForMember(dest => dest.SignDt, config => config.UseDestinationValue());

            CreateMap<OsiRequest, Osi>()
                .ForMember(dest => dest.Id, config => config.UseDestinationValue())
                .ForMember(dest => dest.TakeComission, config => config.UseDestinationValue())
                .ForMember(dest => dest.IsInPromo, config => config.UseDestinationValue())
                .ForMember(dest => dest.FreeMonthPromo, config => config.UseDestinationValue())
                .ForMember(dest => dest.Rca, config => config.UseDestinationValue())
                .ForMember(dest => dest.WizardStep, config => config.UseDestinationValue())
                .ForMember(dest => dest.RegistrationId, config => config.UseDestinationValue())
                .ForMember(dest => dest.BigRepairMrpPercent, config => config.UseDestinationValue())
                .ForMember(dest => dest.IsActive, config => config.UseDestinationValue())
                .ForMember(dest => dest.IsLaunched, config => config.UseDestinationValue())
                .ForMember(dest => dest.Kbe, config => config.UseDestinationValue())
                .ForMember(dest => dest.CanRemakeAccurals, config => config.UseDestinationValue());
        }
    }
}
