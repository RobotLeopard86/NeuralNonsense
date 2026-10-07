using Microsoft.AspNetCore.SignalR;

namespace NeuralNonsense {
    public interface INNClient {
        Task ConnectionEstablished();
    }

    public class NNHub : Hub<INNClient> {
        public async Task Connect() {

        }
    }
}