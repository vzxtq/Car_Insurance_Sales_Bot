using Microsoft.AspNetCore.Authentication;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;

namespace Car_Insurance_Bot.Services
{
    public class MindeeMock
    {
        public static (string Name, string Passport) ExtractData()
        {
            string name = "Ivanov I.I.";
            string passport = "4507 123456";
            return (name, passport);
        }
    }
}