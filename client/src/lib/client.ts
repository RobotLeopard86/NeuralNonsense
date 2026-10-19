import type { HubConnection } from "@microsoft/signalr";
import { connection, tabClosePreventHandler } from "./connection.svelte";
import { push } from "svelte-spa-router";
import { resetState, type ServerMember, game, deriveMember, MemberType } from "./state.svelte";
import { addToast } from "./toast.svelte";
import type { AnswerPhaseData, VotePhaseData } from "./phase";

export const configureHandlers = (hc: HubConnection) => {
    //Disconnection
    hc.onclose((e?: Error) => {
        //Present error if applicable
        if(e) {
            addToast(`You have been unexpectedly disconnected from the room: ${e.message}`, 5000);
            console.error(`Full Error Stacktrace: ${e.stack ? e.stack : "Not Available"}`);
        }

        //Go to homepage
        window.removeEventListener("beforeunload", tabClosePreventHandler);
        push("/");
    });
    hc.on("Disconnected", (reason: string) => {
        //Obediently disconnect like the good website we are
        hc.stop().finally(() => {
            //Reset game state
            resetState();

            //Return to homepage and present error
            push("/");
            window.removeEventListener("beforeunload", tabClosePreventHandler);
            addToast(`You have been disconnected from the room: ${reason}`, 5000);
        });
    });

    //View transition
    hc.on("ViewTransition", (view: string) => {
        push(`/play/${view}`);
    });

    //Player list update
    hc.on("UpdateMemberList", (members: ServerMember[]) => {
        game.state.members = members.map(deriveMember);
    });

	//Member type update
	hc.on("UpdateType", (newType: string) => {
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
    hc.on("HostChanged", (newHost: string) => {
        game.state.hostID = newHost;
		addToast(game.state.hostID === game.self.uuid ? "You are now the host!" : `${game.state.members.find((m) => m.uuid === game.state.hostID)!.name} is now the host!`, 5000);
    });

    //Round update
    hc.on("SetRound", (round: number) => {
        game.state.round = round;
    });

	//Score update
	hc.on("UpdateScore", (newScore: number) => {
		game.self.score = newScore;
	});

	//Phase data push
	hc.on("PushAnswerPhaseData", (apd: AnswerPhaseData) => {
		game.phase = apd;
	});
	hc.on("PushVotePhaseData", (vpd: VotePhaseData) => {
		game.phase = vpd;
	});
}

export const startGame = async () => {
    await connection().invoke("StartGame");
}