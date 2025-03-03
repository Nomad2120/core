using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OSI.Core.Models.AddressRegistry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Web;

namespace OSI.Core.Services
{
    public interface IAddressRegistrySvc
    {
        Task<Region> GetRegionByRca(string rca);

        Task<IEnumerable<Region>> GetRegions();
    }

    public class AddressRegistrySvc : IAddressRegistrySvc
    {
        private readonly IHttpClientFactory httpClientFactory;
        private readonly IConfiguration configuration;

        public AddressRegistrySvc(IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            this.httpClientFactory = httpClientFactory;
            this.configuration = configuration;
        }

        public async Task<Region> GetRegionByRca(string rca)
        {
            using var client = httpClientFactory.CreateClient();
            var httpResponseMessage = await client.GetAsync(configuration["Urls:AddressRegistryApi"] + $"/info/RegionByRca?rca={HttpUtility.UrlEncode(rca)}");
            if (httpResponseMessage.IsSuccessStatusCode)
            {
                return await httpResponseMessage.Content.ReadFromJsonAsync<Region>();
            }
            else
            {
                if (httpResponseMessage.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    throw new Exception("Не найден регион по РКА");
                }
                else
                {
                    throw new Exception("Ошибка получения региона по РКА");
                }
            }
        }

        public async Task<IEnumerable<Region>> GetRegions()
        {
            using var client = httpClientFactory.CreateClient();
            var httpResponseMessage = await client.GetAsync(configuration["Urls:AddressRegistryApi"] + $"/info/Regions");
            if (httpResponseMessage.IsSuccessStatusCode)
            {
                return await httpResponseMessage.Content.ReadFromJsonAsync<IEnumerable<Region>>();
            }
            else
            {
                throw new Exception("Ошибка получения регионов");
            }
        }
    }
}
