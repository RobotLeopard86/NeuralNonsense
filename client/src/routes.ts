import Home from "./routes/Home.svelte";
import JoinLink from "./routes/JoinLink.svelte";
import LaunchProjector from "./routes/LaunchProjector.svelte";
import Lobby from "./routes/Lobby.svelte";
import ContentGen from "./routes/ContentGen.svelte";
import Answering from "./routes/Answering.svelte";
import Voting from "./routes/Voting.svelte";
import Results from "./routes/Results.svelte";
import Interlude from "./routes/Interlude.svelte";
import R3Voting from "./routes/R3Voting.svelte";
import R3Results from "./routes/R3Results.svelte";
import End from "./routes/End.svelte";

export default {
	"/": Home,
	"/join/:code": JoinLink,
	"/projector-launch/:token": LaunchProjector,
	"/play/lobby": Lobby,
	"/play/generation": ContentGen,
	"/play/answer": Answering,
	"/play/vote": Voting,
	"/play/results": Results,
	"/play/interlude": Interlude,
	"/play/vote3": R3Voting,
	"/play/results3": R3Results,
	"/play/end": End,
}