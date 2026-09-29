import * as SignalR from "@microsoft/signalr";

let hc: SignalR.HubConnection | null = null;

const configureHandlers = () => {
    //TODO
}

export const connection = () => {
    if(hc) return hc;
    hc = new SignalR.HubConnectionBuilder()
        .withUrl("/hub")
        .configureLogging(SignalR.LogLevel.Information)
        .withAutomaticReconnect()
        .build();
    configureHandlers();
    return hc;
};