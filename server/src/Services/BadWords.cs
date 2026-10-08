namespace NeuralNonsense {
	public interface IBadWordCheckerService {
		public bool IsOffensive(string input);
	}

	public sealed class FakeBadWordCheckerService : IBadWordCheckerService {
		public bool IsOffensive(string input) {
			return false;
		}
	}
}