using TallyServer.Contract;
using TallyShared.Contract;

namespace TallyServer.Hubs
{
    public interface ITallyClient
    {
        Task ReceiveTally(Tally tally);

        Task RecieveChannel(Input channel);
    }
}
