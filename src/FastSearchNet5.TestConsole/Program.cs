using System;

namespace FastSearchNet5.TestConsole
{
    internal static class Program
    {
        private static void Main()
        {
            var showMenu = true;
            while (showMenu)
            {
                showMenu = MainMenu();
            }
        }

        private static bool MainMenu()
        {
            Console.Clear();
            Console.WriteLine("\nChoose sample to run:");
            Console.WriteLine("1) Scan all disks for file.");
            Console.WriteLine("2) ...");
            Console.WriteLine("3) End");
            Console.Write("\nSelect an option:");

            switch (Console.ReadLine())
            {
                case "1":
                    {
                        Console.WriteLine("Enter file name or pattern to search for:");
                        Samples.ScanDisks.Main(Console.ReadLine());
                    }
                    return true;

                case "2":
                    return true;

                case "3":
                    return false;

                default:
                    return true;
            }
        }

    }
}
