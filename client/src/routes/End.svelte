<script lang="ts">
    import Card from "../components/Card.svelte";
    import { ejectIfNoConnection } from "../lib/connection.svelte";
    import { game, MemberType, type Member } from "../lib/state.svelte";
    import { onMount } from "svelte";
    import { Confetti } from "svelte-confetti";
    import { Trophy } from "@lucide/svelte";

    //onMount(ejectIfNoConnection);

    let results = $derived.by(() => {
        let scoreMappings: Map<number, Member[]> = new Map<number, Member[]>();
        game.state.members.filter((m) => m.type === MemberType.Player).forEach((m) => {
            if(!scoreMappings.has(m.score)) scoreMappings.set(m.score, []);
            scoreMappings.get(m.score)!.push(m);
        });
        let data: { score: number, attainers: Member[] }[] = [];
        scoreMappings.forEach((v, k) => {
            data.push({ score: k, attainers: v });
        });
        return data.sort((a, b) => a.score - b.score).map((entry, idx) => {
            return {
                place: idx + 1,
                attainers: entry.attainers
            };
        });
    });
</script>

<main class="mb-auto w-full flex flex-col items-center px-4">
    <!-- Trophy -->
    <div class="rounded-full bg-[#FF2D78] shadow-[0_0_20px_rgba(255,45,120,0.4)] mb-8">
        <Trophy size={125} color="white" class="m-12" />
    </div>

    <!-- Confetti (yum) -->
    <div class="w-full flex flex-row place-content-center pointer-events-none z-30">
        <Confetti x={[-3, 3]} y={[-0.25, 10]} amount={500}/>
    </div>

    <!-- Winner display -->
    <p class="text-white text-base font-bold">WINNER{results[0] ? results[0].attainers.length > 1 ? "S" : "" : ""}</p>
    <span class="flex flex-row text-white font-bold text-4xl mb-8 text-wrap max-w-2/3">
        {#if results.length > 0}
        {#each results[0].attainers as winner}
            <span class="mx-4">{winner.name}</span>
        {/each}
        {/if}
    </span>

    <!-- Scoreboard list -->
    <Card class="w-full flex flex-col items-center">
        <p class="text-center text-white/50 text-base col-span-2 mb-2">FINAL SCORES</p>
        {#each results as result}
            {#each result.attainers as member}
                <Card class="w-11/12 mx-auto mb-2 flex flex-row items-center">
                    <span class="text-left text-white font-bold">
                        <span class="text-left mr-1 text-white font-semibold text-sm">#{result.place}</span>
                        {member.name}
                        {#if member.uuid == game.self.uuid}
                            <span class="text-[#00E5FF] rounded-4xl bg-[#00E5FF]/25 px-4 py-1 mx-4">YOU</span>
                        {/if}
                    </span>
                    <span class="mx-auto"></span>
                    <span class="font-bold text-[#FF2D78] text-lg text-right">{member.score}</span>
                </Card>
            {/each}
        {/each}
        {#if game.self.type === MemberType.Audience}
            <p class="text-center text-white text-base font-bold">You were in the audience!</p>
        {/if}
    </Card>
</main>