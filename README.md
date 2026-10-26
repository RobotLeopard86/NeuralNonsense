![NN Logo](logo.png)

# Neural Nonsense
#### A party game where you try to be funnier than an AI and your friends

## About
Neural Nonsense is a _Quiplash_-inspired social party game where you face off against your friends to try to see who can be funnier than an AI... and everyone else.  
Created as my submission for the Congressional App Challenge 2026.  

This is the development repository for the game; if you want to play it, check out the hosted version [here](https://www.neuralnonsense.net). Otherwise, keep scrolling down to see technical details.

## Licensing
Neural Nonsense's code is provided under the terms of the PolyForm Perimeter License 1.0.1. Please see [the license file](LICENSE) for information and terms.  

## Project Structure
This repository is structured into two parts: the client and server, the code for which can be found in the respectively-named directories.  

The client is written using Svelte 5, TailwindCSS, and TypeScript.  

The server is written in C# using ASP.NET with SignalR and stores all state in-memory. There is no external database.

## How to Run
First, install dependencies. You'll need Node.js, PNPM, and the .NET SDK including ASP.NET, version 10.0.0 or newer.  

Then, simply clone the repository and use the provided `setup.sh` script. Neural Nonsense is designed to run on Unix-like systems **only**. Do not expect to get it running on Windows. On first run, all necessary NPM packages will be installed via `pnpm`. The development HTTPS certificate will also be created at this point, and will ask for a root password to generate it. 

Finally, go to the `server/src` directory, rename `Secrets.cs.template` to `Secrets.cs`, and fill out the required fields. You may also want to customize `Constants.cs` to your liking.

You can then start the development servers using the provided `run.sh` script (requires `tmux`), which will open the Vite dev server on port `5173`, making it available to the network, and also the ASP.NET backend server on port `6171`. Both use HTTPS, so you may need to add the generated root certificate file (`.certs/rootCA.pem`) to your system's trust store. It is recommended to add `set -g mouse on` to your `tmux` configuration file, which should be at `~/.tmux.conf` to enable mouse and scrolling support in the interface.

## Building for Production
Assuming you've followed all the steps above to get started, simply run the provided `dist.sh` script. This will build the server for production, package the web bundle, and put it together in one package folder. The HTTPS certificates are inherited from before, but you can customize them if you like by simply replacing the certificate files (`cert.pem` is the public certificate, `dev.pem` is the private key).  

The production build packages the .NET runtime, so you don't need to install anything on the system you'd like to run it on. It should just work.

In addition, when running in production, the server will attempt to serve on port `443`, the standard port for HTTPS. This will require root priviliges on most systems, as binding to ports below `1000` is usually restricted. You will be able to access the client on that port, since the ASP.NET Core server assumes responsibility for hosting both the frontend and backend in production.
