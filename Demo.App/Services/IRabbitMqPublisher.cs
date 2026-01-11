namespace Demo.App.Services;

public interface IRabbitMqPublisher
{
    void PublishOrderCreated(object evt, string routingKey);
}
