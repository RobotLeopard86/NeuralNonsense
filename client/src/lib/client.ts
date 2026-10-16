import type { HubConnection } from "@microsoft/signalr";
import { connection, tabClosePreventHandler } from "./connection.svelte";
import { push } from "svelte-spa-router";
import { resetState, type ServerMember, game, deriveMember, MemberType } from "./state.svelte";
import { addToast } from "./toast.svelte";

export const configureHandlers = () => {
    let conn: HubConnection = connection();

    //Disconnection
    conn.onclose((e?: Error) => {
        //Present error if applicable
        if(e) {
            addToast(`You have been unexpectedly disconnected from the room: ${e.message}`, 5000);
            console.error(`Full Error Stacktrace: ${e.stack ? e.stack : "Not Available"}`);
        }

        //Go to homepage
        window.removeEventListener("beforeunload", tabClosePreventHandler);
        push("/");
    });
    conn.on("Disconnected", (reason: string) => {
        //Obediently disconnect like the good website we are
        conn.stop().finally(() => {
            //Reset game state
            resetState();

            //Return to homepage and present error
            push("/");
            window.removeEventListener("beforeunload", tabClosePreventHandler);
            addToast(`You have been disconnected from the room: ${reason}`, 5000);
        });
    });

    //View transition
    conn.on("ViewTransition", (view: string) => {
        push(`/play/${view}`);
    });

    //Player list update
    conn.on("UpdateMemberList", (members: ServerMember[]) => {
        game.state.members = members.map(deriveMember);
    });

	//Member type update
	conn.on("UpdateType", (newType: string) => {
		switch(newType) {
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
	});

    //Host change
    conn.on("HostChanged", (newHost: string) => {
        game.state.hostID = newHost;
    });

    //Round update
    conn.on("NextRound", () => {
        game.state.round++;
    });

	//Score update
	conn.on("UpdateScore", (newScore: number) => {
		game.self.score = newScore;
	});
}

export const startGame = async () => {
    await connection().invoke("StartGame");
}