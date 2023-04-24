using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using NUnit.Framework;
using ProcessMonitorApp;


namespace ProcessMonitorUnitTests
{

    /* IMPORTANT

    1. Interracting with the console and procesess from NUnit proved to be tricky.
       The same tests, with no changes, ran simultaneously in different orders sometimes give false negatives.
       ~Should some fail, isolate and run them separately.~
    2. It appears that the console retains (in cache?) outputs from other tests that are running at the same time,
       so we get a false negative or rather test B reads the output from test A. I tried clearing the console
       with Console.Clear(), but it throws a "handle is invalid" error. It appears that "As per the community the
       recommened way to use Console.Clear() is to check whether the output is redirected or not. This issue is 
       more related to how .Net API's handle this, as it can be produced in any console app which is launched by
       another process, & the output stream is being re-directed."
       ...as I think is the case here (output is redirected to a StringWriter?). This is why sometimes the
        Input_InvalidFormat_IssuesWarning test fails when ran after Input_ValidFormat_BeginsToMonitor.     
    */

    public class ProcessMonitorTests
    {
        //tests for invalid input
        [TestCase("process 1\nKillUnitTest\n")]           //missing monitoring frequency
        [TestCase("process \nKillUnitTest\n")]            //missing 2 parameters
        [TestCase("process 1 process\nKillUnitTest\n")]   //wrong format for monitoring frequency
        [TestCase("process process 1\nKillUnitTest\n")]   //wrong format for maximum lifetime
        [TestCase("process 1 2\nKillUnitTest\n")]         //monitoring frequency > maximum lifetime
        [TestCase("process 1 1 1\nKillUnitTest\n")]       //too many parameters
        public void Input_InvalidFormat_IssuesWarning(string ConsoleInput)
        {
            // Arrange
            var reader = new StringReader(ConsoleInput);  
            Console.SetIn(reader);
            var writer = new StringWriter();
            Console.SetOut(writer);

            string expectedWarning1 = "Invalid input format. Try again";
            string expectedWarning2 = "Monitoring frequency cannot be higher than maximum lifetime. Try again";

            // Act
            ProcessMonitor.Main();
            string output = writer.ToString();

            // Assert
            Assert.That(output.Contains(expectedWarning1) || output.Contains(expectedWarning2));
        }

        //tests for valid input
        [TestCase("notepad 1 1")]            // maximum lifetime == monitoring frequency 
        [TestCase("notepad 2 1")]            // maximum lifetime > monitoring frequency
        public void Input_ValidFormat_BeginsToMonitor(string ConsoleInput)
        {
            Process testProcess = new Process();
            testProcess.StartInfo.FileName = "notepad.exe";
            testProcess.Start();
            var reader = new StringReader(ConsoleInput);
            Console.SetIn(reader);
            var writer = new StringWriter();
            Console.SetOut(writer);

            //i am using a timer because i can't get it to simulate the q keypress
            /* tried using          
            sim.Keyboard.KeyPress(VirtualKeyCode.VK_Q);
            ||
            SendKeys.SendWait("Q");
            */

            string expectedMessage = "Monitoring process";   //no reason to check for the whole thing
            Thread thread = new Thread(() => ProcessMonitor.Main());
            thread.Start();
            Thread.Sleep(2000); // wait for 2 seconds
            string output = writer.ToString();

            Assert.That(output.Contains(expectedMessage));
            testProcess.Kill();
        }

        //test to see if it closes the process after maximum lifetime
        [TestCase("notepad 1 1")]
        public void Process_ValidFormat_KillsAfterLifetime(string ConsoleInput)
        {
            Process testProcess = new Process();
            testProcess.StartInfo.FileName = "notepad.exe";
            testProcess.Start();
            var reader = new StringReader(ConsoleInput);
            Console.SetIn(reader);
            var writer = new StringWriter();
            Console.SetOut(writer);
            string expectedMessage = "Killing process";   //no reason to check for the whole thing
        
            Thread thread = new Thread(() => ProcessMonitor.Main());
            thread.Start();
            Thread.Sleep(62000); // wait for 62 seconds
            string output = writer.ToString();

            Assert.That(output.Contains(expectedMessage) && testProcess.HasExited);
        }

        //test to see that it does not close the process before maximum lifetime
        [TestCase("mspaint 1 1")]
        public void Process_ValidFormat_DoesNotKillBeforeLifetime(string ConsoleInput)
        {
            Process testProcess = new Process();
            testProcess.StartInfo.FileName = "mspaint.exe";
            testProcess.Start();
            var reader = new StringReader(ConsoleInput);
            Console.SetIn(reader);
            var writer = new StringWriter();
            Console.SetOut(writer);
       
            Thread thread = new Thread(() => ProcessMonitor.Main());
            thread.Start();
            Thread.Sleep(30000); // wait for 30 seconds

            Assert.That(!testProcess.HasExited);
            testProcess.Kill();
        }

        //test to see if it continues to monitor after killing one instance
        [TestCase("mspaint 1 1")]
        public void Process_Kills_ContinuesToMonitor(string ConsoleInput)
        {
            Process testProcess = new Process();
            testProcess.StartInfo.FileName = "mspaint.exe";
            Process testProcess2 = new Process();
            testProcess2.StartInfo.FileName = "mspaint.exe";
            testProcess.Start();
            var reader = new StringReader(ConsoleInput);
            Console.SetIn(reader);
            var writer = new StringWriter();
            Console.SetOut(writer);
         
            Thread thread = new Thread(() => ProcessMonitor.Main());
            thread.Start();
            Thread.Sleep(62000); // wait for 62 seconds
            if (testProcess.HasExited)
            {
                testProcess2.Start();
            }
            Thread.Sleep(122000); // wait for 2 min; <1min until it reads it again; 1min for lifeTime

            Assert.That(testProcess2.HasExited);
        }
    }
}