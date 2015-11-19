using System;
using System.Threading.Tasks;
using Orleans;
using Orleans.Runtime;
using Orleans.Streams;

namespace DictStreamProvider
{
    public class MyStreamFailureHandler : IStreamFailureHandler
    {
        private readonly Logger _logger;

        public MyStreamFailureHandler(Logger logger)
        {
            _logger = logger;
        }

        public bool ShouldFaultSubsriptionOnError { get; } = true;

        public Task OnDeliveryFailure(GuidId subscriptionId, string streamProviderName, IStreamIdentity streamIdentity, StreamSequenceToken sequenceToken)
        {
            _logger.AutoError($"provider name: {streamProviderName}, sub id: {subscriptionId}, stream id: {streamIdentity}, token: {sequenceToken}");
            return TaskDone.Done;
        }

        public Task OnSubscriptionFailure(GuidId subscriptionId, string streamProviderName, IStreamIdentity streamIdentity, StreamSequenceToken sequenceToken)
        {
            _logger.AutoError($"provider name: {streamProviderName}, sub id: {subscriptionId}, stream id: {streamIdentity}, token: {sequenceToken}");
            return TaskDone.Done;
        }
    }
}