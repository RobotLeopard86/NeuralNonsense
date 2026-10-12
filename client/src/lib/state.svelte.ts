export enum MemberType {
	Player, Audience, Spectator, Projector
}

export interface Member {
	name: string,
	uuid: string,
	score: number,
	type: MemberType
}

export interface ServerMember {
	name: string,
	uuid: string,
	score: number,
	type: string
}

export const deriveMember = (member: ServerMember) => {
	let mem: Member = {
		name: member.name,
		uuid: member.uuid,
		score: member.score,
		type: MemberType.Spectator
	};
	switch(member.type) {
		case "p":
			mem.type = MemberType.Player;
			break;
		case "a":
			mem.type = MemberType.Audience;
			break;
		case "d":
			mem.type = MemberType.Projector;
			break;
		case "s":
			mem.type = MemberType.Spectator;
			break;
	}
	return mem;
}

export interface GameState {
	members: Member[],
	hostID: string,
	round: number
}

export interface ServerGameState {
	members: ServerMember[],
	hostID: string,
	round: number
}

export const deriveGameState = (state: ServerGameState) => {
	let gs: GameState = {
		members: state.members.map(deriveMember),
		hostID: state.hostID,
		round: state.round
	};
	return gs;
}

export interface AllGameState {
	roomCode: string,
	token: string,
	self: Member
	state: GameState;
}

export let game: AllGameState = $state({
	roomCode: "",
	token: "",
	self: {
		name: "",
		uuid: "",
		type: MemberType.Spectator,
		score: 0
	},
	state: {
		members: [],
		hostID: "",
		round: 0
	}
});

export const resetState = () => {
	game.roomCode = "";
	game.token = "";
	localStorage.removeItem("nn:tkn");
	game.self.name = "";
	game.self.uuid = "";
	game.self.score = 0;
	game.self.type = MemberType.Spectator;
	game.state.members = []
	game.state.hostID = "";
}