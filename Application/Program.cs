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
            bool FullScreen = false;

            if (args.Length > 0)
            {
                if (args[0].Trim().ToUpper() == @"/FULLSCREEN") FullScreen = true;
            }

            // To customize application configuration such as set high DPI settings or default font,
            // see https://aka.ms/applicationconfiguration.
            ApplicationConfiguration.Initialize();

            /*var splash = new SplashForm(FullScreen);
            splash.Show();

            // I call DoEvents here to make sure that the splash-screen is properly shown.
            Application.DoEvents();

            // Just a sleep to mimick your load or init procedure
            Thread.Sleep(2000);

            splash.Close();*/

            Application.Run(new MainForm(FullScreen));
        }
    }
}
