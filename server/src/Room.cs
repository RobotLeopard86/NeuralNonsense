using System.Threading.Channels;

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
			Interlude,
			R3Vote,
			R3Results,
			End
		}

		private struct Deadline {
			public enum Kind {
				GeneralWake,
				JoinExpire,
				AnswersDue,
				VotesDue,
				ResultsDone
			}

			public Kind kind;
			public CancellationTokenSource cancel;
			public long dueBy;
			public object additionalData;
		}
		private List<Deadline> deadlines = new List<Deadline>();

		public struct AIContent {
			public struct Pair {
				public string prompt;
				public string aiAnswer;
			}
			public Pair[] r1, r2;
			public string r3Prompt;
			public string[] r3AiAnswers;
		}
		private AIContent content;

		public string code = "";
		public Phase phase = Phase.Lobby;
		public uint roundNumber {
			get; private set {
				field = value;
				ServiceContainer.instance.hubCtx.Clients.Group(code).SetRound(value);
			}
		} = 0;
		public Dictionary<string, Member> members = new Dictionary<string, Member>(); //UUID, member object

		public string hostUUID = "";
		private uint numProjectorsConnected = 0;
		private bool closeRoom = false;
		private bool phaseChangeProcessed = true;

		private Channel<Command> commandQueue;
		public ChannelWriter<Command> writer;

		private Dictionary<string, byte> idContentMappings = new Dictionary<string, byte>(); //UUID, index into AI content arrays
		private Dictionary<string, string> answers = new Dictionary<string, string>(); //UUID, answer
		private Dictionary<string, bool> votes = new Dictionary<string, bool>(); //UUID, AI or not
		private Dictionary<string, byte[]> votesR3 = new Dictionary<string, byte[]>(); //UUID, answer IDs

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

		private uint ComputeTimeUntilDeadline(long deadline) {
			return (uint)((deadline - TimeProvider.System.GetTimestamp()) / TimeProvider.System.TimestampFrequency);
		}

		private ClientAnswerPhaseData GetAnswerPhaseData(string memberID) {
			ClientAnswerPhaseData capd = new ClientAnswerPhaseData {
				maxAnswerLength = Constants.MAX_ANSWER_LENGTH,
				timer = Constants.ANSWER_PERIOD
			};
			switch(roundNumber) {
				case 1:
					capd.prompt = content.r1[idContentMappings[memberID]].prompt;
					break;
				case 2:
					capd.prompt = content.r2[idContentMappings[memberID]].prompt;
					break;
				case 3:
					capd.prompt = content.r3Prompt;
					break;
			}
			return capd;
		}

		private void HandleInitialJoin(InitialJoinCommand ijc) {
			//Check member slot availability
			if(members.Count > (32 + numProjectorsConnected)) {
				ijc.task.SetException(new ServerCausedException("Too many people in this room!"));
				return;
			}
			if(members.Values.Count((m) => m.type != Member.Type.Projector && m.name == ijc.name) > 0) {
				ijc.task.SetException(new ClientCausedException("Name is in use!"));
				return;
			}

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
					await ServiceContainer.instance.hubCtx.Clients.User(mccc.memberID).ViewTransition("vote");
					break;
				case Phase.Results:
					await ServiceContainer.instance.hubCtx.Clients.User(mccc.memberID).ViewTransition("results");
					break;
				case Phase.Interlude:
					await ServiceContainer.instance.hubCtx.Clients.User(mccc.memberID).ViewTransition("interlude");
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

		private async Task HandleStart(StartCommand sc) {
			//Set phase
			phaseChangeProcessed = false;
			phase = Phase.ContentGen;

			//View transition
			await ServiceContainer.instance.hubCtx.Clients.Group(code).ViewTransition("generation");

			//Set the result
			sc.task.SetResult();
		}

		private async Task HandleMarkReady(MarkReadyCommand mrc) {
			//Set that the member is ready
			members[mrc.memberID].ready = true;

			//Set the result
			mrc.task.SetResult();
		}

		private async Task HandleRemoveMember(RemoveMemberCommand rmc) {
			//End connection
			await ServiceContainer.instance.hubCtx.Groups.RemoveFromGroupAsync(members[rmc.targetMemberID].currentConnection, code);
			await ServiceContainer.instance.hubCtx.Clients.Client(members[rmc.targetMemberID].currentConnection).Disconnected(rmc.reason);

			//Handle actual removal
			await RemoveMemberAndPromote(rmc.targetMemberID);

			//Set the result
			rmc.task.SetResult();
		}

		private async Task HandleAddProjector(AddProjectorCommand apc) {
			//Create projector member object
			Member member = new Member() {
				name = "__PROJECTOR__",
				uuid = Guid.NewGuid().ToString(),
				score = 0,
				type = Member.Type.Spectator
			};
			members.Add(member.uuid, member);

			//Set expiry deadline
			Deadline expiry = new Deadline() {
				kind = Deadline.Kind.JoinExpire,
				cancel = new CancellationTokenSource(),
				dueBy = ComputeDeadline(15),
				additionalData = member.uuid
			};
			deadlines.Add(expiry);

			//Return token
			numProjectorsConnected++;
			apc.task.SetResult(ServiceContainer.instance.tokenGenerator.GenToken(code, member));
		}

		private async Task HandleCurrentAnswerPhase(CurrentPhaseDataCommand<ClientAnswerPhaseData> cpdc) {
			//Get data portion
			CurrentPhaseDataCommand<ClientAnswerPhaseData>.Result res = new CurrentPhaseDataCommand<ClientAnswerPhaseData>.Result {
				data = GetAnswerPhaseData(cpdc.memberID)
			};

			//Compute remaining time
			Deadline? deadline = deadlines.First(d => d.kind == Deadline.Kind.AnswersDue);
			if(deadline.HasValue) res.timeRemaining = ComputeTimeUntilDeadline(deadline.Value.dueBy) * 1000;
			else res.timeRemaining = Constants.ANSWER_PERIOD * 1000;

			//Set value
			cpdc.task.SetResult(res);
		}

		public async Task RemoveMemberAndPromote(string uuid) {
			//Projector decrement
			if(members[uuid].type == Member.Type.Projector) numProjectorsConnected--;

			//Remove them
			members.Remove(uuid);

			//Set room close flag if this was the last person
			if(members.Count - numProjectorsConnected <= 0) {
				closeRoom = true;
				return;
			}

			//Promotion check
			if(phase == Phase.Lobby) {
				//Promotion permitted in the lobby
				try {
					members.Values.First(m => m.type == Member.Type.Audience).type = Member.Type.Player;
				} catch {
					//Exception means no audience members to promote, ok to swallow
				}
			}

			//Update member list
			await ServiceContainer.instance.hubCtx.Clients.Group(code).UpdateMemberList(members.Values.Select(member => new ClientMember(member)).ToArray());

			//Re-assign host role
			if(hostUUID == uuid) {
				hostUUID = members.Keys.First();
				await ServiceContainer.instance.hubCtx.Clients.Group(code).HostChanged(hostUUID);
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
						case MarkReadyCommand mrc:
							await HandleMarkReady(mrc);
							break;
						case RemoveMemberCommand rmc:
							await HandleRemoveMember(rmc);
							break;
						case AddProjectorCommand apc:
							await HandleAddProjector(apc);
							break;
						case CurrentPhaseDataCommand<ClientAnswerPhaseData> cpdc:
							await HandleCurrentAnswerPhase(cpdc);
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
								await RemoveMemberAndPromote(deadline.additionalData as string ?? throw new ServerCausedException("Unexpected deadline data format error!"));
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
						phaseChangeProcessed = true;
						if(members.Values.Where(m => m.type == Member.Type.Player || m.type == Member.Type.Audience).Count(m => !m.ready) == 0) {
							//Set phases and client state
							phaseChangeProcessed = false;
							phase = Phase.Answer;
							foreach(Member m in members.Values) m.ready = false;

							//Generate content
							byte playerCount = (byte)members.Values.Count(m => m.type == Member.Type.Player);
							content = new AIContent {
								r1 = new AIContent.Pair[playerCount],
								r2 = new AIContent.Pair[playerCount],
								r3AiAnswers = new string[playerCount],
								r3Prompt = ""
							};
							ServiceContainer.instance.aiGenerator.GenerateContent(ref content);

							//Assign IDs
							idContentMappings = new Dictionary<string, byte>();
							byte[] possible = new byte[playerCount];
							for(byte i = 0; i < playerCount; i++) possible[i] = i;
							Random.Shared.Shuffle(possible);
							foreach(Member m in members.Values.Where(m => m.type == Member.Type.Player)) {
								idContentMappings[m.uuid] = possible.Last();
								possible = possible.Take(possible.Length - 1).ToArray();
							}
							continue;
						}

						//Set wakeup deadline for re-check
						Deadline wake = new Deadline() {
							kind = Deadline.Kind.GeneralWake,
							cancel = new CancellationTokenSource(),
							dueBy = ComputeDeadline(1)
						};
						deadlines.Add(wake);

						break;
					case Phase.Answer:
						if(!phaseChangeProcessed) {
							//Advance round
							roundNumber++;

							//Send member phase data
							foreach(Member m in members.Values.Where(m => m.type == Member.Type.Player)) {
								ClientAnswerPhaseData capd = GetAnswerPhaseData(m.uuid);
								await ServiceContainer.instance.hubCtx.Clients.Client(m.currentConnection).PushAnswerPhaseData(capd);
							}

							//Set deadline
							Deadline due = new Deadline() {
								kind = Deadline.Kind.AnswersDue,
								cancel = new CancellationTokenSource(),
								dueBy = ComputeDeadline(Constants.ANSWER_PERIOD / 1000)
							};
							deadlines.Add(due);

							//Send state transitions
							await ServiceContainer.instance.hubCtx.Clients.Group(code).ViewTransition("answer");

							//Mark processed
							phaseChangeProcessed = true;
						}
						break;
					case Phase.Vote:
						break;
					case Phase.Results:
						break;
					case Phase.Interlude:
						break;
					case Phase.R3Vote:
						break;
					case Phase.R3Results:
						break;
					case Phase.End:
						roundNumber = 0;
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