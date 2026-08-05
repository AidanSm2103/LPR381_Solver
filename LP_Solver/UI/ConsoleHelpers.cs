using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace LP_Solver.UI
{
    public static class ConsoleHelpers
    {
        public static void PrintHeader(string title)
        {
            Console.WriteLine();
            var bar = new string('=', title.Length + 4);
            var prevColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine(bar);
            Console.WriteLine($"  {title}");
            Console.WriteLine(bar);
            Console.ForegroundColor = prevColor;
        }

        public static int PrintMenuAndGetChoice(string title, List<string> options)
        {
            PrintHeader(title);

            for (int i = 0; i < options.Count; i++)
            {
                Console.WriteLine($"  {i + 1}. {options[i]}");
            }
            Console.WriteLine();

            while (true)
            {
                Console.Write("Select an option: ");
                string? input = Console.ReadLine();

                if (int.TryParse(input, out int choice) && choice >= 1 && choice <= options.Count)
                {
                    return choice;
                }

                PrintError($"Enter a number between 1 and {options.Count}.");
            }
        }

        public static string ReadNonEmptyLine(string prompt)
        {
            while (true)
            {
                Console.Write(prompt);
                string? input = Console.ReadLine();

                if (!string.IsNullOrWhiteSpace(input))
                    return input.Trim();

                PrintError("Input cannot be empty.");
            }
        }

        public static double ReadDouble(string prompt)
        {
            while (true)
            {
                Console.Write(prompt);
                string? input = Console.ReadLine();

                if (double.TryParse(input, out double value))
                    return value;

                PrintError("Enter a valid number.");
            }
        }

        public static int ReadInt(string prompt, int min, int max)
        {
            while (true)
            {
                Console.Write(prompt);
                string? input = Console.ReadLine();

                if (int.TryParse(input, out int value) && value >= min && value <= max)
                    return value;

                PrintError($"Enter a whole number between {min} and {max}.");
            }
        }

        public static void PrintError(string message)
        {
            var prevColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"  ! {message}");
            Console.ForegroundColor = prevColor;
        }

        public static void PrintSuccess(string message)
        {
            var prevColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"  {message}");
            Console.ForegroundColor = prevColor;
        }

        public static void PrintInfo(string message)
        {
            Console.WriteLine($"  {message}");
        }

        public static void Pause()
        {
            Console.WriteLine();
            Console.WriteLine("Press any key to continue...");
            Console.ReadKey(true);
        }
    }
}

