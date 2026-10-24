namespace NeuralNonsense {
	public sealed class Constants {
		//HTTPS certificate
		public const string HTTPS_CERT_PATH = "../.certs/cert.pem";
		public const string HTTPS_KEY_PATH = "../.certs/dev.pem";

		//Dev server
		public const string DEV_SERVER_HOSTNAME = "localhost:5173";

		//JWT identities
		public const string JWT_ISSUER_IDENTITY = "Eucalyptus";
		public const string JWT_AUDIENCE_IDENTITY = "CocosNucifera";

		//General settings
		public const uint ROOM_CODE_LENGTH = 6;
		public const string ROOM_CODE_ALPHABET = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
		public const uint MAX_NAME_LENGTH = 32;
		public const uint MAX_ANSWER_LENGTH = 200;
		public const bool MASTER_MODERATION_DISABLE = false;
		public const byte UNANIMITY_QUALIFIER_NUMERATOR = 1;
		public const byte UNANIMITY_QUALIFIER_DENOMINATOR = 2;

		//Timers (all in seconds)
		public const uint ANSWER_PERIOD = 60000;
		public const uint VOTING_PERIOD = 30000;
		public const uint RESULTS_DISPLAY_PERIOD = 10000;
		public const uint R3_VOTING_PERIOD = 120000;
		public const uint R3_RESULTS_DISPLAY_PERIOD = 20000;
		public const uint SUSPENSION_PERIOD = 300000;

		//AI generation data
		public const string OLLAMA_HOSTNAME = "http://localhost:11434";
		public const string OLLAMA_MODEL = "gemma4:e4b";
		public const bool USE_LIVE_GEN = false;
	}
}