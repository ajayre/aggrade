namespace AgGrade
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            bool Windowed = false;
            bool NoSplash = false;

            if (args.Contains("/WINDOWED")) Windowed = true;
            if (args.Contains("/NOSPLASH")) NoSplash = true;

            // To customize application configuration such as set high DPI settings or default font,
            // see https://aka.ms/applicationconfiguration.
            ApplicationConfiguration.Initialize();

            SplashForm? splash = null;
            if (!NoSplash)
            {
                splash = new SplashForm(Windowed);
                splash.Show();

                // I call DoEvents here to make sure that the splash-screen is properly shown.
                Application.DoEvents();

                // Just a sleep to mimick your load or init procedure
                Thread.Sleep(2000);
            }

            MainForm Main = new MainForm(Windowed);
            Main.Shown += (sender, e) => { splash?.Close(); };

            Application.Run(new MainForm(Windowed));
        }
    }
}
