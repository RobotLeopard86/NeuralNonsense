export enum MemberType {
	Player, Audience, Spectator, Projector
}

export interface GameState {
	roomCode: string,
	token: string,
	type: MemberType,
	score: number
}

export let game: GameState = $state({
	roomCode: "",
	token: "",
	type: MemberType.Spectator,
	score: 0
});