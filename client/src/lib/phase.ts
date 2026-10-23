export interface AnswerPhaseData {
    prompt: string,
    maxAnswerLength: number,
    timer: number
}

export interface VotePhaseData {
    prompt: string,
    timer: number,
    humanID: string,
    humanAnswer: string,
    aiAnswer: string
}

export interface ResultsPhaseData {
    prompt: string,
    humanID: string,
    humanAnswer: string,
    aiAnswer: string,
    humanPoints: number,
	numVotes: number,
    unanimity: boolean
}

export interface R3VotePhaseData {
    prompt: string,
    timer: number,
    answers: string[],
    yourAnswerIndex: number,
    selectCount: number
}

export interface R3ResultsPhaseData {
    prompt: string,
    answers: string[], //Indices shared <-----| (use game.r3AnswerIndexMapping for consistent ordering)
	wasHuman: boolean[], //Indices shared <---|
    humanPoints: number[], //Indices shared <-|
	numVotes: number[], //Indices shared <----|
    unanimity: boolean[], //Indices shared <--|
	creators: string[], //Indices shared <----|
}