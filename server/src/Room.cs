
using System.Data;
using System.Threading.Channels;
using NeuralNonsense.Commands;

namespace NeuralNonsense {
	public sealed class Room {
		public sealed class Member {
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

		private struct Deadline {
			public enum Kind {
				JoinExpire
			}

			public Kind kind;
			public CancellationTokenSource cancel;
			public long dueBy;
			public object additionalData;
		}
		private List<Deadline> deadlines = new List<Deadline>();

		public string code = "";
		public Phase phase = Phase.Lobby;
		public Dictionary<string, Member> members = new Dictionary<string, Member>(); //UUID, member object

		public string hostUUID = "";
		private uint numProjectorsConnected = 0;

		private Channel<Command> commandQueue;
		public ChannelWriter<Command> writer;

		public Room() {
			commandQueue = Channel.CreateUnbounded<Command>(new UnboundedChannelOptions() {
				SingleReader = true,
				SingleWriter = false
			});
			writer = commandQueue.Writer;
		}

		private long ComputeDeadline(uint seconds) {
			return TimeProvider.System.GetTimestamp() + (TimeProvider.System.TimestampFrequency * seconds);
		}

		private void RemoveMemberAndPromote(string uuid) {

		}

		private void HandleInitialJoin(InitialJoinCommand ijc) {
			//Check member slot availability
			if(members.Count > (32 + numProjectorsConnected)) throw new ServerCausedException("Too many people in this room!");
			if(members.Count((m) => m.Value.name == ijc.name) > 0) throw new ClientCausedException("Name is in use!");

			//Create member object
			Member member = new Member() {
				name = ijc.name,
				uuid = Guid.NewGuid().ToString(),
				score = 0,
				type = new Func<Member.Type>(() => {
					if(phase != Phase.Lobby) return Member.Type.Spectator;
					return members.Count <= 8 ? Member.Type.Player : Member.Type.Audience;
				}).Invoke()
			};
			members.Add(member.uuid, member);

			//Default host
			if(members.Count == 1) hostUUID = member.uuid;

			//Set expiry deadline
			Deadline expiry = new Deadline() {
				kind = Deadline.Kind.JoinExpire,
				cancel = new CancellationTokenSource(),
				dueBy = ComputeDeadline(15),
				additionalData = member.uuid
			};
			deadlines.Add(expiry);

			//Reply
			JoinRoomResponse jrr = new JoinRoomResponse() {
				token = ServiceContainer.instance.tokenGenerator.GenToken(code, member),
				type = member.type
			};
			ijc.task.SetResult(jrr);
		}

		private void HandleMemberConnectionComplete(MemberConnectionCompleteCommand mccc) {
			deadlines.First(deadline => deadline.kind == Deadline.Kind.JoinExpire && (deadline.additionalData as string) == mccc.memberID).cancel.Cancel();
		}

		public async Task RunAsync(CancellationToken token) {
			while(!token.IsCancellationRequested) {
				try {
					//Process commands
					Command? cmd;
					while(commandQueue.Reader.TryRead(out cmd)) {
						switch(cmd) {
							case InitialJoinCommand ijc:
								HandleInitialJoin(ijc);
								break;
							case MemberConnectionCompleteCommand mccc:
								HandleMemberConnectionComplete(mccc);
								break;
						}
					}

					//Check deadlines
					List<Deadline> toRemove = new List<Deadline>();
					long untilNext = long.MaxValue;
					long now = TimeProvider.System.GetTimestamp();
					foreach(Deadline deadline in deadlines) {
						if(deadline.cancel.IsCancellationRequested) {
							toRemove.Add(deadline);
							continue;
						}
						if(deadline.dueBy >= now) {
							toRemove.Add(deadline);
							switch(deadline.kind) {
								case Deadline.Kind.JoinExpire:
									RemoveMemberAndPromote(deadline.additionalData as string ?? throw new ServerCausedException("Unexpected deadline data format error!"));
									break;
							}
							continue;
						}
						untilNext = Math.Min(untilNext, deadline.dueBy - now);
					}
					deadlines.RemoveAll((d) => toRemove.Contains(d));

					//Game state machine
					switch(phase) {
						case Phase.Lobby:
							break;
						case Phase.ContentGen:
							break;
						case Phase.R1Answer:
							break;
						case Phase.R1Vote:
							break;
						case Phase.R2Answer:
							break;
						case Phase.R2Vote:
							break;
						case Phase.R3Answer:
							break;
						case Phase.R3Vote:
							break;
						case Phase.Results:
							break;
					}

					//Wait until we get a new command or until the next deadline
					await commandQueue.Reader.WaitToReadAsync(token);
				} catch(Exception e) {
					Console.Error.WriteLine("ROOM " + code + " ERROR: " + e.Message);
				}
			}
		}
	}
}