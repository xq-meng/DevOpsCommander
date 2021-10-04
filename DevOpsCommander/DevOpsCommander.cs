using Azure.Storage.Queues;
using MessageQueue;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace DevOpsCommander
{
    class DevOpsCommander
    {
        public class Options
        {
            public string ConnectionString { get; set; }

            public string QueueName { get; set; }

            public string Command { get; set; }
        }

        static void MessageAnalysis(string theMessage, out string prefix, out string message)
        {
            if(string.IsNullOrEmpty(theMessage) || theMessage[0] != '[')
            {
                prefix = "";
                message = theMessage;
                return;
            }
            int prefixIndex = theMessage.IndexOf(']');
            prefix = theMessage.Substring(0, prefixIndex + 1);
            message = theMessage[(prefixIndex + 1)..];
        }

        static async Task Main(string[] args)
        {
            if(args.Length < 3)
            {
                return;
            }

            var options = new Options();
            options.ConnectionString = args[0];
            options.QueueName = args[1];
            options.Command = args[2];
            await Run(options);
        }

        static async Task Run(Options options) 
        { 
            Console.WriteLine("Azure Queue Storage client library v12.");

            if(string.IsNullOrEmpty(options.Command))
            {
                return;
            }

            string ConnectionString = options.ConnectionString;
            string QueueName = options.QueueName;

            string prefix = "[AZURECLOUD]";
            string toSend = prefix + options.Command;

            QueueClient theQueue = await MQOperation.QueueConnection(ConnectionString, QueueName);
            await MQOperation.InsertMessageAsync(theQueue, toSend);

            // Wait for end signal.
            DateTime timeSpanStart = DateTime.Now;
            while(true)
            {
                DateTime timeSpanEnd = DateTime.Now;
                TimeSpan timeSpan = timeSpanEnd - timeSpanStart; 
                if(timeSpan.TotalMinutes > 30)
                {
                    Console.WriteLine("DevOps Commander time out. ");
                    break;
                }
                string topMessage = await MQOperation.PeekTopMessageAsync(theQueue);
                MessageAnalysis(topMessage, out string tempPrefix, out string tempMessage);
                if(tempPrefix == "[WORKING_MACHINE]")
                {
                    timeSpanStart = DateTime.Now;
                    topMessage = await MQOperation.RetrieveNextMessageAsync(theQueue);
                    Console.WriteLine(topMessage);
                    if(tempMessage == "[END_SIGNAL]")
                    {
                        Console.WriteLine("Retrieved the end signal. ");
                        break;
                    }
                }
                Thread.Sleep(1000);
            }
        }
    }
}
