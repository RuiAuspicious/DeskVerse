namespace DeskVerse;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        using var singleInstance = new Mutex(true, "Local\\DeskVerse.SingleInstance", out var isFirstInstance);
        if (!isFirstInstance)
        {
            return;
        }

        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
        Application.ThreadException += (_, args) => AppLogger.Log(args.Exception, "Unhandled UI thread exception");
        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
        {
            if (args.ExceptionObject is Exception exception)
            {
                AppLogger.Log(exception, "Unhandled application exception");
            }
        };

        ApplicationConfiguration.Initialize();
        Application.Run(new HitokotoWidgetForm());
    }
}
