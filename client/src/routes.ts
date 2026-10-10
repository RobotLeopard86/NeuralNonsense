import Home from "./routes/Home.svelte";
import JoinLink from "./routes/JoinLink.svelte";
import Lobby from "./routes/Lobby.svelte";

export default {
	"/": Home,
	"/join/:code": JoinLink,
	"/play/lobby": Lobby
}