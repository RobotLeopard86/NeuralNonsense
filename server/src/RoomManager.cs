using System.Collections.Concurrent;
using Microsoft.AspNetCore.SignalR;

namespace NeuralNonsense {
	public sealed class RoomManager {
		public static RoomManager instance = new RoomManager();

		public ConcurrentDictionary<string, Room> rooms = new ConcurrentDictionary<string, Room>();
		public IHubContext<NNHub>? hubCtx;
	}
}