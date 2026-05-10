using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PrviProjekat
{
   public static class Log
{
    private static readonly object lockObj = new object();

    public static void Info(string msg)
    {
        lock (lockObj)
        {
            Console.WriteLine($"[INFO] {DateTime.Now:HH:mm:ss} - {msg}");
        }
    }

    public static void Error(string msg)
    {
        lock (lockObj)
        {
            Console.ForegroundColor = ConsoleColor.Red; // Promeni boju u crvenu
        Console.WriteLine($"[ERROR] {DateTime.Now:HH:mm:ss} - {msg}");
        Console.ResetColor(); // Vrati na staru boju
        }
    }
}
}