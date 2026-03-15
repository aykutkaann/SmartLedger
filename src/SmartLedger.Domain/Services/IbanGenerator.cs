using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartLedger.Domain.Services
{

    public interface IIbanGenerator
    {
        string Generate(string countryCode = "TR");
    }

    public class IbanGenerator : IIbanGenerator
    {
        private static readonly Random _rn = Random.Shared;

        public string Generate(string countryCode = "TR")
        {
            var bankCode = "00081";
            var accountNo = string.Concat(Enumerable.Range(0, 16).Select(_ => _rn.Next(0, 10)));
            var checkDigits = ComputeCheckDigits(countryCode, bankCode, accountNo);
            return $"{countryCode}{checkDigits}{bankCode}{accountNo}";
        }


        private static string ComputeCheckDigits(string country, string bank, string account)
        {
            var rearranged = $"{bank}{account}{country}00";
            var numeric = string.Concat(rearranged.Select(c => char.IsLetter(c) ? (c - 'A' + 10).ToString() : c.ToString()));
            var remainder = ComputeMod97(numeric);
            var check = 98 - remainder;
            return check.ToString("D2");
        }

        private static int ComputeMod97(string numericString)
        {
            int remainder = 0;
            foreach (char c in numericString)
                remainder = (remainder * 10 + (c - '0')) % 97;
            return remainder;
        }
    }
}
