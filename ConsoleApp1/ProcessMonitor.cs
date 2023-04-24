using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Timers;

namespace ProcessMonitorApp
{
    public class ProcessMonitor
    {
        static bool clearTimer = true;
        static bool clearInput = false;

        static System.Timers.Timer timer;

        static string processName;

        static int monitoringFrequency;
        static int Lifetime;

        public static void Main()
        {
            Console.WriteLine("Usage: Give process name, maximum lifetime (in minutes) and monitoring frequency (in minutes) separated by 1 space each");
            string[] args = Console.ReadLine().Split(' ');

            while (!clearInput)  //check if the input is in the correct form
            {
                while (args.Length != 3)
                {
                    if (args[0] == "KillUnitTest")    //for testing only (it will hang on a ReadLine method otherwise, even on a diffrent thread)
                    {
                        return;
                    }

                    Console.WriteLine("Invalid input format. Try again");
                    args = Console.ReadLine().Split(' ');
                }
                try
                {
                    processName = args[0];
                    Lifetime = int.Parse(args[1]);
                    monitoringFrequency = int.Parse(args[2]);
                    clearInput = true;

                    while (monitoringFrequency > Lifetime)
                    {
                        Console.WriteLine("Monitoring frequency cannot be higher than maximum lifetime. Try again");
                        args = Console.ReadLine().Split(' ');
                        clearInput = false;
                        break;
                    }
                }
                catch
                {
                    Console.WriteLine("Invalid input format. Try again");
                    args = Console.ReadLine().Split(' ');
                }
            }


            Console.WriteLine($"Monitoring process {processName} every {monitoringFrequency} minutes. Killing processes running longer than {Lifetime} minutes.");
            Console.WriteLine($"Press Q to stop");

            timer = new System.Timers.Timer(monitoringFrequency * 60000);
            timer.Elapsed += OnTimerElapsed;  //timer rather than using Thread.Sleep, because we still need to look for Q
            timer.AutoReset = false;

            Dictionary<int, DateTime> processStartTimes = new Dictionary<int, DateTime>();

            while (true)
            {
                while (!(Console.KeyAvailable && Console.ReadKey(true).Key == ConsoleKey.Q))
                {
                    Thread.Sleep(10);           //prevent overloading

                    while (clearTimer)
                    {
                        Process[] processes = Process.GetProcessesByName(processName);         //can be multiple processes with the same name

                        if (processes.Length != 0)
                        {
                            foreach (Process process in processes)
                            {
                                int processId = process.Id;

                                if (!processStartTimes.ContainsKey(processId))
                                {
                                    // It's a new one so add it to the dictionary with the current time
                                    processStartTimes[processId] = DateTime.Now;
                                }
                                else
                                {
                                    // It's an existing one so we check its elapsed time
                                    TimeSpan life = DateTime.Now - processStartTimes[processId];

                                    if (life.TotalMinutes > Lifetime)
                                    {
                                        try
                                        {   //there is (small) chance that one process was clossed and another one opened, with the same name and ID(can be reused once closed), in between our checks
                                            //some cannot be killed unless the user is admin
                                        
                                            Console.WriteLine($"Killing process {processName} ({processId}) which has been running for {life.TotalMinutes} minutes.");
                                            process.Kill();    
                                        }
                                        catch
                                        {
                                            Console.WriteLine($" Access to process {processName} with ID {processId} is denied. It cannot be killed");
                                        }
                                    }
                                }
                            }
                        }
                        timer.Start();
                        clearTimer = false;
                    }
                }
                Console.WriteLine("Monitoring aborted");
                clearTimer = false;
                timer.Stop();
            }
        }

    static void OnTimerElapsed(object sender, ElapsedEventArgs e)
        {
            clearTimer = true;
            timer.Enabled = true;
        }
    }
}