using System;

namespace ConsoleApp1 // Note: actual namespace depends on the project name.
{
    internal interface IA
    {
        public int Mail { get; set; }

    }
    public class AA : IA
    {
        public int Mail { get; set; }
    }
    internal class Program
    {
        private static async Task countDown(string name)
        {
            for (var i = 0; i < 10; i++)
            {
                Console.WriteLine($"{name}:{i}");
                if (name == "a")
                {
                    await Task.Delay(1000);
                }
                else
                {
                    await Task.Delay(2000);
                }
                
            }
        }
        static void Main(string[] args)
        {
            var a = countDown("a");
            var b = countDown("b");
            Task.WaitAll(a, b);
            Console.WriteLine("done");
            Task.Delay(100000);
        }
    }
}