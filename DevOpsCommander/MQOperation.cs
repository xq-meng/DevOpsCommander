using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;
using System;
using System.Threading.Tasks;

namespace MessageQueue
{
    public class MQOperation
    {
        public static Task<QueueClient> QueueConnection(string ConnectionString, string QueueName)
        {
            QueueClient queueClient = new QueueClient(ConnectionString, QueueName);

            return Task.FromResult(queueClient);
        }

        public static async Task InsertMessageAsync(QueueClient theQueue, string newMessage)
        {
            if(null != await theQueue.CreateIfNotExistsAsync())
            {
                Console.WriteLine("The queue was created.");
            }

            await theQueue.SendMessageAsync(newMessage);
        }

        public static async Task<string> RetrieveNextMessageAsync(QueueClient theQueue)
        {
            if(await theQueue.ExistsAsync())
            {
                QueueProperties properties = await theQueue.GetPropertiesAsync();
                
                if(properties.ApproximateMessagesCount > 0)
                {
                    QueueMessage[] retrievedMessage = await theQueue.ReceiveMessagesAsync(1);
                    if(retrievedMessage.Length == 0)
                    {
                        return "";
                    }
                    string theMessage = retrievedMessage[0].Body.ToString();
                    await theQueue.DeleteMessageAsync(retrievedMessage[0].MessageId, retrievedMessage[0].PopReceipt);
                    return theMessage;
                }

                else
                {
                    return "";
                }
            }

            return "";
        }

        public static async Task<string> PeekTopMessageAsync(QueueClient theQueue)
        {
            if(await theQueue.ExistsAsync())
            {
                QueueProperties properties = await theQueue.GetPropertiesAsync();

                if(properties.ApproximateMessagesCount > 0)
                {
                    PeekedMessage[] retrievedMessage = await theQueue.PeekMessagesAsync(1);
                    if(retrievedMessage.Length == 0)
                    {
                        return "";
                    }
                    string theMessage = retrievedMessage[0].Body.ToString();
                    return theMessage;
                }
                else
                {
                    return "";
                }
            }
            return "";
        }

    }
}
