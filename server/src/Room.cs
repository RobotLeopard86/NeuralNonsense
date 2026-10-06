
using System.Threading.Channels;
using Microsoft.AspNetCore.SignalR;
using NeuralNonsense.Commands;

namespace NeuralNonsense {
	public sealed class Room {
		public sealed class Player {
			public enum Type {
				Player, Audience, Spectator, Projector
			}

			public string name = "";
			public string uuid = "";
			public uint score = 0;
			public Type type;
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

		private Channel<Command> commandQueue;
		public ChannelWriter<Command> writer;

		public Room() {
			commandQueue = Channel.CreateUnbounded<Command>(new UnboundedChannelOptions() {
				SingleReader = true,
				SingleWriter = false
			});
			writer = commandQueue.Writer;
		}

		private void HandleInitialJoin(InitialJoinCommand ijc) {

		}

		public async Task RunAsync() {
			while(true) {
				//Process commands
				Command? cmd;
				while(commandQueue.Reader.TryRead(out cmd)) {
					switch(cmd) {
						case InitialJoinCommand ijc:
							HandleInitialJoin(ijc);
							break;
					}
				}
			}
		}
	}
}