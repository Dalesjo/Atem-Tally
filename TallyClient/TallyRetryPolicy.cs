using Microsoft.AspNetCore.SignalR.Client;

namespace TallyClient
{
    internal class TallyRetryPolicy: IRetryPolicy
    {
        public TimeSpan? NextRetryDelay(RetryContext retryContext)
        {
            if (retryContext.ElapsedTime < TimeSpan.FromSeconds(60))
            {
                return TimeSpan.FromSeconds(5);
            }

            return TimeSpan.FromSeconds(10);

            // If we've been reconnecting for more than 60 seconds so far, stop reconnecting.
            // return null;
        }
    }
}