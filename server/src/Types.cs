namespace NeuralNonsense {
    public struct CreateRoomResponse {
        public string code;
    }
    public struct JoinRoomRequest {
        public string code;
        public string playerName;
    }
    public struct JoinRoomResponse {
        public string memberID;
    }

    public class ClientCausedException : Exception {
        public ClientCausedException(string msg) : base(msg) { }
    };
    public class ServerCausedException : Exception {
        public ServerCausedException(string msg) : base(msg) { }
    };
}