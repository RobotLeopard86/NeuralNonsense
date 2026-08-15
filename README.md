# Neural Nonsense
#### A party game where you try to be funnier than an AI and your friends

## About
Neural Nonsense is a _Quiplash_-inspired social party game where you face off against your friends to try to see who can be funnier than an AI... and everyone else.  
Created as my submission for the Congressional App Challenge 2026.  

This is the development repository for the game; if you want to play it, check out the hosted version [here](https://youtu.be/dQw4w9WgXcQ?t=0). Otherwise, keep scrolling down to see technical details.

## Licensing
Neural Nonsense's code is provided under the terms of the PolyForm Perimeter License 1.0.1. Please see [the license file](LICENSE) for information and terms.  

## Project Structure
This repository is structured into two parts: the client and server, the code for which can be found in the respectively-named directories.  

The client is written using Svelte, TailwindCSS, and TypeScript.  

The server is written in C# using ASP.NET, using AWS services to handle LLM calls and databases in production. Local development uses Ollama and SQLite.  
Neural Nonsense's game server runs in an OCI container that can be built using the included `Containerfile` in production.

