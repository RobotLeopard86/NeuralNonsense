namespace NeuralNonsense {
    public sealed class API {
        public static API instance = new API();

        public async Task<string> Test() {
            return "Hello!";
        }
    };
}