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
		public const bool MASTER_MODERATION_DISABLE = false;
	}
}