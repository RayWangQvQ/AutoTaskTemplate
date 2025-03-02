namespace AutoTaskTemplate.PlaywrightGenerator;

internal static class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("Hello, World!");

        InstallBrowser();

        var entranceUrl = "https://www.bilibili.com/";
        Microsoft.Playwright.Program.Main(["codegen", entranceUrl]);
    }

    private static void InstallBrowser()
    {
        var exitCode = Microsoft.Playwright.Program.Main(["install", "--with-deps", "chromium"]);
        if (exitCode != 0)
        {
            throw new Exception($"Playwright exited with code {exitCode}");
        }
    }
}