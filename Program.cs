using ShortestPathFinder.Forms;

namespace ShortestPathFinder
{

    internal static class Program
    {

        [STAThread]
        static void Main()
        {
            ApplicationConfiguration.Initialize();
            Application.Run(new MainForm());
        }
    }
}
