<script lang="ts">
    import { onMount } from "svelte";
    import LoadingSpinner from "../components/LoadingSpinner.svelte";
    import { deriveGameState, game, MemberType, resetTabState, type ServerGameState, type ServerMember } from "../lib/state.svelte";
    import { connect, connection, tabClosePreventHandler, type JWTPayload } from "../lib/connection.svelte";
    import * as jose from "jose";

    let { params = {} } = $props();

    onMount(async() => {
        //Prepare tab
        resetTabState();
        game.token = params.token;

        //Load data from token
        const claims = jose.decodeJwt<JWTPayload>(game.token);
        if(claims.exp && claims.exp < Math.round(+new Date() / 1000)) {
            //Token expired, cancel projector launch
            window.close();
            return;
        }

        //Set data
        game.roomCode = claims.room;
        game.self.name = claims.name;
        game.self.uuid = claims.sub;

        //Connect to server and request status update
        try {
            await connect();
        } catch(e) {
            window.close();
            return;
        }
        const state: ServerGameState = await connection().invoke("RestoreCurrentState");
        const self: ServerMember = await connection().invoke("RestoreCurrentSelf");

        //Enable tab close warning dialog
        window.addEventListener("beforeunload", tabClosePreventHandler);

        //Restore from status
        game.state = deriveGameState(state);
        game.self.score = self.score;
        game.self.type = MemberType.Projector;
    });
</script>

<main class="w-full min-h-full flex flex-col place-content-center px-4">
    <LoadingSpinner size={150} stroke={7} minDegrees={1} maxDegrees={120} />
</main>