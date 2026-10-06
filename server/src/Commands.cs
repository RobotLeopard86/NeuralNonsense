namespace NeuralNonsense.Commands {
    public abstract class Command { }

    public abstract class CommandWithReply<T> : Command {
        public TaskCompletionSource<T> task;

        protected CommandWithReply() {
            task = new TaskCompletionSource<T>();
        }
    }

    public class InitialJoinCommand : CommandWithReply<JoinRoomResponse> {
        public required string playerName;
    }
}