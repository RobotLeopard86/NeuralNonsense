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
    //TODO
}

export const connect = async () => {
    hc = new SignalR.HubConnectionBuilder()
        .withUrl("/portal/hub", {
            accessTokenFactory: () => game.token
        })
        .configureLogging(SignalR.LogLevel.Information)
        .withAutomaticReconnect()
        .build();
    configureHandlers();
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
	await connect();
	const status: StatusUpdateResponse = await connection().invoke("RequestStatusUpdate");

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