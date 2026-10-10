using System.Collections.Concurrent;
using NeuralNonsense.Commands;

namespace NeuralNonsense {
	public sealed class GameManager {
		public static GameManager instance = new GameManager();

		public ConcurrentDictionary<string, Room> rooms = new ConcurrentDictionary<string, Room>();

		public async Task<string> CreateRoom(CancellationToken shutdownToken) {
			string code;
			do {
				code = Random.Shared.GetString(Constants.ROOM_CODE_ALPHABET, (int)Constants.ROOM_CODE_LENGTH);
			} while(!ServiceContainer.instance.badWordChecker.IsOffensive(code) && !rooms.ContainsKey(code) && rooms.TryAdd(code, new Room()));
			Room room = rooms[code];
			room.code = code;
			_ = room.RunAsync(shutdownToken);
			return code;
		}

		public async Task<JoinRoomResponse> JoinRoom(JoinRoomRequest req) {
			if(string.IsNullOrEmpty(req.code)) throw new ClientCausedException("Invalid room code!");
			if(req.code.Length != Constants.ROOM_CODE_LENGTH) throw new ClientCausedException("Invalid room code!");
			if(req.code.Count((c) => !Constants.ROOM_CODE_ALPHABET.Contains(c)) > 0) throw new ClientCausedException("Invalid room code!");
			if(!rooms.ContainsKey(req.code)) throw new ClientCausedException("No such room!");
			if(string.IsNullOrWhiteSpace(req.name)) throw new ClientCausedException("Names cannot be empty!");
			if(req.name.Length > Constants.MAX_NAME_LENGTH) throw new ClientCausedException("Selected name is too long!");
			if(!Constants.MASTER_MODERATION_DISABLE && ServiceContainer.instance.badWordChecker.IsOffensive(req.name)) throw new ClientCausedException("No bad words in names, please!");
			InitialJoinCommand ijc = new InitialJoinCommand() {
				name = req.name
			};
			await rooms[req.code].writer.WriteAsync(ijc);
			await ijc.task.Task;
			return ijc.task.Task.Result;
		}
	}
}