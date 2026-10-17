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

	public class RemoveMemberCommand : CommandWithoutValue {
		public required string targetMemberID;
		public required string reason;
	}

	public class StartCommand : CommandWithoutValue { }

	public class MarkReadyCommand : CommandWithoutValue {
		public required string memberID;
	}

	public class AddProjectorCommand : CommandWithValue<string> { }
}