using Microsoft.EntityFrameworkCore.Metadata;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

public class RabbitMQService
{
    private readonly ConnectionFactory _factory;
    private readonly string _queueName = "unfinished_appointments";

    public RabbitMQService(IConfiguration configuration)
    {
        _factory = new ConnectionFactory
        {
            HostName = "goose.rmq2.cloudamqp.com",
            VirtualHost = "jicdvrpu",
            UserName = "jicdvrpu",
            Password = "xxRj-yVmRpRlfs7RSC3E5-xdBgl8puZA",
            Uri = new Uri("amqps://jicdvrpu:xxRj-yVmRpRlfs7RSC3E5-xdBgl8puZA@goose.rmq2.cloudamqp.com/jicdvrpu"),
            Ssl = new SslOption
            {
                Enabled = true,
                ServerName = "goose.rmq2.cloudamqp.com"
            }
        };
    }

    public void PublishMessage(UnfinishedAppointmentDto appointment)
    {
        using var connection = _factory.CreateConnection();
        using var channel = connection.CreateModel();

        channel.QueueDeclare(queue: _queueName, durable: true, exclusive: false, autoDelete: false, arguments: null);

        var message = JsonSerializer.Serialize(appointment);
        var body = Encoding.UTF8.GetBytes(message);

        var properties = channel.CreateBasicProperties();
        properties.Persistent = true;

        channel.BasicPublish(exchange: "", routingKey: _queueName, basicProperties: properties, body: body);
    }

    public EventingBasicConsumer CreateConsumer(RabbitMQ.Client.IModel channel, EventHandler<BasicDeliverEventArgs> onMessageReceived)
    {
        var consumer = new EventingBasicConsumer(channel);
        consumer.Received += onMessageReceived;
        return consumer;
    }

    public RabbitMQ.Client.IModel CreateChannel()
    {
        var connection = _factory.CreateConnection();
        var channel = connection.CreateModel();
        channel.QueueDeclare(queue: _queueName, durable: true, exclusive: false, autoDelete: false, arguments: null);
        return channel;
    }
}
