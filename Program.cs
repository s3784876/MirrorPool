using System;

namespace Mirror_Pool
{
  class Program
  {
    static void Main(string[] args)
    {
      foreach (var item in args)
      {
        Actor a = new Actor(item);
        a.CheckAll();
      }
    }
  }
}