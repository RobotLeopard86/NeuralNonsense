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
				R3VotesDue,
				ResultsDone,
				CancelGame
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
		private Dictionary<string, bool> votes = new Dictionary<string, bool>(); //UUID, selected human answer or not
		public byte currentAnswerToVoteOn { get; private set; } = 0;
		private string[] votingOrder = { }; //Order of member UUIDs to vote in
		private Dictionary<string, byte[]> votesR3 = new Dictionary<string, byte[]>(); //UUID, answer IDs

		private ClientResultsPhaseData currentResults = new ClientResultsPhaseData();
		private ClientR3ResultsPhaseData currentR3Results = new ClientR3ResultsPhaseData();

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

		private async Task RemoveMemberAndPromote(string uuid) {
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
				Member? m = members.Values.First(m => m.type == Member.Type.Audience);
				if(m != null) {
					m.type = Member.Type.Player;
					await ServiceContainer.instance.hubCtx.Clients.Client(m.currentConnection).UpdateType("p");
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

		private ClientAnswerPhaseData GetAnswerPhaseData(string memberID) {
			ClientAnswerPhaseData capd = new ClientAnswerPhaseData {
				maxAnswerLength = Constants.MAX_ANSWER_LENGTH,
				timer = Constants.ANSWER_PERIOD
			};
			if(members[memberID].type != Member.Type.Player) {
				capd.prompt = "";
				return capd;
			}
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

		private ClientVotePhaseData GetVotePhaseData() {
			ClientVotePhaseData cvpd = new ClientVotePhaseData {
				timer = Constants.VOTING_PERIOD,
				humanAnswer = answers[votingOrder[currentAnswerToVoteOn]]
			};
			switch(roundNumber) {
				case 1:
					cvpd.prompt = content.r1[idContentMappings[votingOrder[currentAnswerToVoteOn]]].prompt;
					cvpd.aiAnswer = content.r1[idContentMappings[votingOrder[currentAnswerToVoteOn]]].aiAnswer;
					break;
				case 2:
					cvpd.prompt = content.r2[idContentMappings[votingOrder[currentAnswerToVoteOn]]].prompt;
					cvpd.aiAnswer = content.r2[idContentMappings[votingOrder[currentAnswerToVoteOn]]].aiAnswer;
					break;
			}
			return cvpd;
		}

		private ClientR3VotePhaseData GetR3VotePhaseData(string memberID) {
			ClientR3VotePhaseData cr3vpd = new ClientR3VotePhaseData {
				prompt = content.r3Prompt,
				timer = Constants.VOTING_PERIOD,
				answers = new string[answers.Count + content.r3AiAnswers.Length],
				selectCount = (byte)members.Values.Count(m => m.type == Member.Type.Player)
			};
			int answerIndex = (byte)answers.Keys.ToArray().IndexOf(memberID);
			if(answerIndex != -1) cr3vpd.yourAnswerIndex = (byte)answerIndex;
			else cr3vpd.yourAnswerIndex = byte.MaxValue;
			byte i;
			for(i = 0; i < content.r3AiAnswers.Length; i++) cr3vpd.answers[i] = content.r3AiAnswers[i];
			for(; i < content.r3AiAnswers.Length + answers.Count; i++) cr3vpd.answers[i] = answers.Values.ElementAt(i - content.r3AiAnswers.Length);
			return cr3vpd;
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
				}).Invoke(),
				ready = false
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
			members[mccc.memberID].ready = true;
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

		private async Task HandleNotifyDisconnected(NotifyDisconnectedCommand ndc) {
			//Get rid of connection ID
			members[ndc.memberID].currentConnection = "";

			//Phase-specific behavior
			if(phase == Phase.Lobby || phase == Phase.End) {
				//If we're in the lobby or on the end screen, your spot is gone
				await RemoveMemberAndPromote(ndc.memberID);
			} else {
				//If we're in a game, then we trigger suspension
				Deadline cancel = new Deadline() {
					kind = Deadline.Kind.JoinExpire,
					cancel = new CancellationTokenSource(),
					dueBy = ComputeDeadline(Constants.SUSPENSION_PERIOD / 1000)
				};
				deadlines.Add(cancel);
			}

			//Set the result
			ndc.task.SetResult();
		}

		private async Task HandleStart(StartCommand sc) {
			//Set phase
			phaseChangeProcessed = false;
			phase = Phase.ContentGen;

			//Reset member scores
			foreach(Member m in members.Values) {
				m.score = 0;
				await ServiceContainer.instance.hubCtx.Clients.Client(m.currentConnection).UpdateScore(m.score);
			}

			//Kick any non-ready players
			foreach(Member m in members.Values.Where(m => !m.ready)) {
				await ServiceContainer.instance.hubCtx.Clients.Client(m.currentConnection).Disconnected("The game session started before you (re)connected!");
				await ServiceContainer.instance.hubCtx.Groups.RemoveFromGroupAsync(m.currentConnection, code);
				await RemoveMemberAndPromote(m.uuid);
			}

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
				data = GetAnswerPhaseData(cpdc.memberID),
				finished = answers.ContainsKey(cpdc.memberID)
			};

			//Compute remaining time
			Deadline? deadline = deadlines.First(d => d.kind == Deadline.Kind.AnswersDue);
			if(deadline.HasValue) res.timeRemaining = ComputeTimeUntilDeadline(deadline.Value.dueBy) * 1000;
			else res.timeRemaining = Constants.ANSWER_PERIOD * 1000;

			//Set value
			cpdc.task.SetResult(res);
		}

		private async Task HandleCurrentVotePhase(CurrentPhaseDataCommand<ClientVotePhaseData> cpdc) {
			//Get data portion
			CurrentPhaseDataCommand<ClientVotePhaseData>.Result res = new CurrentPhaseDataCommand<ClientVotePhaseData>.Result {
				data = GetVotePhaseData(),
				finished = votes.ContainsKey(cpdc.memberID)
			};

			//Compute remaining time
			Deadline? deadline = deadlines.First(d => d.kind == Deadline.Kind.VotesDue);
			if(deadline.HasValue) res.timeRemaining = ComputeTimeUntilDeadline(deadline.Value.dueBy) * 1000;
			else res.timeRemaining = Constants.ANSWER_PERIOD * 1000;

			//Set value
			cpdc.task.SetResult(res);
		}

		private async Task HandleCurrentResultsPhase(CurrentPhaseDataCommand<ClientResultsPhaseData> cpdc) {
			//Get data portion
			CurrentPhaseDataCommand<ClientResultsPhaseData>.Result res = new CurrentPhaseDataCommand<ClientResultsPhaseData>.Result {
				data = currentResults,
				timeRemaining = 0,
				finished = false
			};

			//Set value
			cpdc.task.SetResult(res);
		}

		private async Task HandleCurrentR3VotePhase(CurrentPhaseDataCommand<ClientR3VotePhaseData> cpdc) {
			//Get data portion
			CurrentPhaseDataCommand<ClientR3VotePhaseData>.Result res = new CurrentPhaseDataCommand<ClientR3VotePhaseData>.Result {
				data = GetR3VotePhaseData(cpdc.memberID),
				finished = false
			};

			//Compute remaining time
			Deadline? deadline = deadlines.First(d => d.kind == Deadline.Kind.VotesDue);
			if(deadline.HasValue) res.timeRemaining = ComputeTimeUntilDeadline(deadline.Value.dueBy) * 1000;
			else res.timeRemaining = Constants.ANSWER_PERIOD * 1000;

			//Set value
			cpdc.task.SetResult(res);
		}

		private async Task HandleCurrentR3ResultsPhase(CurrentPhaseDataCommand<ClientR3ResultsPhaseData> cpdc) {
			//Get data portion
			CurrentPhaseDataCommand<ClientR3ResultsPhaseData>.Result res = new CurrentPhaseDataCommand<ClientR3ResultsPhaseData>.Result {
				data = currentR3Results,
				timeRemaining = 0,
				finished = false
			};

			//Set value
			cpdc.task.SetResult(res);
		}

		private async Task HandleSubmitAnswer(SubmitAnswerCommand sac) {
			//Check if answer is done already
			if(answers.ContainsKey(sac.memberID)) {
				sac.task.SetException(new ClientCausedException("You have already submitted your answer!"));
				return;
			}

			//Set answer
			answers[sac.memberID] = sac.answer;

			//Set the result
			sac.task.SetResult();
		}

		private async Task HandleVote(VoteCommand vc) {
			//Check if user has already voted
			if(votes.ContainsKey(vc.memberID)) {
				vc.task.SetException(new ClientCausedException("You have already voted!"));
				return;
			}

			//Lodge vote
			votes[vc.memberID] = vc.choseHuman;

			//Set the result
			vc.task.SetResult();
		}

		private async Task HandlePlayAgain(PlayAgainCommand pac) {
			//If we're in the end state, revert to the lobby
			if(phase == Phase.End) {
				phase = Phase.Lobby;
				foreach(Member m in members.Values.Where(m => m.type == Member.Type.Projector)) {
					await ServiceContainer.instance.hubCtx.Clients.Client(m.currentConnection).ViewTransition("lobby");
				}
			}

			//Mark the player as "ready" to signal rejoin
			members[pac.memberID].ready = true;

			//Send the user to the lobby
			await ServiceContainer.instance.hubCtx.Clients.Client(members[pac.memberID].currentConnection).ViewTransition("lobby");
		}

		private async Task EndVoting() {
			//Get content pair
			AIContent.Pair p;
			switch(roundNumber) {
				case 1:
					p = content.r1[idContentMappings[votingOrder[currentAnswerToVoteOn]]];
					break;
				case 2:
					p = content.r2[idContentMappings[votingOrder[currentAnswerToVoteOn]]];
					break;
				default: return;
			}

			//Set up results object
			currentResults = new ClientResultsPhaseData {
				prompt = p.prompt,
				humanID = votingOrder[currentAnswerToVoteOn],
				humanAnswer = answers[votingOrder[currentAnswerToVoteOn]],
				aiAnswer = p.aiAnswer,
				humanPoints = (uint)Math.Ceiling((double)votes.Values.Count(v => v) / votes.Count * roundNumber * 1000),
				unanimity = votes.Values.Count(v => v) == votes.Count
			};
			if(currentResults.unanimity) currentResults.humanPoints += 500;

			//Apply scoring
			Member m = members[currentResults.humanID];
			m.score += currentResults.humanPoints;
			await ServiceContainer.instance.hubCtx.Clients.Client(m.currentConnection).UpdateScore(m.score);

			//Advance phase
			phaseChangeProcessed = false;
			phase = Phase.Results;
		}

		private async Task EndVotingR3() {
			//Reset results object
			currentR3Results = new ClientR3ResultsPhaseData {
				prompt = content.r3Prompt,
				answers = new string[answers.Count + content.r3AiAnswers.Length],
				wasHuman = new bool[answers.Count + content.r3AiAnswers.Length],
				humanPoints = new uint[answers.Count + content.r3AiAnswers.Length],
				unanimity = new bool[answers.Count + content.r3AiAnswers.Length],
				creators = new string[answers.Count + content.r3AiAnswers.Length],
			};

			//Precompute ballot sizes and the largest ballot across all submitted ballots.
			int maxBallotSize = 0;
			foreach(byte[] ballot in votesR3.Values) {
				if(ballot.Length > maxBallotSize) maxBallotSize = ballot.Length;
			}

			//Unanimity check function
			bool IsUnanimous(byte answerID, string excludedVoterID) {
				//Setup
				int qualifyingBallots = 0;
				int qualifyingPicks = 0;

				//Go through all the votes :)
				foreach(var (voterID, ballot) in votesR3) {
					//Skip excluded voter (used in human calculations to exclude author)
					if(voterID == excludedVoterID) continue;

					//Skip empty ballots
					int k = ballot.Length;
					if(k == 0) continue;

					//Qualifying ballots are those where their sizes are greater than the largest ballot size times the unanimity qualifier threshold
					if((long)k * Constants.UNANIMITY_QUALIFIER_DENOMINATOR < (long)Constants.UNANIMITY_QUALIFIER_NUMERATOR * maxBallotSize) continue;

					//Update data
					qualifyingBallots++;
					if(Array.IndexOf(ballot, answerID) >= 0) qualifyingPicks++;
				}

				//At last, the magic
				return qualifyingBallots >= 1 && qualifyingPicks == qualifyingBallots;
			}

			//Process AI answers
			byte i;
			for(i = 0; i < content.r3AiAnswers.Length; i++) {
				currentR3Results.answers[i] = content.r3AiAnswers[i];
				currentR3Results.wasHuman[i] = false;
				currentR3Results.humanPoints[i] = 0;
				currentR3Results.unanimity[i] = IsUnanimous(i, "");
				currentR3Results.creators[i] = "";
			}

			//Process human answers
			for(; i < content.r3AiAnswers.Length + answers.Count; i++) {
				//Round constants
				byte j = (byte)(i - content.r3AiAnswers.Length);
				string authorID = answers.Keys.ElementAt(j);

				//Partial ballot weighting data
				uint weightedPicks = 0; //Sum of ballot sizes over all non-empty ballots that picked this answer
				uint totalWeight = 0;   //Sum of ballot sizes over all non-empty ballots in general

				//Calculate partial ballot weighting for scoring
				//Basically, we don't want partial ballots to unfairly influence scoring, so we weight the data accordingly
				foreach(var (voterID, ballot) in votesR3) {
					//Exclude author
					if(voterID == authorID) continue;

					//Skip empty ballots
					uint k = (uint)ballot.Length;
					if(k == 0) continue;

					//Add to weighting
					totalWeight += k;
					if(Array.IndexOf(ballot, i) >= 0) weightedPicks += k;
				}

				//Set fields
				currentR3Results.answers[i] = answers.Values.ElementAt(j);
				currentR3Results.wasHuman[i] = true;
				currentR3Results.humanPoints[i] = totalWeight > 0 ? ((1000 * weightedPicks + totalWeight - 1) / totalWeight) : 0; //This is just integer ceiling division
				currentR3Results.unanimity[i] = IsUnanimous(i, authorID);
				currentR3Results.creators[i] = answers.Keys.ElementAt(j);

				//Apply member scoring
				Member m = members[currentR3Results.creators[i]];
				m.score += currentR3Results.humanPoints[i];
				await ServiceContainer.instance.hubCtx.Clients.Client(m.currentConnection).UpdateScore(m.score);
			}

			//Advance phase
			phaseChangeProcessed = false;
			phase = Phase.R3Results;
		}

		public async Task RunAsync(CancellationToken token) {
			while(!closeRoom && !token.IsCancellationRequested) {
				//Process commands
				Command? cmd;
				while(commandQueue.Reader.TryRead(out cmd)) {
					try {
						switch(cmd) {
							case InitialJoinCommand ijc:
								HandleInitialJoin(ijc);
								break;
							case MemberConnectionCompleteCommand mccc:
								await HandleMemberConnectionComplete(mccc);
								break;
							case NotifyDisconnectedCommand ndc:
								await HandleNotifyDisconnected(ndc);
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
							case CurrentPhaseDataCommand<ClientVotePhaseData> cpdc:
								await HandleCurrentVotePhase(cpdc);
								break;
							case CurrentPhaseDataCommand<ClientResultsPhaseData> cpdc:
								await HandleCurrentResultsPhase(cpdc);
								break;
							case CurrentPhaseDataCommand<ClientR3VotePhaseData> cpdc:
								await HandleCurrentR3VotePhase(cpdc);
								break;
							case CurrentPhaseDataCommand<ClientR3ResultsPhaseData> cpdc:
								await HandleCurrentR3ResultsPhase(cpdc);
								break;
							case SubmitAnswerCommand sac:
								await HandleSubmitAnswer(sac);
								break;
							case VoteCommand vc:
								await HandleVote(vc);
								break;
							case PlayAgainCommand pac:
								await HandlePlayAgain(pac);
								break;
						}
					} catch(Exception e) {
						Console.Error.WriteLine("Exception thrown in command processing: " + e.Message + (!string.IsNullOrEmpty(e.StackTrace) ? "\n" + e.StackTrace : ""));
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
						try {
							switch(deadline.kind) {
								case Deadline.Kind.JoinExpire:
									await RemoveMemberAndPromote(deadline.additionalData as string ?? throw new ServerCausedException("Unexpected deadline data format error!"));
									break;
								case Deadline.Kind.AnswersDue:
									//Get any missing answers
									if(answers.Count != members.Count(m => !string.IsNullOrEmpty(m.Value.currentConnection) && m.Value.type == Member.Type.Player)) {
										foreach(Member m in members.Values.Where(m => !string.IsNullOrEmpty(m.currentConnection) && m.type == Member.Type.Player)) {
											answers[m.uuid] = await ServiceContainer.instance.hubCtx.Clients.Client(m.currentConnection).RequestMissingAnswer();
										}
									}

									//Advance to voting
									votingOrder = answers.Keys.ToArray();
									currentAnswerToVoteOn = 0;
									Random.Shared.Shuffle(votingOrder);
									phaseChangeProcessed = false;
									phase = roundNumber < 3 ? Phase.Vote : Phase.R3Vote;
									break;
								case Deadline.Kind.VotesDue:
									await EndVoting();
									break;
								case Deadline.Kind.ResultsDone:
									if(roundNumber >= 3) {
										//Game's done!
										phase = Phase.End;
										phaseChangeProcessed = false;
									} else if(++currentAnswerToVoteOn >= votingOrder.Length) {
										//Voting for the round done, advance to interlude
										phase = Phase.Interlude;
										phaseChangeProcessed = false;
									} else {
										//Next vote
										phase = Phase.Vote;
										phaseChangeProcessed = false;
									}
									break;
								case Deadline.Kind.R3VotesDue:
									await EndVotingR3();
									break;
								case Deadline.Kind.CancelGame:
									await ServiceContainer.instance.hubCtx.Clients.Group(code).Disconnected("This room has closed due to inactivity.");
									closeRoom = true;
									break;
								default: break;
							}
							continue;
						} catch(Exception e) {
							Console.Error.WriteLine("Exception thrown in deadline processing: " + e.Message + (!string.IsNullOrEmpty(e.StackTrace) ? "\n" + e.StackTrace : ""));
						}
					}
					untilNext = Math.Min(untilNext, deadline.dueBy - now);
				}
				deadlines.RemoveAll(toRemove.Contains);

				//Game state machine
				try {
					switch(phase) {
						case Phase.Lobby:
							break;
						case Phase.ContentGen:
							if(!phaseChangeProcessed) {
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

								phaseChangeProcessed = true;
							}

							//Readiness check
							if(members.Values.Where(m => m.type == Member.Type.Player || m.type == Member.Type.Audience).Count(m => !m.ready) == 0) {
								//Set phases and client state
								phaseChangeProcessed = false;
								phase = Phase.Answer;
								foreach(Member m in members.Values) m.ready = false;
								roundNumber = 1;
								continue;
							}

							//Set wakeup deadline for re-check
							{
								Deadline wake = new Deadline() {
									kind = Deadline.Kind.GeneralWake,
									cancel = new CancellationTokenSource(),
									dueBy = ComputeDeadline(1)
								};
								deadlines.Add(wake);
							}

							break;
						case Phase.Answer:
							if(!phaseChangeProcessed) {
								//Clear data
								answers.Clear();

								//Send member phase data
								foreach(Member m in members.Values.Where(m => !string.IsNullOrEmpty(m.currentConnection))) {
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
								break;
							}

							//Early proceed
							if(answers.Count == members.Count(m => !string.IsNullOrEmpty(m.Value.currentConnection) && m.Value.type == Member.Type.Player)) {
								//Advance to voting
								votingOrder = answers.Keys.ToArray();
								currentAnswerToVoteOn = 0;
								Random.Shared.Shuffle(votingOrder);
								phaseChangeProcessed = false;
								phase = roundNumber < 3 ? Phase.Vote : Phase.R3Vote;
								deadlines.First(d => d.kind == Deadline.Kind.AnswersDue).cancel.Cancel();
								continue;
							}

							//Set wakeup deadline for re-check
							{
								Deadline wake = new Deadline() {
									kind = Deadline.Kind.GeneralWake,
									cancel = new CancellationTokenSource(),
									dueBy = ComputeDeadline(1)
								};
								deadlines.Add(wake);
							}

							break;
						case Phase.Vote:
							if(!phaseChangeProcessed) {
								//Clear data
								votes.Clear();

								//Send phase data
								await ServiceContainer.instance.hubCtx.Clients.Group(code).PushVotePhaseData(GetVotePhaseData());

								//Set deadline
								Deadline due = new Deadline() {
									kind = Deadline.Kind.VotesDue,
									cancel = new CancellationTokenSource(),
									dueBy = ComputeDeadline(Constants.VOTING_PERIOD / 1000)
								};
								deadlines.Add(due);

								//Send state transitions
								await ServiceContainer.instance.hubCtx.Clients.Group(code).ViewTransition("vote");

								//Mark processed
								phaseChangeProcessed = true;
							}

							//Early proceed
							if(votes.Count == members.Values.Count(m => !string.IsNullOrEmpty(m.currentConnection) && (m.type == Member.Type.Player || m.type == Member.Type.Audience))) {
								//Advance to results
								await EndVoting();
								deadlines.First(d => d.kind == Deadline.Kind.VotesDue).cancel.Cancel();
								continue;
							}

							//Set wakeup deadline for re-check
							{
								Deadline wake = new Deadline() {
									kind = Deadline.Kind.GeneralWake,
									cancel = new CancellationTokenSource(),
									dueBy = ComputeDeadline(1)
								};
								deadlines.Add(wake);
							}
							break;
						case Phase.Results:
							if(!phaseChangeProcessed) {
								//Send phase data
								await ServiceContainer.instance.hubCtx.Clients.Group(code).PushResultsPhaseData(currentResults);

								//Set deadline
								Deadline due = new Deadline() {
									kind = Deadline.Kind.ResultsDone,
									cancel = new CancellationTokenSource(),
									dueBy = ComputeDeadline(Constants.RESULTS_DISPLAY_PERIOD / 1000)
								};
								deadlines.Add(due);

								//Send state transitions
								await ServiceContainer.instance.hubCtx.Clients.Group(code).ViewTransition("results");

								//Mark processed
								phaseChangeProcessed = true;
							}
							break;
						case Phase.Interlude:
							if(!phaseChangeProcessed) {
								//Advance round
								roundNumber++;

								//Send state transitions
								await ServiceContainer.instance.hubCtx.Clients.Group(code).ViewTransition("interlude");

								//Mark processed
								phaseChangeProcessed = true;
							}

							//Readiness check
							if(members.Values.Where(m => m.type == Member.Type.Player || m.type == Member.Type.Audience).Count(m => !m.ready) == 0) {
								//Set phases and client state
								phaseChangeProcessed = false;
								phase = Phase.Answer;
								foreach(Member m in members.Values) m.ready = false;
								continue;
							}
							break;
						case Phase.R3Vote:
							if(!phaseChangeProcessed) {
								//Clear data
								votesR3.Clear();

								//Send member phase data
								foreach(Member m in members.Values.Where(m => !string.IsNullOrEmpty(m.currentConnection))) {
									ClientR3VotePhaseData capd = GetR3VotePhaseData(m.uuid);
									await ServiceContainer.instance.hubCtx.Clients.Client(m.currentConnection).PushR3VotePhaseData(capd);
								}

								//Set deadline
								Deadline due = new Deadline() {
									kind = Deadline.Kind.R3VotesDue,
									cancel = new CancellationTokenSource(),
									dueBy = ComputeDeadline(Constants.R3_VOTING_PERIOD / 1000)
								};
								deadlines.Add(due);

								//Send state transitions
								await ServiceContainer.instance.hubCtx.Clients.Group(code).ViewTransition("vote3");

								//Mark processed
								phaseChangeProcessed = true;
							}
							break;
						case Phase.R3Results:
							if(!phaseChangeProcessed) {
								//Send phase data
								await ServiceContainer.instance.hubCtx.Clients.Group(code).PushR3ResultsPhaseData(currentR3Results);

								//Set deadline
								Deadline due = new Deadline() {
									kind = Deadline.Kind.ResultsDone,
									cancel = new CancellationTokenSource(),
									dueBy = ComputeDeadline(Constants.R3_RESULTS_DISPLAY_PERIOD / 1000)
								};
								deadlines.Add(due);

								//Send state transitions
								await ServiceContainer.instance.hubCtx.Clients.Group(code).ViewTransition("results3");

								//Mark processed
								phaseChangeProcessed = true;
							}
							break;
						case Phase.End:
							if(!phaseChangeProcessed) {
								await ServiceContainer.instance.hubCtx.Clients.Group(code).UpdateMemberList(members.Values.Select(member => new ClientMember(member)).ToArray());
								await ServiceContainer.instance.hubCtx.Clients.Group(code).ViewTransition("end");
								roundNumber = 0;
								phaseChangeProcessed = true;
							}
							break;
					}
				} catch(Exception e) {
					Console.Error.WriteLine("Exception thrown in the game state machine: " + e.Message + (!string.IsNullOrEmpty(e.StackTrace) ? "\n" + e.StackTrace : ""));
				}

				//Wait until we get a new command or until the next deadline
				if(untilNext == long.MaxValue) {
					await commandQueue.Reader.WaitToReadAsync(token);
				} else {
					await Task.WhenAny(commandQueue.Reader.WaitToReadAsync(token).AsTask(), Task.Delay(TimeSpan.FromSeconds((double)untilNext / TimeProvider.System.TimestampFrequency), token));
				}
			}

			if(token.IsCancellationRequested) {
				await ServiceContainer.instance.hubCtx.Clients.Group(code).Disconnected("The server is shutting down!");
			}
		}
	}
}