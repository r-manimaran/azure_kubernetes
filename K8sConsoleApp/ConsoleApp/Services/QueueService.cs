using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileProcessingApp.Services;

public class QueueService
{
    public readonly QueueClient _queueClient;
    public QueueService(string queueConnectionString, string queueName)
    {
        _queueClient = new QueueClient(queueConnectionString, queueName);
    }

    public async Task<string?> ReceiveMessageAsync()
    {
        QueueMessage[] receivedMessages = await _queueClient.ReceiveMessagesAsync(maxMessages:1,
            visibilityTimeout: TimeSpan.FromSeconds(30));

        if (receivedMessages.Length == 0)
            return null;

        string base64Message = receivedMessages[0].Body.ToString();
        byte[] decodedBytes = Convert.FromBase64String(base64Message);
        string messageText = Encoding.UTF8.GetString(decodedBytes);

        await _queueClient.DeleteMessageAsync(receivedMessages[0].MessageId, receivedMessages[0].PopReceipt);

        return messageText;
    }
}
