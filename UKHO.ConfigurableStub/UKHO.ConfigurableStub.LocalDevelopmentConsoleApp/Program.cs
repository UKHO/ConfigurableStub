using System;


namespace UKHO.ConfigurableStub.LocalDevelopmentConsoleApp
{
    class Program
    {
        static void Main(string[] args)
        {
            var StubManager = new StubManager();
            StubManager.Start();
            Console.ReadLine();
        }
    }
}
