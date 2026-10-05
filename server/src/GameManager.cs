using System.Collections.Concurrent;
using Microsoft.AspNetCore.SignalR;

namespace NeuralNonsense {
	public sealed class GameManager {
		public static GameManager instance = new GameManager();

		public ConcurrentDictionary<string, Room> rooms = new ConcurrentDictionary<string, Room>();
		public IHubContext<NNHub>? hubCtx;
		public BadWordChecker badWordChecker = new FakeBadWordChecker();

		public async Task<string> CreateRoom() {
			const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
			string code;
			do {
				code = Random.Shared.GetString(alphabet, 4);
			} while(!badWordChecker.IsOffensive(code) && !rooms.ContainsKey(code) && rooms.TryAdd(code, new Room()));
			Room room = rooms[code];
			room.code = code;
			return code;
		}
	}
}