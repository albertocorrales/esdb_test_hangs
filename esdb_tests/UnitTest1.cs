using System;
using Xunit;
using Ductus.FluentDocker.Builders;
using System.Threading.Tasks;
using EventStore.ClientAPI;
using System.Text;

namespace esdb_tests
{
    public class UnitTest1
    {
        private const int tcpPort = 1113;

        public UnitTest1()
        {
            new Builder()
            .UseContainer()
            .WithName("esdb-tests")
            .UseImage("eventstore/eventstore:20.10.2-bionic")
            .Command("--insecure --enable-external-tcp")
            .ReuseIfExists()
            .ExposePort(tcpPort, tcpPort)
            .WaitForPort($"{tcpPort}/tcp", TimeSpan.FromSeconds(20))
            .Build()
            .Start();
        }

        [Fact]
        public async Task Test1()
        {
            var connection = EventStoreConnection.Create($"ConnectTo=tcp://admin:changeit@127.0.0.1:{tcpPort};UseSslConnection=false;DefaultCredentials=admin:changeit");
            await connection.ConnectAsync();

            string streamName = Guid.NewGuid().ToString();
            const string eventType = "event-type";
            const string data = "{ \"a\":\"2\"}";
            const string metadata = "{}";

            var eventPayload = new EventData(
                eventId: Guid.NewGuid(),
                type: eventType,
                isJson: true,
                data: Encoding.UTF8.GetBytes(data),
                metadata: Encoding.UTF8.GetBytes(metadata)
            );
            var result = await connection.AppendToStreamAsync(streamName, ExpectedVersion.Any, eventPayload);

            var readEvents = await connection.ReadStreamEventsForwardAsync(streamName, 0, 10, true);

            foreach (var evt in readEvents.Events)
            {
                Console.WriteLine(Encoding.UTF8.GetString(evt.Event.Data));
                Assert.Equal(evt.Event.EventStreamId, streamName);
            }

        }
    }
}
