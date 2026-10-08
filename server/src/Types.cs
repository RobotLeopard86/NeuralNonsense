namespace NeuralNonsense {
    public struct CreateRoomResponse {
        public string code;
    }
    public struct JoinRoomRequest {
        public string code;
        public string name;
    }
    public struct JoinRoomResponse {
        public string memberID;
        public Room.Member.Type type;
    }

    public class ClientCausedException : Exception {
        public ClientCausedException(string msg) : base(msg) { }
    };
    public class ServerCausedException : Exception {
        public ServerCausedException(string msg) : base(msg) { }
    };
}