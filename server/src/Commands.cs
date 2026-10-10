namespace NeuralNonsense.Commands {
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
	}
}