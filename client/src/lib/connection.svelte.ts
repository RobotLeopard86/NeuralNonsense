import * as SignalR from "@microsoft/signalr";
import { game, MemberType } from "./state.svelte";
import { push } from "svelte-spa-router";
import * as jose from "jose";

let hc: SignalR.HubConnection | null = null;
export const connection = () => {
    if (hc) return hc;
    else throw "The connection is not available yet, you idiot!"
};

const configureHandlers = () => {
	//Disconnection
    connection().on("Disconnected", async(reason: string) => {
		//Obediently disconnect like the good website we are
		await connection().stop();

		//Return to homepage and present error
		push("/");
		alert(`You have been disconnected from the room: ${reason}`);
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

interface StatusUpdateResponse {
	type: string,
	score: number
}

export const tryRestoreConnection = async() => {
	//Don't restore if we already have a token
	if(game.token !== "") return;

	//Fetch token
	const localToken = localStorage.getItem("nn:tkn");
	if(localToken == null) return;

	//Decode claims
	game.token = localToken!;
	const claims = jose.decodeJwt<JWTPayload>(localToken);
	game.roomCode = claims.room;

	//Reconnect and request status update
	try {
		await connect();
	} catch(e) {
		game.token = "";
		localStorage.removeItem("nn:tkn");
		alert(`Failed to reconnect to the room: ${e}`);
		return;
	}
	const status: StatusUpdateResponse = await connection().invoke("RequestStatusUpdate");

	//Enable tab close warning dialog
	window.addEventListener("beforeunload", (e: BeforeUnloadEvent) => e.preventDefault());

	//Restore from status
	game.score = status.score;
	switch(status.type) {
		case "p":
			game.type = MemberType.Player;
			break;
		case "a":
			game.type = MemberType.Audience;
			break;
		case "d":
			game.type = MemberType.Projector;
			break;
		case "s":
			game.type = MemberType.Spectator;
			break;	
	} 
}

export const ejectIfNoConnection = async() => {
	//Try to restore connection before checking
	await tryRestoreConnection();

	if(!hc) {
		console.log("No session exists, redirecting to home...");
		push("/");
	}
}

export const rejoinIfConnected = async() => {
	//Try to restore connection before checking
	await tryRestoreConnection();

	//Have the server update our state
	hc && await connection().invoke("SendRejoinViewUpdate");
}

export const joinGame = async(roomCode: string, memberName: string) => {
	//Reset game state
	game.roomCode = roomCode;
	game.score = 0;

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
	if (!result.ok) {
		alert(`Failed to join the room: ${await result.text()}`);
		return;
	}

	//Store token and initial state
	const body = await result.json();
	game.token = body.token;
	localStorage.setItem("nn:tkn", game.token);
	switch(body.type) {
		case "p":
			game.type = MemberType.Player;
			break;
		case "a":
			game.type = MemberType.Audience;
			break;
		case "d":
			game.type = MemberType.Projector;
			break;
		case "s":
			game.type = MemberType.Spectator;
			break;	
	}

	//Connect to the hub
	try {
		await connect();
	} catch(e) {
		game.token = "";
		localStorage.removeItem("nn:tkn");
		alert(`Failed to connect to the room: ${e}`);
		return;
	}

	//Enable tab close warning dialog
	window.addEventListener("beforeunload", (e: BeforeUnloadEvent) => e.preventDefault());
};