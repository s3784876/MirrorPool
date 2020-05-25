using System.Linq;
using System;

namespace Mirror_Pool
{
  class ConsoleTools
  {
    private static int ListOptions(String[] options, bool confirm, bool newAccepted)
    {
      int selection = int.MinValue;

      int lowerLimit = newAccepted ? -1 : 0;
      do
      {
        do
        {
          Console.Clear();
          //Get Decision
          Console.WriteLine("Please Select One of The Following");
          for (int i = 0; i < options.Length; i++)
          {
            Console.WriteLine($"\t[{i}]:\t{options[i]}");
          }
          if (newAccepted)
            Console.WriteLine("\t or [-1] for new");

          selection = int.Parse(Console.ReadLine());

        } while (selection < lowerLimit || selection >= options.Length);


        //Get Confirmation
        if (confirm)
        {
          Console.Write("You have selected to create/modify ");

          if (selection == -1)
            Console.Write("A New Element");

          else
            Console.Write(options[selection]);


          Console.WriteLine(" is that correct? [Y/N]");
        }

      } while (confirm && Char.ToUpper(Console.ReadLine().First()) != 'Y');
      return selection;
    }

    public static char GetDecision(char[] options, string[] descriptions)
    {
      char response;

      do
      {
        Console.Clear();
        for (int i = 0; i < options.Length; i++)
        {
          Console.WriteLine($"\t[{options[i]}]:\t{descriptions[i]}");
        }

        response = Console.ReadKey().KeyChar;
      } while (!options.Contains(response));


      return response;
    }

  }
}