import * as signalR from "@microsoft/signalr";

class SignalRService {
    private connection: signalR.HubConnection | null = null;
    private callbacks: { [key: string]: ((...args: any[]) => void)[] } = {};

    public async startConnection(token: string): Promise<void> {
        if (this.connection?.state === signalR.HubConnectionState.Connected) return;

        this.connection = new signalR.HubConnectionBuilder()
            .withUrl("http://localhost:5266/hubs/chat", {
                accessTokenFactory: () => token,
            })
            .withAutomaticReconnect()
            .build();

        this.connection.onclose((error) => {
            console.error("SignalR Connection closed", error);
        });

        // Re-register callbacks
        Object.keys(this.callbacks).forEach((methodName) => {
            this.callbacks[methodName].forEach((callback) => {
                this.connection?.on(methodName, callback);
            });
        });

        try {
            await this.connection.start();
            console.log("SignalR Connected");
        } catch (err) {
            console.error("SignalR Connection Error", err);
            throw err;
        }
    }

    public async stopConnection(): Promise<void> {
        if (this.connection) {
            await this.connection.stop();
            this.connection = null;
        }
    }

    public on(methodName: string, callback: (...args: any[]) => void) {
        if (!this.callbacks[methodName]) {
            this.callbacks[methodName] = [];
        }
        this.callbacks[methodName].push(callback);

        if (this.connection) {
            this.connection.on(methodName, callback);
        }
    }

    public off(methodName: string, callback: (...args: any[]) => void) {
        if (this.callbacks[methodName]) {
            this.callbacks[methodName] = this.callbacks[methodName].filter((cb) => cb !== callback);
        }
        if (this.connection) {
            this.connection.off(methodName, callback);
        }
    }

    public async invoke(methodName: string, ...args: any[]) {
        if (this.connection?.state === signalR.HubConnectionState.Connected) {
            return await this.connection.invoke(methodName, ...args);
        } else {
            console.warn("SignalR not connected. Cannot invoke", methodName);
            throw new Error("SignalR not connected");
        }
    }
}

export default new SignalRService();
