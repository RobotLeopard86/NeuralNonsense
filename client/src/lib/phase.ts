export interface AnswerPhaseData {
    prompt: string,
    maxAnswerLength: number,
    timer: number
}

export interface VotePhaseData {
    prompt: string,
    timer: number,
    answerA: string,
    answerB: string
}

export interface ResultsPhaseData {
    prompt: string,
    humanAnswer: string,
    aiAnswer: string,
    humanPoints: number
}

export interface R3VotePhaseData {
    prompt: string,
    timer: number,
    answers: string[],
    answerIndexMapping: number[], //[0] = 2 -> answers[0] = server-side answers[2]
    selectCount: number
}

export interface R3ResultsPhaseData {
    prompt: string,
    humanAnswers: string[], //Indices shared <-| (use game.r3AnswerIndexMapping for consistent ordering)
    humanPoints: number[], //Indices shared <--|
    aiAnswers: string[]
}