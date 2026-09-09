using System;
using System.Threading;

class Program
{
    // This variable lives on the Shared Heap
    static int sharedCounter = 0;

    static void Main()
    {

        Thread[] threads = new Thread[10];

        for (int i = 0; i < 10; i++)
        {
            threads[i] = new Thread(() =>
            {
                // Each thread runs this loop on its own isolated Stack,
                // but targets the exact same Heap variable.
                for (int j = 0; j < 10000; j++)
                {
                    sharedCounter++; // THE DANGER ZONE
                }
            });

            threads[i].Start();
        }

        // Wait for all threads to finish
        foreach (var t in threads)
        {
            t.Join();
        }

        // We expect 10 threads * 10,000 = 100,000.
        // Run this. You will rarely get 100,000. You will get 74,321, or 91,012.
        Console.WriteLine($"Final Counter Value: {sharedCounter}");
    }
}
