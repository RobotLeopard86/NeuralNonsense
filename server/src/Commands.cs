namespace NeuralNonsense {
	public abstract class Command { }

	public abstract class CommandWithoutValue : Command {
		public TaskCompletionSource task;

		protected CommandWithoutValue() {
			task = new TaskCompletionSource();
		}
	}

	public abstract class CommandWithValue<T> : Command {
		public TaskCompletionSource<T> task;

		protected CommandWithValue() {
			task = new TaskCompletionSource<T>();
		}
	}

	public class InitialJoinCommand : CommandWithValue<JoinRoomResponse> {
		public required string name;
	}

	public class MemberConnectionCompleteCommand : CommandWithoutValue {
		public required string memberID;
		public required string connID;
	}

	public class NotifyDisconnectedCommand : CommandWithoutValue {
		public required string memberID;
	}

	public class RemoveMemberCommand : CommandWithoutValue {
		public required string targetMemberID;
		public required string reason;
	}

	public class StartCommand : CommandWithoutValue { }

	public class MarkReadyCommand : CommandWithoutValue {
		public required string memberID;
	}

	public class AddProjectorCommand : CommandWithValue<string> { }

	public class SubmitAnswerCommand : CommandWithoutValue {
		public required string memberID;
		public required string answer;
	}

	public class VoteCommand : CommandWithoutValue {
		public required string memberID;
		public required bool choseHuman;
	}

	public class R3AddVoteCommand : CommandWithoutValue {
		public required string memberID;
		public required byte answerID;
	}

	public class R3RemoveVoteCommand : CommandWithoutValue {
		public required string memberID;
		public required byte answerID;
	}

	public class R3GetVotesCommand : CommandWithValue<bool[]> {
		public required string memberID;
	}

	public class CurrentPhaseDataCommand<T> : CommandWithValue<CurrentPhaseDataCommand<T>.Result> {
		public struct Result {
			public T data;
			public uint timeRemaining;
			public bool finished;
		}

		public required string memberID;
	}

	public class PlayAgainCommand : CommandWithoutValue {
		public required string memberID;
	}
}