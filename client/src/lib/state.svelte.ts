export enum MemberType {
    Player, Audience, Spectator, Projector
}

export interface GameState {
    roomCode: string,
    memberID: string,
    role: MemberType
}

export let game: GameState = $state({
    roomCode: "",
    memberID: "",
    role: MemberType.Spectator
});