// VersionCheck.cs
using Telegram.Bot;

public static class VersionCheck
{
    public static void Check()
    {
        var client = new TelegramBotClient("dummy_token");

        // Проверка доступности методов
        Console.WriteLine("Проверка методов Telegram.Bot:");
        Console.WriteLine($"- Type: {client.GetType()}");
        Console.WriteLine($"- Assembly: {client.GetType().Assembly.FullName}");

        // Вывод всех методов
        var methods = client.GetType().GetMethods()
            .Where(m => m.Name.Contains("Webhook") || m.Name.Contains("SendText"))
            .Select(m => m.Name)
            .Distinct();

        Console.WriteLine("\nДоступные методы:");
        foreach (var method in methods)
        {
            Console.WriteLine($"- {method}");
        }
    }
}