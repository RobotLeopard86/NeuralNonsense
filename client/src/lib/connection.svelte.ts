import * as SignalR from "@microsoft/signalr";
import { game } from "./state.svelte";

let hc: SignalR.HubConnection | null = null;

const configureHandlers = () => {
    //TODO
}

export const connection = () => {
    if(hc) return hc;
    hc = new SignalR.HubConnectionBuilder()
        .withUrl("/hub", {
            accessTokenFactory: () => game.token
        })
        .configureLogging(SignalR.LogLevel.Information)
        .withAutomaticReconnect()
        .build();
    configureHandlers();
    return hc;
};