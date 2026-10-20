namespace NeuralNonsense {
	public struct CreateRoomResponse {
		public string code;
	}
	public struct JoinRoomRequest {
		public string code;
		public string name;
	}
	public struct JoinRoomResponse {
		public string token;
		public string type;
	}

	public class ClientCausedException : Exception {
		public ClientCausedException(string msg) : base(msg) { }
	};
	public class ServerCausedException : Exception {
		public ServerCausedException(string msg) : base(msg) { }
	};

	public struct ClientMember {
		public string name;
		public string uuid;
		public uint score;
		public string type;

		public ClientMember(Room.Member m) {
			name = m.name;
			uuid = m.uuid;
			score = m.score;
			switch(m.type) {
				case Room.Member.Type.Player:
					type = "p";
					break;
				case Room.Member.Type.Audience:
					type = "a";
					break;
				case Room.Member.Type.Projector:
					type = "d";
					break;
				case Room.Member.Type.Spectator:
					type = "s";
					break;
				default:
					type = "";
					break;
			}
		}
	}

	public struct ClientGameState {
		public string hostID;
		public ClientMember[] members;
		public uint round;
	}

	public struct ClientAnswerPhaseData {
		public string prompt;
		public uint maxAnswerLength;
		public uint timer;
	}

	public struct ClientVotePhaseData {
		public string prompt;
		public uint timer;
		public string humanID;
		public string humanAnswer;
		public string aiAnswer;
	}

	public struct ClientResultsPhaseData {
		public string prompt;
		public string humanID;
		public string humanAnswer;
		public string aiAnswer;
		public uint humanPoints;
		public bool unanimity;
	}

	public struct ClientR3VotePhaseData {
		public string prompt;
		public uint timer;
		public string[] answers;
		public byte yourAnswerIndex;
		public uint selectCount;
	}

	public struct ClientR3ResultsPhaseData {
		public string prompt;
		public string[] humanAnswers;
		public uint[] humanPoints;
		public bool[] unanimity;
		public string[] aiAnswers;
	}
}