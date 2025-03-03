using ESoft.CommonLibrary;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace OSI.Core.Services
{
    public interface IOTPSvc
    {
        string GenerateOTP(string login);
        bool VerifyOTP(string login, string otp);
    }

    public class OTPSvc : IOTPSvc
    {
        private readonly IConfiguration configuration;

        public OTPSvc(IConfiguration configuration)
        {
            this.configuration = configuration;
        }

        public string GenerateOTP(string login)
        {
            return GenerateOTP(DateTime.UtcNow, login);
        }

        public bool VerifyOTP(string login, string otp)
        {
            var utcNow = DateTime.UtcNow;
            var otps = Enumerable.Range(0, configuration.GetValue("OTP:MinutesValid", 5)).Select(i => GenerateOTP(utcNow.AddMinutes(-i), login));
            return otps.Contains(otp);
        }

        private static string GenerateOTP(DateTime utcDateTime, string login)
        {
            var bytes = HashHelper.GetHashBytes<SHA384>(utcDateTime.ToString("g") + login);
            return new string(Enumerable.Range(0, 6).Select(i => (char)bytes[(bytes.Length / 6 * i)..(bytes.Length / 6 * (i + 1))].Aggregate((seed, b) => (byte)((seed + b % 10) % 10 + 48))).ToArray());
        }
    }
}
