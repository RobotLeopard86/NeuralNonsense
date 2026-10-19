using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace NeuralNonsense {
	public interface INNClient {
		Task Disconnected(string reason);
		Task ViewTransition(string view);
		Task UpdateMemberList(ClientMember[] members);
		Task HostChanged(string newHost);
		Task SetRound(uint round);
		Task PushAnswerPhaseData(ClientAnswerPhaseData capd);
		Task PushVotePhaseData(ClientVotePhaseData cvpd);
		Task<string> RequestMissingAnswer();
	}

	[Authorize]
	public class NNHub : Hub<INNClient> {
		private string memberID = "";
		private string roomCode = "";

		private async Task Abort(string reason) {
			await Clients.Caller.Disconnected(reason);
			_ = Task.Run(async () => {
				await Task.Delay(TimeSpan.FromSeconds(5), Context.ConnectionAborted);
				Context.Abort();
			}, Context.ConnectionAborted);
		}

		private async Task LoadUserAndVerify() {
			//Get data from context
			memberID = Context.UserIdentifier!;
			roomCode = Context.User!.FindFirst("room")!.Value;

			//Validate room code
			if(roomCode.Length != Constants.ROOM_CODE_LENGTH) { await Abort("Invalid room code!"); await base.OnConnectedAsync(); }
			if(roomCode.Count((c) => !Constants.ROOM_CODE_ALPHABET.Contains(c)) > 0) { await Abort("Invalid room code!"); await base.OnConnectedAsync(); }
			if(!ServiceContainer.instance.gameManager.rooms.ContainsKey(roomCode)) { await Abort("No such room!"); await base.OnConnectedAsync(); }

			//Validate room membership
			if(!ServiceContainer.instance.gameManager.rooms[roomCode].members.ContainsKey(memberID)) { await Abort("You are not part of this room!"); await base.OnConnectedAsync(); }
		}

		public override async Task OnConnectedAsync() {
			//Verify
			await LoadUserAndVerify();

			//Add to group
			Room room = ServiceContainer.instance.gameManager.rooms[roomCode];
			await Groups.AddToGroupAsync(Context.ConnectionId, roomCode);
			await Clients.Group(roomCode).UpdateMemberList(room.members.Values.Select(member => new ClientMember(member)).ToArray());

			//Send connection complete command
			MemberConnectionCompleteCommand mccc = new MemberConnectionCompleteCommand() {
				memberID = memberID,
				connID = Context.ConnectionId
			};
			await room.writer.WriteAsync(mccc);
			await mccc.task.Task;

			//Base class logic
			await base.OnConnectedAsync();
		}

		public override async Task OnDisconnectedAsync(Exception? exception) {
			try {
				//Verify
				await LoadUserAndVerify();

				//Invalidate connection to mark disconnected
				NotifyDisconnectedCommand ndc = new NotifyDisconnectedCommand() {
					memberID = memberID
				};
				await ServiceContainer.instance.gameManager.rooms[roomCode].writer.WriteAsync(ndc);
				await ndc.task.Task;

				//Remove from group
				await Groups.RemoveFromGroupAsync(Context.ConnectionId, roomCode);
			} catch {
				//Exception means removal was already handled by a room routine, so we're good
				//Hence we swallow it
			}

			//Report exception
			if(exception != null) await Abort(exception!.Message);
			await base.OnDisconnectedAsync(exception);
		}

		public async Task<ClientMember> RestoreCurrentSelf() {
			//Verify
			await LoadUserAndVerify();

			//Return the user
			return new ClientMember(ServiceContainer.instance.gameManager.rooms[roomCode].members.Values.First(m => m.uuid == memberID));
		}

		public async Task<ClientGameState> RestoreCurrentState() {
			//Verify
			await LoadUserAndVerify();

			//Return the state
			Room room = ServiceContainer.instance.gameManager.rooms[roomCode];
			return new ClientGameState {
				hostID = room.hostUUID,
				members = room.members.Values.Select(member => new ClientMember(member)).ToArray(),
				round = room.roundNumber
			};
		}

		public async Task ExplicitLeave() {
			//Verify
			await LoadUserAndVerify();

			//Send leave command
			RemoveMemberCommand rmc = new RemoveMemberCommand() {
				targetMemberID = memberID,
				reason = ""
			};
			await ServiceContainer.instance.gameManager.rooms[roomCode].writer.WriteAsync(rmc);
			await rmc.task.Task;

			//Abort connection
			await Abort("Thanks for playing!");
		}

		public async Task KickUser(string uuid) {
			//Verify
			await LoadUserAndVerify();

			//Check host permissions
			Room room = ServiceContainer.instance.gameManager.rooms[roomCode];
			if(memberID != room.hostUUID) throw new ClientCausedException("You cannot perform this action as you are not the host!");

			//Send leave command
			RemoveMemberCommand rmc = new RemoveMemberCommand() {
				targetMemberID = uuid,
				reason = "You have been kicked by the host."
			};
			await room.writer.WriteAsync(rmc);
			await rmc.task.Task;
		}

		public async Task StartGame() {
			//Verify
			await LoadUserAndVerify();

			//Check host permissions
			Room room = ServiceContainer.instance.gameManager.rooms[roomCode];
			if(memberID != room.hostUUID) throw new ClientCausedException("You cannot perform this action as you are not the host!");

			//Validate player count to be sure
			//if(room.members.Count < 3) throw new ClientCausedException("3+ players required to start the game!");

			//Send start command
			StartCommand sc = new StartCommand();
			await room.writer.WriteAsync(sc);
			await sc.task.Task;
		}

		public async Task<string> AddProjector() {
			//Verify
			await LoadUserAndVerify();

			//Check host permissions
			Room room = ServiceContainer.instance.gameManager.rooms[roomCode];
			if(memberID != room.hostUUID) throw new ClientCausedException("You cannot perform this action as you are not the host!");

			//Send projector addition command
			AddProjectorCommand apc = new AddProjectorCommand();
			await room.writer.WriteAsync(apc);
			await apc.task.Task;

			//Return projector token
			return apc.task.Task.Result;
		}

		public async Task SignalReady() {
			//Verify
			await LoadUserAndVerify();

			//Send ready command
			MarkReadyCommand mrc = new MarkReadyCommand() {
				memberID = memberID
			};
			await ServiceContainer.instance.gameManager.rooms[roomCode].writer.WriteAsync(mrc);
			await mrc.task.Task;
		}

		public async Task SubmitAnswer(string answer) {
			//Verify
			await LoadUserAndVerify();

			//Check permissions
			Room room = ServiceContainer.instance.gameManager.rooms[roomCode];
			if(room.members[memberID].type != Room.Member.Type.Player) throw new ClientCausedException("You may not peform this action!");

			//Send submission command
			SubmitAnswerCommand sac = new SubmitAnswerCommand() {
				memberID = memberID,
				answer = ServiceContainer.instance.badWordChecker.Censor(answer)
			};
			await room.writer.WriteAsync(sac);
			await sac.task.Task;
		}

		public async Task Vote(bool choseHuman) {
			//Verify
			await LoadUserAndVerify();

			//Check permissions
			Room room = ServiceContainer.instance.gameManager.rooms[roomCode];
			if(room.members[memberID].type != Room.Member.Type.Player && room.members[memberID].type != Room.Member.Type.Audience) throw new ClientCausedException("You may not peform this action!");

			//Send voting command
			VoteCommand vc = new VoteCommand() {
				memberID = memberID,
				choseHuman = choseHuman
			};
			await room.writer.WriteAsync(vc);
			await vc.task.Task;
		}

		public async Task AddVoteR3(byte answerID) {
			//Verify
			await LoadUserAndVerify();

			//Check permissions
			Room room = ServiceContainer.instance.gameManager.rooms[roomCode];
			if(room.members[memberID].type != Room.Member.Type.Player && room.members[memberID].type != Room.Member.Type.Audience) throw new ClientCausedException("You may not peform this action!");

			//Send vote addition command
			R3AddVoteCommand avc = new R3AddVoteCommand() {
				memberID = memberID,
				answerID = answerID
			};
			await ServiceContainer.instance.gameManager.rooms[roomCode].writer.WriteAsync(avc);
			await avc.task.Task;
		}

		public async Task RmVoteR3(byte answerID) {
			//Verify
			await LoadUserAndVerify();

			//Check permissions
			Room room = ServiceContainer.instance.gameManager.rooms[roomCode];
			if(room.members[memberID].type != Room.Member.Type.Player && room.members[memberID].type != Room.Member.Type.Audience) throw new ClientCausedException("You may not peform this action!");

			//Send vote addition command
			R3RemoveVoteCommand rvc = new R3RemoveVoteCommand() {
				memberID = memberID,
				answerID = answerID
			};
			await ServiceContainer.instance.gameManager.rooms[roomCode].writer.WriteAsync(rvc);
			await rvc.task.Task;
		}

		public async Task<CurrentPhaseDataCommand<ClientAnswerPhaseData>.Result> RefreshAnswerPhaseData() {
			//Verify
			await LoadUserAndVerify();

			//Send data request command
			CurrentPhaseDataCommand<ClientAnswerPhaseData> cpdc = new CurrentPhaseDataCommand<ClientAnswerPhaseData>() {
				memberID = memberID
			};
			await ServiceContainer.instance.gameManager.rooms[roomCode].writer.WriteAsync(cpdc);
			await cpdc.task.Task;

			//Return
			return cpdc.task.Task.Result;
		}
	}
}