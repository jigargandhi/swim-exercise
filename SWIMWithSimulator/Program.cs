using System;

namespace SWIMWithSimulator
{
    class Program
    {
        static void Main(string[] args)
        {
            Application application = new Application(args[0]);
            application.Run();
        }
    }
}
