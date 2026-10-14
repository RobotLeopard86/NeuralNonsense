import Home from "./routes/Home.svelte";
import JoinLink from "./routes/JoinLink.svelte";
import Lobby from "./routes/Lobby.svelte";
import ContentGen from "./routes/ContentGen.svelte";
import Answering from "./routes/Answering.svelte";
import LaunchProjector from "./routes/LaunchProjector.svelte";

export default {
	"/": Home,
	"/join/:code": JoinLink,
	"/projector-launch/:token": LaunchProjector,
	"/play/lobby": Lobby,
	"/play/generation": ContentGen,
	"/play/answer": Answering,
}