using Microsoft.AspNetCore.Identity;

namespace MyAiFeedbackSystem.WebApp;

public static class AdminPasswordCommand
{
    public static bool TryRun(string[] args)
    {
        if (args.Length != 1 || args[0] != "--hash-admin-password")
            return false;

        if (Console.IsInputRedirected)
            throw new InvalidOperationException("Run this command in an interactive terminal.");

        Console.Write("New administrator password: ");
        var password = ReadPassword();
        Console.WriteLine();
        if (password.Length < 12)
            throw new InvalidOperationException("Use at least 12 characters for the administrator password.");

        var hash = new PasswordHasher<string>().HashPassword("admin", password);
        Console.WriteLine("Store this hash with: dotnet user-secrets set Admin:PasswordHash <hash> --project src/WebApp");
        Console.WriteLine(hash);
        return true;
    }

    private static string ReadPassword()
    {
        var password = new System.Text.StringBuilder();
        while (true)
        {
            var key = Console.ReadKey(intercept: true);
            if (key.Key == ConsoleKey.Enter)
                return password.ToString();
            if (key.Key == ConsoleKey.Backspace && password.Length > 0)
                password.Length--;
            else if (!char.IsControl(key.KeyChar))
                password.Append(key.KeyChar);
        }
    }
}
