export interface GameState {
    roomCode: string,
    playerId: string,
    token: string
}

export let game: GameState = $state({
    roomCode: "",
    playerId: "",
    token: ""
});