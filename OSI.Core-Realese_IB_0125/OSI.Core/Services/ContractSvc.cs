using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using OSI.Core.Models;
using OSI.Core.Models.Db;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace OSI.Core.Services
{
    public interface IContractSvc : IModelService<OSIBillingDbContext, Contract>
    {
        Task<ApiResponse> CheckBankCode(string bankCode, string remoteIpAddress);
    }

    public class ContractSvc : ModelService<OSIBillingDbContext, Contract>, IContractSvc
    {
        public async Task<ApiResponse> CheckBankCode(string bankCode, string remoteIpAddress)
        {
            static IPNetwork ParseIPNetwork(string ipNetwork)
            {
                string[] ipNetworkParts = ipNetwork.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                return new IPNetwork(IPAddress.Parse(ipNetworkParts[0]), int.Parse(ipNetworkParts.ElementAtOrDefault(1) ?? "32"));
            }

            var contract = await GetModelByFunc(c => c.BankCode == bankCode);
            if (contract == null)
            {
                return new ApiResponse(301, "Неверный код банка");
            }
            if (contract.IpList != "*" && !contract.IpList.Split(',', StringSplitOptions.TrimEntries)
                .Any(ip => ip.Contains('/') ? ParseIPNetwork(ip).Contains(IPAddress.Parse(remoteIpAddress)) : remoteIpAddress == ip))
            {
                return new ApiResponse(302, "IP адрес не соответствует данному коду банка");
            }
            return new ApiResponse();
        }
    }
}
