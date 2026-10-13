export interface Toast {
    message: string,
    duration: number,
    done: boolean,
    id: string
}

export let toasts: Toast[] = $state([] as Toast[]);

export const addToast = (message: string, duration: number) => {
    toasts.push({
        message: message,
        duration: duration,
        done: false,
        id: crypto.randomUUID()
    });
}