import * as SignalR from "@microsoft/signalr";
import { deriveGameState, deriveMember, game, MemberType, resetState, type ServerGameState, type Member, type ServerMember } from "./state.svelte";
import { push } from "svelte-spa-router";
import * as jose from "jose";

let hc: SignalR.HubConnection | null = null;
export const connection = () => {
	if(hc) return hc;
	else throw "The connection is not available yet, you idiot!"
};

const tabClosePreventHandler = (e: BeforeUnloadEvent) => e.preventDefault();

const configureHandlers = () => {
	//Disconnection
	connection().onclose((e?: Error) => {
		//Present error if applicable
		if(e) {
			alert(`You have been unexpectedly disconnected from the room: ${e.message}`);
			console.error(`Full Error Stacktrace: ${e.stack ? e.stack : "Not Available"}`);
		}

		//Go to homepage
		window.removeEventListener("beforeunload", tabClosePreventHandler);
		push("/");
	});
	connection().on("Disconnected", (reason: string) => {
		//Obediently disconnect like the good website we are
		connection().stop().finally(() => {
			//Reset game state
			resetState();

			//Return to homepage and present error
			push("/");
			window.removeEventListener("beforeunload", tabClosePreventHandler);
			alert(`You have been disconnected from the room: ${reason}`);
		});
	});

	//View transition
	connection().on("ViewTransition", (view: string) => {
		push(`/play/${view}`);
	});

	//Player list update
	connection().on("UpdateMemberList", (members: ServerMember[]) => {
		game.state.members = members.map(deriveMember);
	});

	//Host change
	connection().on("HostChanged", (newHost: string) => {
		game.state.hostID = newHost;
	});

	//Round update
	connection().on("NextRound", () => {
		game.state.round++;
	});
}

export const connect = async () => {
	//Build hub connection
	hc = new SignalR.HubConnectionBuilder()
		.withUrl("/portal/hub", {
			accessTokenFactory: () => game.token
		})
		.configureLogging(SignalR.LogLevel.Information)
		.withAutomaticReconnect()
		.build();

	//Register client methods
	configureHandlers();

	//Connect to hub
	await hc.start();
}

interface JWTPayload {
	sub: string,
	room: string,
	name: string
}

export const tryRestore = async () => {
	//Don't restore if we already have a token
	if(game.token !== "") return;

	//Fetch token
	const localToken = localStorage.getItem("nn:tkn");
	if(localToken == null) return;

	//Decode claims
	game.token = localToken!;
	const claims = jose.decodeJwt<JWTPayload>(localToken);
	if(claims.exp && claims.exp < Math.round(+new Date() / 1000)) {
		//Token expired, definitely can't restore
		resetState();
		return;
	}

	//Set data
	game.roomCode = claims.room;
	game.self.name = claims.name;
	game.self.uuid = claims.sub;

	//Reconnect to server and request status update
	try {
		await connect();
	} catch(e) {
		resetState();
		alert(`Failed to reconnect to the room: ${e}`);
		return;
	}
	const state: ServerGameState = await connection().invoke("RestoreCurrentState");
	const self: ServerMember = await connection().invoke("RestoreCurrentSelf");

	//Enable tab close warning dialog
	window.addEventListener("beforeunload", tabClosePreventHandler);

	//Restore from status
	game.state = deriveGameState(state);
	game.self.score = self.score;
	switch(self.type) {
		case "p":
			game.self.type = MemberType.Player;
			break;
		case "a":
			game.self.type = MemberType.Audience;
			break;
		case "d":
			game.self.type = MemberType.Projector;
			break;
		case "s":
			game.self.type = MemberType.Spectator;
			break;
	}
}

export const ejectIfNoConnection = async () => {
	//Try to restore connection before checking
	await tryRestore();

	if(!hc) {
		console.log("No session exists, redirecting to home...");
		resetState();
		window.removeEventListener("beforeunload", tabClosePreventHandler);
		push("/");
	}
}

export const joinGame = async (roomCode: string, memberName: string) => {
	//Reset game state
	resetState();
	game.roomCode = roomCode;

	//Call join endpoint
	const result = await fetch("/portal/join", {
		method: "POST",
		body: JSON.stringify({
			code: roomCode,
			name: memberName
		}),
		headers: {
			"Content-Type": "application/json"
		}
	});

	//Alert the user on failure
	if(!result.ok) {
		resetState();
		alert(`Failed to join the room: ${await result.text()}`);
		return;
	}

	//Store token and initial state
	const body = await result.json();
	game.token = body.token;
	localStorage.setItem("nn:tkn", game.token);
	const claims = jose.decodeJwt<JWTPayload>(game.token);
	game.self.name = claims.name;
	game.self.uuid = claims.sub;
	game.self.score = 0;
	switch(body.type) {
		case "p":
			game.self.type = MemberType.Player;
			break;
		case "a":
			game.self.type = MemberType.Audience;
			break;
		case "d":
			game.self.type = MemberType.Projector;
			break;
		case "s":
			game.self.type = MemberType.Spectator;
			break;
	}

	//Connect to the hub
	try {
		await connect();
	} catch(e) {
		resetState();
		alert(`Failed to connect to the room: ${e}`);
		return;
	}

	//Request status
	const state: ServerGameState = await connection().invoke("RestoreCurrentState");
	const self: ServerMember = await connection().invoke("RestoreCurrentSelf");

	//Enable tab close warning dialog
	window.addEventListener("beforeunload", tabClosePreventHandler);

	//Restore from status
	game.state = deriveGameState(state);
	game.self.score = self.score;
	switch(self.type) {
		case "p":
			game.self.type = MemberType.Player;
			break;
		case "a":
			game.self.type = MemberType.Audience;
			break;
		case "d":
			game.self.type = MemberType.Projector;
			break;
		case "s":
			game.self.type = MemberType.Spectator;
			break;
	}
};