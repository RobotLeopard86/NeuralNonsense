export interface GameState {
    roomCode: string,
    playerId: string,
}

export let game: GameState = $state({
    roomCode: "",
    playerId: "",
});