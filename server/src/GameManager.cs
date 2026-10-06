using System.Collections.Concurrent;
using Microsoft.AspNetCore.SignalR;
using NeuralNonsense.Commands;

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
				code = Random.Shared.GetString(alphabet, 6);
			} while(!badWordChecker.IsOffensive(code) && !rooms.ContainsKey(code) && rooms.TryAdd(code, new Room()));
			Room room = rooms[code];
			room.code = code;
			_ = room.RunAsync();
			return code;
		}

		public async Task<JoinRoomResponse> JoinRoom(string roomCode, string playerName) {
			if(roomCode.Length != 6) throw new ClientCausedException("Invalid room code!");
			if(roomCode.ToArray().Where((c) => !"ABCDEFGHIJKLMNOPQRSTUVWXYZ".Contains(c)).ToArray().Length > 0) throw new ClientCausedException("Invalid room code!");
			if(!rooms.ContainsKey(roomCode)) throw new ClientCausedException("No such room!");
			if(badWordChecker.IsOffensive(playerName)) throw new ClientCausedException("No bad words in names, please!");
			InitialJoinCommand ijc = new InitialJoinCommand() {
				playerName = playerName
			};
			await rooms[roomCode].writer.WriteAsync(ijc);
			await ijc.task.Task;
			return ijc.task.Task.Result;
		}
	}
}