#!/usr/bin/env bash

set -euo pipefail

CMD1="${1:-(cd client; pnpm dev)}"
CMD2="${2:-(cd server; dotnet run)}"
SESSION="dual-pane-$$"   # unique per invocation; avoids clashing with existing sessions

if ! command -v tmux >/dev/null 2>&1; then
    echo "error: tmux is not installed" >&2
    exit 1
fi

# Each pane runs its command, then kills the session when the command exits
# (regardless of exit status). Killing the session terminates the other pane's
# process too (it receives SIGHUP).
KILL="tmux kill-session -t '$SESSION'"

# -d: create detached so we can set up panes before attaching.
tmux new-session -d -s "$SESSION" "$CMD1; $KILL"

# Split horizontally (side by side). Use -v instead for stacked panes.
tmux split-window -h -t "$SESSION" "$CMD2; $KILL"

# Start with focus on the left pane.
tmux select-pane -t "$SESSION:0.0"

# Attach, or switch if we're already inside tmux.
if [ -n "${TMUX:-}" ]; then
    exec tmux switch-client -t "$SESSION"
else
    exec tmux attach-session -t "$SESSION"
fi