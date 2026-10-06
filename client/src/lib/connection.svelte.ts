import * as SignalR from "@microsoft/signalr";
import { game } from "./state.svelte";

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
        .withUrl("/portal/hub")
        .configureLogging(SignalR.LogLevel.Information)
        .withAutomaticReconnect()
        .build();
    configureHandlers();
    await hc.start();
}