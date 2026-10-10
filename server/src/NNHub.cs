using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using NeuralNonsense.Commands;

namespace NeuralNonsense {
	public interface INNClient {
		Task Disconnected(string reason);
		Task ConnectionEstablished();
	}

	[Authorize]
	public class NNHub : Hub<INNClient> {
		private string memberID;
		private string roomCode;

		public NNHub() {
			memberID = Context.UserIdentifier!;
			roomCode = Context.User!.FindFirst("room")!.Value;
		}

		private async Task Abort(string reason) {
			await Clients.Caller.Disconnected(reason);
			_ = Task.Run(async () => {
				await Task.Delay(TimeSpan.FromSeconds(5), Context.ConnectionAborted);
				Context.Abort();
			}, Context.ConnectionAborted);
		}

		public override async Task OnConnectedAsync() {
			//Validate room code
			if(roomCode.Length != Constants.ROOM_CODE_LENGTH) { await Abort("Invalid room code!"); await base.OnConnectedAsync(); }
			if(roomCode.Count((c) => !Constants.ROOM_CODE_ALPHABET.Contains(c)) > 0) { await Abort("Invalid room code!"); await base.OnConnectedAsync(); }
			if(!GameManager.instance.rooms.ContainsKey(roomCode)) { await Abort("No such room!"); await base.OnConnectedAsync(); }

			//Validate room membership
			Room room = GameManager.instance.rooms[roomCode];
			if(!room.members.ContainsKey(memberID)) { await Abort("You are not part of this room!"); await base.OnConnectedAsync(); }

			//Add to group
			await Groups.AddToGroupAsync(Context.ConnectionId, roomCode);

			//Send connection complete command
			MemberConnectionCompleteCommand mccc = new MemberConnectionCompleteCommand() {
				memberID = memberID
			};
			await room.writer.WriteAsync(mccc);
			await mccc.task.Task;

			await base.OnConnectedAsync();
		}

		public override async Task OnDisconnectedAsync(Exception? exception) {
			//Remove from group and send exception if needed
			await Groups.RemoveFromGroupAsync(Context.ConnectionId, roomCode);
			if(exception != null) await Abort(exception!.Message);
			await base.OnDisconnectedAsync(exception);
		}
	}
}