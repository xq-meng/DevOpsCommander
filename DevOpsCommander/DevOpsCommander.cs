using Azure.Storage.Queues;
using MessageQueue;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace DevOpsCommander
{
    internal class DevOpsCommander
    {
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
            Console.WriteLine("Azure Queue Storage client library v12.");

            string ConnectionString, QueueName;
            // Read json
            Dictionary<string, string> messageQueueConfig;
            try
            {
                string messageQueueConfigText = System.IO.File.ReadAllText("AzureStorgeQueue.json");
                messageQueueConfig = JsonConvert.DeserializeObject<Dictionary<string, string>>(messageQueueConfigText);
                ConnectionString = messageQueueConfig["ConnectionString"];
                QueueName = messageQueueConfig["QueueName"];
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return;
            }

            if(args.Length != 1)
            {
                return;
            }

            string prefix = "[AZURECLOUD]";
            string toSend = prefix + args[0];

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
