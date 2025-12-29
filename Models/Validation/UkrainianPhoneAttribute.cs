using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace ProjectsDonetskWaterHope.Validation
{
    public class UkrainianPhoneAttribute : ValidationAttribute
    {

        private static readonly Regex ValidPhoneRegex =
            new(@"^(?:\+380\d{9}|0\d{9})$", RegexOptions.Compiled);

        public override bool IsValid(object? value)
        {
            if (value == null)
                return true; 

            var phone = value.ToString()!.Trim();

            phone = Regex.Replace(phone, @"[^\d+]", "");

            return ValidPhoneRegex.IsMatch(phone);
        }

        public static string NormalizePhone(string phone)
        {
            if (string.IsNullOrWhiteSpace(phone))
                return phone;

            phone = Regex.Replace(phone, @"[^\d+]", "");

            if (phone.StartsWith("0"))
                return "+38" + phone;

            if (phone.StartsWith("+380"))
                return phone;

            throw new ValidationException("Некоректний номер телефону");
        }
    }
}