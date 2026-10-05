
using Microsoft.AspNetCore.SignalR;

namespace NeuralNonsense {
	public sealed class Room {
		public sealed class Player {
			public string name = "";
			public string uuid = "";
			public uint score = 0;
		}

		public enum Phase {
			Lobby,
			ContentGen,
			R1Answer,
			R1Vote,
			R2Answer,
			R2Vote,
			R3Answer,
			R3Vote,
			Results
		}

		public string code = "";
		public Phase phase = Phase.Lobby;
		public List<Player> players = new List<Player>();
	}
}