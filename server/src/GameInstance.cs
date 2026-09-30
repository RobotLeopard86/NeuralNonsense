namespace NeuralNonsense {
    public sealed class GameInstance {
        public sealed class Player {
            public string name = "";
        }

        public string roomCode = "";
        public Dictionary<string, Player> players = new Dictionary<string, Player>();
    };
}