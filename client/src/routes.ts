import Home from "./routes/Home.svelte";
import JoinLink from "./routes/JoinLink.svelte";
import Lobby from "./routes/Lobby.svelte";
import ContentGen from "./routes/ContentGen.svelte";

export default {
	"/": Home,
	"/join/:code": JoinLink,
	"/play/lobby": Lobby,
	"/play/generation": ContentGen
}