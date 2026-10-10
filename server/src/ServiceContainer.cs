using Microsoft.AspNetCore.SignalR;

namespace NeuralNonsense {
	public sealed class ServiceContainer {
		private static ServiceContainer? _instance;
		public static ServiceContainer instance { get => _instance == null ? throw new ServerCausedException("No service container has been configured!") : _instance; set => _instance = value; }

		public required IHubContext<NNHub, INNClient> hubCtx;
		public required IBadWordCheckerService badWordChecker;
		public required TokenGeneratorService tokenGenerator;
	}
}