namespace OpenTuningTool;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();

        // Add global exception handlers to prevent silent crashes
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
        Application.ThreadException += (sender, args) => ShowError(args.Exception);
        AppDomain.CurrentDomain.UnhandledException += (sender, args) => ShowError(args.ExceptionObject as Exception);

        Application.Run(new Form1());
    }

    private static void ShowError(Exception? ex)
    {
        MessageBox.Show(
            $"An unexpected error occurred. The application will attempt to continue, but it may be in an unstable state.\n\nError: {ex?.Message}\n\nTrace:\n{ex?.StackTrace}",
            "Application Error",
            MessageBoxButtons.OK,
            MessageBoxIcon.Error);
    }
}
