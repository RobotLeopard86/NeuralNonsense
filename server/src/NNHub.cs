using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace NeuralNonsense {
    public interface INNClient {
        Task ConnectionEstablished();
    }

    [Authorize]
    public class NNHub : Hub<INNClient> {
        private Room ResolveRoom() {

        }

        public override Task OnConnectedAsync() {
            return base.OnConnectedAsync();
        }
    }
}