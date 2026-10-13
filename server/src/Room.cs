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
			public string currentConnection = "";
			public bool ready = false;
		}

		public enum Phase {
			Lobby,
			ContentGen,
			Answer,
			Vote,
			Results,
			R3Vote,
			R3Results,
			End
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
		public uint roundNumber {
			get; private set {
				if(value != field + 1) return;
				field = value;
				ServiceContainer.instance.hubCtx.Clients.Group(code).NextRound();
			}
		} = 0;
		public Dictionary<string, Member> members = new Dictionary<string, Member>(); //UUID, member object

		public string hostUUID = "";
		private uint numProjectorsConnected = 0;
		private bool closeRoom = false;

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
				type = ""
			};
			switch(member.type) {
				case Member.Type.Player:
					jrr.type = "p";
					break;
				case Member.Type.Audience:
					jrr.type = "a";
					break;
				case Member.Type.Projector:
					jrr.type = "d";
					break;
				case Member.Type.Spectator:
					jrr.type = "s";
					break;
			}
			ijc.task.SetResult(jrr);
		}

		private async Task HandleMemberConnectionComplete(MemberConnectionCompleteCommand mccc) {
			//Cancel join deadline
			try {
				deadlines.First(deadline => deadline.kind == Deadline.Kind.JoinExpire && (deadline.additionalData as string) == mccc.memberID).cancel.Cancel();
			} catch { }

			//Kick old connection
			if(!string.IsNullOrEmpty(members[mccc.memberID].currentConnection)) {
				await ServiceContainer.instance.hubCtx.Groups.RemoveFromGroupAsync(members[mccc.memberID].currentConnection, code);
				await ServiceContainer.instance.hubCtx.Clients.Client(members[mccc.memberID].currentConnection).Disconnected("You have logged in from somewhere else!");
			}

			//Save connection
			members[mccc.memberID].currentConnection = mccc.connID;

			//Send client to the appropriate view
			switch(phase) {
				case Phase.Lobby:
					await ServiceContainer.instance.hubCtx.Clients.User(mccc.memberID).ViewTransition("lobby");
					break;
				case Phase.ContentGen:
					await ServiceContainer.instance.hubCtx.Clients.User(mccc.memberID).ViewTransition("generation");
					break;
				case Phase.Answer:
					await ServiceContainer.instance.hubCtx.Clients.User(mccc.memberID).ViewTransition("answer");
					break;
				case Phase.Vote:
					await ServiceContainer.instance.hubCtx.Clients.User(mccc.memberID).ViewTransition("voting");
					break;
				case Phase.Results:
					await ServiceContainer.instance.hubCtx.Clients.User(mccc.memberID).ViewTransition("results");
					break;
				case Phase.R3Vote:
					await ServiceContainer.instance.hubCtx.Clients.User(mccc.memberID).ViewTransition("vote3");
					break;
				case Phase.R3Results:
					await ServiceContainer.instance.hubCtx.Clients.User(mccc.memberID).ViewTransition("results3");
					break;
				case Phase.End:
					await ServiceContainer.instance.hubCtx.Clients.User(mccc.memberID).ViewTransition("end");
					break;
			}

			//Set the result
			mccc.task.SetResult();
		}

		private async Task HandleStart(StartCommand _) {
			//Set phase
			phase = Phase.ContentGen;

			//View transition
			await ServiceContainer.instance.hubCtx.Clients.Group(code).ViewTransition("generation");
		}

		public void RemoveMemberAndPromote(string uuid) {
			//Remove them
			members.Remove(uuid);

			//Set room close flag if this was the last person
			if(members.Count <= 0) {
				closeRoom = true;
				return;
			}

			//Promotion check
			if(phase == Phase.Lobby) {
				//Promotion permitted in the lobby
				members.Values.First(m => m.type == Member.Type.Audience).type = Member.Type.Player;
				ServiceContainer.instance.hubCtx.Clients.Group(code).UpdateMemberList(members.Values.Select(member => new ClientMember(member)).ToArray());
			}

			//Re-assign host role
			if(hostUUID == uuid) {
				hostUUID = members[members.Keys.ElementAt(0)].uuid;
				ServiceContainer.instance.hubCtx.Clients.Group(code).HostChanged(hostUUID);
			}
		}

		public async Task RunAsync(CancellationToken token) {
			while(!closeRoom && !token.IsCancellationRequested) {
				//Process commands
				Command? cmd;
				while(commandQueue.Reader.TryRead(out cmd)) {
					switch(cmd) {
						case InitialJoinCommand ijc:
							HandleInitialJoin(ijc);
							break;
						case MemberConnectionCompleteCommand mccc:
							await HandleMemberConnectionComplete(mccc);
							break;
						case StartCommand sc:
							await HandleStart(sc);
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
					if(deadline.dueBy <= now) {
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
				deadlines.RemoveAll(toRemove.Contains);

				//Game state machine
				switch(phase) {
					case Phase.Lobby:
						break;
					case Phase.ContentGen:
						if(members.Values.Where(m => m.type == Member.Type.Player || m.type == Member.Type.Audience).Count(m => !m.ready) == 0) {
							phase = Phase.Answer;
							await ServiceContainer.instance.hubCtx.Clients.Group(code).ViewTransition("answer");
						}
						break;
					case Phase.Answer:
						break;
					case Phase.Vote:
						break;
					case Phase.Results:
						break;
					case Phase.R3Vote:
						break;
					case Phase.R3Results:
						break;
					case Phase.End:
						break;
				}

				//Wait until we get a new command or until the next deadline
				if(untilNext == long.MaxValue) {
					await commandQueue.Reader.WaitToReadAsync(token);
				} else {
					await Task.WhenAny(commandQueue.Reader.WaitToReadAsync(token).AsTask(), Task.Delay(TimeSpan.FromSeconds((double)untilNext / TimeProvider.System.TimestampFrequency), token));
				}
			}
		}
	}
}