using Azure.Storage.Queues;
using CommandLine;
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
            [Option('s', "ConnectionString", Required = true, HelpText = "Azure storage queue connection string.")]
            public string ConnectionString { get; set; }

            [Option('q', "QueueName", Required = true, HelpText = "Azure storage queue name.")]
            public string QueueName { get; set; }

            [Option('c', "Command", Required = false, HelpText = "Input the command you want to execute on SapPor.")]
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
            ParserResult<Options> parserResult = Parser.Default.ParseArguments<Options>(args);
            if (parserResult.Tag != ParserResultType.Parsed)
            {
                _ = ((NotParsed<Options>)parserResult).Errors;
                return;
            }

            var options = ((Parsed<Options>)parserResult).Value;
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
